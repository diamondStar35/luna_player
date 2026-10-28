using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using MpvNet;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    private bool BeginSelectionSeek(double seconds, bool restoreEndBehavior, bool play)
    {
        var target = Precision.Normalize(Math.Max(0, seconds));
        _selectionSeekPending = true;
        _restoreEndBehaviorAfterSelectionSeek = restoreEndBehavior;
        _playAfterSelectionSeek = play;
        _selectionSeekTarget = target;
        if (_session.TryDo(mpv => mpv.Command("seek", target, "absolute+exact")))
            return true;
        CancelSelectionSeek();
        return false;
    }

    private void HandleFileLoaded(MpvEvent _)
    {
        lock (_loadSync)
        {
            if (_disposed || !_awaitingLoadRestart || !Paths.AreSame(_session.ReadString("path"), _loadingPath))
                return;
            var entryId = _session.PositiveEntryId();
            if (_loadingSourceEntryId.HasValue && entryId == _loadingSourceEntryId)
                return;
            _loadingFileSeen = true;
            _loadingEntryId = entryId;
        }
    }

    private void HandlePlaybackRestart(MpvEvent _)
    {
        lock (_loadSync)
            HandlePlaybackRestartCore();
    }

    private void HandlePlaybackRestartCore()
    {
        if (_disposed)
            return;
        // Chapters share a path. A queued restart from an outgoing seek is not proof that the new
        // chapter loaded: wait for that replacement's FILE_LOADED and native entry identity first.
        if (_awaitingLoadRestart && _loadingFileSeen && Paths.AreSame(_session.ReadString("path"), _loadingPath)
            && (!_loadingEntryId.HasValue || _session.PositiveEntryId() == _loadingEntryId))
        {
            _awaitingLoadRestart = false;
            _loadingPath = null;
            _loadingPaused = null;
            _loadingSourceEntryId = null;
            _lastPosition = null;
            _activeEntryId = _session.PositiveEntryId();
            _activePath = _session.ReadString("path");
            // Initial tags may arrive before playback-restart, while reads still reject the outgoing
            // entry's metadata. Refresh again now that the new stream's tags can be read safely.
            MetadataChanged?.Invoke();
            ChaptersChanged?.Invoke();
            if (_queuedNext)
                _queuedSourceEntryId = _session.PositiveEntryId();
            if (_crossfade && _nextRequest is LoadRequest next)
                PrepareAudioSuccessor(next);
        }
        if (_selectionSeekPending)
        {
            // Playback-restart events from an earlier seek may already be waiting in mpv's event queue.
            // Only finish this transition once the seek we issued is no longer running and its target is current.
            if (_session.ReadBoolean("seeking") == true
                || Elapsed is double elapsed && Math.Abs(elapsed - _selectionSeekTarget) > 0.5)
                return;
            var restoreEndBehavior = _restoreEndBehaviorAfterSelectionSeek;
            var play = _playAfterSelectionSeek;
            CancelSelectionSeek();
            if (restoreEndBehavior || _endBehaviorRestorePending)
            {
                _endBehaviorRestorePending = false;
                ApplyEndBehavior();
            }
            if (play)
                ResumePlayback();
            RetryQueuedAudio();
            SelectionSeekCompleted?.Invoke();
        }
        if (_fadeInOnRestart)
        {
            _fadeInOnRestart = false;
            _endFadeActive = false;
            // Loading already muted the new entry. Do not replace a transport request made while
            // it was opening; mpv owns that envelope until the pause, resume or seek is settled.
            if (!IsPaused && _session.ReadBoolean("audio-fade/transport-active") != true)
                FadeVolume(1, _fadeIntervalMilliseconds);
        }
        if (_pendingAdvance is LoadRequest advance
            && Paths.AreSame(_session.ReadString("path"), advance.Path))
        {
            _pendingAdvance = null;
            _activeEntryId = _session.PositiveEntryId();
            _activePath = advance.Path;
            _playbackRange = advance.Range;
            _chapterStopEnd = null;
            _chapterStopCompleted = false;
            _lastPosition = null;
            _completedAdvance = new QueuedAdvance(advance.Path, _pendingSourcePosition, advance.ItemId);
            Ended?.Invoke(EndReason.QueuedAdvance);
        }
    }

    private void HandlePositionChange(string _, object? value)
    {
        if (!_awaitingLoadRestart && _pendingAdvance is null
            && MpvSession.TryConvertDouble(value, out var lastPosition)
            && (!_activeEntryId.HasValue || _session.PositiveEntryId() == _activeEntryId))
            _lastPosition = lastPosition;
        UpdateEndFade(value);
        if (_disposed
            || !_releasedSelectionEndPending
            || _releasedSelectionPassed
            || !MpvSession.TryConvertDouble(value, out var position)
            || position <= _releasedSelectionEnd + 0.001)
            return;
        var entryId = _session.PositiveEntryId();
        if (_releasedSelectionEntryId.HasValue
            && entryId.HasValue
            && _releasedSelectionEntryId.Value != entryId.Value)
            return;
        // The old boundary was removed before it fired and playback has demonstrably crossed it. End
        // behaviour is safe to restore now; the pending identity remains until the eventual real EOF so
        // a delayed event can still be classified from its timestamp instead of advancing by mistake.
        _releasedSelectionPassed = true;
        _releasedSelectionWasPlaying = false;
        _releasedSelectionWasCompleted = false;
        if (_endBehaviorRestorePending)
        {
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
            RetryQueuedAudio();
        }
    }

    private void UpdateEndFade(object? value)
    {
        if (_disposed || _awaitingLoadRestart || !ShouldFadeTransitions || _selectionRangeActive || _repeatFile
            || _endBehavior != EndBehavior.Advance || _queuedNext || _chapterStopEnd.HasValue || IsPaused
            || _session.ReadBoolean("audio-fade/transport-active") == true
            || (!_endFadeActive && _session.ReadBoolean("audio-fade/active") == true)
            || !MpvSession.TryConvertDouble(value, out var position) || Duration is not double duration)
            return;
        var remaining = (_playbackRange?.End ?? duration) - position;
        var interval = _fadeIntervalMilliseconds / 1000.0;
        if (remaining > interval)
        {
            if (_endFadeActive)
            {
                _endFadeActive = false;
                _session.SetFade(1);
            }
            return;
        }
        if (_endFadeActive)
            return;
        _endFadeActive = true;
        // Once armed, the output mixer owns every sample of the ramp. Position notifications must
        // not replace it with block-sized gain changes. Account for playback speed at the boundary.
        var speed = Math.Max(0.01, _session.ReadDouble("speed") ?? 1);
        var milliseconds = (int)Math.Clamp(remaining / speed * 1000, 0, 60000);
        FadeVolume(0, milliseconds);
    }

    private void CancelSelectionSeek()
    {
        _selectionSeekPending = false;
        _restoreEndBehaviorAfterSelectionSeek = false;
        _playAfterSelectionSeek = false;
        _selectionSeekTarget = 0;
    }

}
