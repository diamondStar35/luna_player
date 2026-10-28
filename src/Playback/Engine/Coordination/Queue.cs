using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    private bool CurrentIsAudio()
        => _session.ReadObject("track-list") is IEnumerable<object?> tracks && Probe.Tracks(tracks).AudioOnly;

    private bool LoadWithCrossfade(LoadRequest request, bool paused, bool fadeTransition, long generation)
    {
        var probe = _probe!;
        if (probe.CachedResult(request.Path) is Probe.Result cached)
            return CompleteManualLoad(request, paused, fadeTransition, cached.AudioOnly);
        // First playback never waits on a probe. Only an explicitly requested overlap needs to know
        // the destination's tracks before capturing the outgoing audio.
        _loaded = true;
        _manualRequest = request;
        _loadingPaused = paused;
        _ = Task.Run(() =>
        {
            var result = probe.Read(request.Path, CancellationToken.None,
                () => !_disposed && generation == Volatile.Read(ref _loadGeneration));
            lock (_loadSync)
            {
                if (_disposed || generation != _loadGeneration)
                    return;
                var destination = _manualRequest ?? request;
                var requestedPause = _loadingPaused ?? paused;
                _manualRequest = null;
                _loadingPaused = null;
                if (!CompleteManualLoad(destination, requestedPause, fadeTransition, result?.AudioOnly == true))
                    Ended?.Invoke(EndReason.Error);
            }
        });
        return true;
    }

    private bool CompleteManualLoad(LoadRequest request, bool paused, bool fadeTransition, bool overlap)
    {
        overlap &= _crossfade && _crossfadeManual && !paused && !IsPaused
            && !_currentUsesAudioFile && CurrentIsAudio();
        _fadeInOnRestart = !overlap && fadeTransition && ShouldFadeTransitions;
        if (!overlap)
            _session.SetFade(_fadeInOnRestart ? 0 : 1);
        return LoadCore(request, paused, overlap);
    }

    private void PrepareAudioSuccessor(LoadRequest request)
    {
        if (_awaitingLoadRestart || _selectionRangeActive || _selectionSeekPending || _endBehaviorRestorePending)
            return;
        if (!CurrentIsAudio())
        {
            ClearQueuedNext();
            return;
        }
        var generation = ++_queueGeneration;
        var load = _loadGeneration;
        var probe = _probe!;
        if (probe.CachedResult(request.Path) is Probe.Result cached)
        {
            AppendAudioSuccessor(request, cached.AudioOnly);
            return;
        }
        _ = Task.Run(() =>
        {
            var result = probe.Read(request.Path, CancellationToken.None,
                () => !_disposed && generation == Volatile.Read(ref _queueGeneration)
                    && load == Volatile.Read(ref _loadGeneration));
            lock (_loadSync)
            {
                if (_disposed || generation != _queueGeneration || load != _loadGeneration
                    || !ReferenceEquals(_nextRequest, request))
                    return;
                AppendAudioSuccessor(request, result?.AudioOnly == true);
            }
        });
    }

    private void AppendAudioSuccessor(LoadRequest request, bool audioOnly)
    {
        if (!ReferenceEquals(_nextRequest, request))
            return;
        if (_awaitingLoadRestart || _selectionRangeActive || _selectionSeekPending || _endBehaviorRestorePending)
            return; // playback-restart retries with the now-cached metadata
        if (!_crossfade || _holdAutomaticAdvance || !audioOnly || !CurrentIsAudio())
        {
            ClearQueuedNext();
            return;
        }
        if (AppendNext())
        {
            _queuedSourceEntryId = _session.PositiveEntryId();
            CancelEndFade();
        }
    }

    private void RetryQueuedAudio()
    {
        if (_crossfade && _nextRequest is LoadRequest next)
            PrepareAudioSuccessor(next);
    }

    public QueuedAdvance? TakeQueuedAdvance()
    {
        lock (_loadSync)
        {
            var advance = _completedAdvance;
            _completedAdvance = null;
            return advance;
        }
    }

    public double? PositionFor(string path)
    {
        lock (_loadSync)
        {
            if (!Paths.AreSame(path, _activePath))
                return null;
            if (!_awaitingLoadRestart && _pendingAdvance is null)
            {
                var position = Elapsed;
                if (_activeEntryId == _session.PositiveEntryId())
                    return position;
            }
            return _lastPosition;
        }
    }

    public void HoldAutomaticAdvance(bool hold)
    {
        lock (_loadSync)
        {
            if (_holdAutomaticAdvance == hold)
                return;
            _holdAutomaticAdvance = hold;
            if (hold)
                ClearQueuedNext();
            if (!hold && _heldEndNotified && EndReached)
            {
                // Releasing a fired sleep timer must not unload its paused item. A subsequent explicit
                // Play or seek will replace or release the held boundary safely.
                _endBehaviorRestorePending = true;
                return;
            }
            if (!_selectionRangeActive && !_endBehaviorRestorePending)
                ApplyEndBehavior();
        }
    }

    private bool SeekPendingManualLoad(double target, bool absolute)
    {
        lock (_loadSync)
        {
            if (_manualRequest is not LoadRequest request)
                return false;
            _manualRequest = request with
            {
                StartPosition = Math.Max(0, absolute ? target : (request.StartPosition ?? 0) + target),
            };
            return true;
        }
    }
}
