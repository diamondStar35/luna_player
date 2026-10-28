using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    /// <remarks>All load-related mpv work is asynchronous because synchronous property changes immediately
    /// after <c>loadfile</c> can stall the UI. Pause and subtitle state travel as per-file options. A separate
    /// audio URL cannot be safely flattened into those options, so <c>audio-files</c> is queued first; mpv
    /// processes both requests in order.</remarks>
    public bool Load(string path, double? startPosition = null, bool paused = false, string? audioFile = null,
        bool noVideo = false, bool fadeTransition = false, PlaybackRange? range = null, string? itemId = null, double? chapterStop = null)
    {
        lock (_loadSync)
            return BeginLoad(path, startPosition, paused, audioFile, noVideo, fadeTransition, range, itemId, chapterStop);
    }

    private bool BeginLoad(string path, double? startPosition, bool paused, string? audioFile,
        bool noVideo, bool fadeTransition, PlaybackRange? range, string? itemId, double? chapterStop)
    {
        var overlap = fadeTransition && _crossfade && _crossfadeManual && _loaded && !IsPaused
            && !_awaitingLoadRestart && !_currentUsesAudioFile && audioFile is null && CurrentIsAudio();
        var generation = ++_loadGeneration;
        ++_queueGeneration;
        _loadingSourceEntryId = _session.PositiveEntryId();
        _loadingFileSeen = false;
        _loadingEntryId = null;
        _pendingAdvance = null;
        _completedAdvance = null;
        _manualRequest = null;
        _endFadeActive = false;
        CancelSelectionSeek();
        _endBehaviorRestorePending = false;
        // A delayed EOF from the replaced entry is still identified below, but it must never resume by
        // seeking the newly loaded entry to the old selection's timestamp.
        _releasedSelectionWasPlaying = false;
        _releasedSelectionWasCompleted = false;
        var request = new LoadRequest(path, startPosition, audioFile, noVideo, range, itemId, chapterStop);
        _nextRequest = null;
        _queuedNext = false;
        _queuedSourceEntryId = null;
        _awaitingLoadRestart = true;
        _loadingPath = path;
        _fadeInOnRestart = !overlap && fadeTransition && ShouldFadeTransitions;
        // Ordinary replacement fades begin with the destination muted. Native overlaps retain the
        // outgoing envelope until the mixer captures it, avoiding a sudden gain jump before the overlap.
        if (!overlap)
            _session.SetFade(_fadeInOnRestart ? 0 : 1);
        // Cancel the old successor immediately, including while a manual destination's metadata is
        // being inspected. A replaced request may never advance the old queue in the meantime.
        if (overlap)
            _session.TryDo(static mpv => mpv.CommandAsync(0, "playlist-clear"));
        var loaded = overlap ? LoadWithCrossfade(request, paused, fadeTransition, generation) : LoadCore(request, paused);
        _selectionRangeActive = false;
        _selectionRangeRepeats = false;
        _selectionRangeCompleted = false;
        _selectionRangeEnd = 0;
        _selectionRangeEntryId = null;
        return loaded;
    }

    public bool QueueNext(string path, double? startPosition = null, string? audioFile = null, bool noVideo = false, PlaybackRange? range = null, string? itemId = null)
    {
        lock (_loadSync)
            return QueueNextCore(path, startPosition, audioFile, noVideo, range, itemId);
    }

    private bool QueueNextCore(string path, double? startPosition, string? audioFile, bool noVideo, PlaybackRange? range, string? itemId)
    {
        if ((!_crossfade && _gaplessPlayback == GaplessPlaybackMode.Disabled) || _holdAutomaticAdvance
            || string.IsNullOrWhiteSpace(path)
            || _currentUsesAudioFile
            // mpv's audio-files property is global rather than a safe per-file option. A split YouTube
            // stream therefore keeps the existing best-effort path instead of risking the current audio
            // being attached to the queued video.
            || !string.IsNullOrWhiteSpace(audioFile))
        {
            ClearQueuedNext();
            return false;
        }
        var request = new LoadRequest(path, startPosition, null, noVideo, range, itemId);
        if (_queuedNext && _nextRequest == request)
            return true;
        // Invalidate the old native entry before waiting for replacement metadata. Otherwise it could
        // start while the managed request already describes a different successor.
        if (_nextRequest is not null && _nextRequest != request)
            ClearQueuedNext();
        _nextRequest = request;
        if (_crossfade)
        {
            PrepareAudioSuccessor(_nextRequest);
            return true;
        }
        var appended = AppendNext();
        if (appended && !_awaitingLoadRestart)
            _queuedSourceEntryId = _session.PositiveEntryId();
        if (appended)
            CancelEndFade();
        return appended;
    }

    public void ClearQueuedNext()
    {
        lock (_loadSync)
        {
            ++_queueGeneration;
            _nextRequest = null;
            _queuedNext = false;
            _queuedSourceEntryId = null;
            _session.TryDo(static mpv => mpv.CommandAsync(0, "playlist-clear"));
        }
    }

    public void ConfigurePlaybackEffects(AudioSettings settings)
    {
        lock (_loadSync)
            ConfigurePlaybackEffectsCore(settings);
    }

    private void ConfigurePlaybackEffectsCore(AudioSettings settings)
    {
        _fadeIntervalMilliseconds = Math.Clamp(settings.FadeIntervalMilliseconds, 0,
            AudioSettings.MaximumFadeIntervalMilliseconds);
        _fadeSeeking = settings.FadeSeeking;
        _seekFadeIntervalMilliseconds = Math.Clamp(settings.SeekFadeIntervalMilliseconds, 1,
            AudioSettings.MaximumFadeIntervalMilliseconds);
        _fadeFileTransitions = settings.FadeFileTransitions;
        _fadePauseResume = settings.FadePauseResume;
        _gaplessPlayback = settings.GaplessPlayback;
        _crossfadeMilliseconds = Math.Clamp(settings.CrossfadeMilliseconds, 1, AudioSettings.MaximumCrossfadeMilliseconds);
        _crossfadeEqualPower = settings.CrossfadeCurve == CrossfadeCurve.EqualPower;
        _crossfadeManual = settings.CrossfadeManualNavigation;
        var hadCrossfade = _crossfade;
        _crossfade = settings.Crossfade && _session.TryDo(mpv =>
            mpv.SetAutomaticCrossfade(_crossfadeMilliseconds / 1000.0, _crossfadeEqualPower));
        if (!_crossfade && hadCrossfade)
            _session.TryDo(static mpv => mpv.SetAutomaticCrossfade(0));
        if (_crossfade)
            _probe ??= new Probe();
        if (!ShouldFadeTransitions)
            CancelEndFade();
        _session.SetPropertySafely("gapless-audio", _gaplessPlayback switch
        {
            GaplessPlaybackMode.CompatibleFiles => "weak",
            GaplessPlaybackMode.AlwaysWhenPossible => "yes",
            _ => "no",
        });
        _session.SetPropertySafely("prefetch-playlist", _crossfade || _gaplessPlayback != GaplessPlaybackMode.Disabled);
        if (!_crossfade && _gaplessPlayback == GaplessPlaybackMode.Disabled)
            ClearQueuedNext();
    }

    private bool LoadCore(LoadRequest request, bool paused, bool crossfade = false)
    {
        _playbackRange = request.Range;
        _chapterStopEnd = request.ChapterStop;
        _chapterStopCompleted = false;
        _heldEndNotified = false;
        var loaded = _session.TryDo(mpv =>
        {
            mpv.SetPropertyAsync(0, "audio-files",
                request.AudioFile is null ? Array.Empty<string>() : new[] { request.AudioFile });
            var options = LoadOptions(request, paused);
            if (crossfade)
                mpv.CrossfadeLoadFileAsync(request.Path, _crossfadeMilliseconds / 1000.0, _crossfadeEqualPower, options);
            else
                mpv.LoadFileAsync(request.Path, "replace", options);
        });
        _loaded = loaded;
        _currentUsesAudioFile = request.AudioFile is not null;
        _loadingPaused = paused;
        if (loaded && !_crossfade && _nextRequest is not null)
            AppendNext();
        return loaded;
    }

    private bool AppendNext()
    {
        if (_nextRequest is not LoadRequest request)
            return false;
        var appended = _session.TryDo(mpv =>
        {
            mpv.CommandAsync(0, "playlist-clear");
            mpv.LoadFileAsync(request.Path, "append",
                LoadOptions(request, paused: false));
        });
        _queuedNext = appended;
        return appended;
    }

    private Dictionary<string, object?> LoadOptions(LoadRequest request, bool paused)
    {
        var options = new Dictionary<string, object?>
        {
            // Track IDs do not carry across files, and the user opts subtitles in per file through the menu.
            ["sid"] = "no",
            ["pause"] = paused ? "yes" : "no",
            // A YouTube video opened as audio can still resolve to a stream containing a picture. Leave it
            // undecoded for that item without affecting the video choice on the items around it.
            ["vid"] = request.NoVideo ? "no" : "auto",
            // A one-shot selection sets this dynamically. Never let that boundary leak into another file.
            ["end"] = (request.ChapterStop ?? request.Range?.End) is double end ? end : "none",
            ["keep-open"] = request.ChapterStop.HasValue || _holdAutomaticAdvance || _endBehavior == EndBehavior.None ? "yes" : "no",
            ["loop-file"] = !_holdAutomaticAdvance && request.ChapterStop is null && request.Range is null && _endBehavior == EndBehavior.Loop ? "inf" : "no",
        };
        if (request.StartPosition.HasValue || request.Range.HasValue)
            options["start"] = Precision.Normalize(request.StartPosition ?? request.Range!.Value.Start);
        return options;
    }

    private bool ShouldFadeTransitions => _fadeFileTransitions && _fadeIntervalMilliseconds > 0;

    private sealed record LoadRequest(string Path, double? StartPosition, string? AudioFile, bool NoVideo,
        PlaybackRange? Range = null, string? ItemId = null, double? ChapterStop = null);

    public void Stop()
    {
        lock (_loadSync)
            StopCore();
    }

    private void StopCore()
    {
        ++_loadGeneration;
        ++_queueGeneration;
        _pendingAdvance = null;
        _completedAdvance = null;
        _manualRequest = null;
        _playbackRange = null;
        _chapterStopEnd = null;
        _chapterStopCompleted = false;
        _heldEndNotified = false;
        _loaded = false;
        _currentUsesAudioFile = false;
        _awaitingLoadRestart = false;
        _loadingPath = null;
        _loadingPaused = null;
        _fadeInOnRestart = false;
        _endFadeActive = false;
        _activeEntryId = null;
        _activePath = null;
        _lastPosition = null;
        ClearQueuedNext();
        _session.SetFade(1);
        CancelSelectionSeek();
        _endBehaviorRestorePending = false;
        _releasedSelectionEndPending = false;
        _releasedSelectionWasPlaying = false;
        _releasedSelectionWasCompleted = false;
        _releasedSelectionPassed = false;
        _session.TryDo(static mpv => mpv.Command("stop"));
    }

}
