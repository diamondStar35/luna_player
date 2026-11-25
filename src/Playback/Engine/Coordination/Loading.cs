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
        bool noVideo = false, bool fadeTransition = false, PlaybackRange? range = null, string? itemId = null, double? chapterStop = null, IReadOnlyDictionary<string, string>? headers = null, Boundaries? boundaries = null)
    {
        lock (_loadSync)
            return BeginLoad(path, startPosition, paused, audioFile, noVideo, fadeTransition, range, itemId, chapterStop, headers, boundaries);
    }

    private bool BeginLoad(string path, double? startPosition, bool paused, string? audioFile,
        bool noVideo, bool fadeTransition, PlaybackRange? range, string? itemId, double? chapterStop, IReadOnlyDictionary<string, string>? headers, Boundaries? boundaries)
    {
        var declick = _replacement is not null || _loaded && !IsPaused && !_awaitingLoadRestart;
        var overlap = fadeTransition && _crossfade && _crossfadeManual && _loaded && !IsPaused
            && !_awaitingLoadRestart && !_replacementMuted && !_currentUsesAudioFile && audioFile is null
            && (boundaries is null || boundaries.IsEmpty) && CurrentIsAudio();
        var generation = ++_loadGeneration;
        _nativeLoads.Clear();
        _nativeEntries.Clear();
        _queuedEntry = null;
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
        var request = new LoadRequest(path, startPosition, audioFile, noVideo, range, itemId, chapterStop, headers, boundaries);
        SetLoadBoundaries(boundaries ?? Boundaries.None);
        _nextRequest = null;
        _queuedNext = false;
        _queuedSourceEntryId = null;
        _awaitingLoadRestart = true;
        _loadingPath = path;
        _replacement = null;
        _loadingPaused = paused;
        _fadeInOnRestart = !overlap;
        _restartFadeMilliseconds = fadeTransition && ShouldFadeTransitions
            ? Math.Max(DeclickMilliseconds, _fadeIntervalMilliseconds) : DeclickMilliseconds;
        // Cancel the old successor immediately, including while a manual destination's metadata is
        // being inspected. A replaced request may never advance the old queue in the meantime.
        if (overlap)
            _session.TryDo(static mpv => mpv.CommandAsync(0, "playlist-clear"));
        ReleasePendingSpotifyAudio(path);
        var loaded = overlap ? LoadWithCrossfade(request, paused, generation)
            : declick ? BeginReplacement(request, paused, generation) : LoadCore(request, paused);
        return loaded;
    }

    public bool QueueNext(string path, double? startPosition = null, string? audioFile = null, bool noVideo = false, PlaybackRange? range = null, string? itemId = null, IReadOnlyDictionary<string, string>? headers = null)
    {
        lock (_loadSync)
            return QueueNextCore(path, startPosition, audioFile, noVideo, range, itemId, headers);
    }

    private bool QueueNextCore(string path, double? startPosition, string? audioFile, bool noVideo, PlaybackRange? range, string? itemId, IReadOnlyDictionary<string, string>? headers)
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
        var request = new LoadRequest(path, startPosition, null, noVideo, range, itemId, Headers: headers);
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
            _queuedEntry = null;
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
        request = request with { Boundaries = _boundaries };
        _playbackRange = request.Range;
        _chapterStopEnd = request.ChapterStop;
        _chapterStopCompleted = false;
        _heldEndNotified = false;
        // Decode replacements while paused so configuring their filters cannot consume
        // the first part of the file. Native crossfades keep their continuous output path.
        var options = LoadOptions(request, paused: !crossfade || paused);
        var generation = _loadGeneration;
        var command = RegisterNativeLoad(request, append: false);
        var muted = _fadeInOnRestart;
        var loaded = _session.TryDo(mpv =>
        {
            if (_disposed || generation != Volatile.Read(ref _loadGeneration)) return;
            if (!crossfade)
            {
                _session.TryDoNow(native => native.FadeVolume(muted ? 0 : 1, 0));
                _audio.ClearEqualizerForLoad();
            }
            if (_disposed || generation != Volatile.Read(ref _loadGeneration)) return;
            mpv.SetPropertyAsync(0, "audio-files",
                request.AudioFile is null ? Array.Empty<string>() : new[] { request.AudioFile });
            if (crossfade)
                mpv.CrossfadeLoadFileAsync(request.Path, _crossfadeMilliseconds / 1000.0, _crossfadeEqualPower, options, command);
            else
                mpv.LoadFileAsync(request.Path, "replace", options, command);
        });
        _loaded = loaded;
        _currentUsesAudioFile = request.AudioFile is not null;
        _replacementMuted = !crossfade;
        _loadingPaused = paused;
        if (loaded && !_crossfade && _nextRequest is not null)
            AppendNext();
        return loaded;
    }

    private bool AppendNext()
    {
        if (_nextRequest is not LoadRequest request)
            return false;
        var options = LoadOptions(request, paused: false);
        var command = RegisterNativeLoad(request, append: true);
        var appended = _session.TryDo(mpv =>
        {
            mpv.CommandAsync(0, "playlist-clear");
            mpv.LoadFileAsync(request.Path, "append", options, command);
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
        };
        // Keep temporary controls separate from the input's chapter/item bounds. FinishReplacement
        // installs the latest loop and selection on the opened input before its audio is unpaused.
        foreach (var pair in BoundaryOptions(Boundaries.None, request.Range, request.ChapterStop))
            options[pair.Key] = pair.Value;
        if (request.StartPosition.HasValue || request.Range.HasValue)
            options["start"] = Precision.Normalize(request.StartPosition ?? request.Range!.Value.Start);
        // String-list escaping differs from loadfile's key/value quoting. One HTTP header block
        // also preserves a value ending in a backslash without escaping the next list separator.
        if (request.Headers is { Count: > 0 })
            options["http-header-fields"] = string.Join("\r\n", request.Headers.Select(pair =>
                pair.Key + ": " + pair.Value)).Replace(",", @"\,", StringComparison.Ordinal);
        else options["http-header-fields-clr"] = "";
        return options;
    }

    private bool ShouldFadeTransitions => _fadeFileTransitions && _fadeIntervalMilliseconds > 0;

    private sealed record LoadRequest(string Path, double? StartPosition, string? AudioFile, bool NoVideo,
        PlaybackRange? Range = null, string? ItemId = null, double? ChapterStop = null, IReadOnlyDictionary<string, string>? Headers = null, Boundaries? Boundaries = null);

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
        _replacement = null;
        _playbackRange = null;
        SetLoadBoundaries(Boundaries.None);
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
        ReleasePendingSpotifyAudio();
    }

}
