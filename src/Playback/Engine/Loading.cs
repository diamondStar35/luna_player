using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Playback;

internal sealed partial class Engine
{
    /// <remarks>All load-related mpv work is asynchronous because synchronous property changes immediately
    /// after <c>loadfile</c> can stall the UI. Pause and subtitle state travel as per-file options. A separate
    /// audio URL cannot be safely flattened into those options, so <c>audio-files</c> is queued first; mpv
    /// processes both requests in order.</remarks>
    public bool Load(string path, double? startPosition = null, bool paused = false, string? audioFile = null,
        bool noVideo = false, bool fadeTransition = false)
    {
        CancelSelectionSeek();
        _seekFadeActive = false;
        _endBehaviorRestorePending = false;
        // A delayed EOF from the replaced entry is still identified below, but it must never resume by
        // seeking the newly loaded entry to the old selection's timestamp.
        _releasedSelectionWasPlaying = false;
        _releasedSelectionWasCompleted = false;
        var request = new LoadRequest(path, startPosition, audioFile, noVideo);
        _nextRequest = null;
        _queuedNext = false;
        _queuedSourceEntryId = null;
        _awaitingLoadRestart = true;
        _loadingPath = path;
        _gaplessAdvancePending = false;
        _resumeFadePending = false;
        _resumeFadePosition = null;
        _fadeInOnRestart = fadeTransition && ShouldFadeTransitions;
        // Replacement loads cannot be delayed after Luna has already moved its playlist index: during
        // that delay metadata and another navigation command would still belong to the outgoing file.
        // Natural endings receive a full fade-out from the position observer; explicit navigation starts
        // the replacement muted and fades it in as soon as mpv reports playback-restart.
        _fader.Set(_fadeInOnRestart ? 0 : 1);
        var loaded = LoadCore(request, paused);
        _selectionRangeActive = false;
        _selectionRangeRepeats = false;
        _selectionRangeCompleted = false;
        _selectionRangeEnd = 0;
        _selectionRangeEntryId = null;
        return loaded;
    }

    public bool QueueNext(string path, double? startPosition = null, string? audioFile = null, bool noVideo = false)
    {
        if (_gaplessPlayback == GaplessPlaybackMode.Disabled || string.IsNullOrWhiteSpace(path)
            || _currentUsesAudioFile
            // mpv's audio-files property is global rather than a safe per-file option. A split YouTube
            // stream therefore keeps the existing best-effort path instead of risking the current audio
            // being attached to the queued video.
            || !string.IsNullOrWhiteSpace(audioFile))
        {
            ClearQueuedNext();
            return false;
        }
        _nextRequest = new LoadRequest(path, startPosition, null, noVideo);
        var appended = AppendNext();
        if (appended && !_awaitingLoadRestart)
            _queuedSourceEntryId = PositiveEntryId();
        if (appended)
            CancelEndFade();
        return appended;
    }

    public void ClearQueuedNext()
    {
        _nextRequest = null;
        _queuedNext = false;
        _queuedSourceEntryId = null;
        TryDo(static mpv => mpv.CommandAsync(0, "playlist-clear"));
    }

    public void ConfigurePlaybackEffects(AudioSettings settings)
    {
        _fadeIntervalMilliseconds = Math.Clamp(settings.FadeIntervalMilliseconds, 0,
            AudioSettings.MaximumFadeIntervalMilliseconds);
        _fadeSeeking = settings.FadeSeeking;
        _seekFadeIntervalMilliseconds = Math.Clamp(settings.SeekFadeIntervalMilliseconds, 1,
            AudioSettings.MaximumFadeIntervalMilliseconds);
        _fadeFileTransitions = settings.FadeFileTransitions;
        _fadePauseResume = settings.FadePauseResume;
        _gaplessPlayback = settings.GaplessPlayback;
        if (!ShouldFadeTransitions)
            CancelEndFade();
        SetPropertySafely("gapless-audio", _gaplessPlayback switch
        {
            GaplessPlaybackMode.CompatibleFiles => "weak",
            GaplessPlaybackMode.AlwaysWhenPossible => "yes",
            _ => "no",
        });
        SetPropertySafely("prefetch-playlist", _gaplessPlayback != GaplessPlaybackMode.Disabled);
        if (_gaplessPlayback == GaplessPlaybackMode.Disabled)
            ClearQueuedNext();
    }

    private bool LoadCore(LoadRequest request, bool paused)
    {
        var loaded = TryDo(mpv =>
        {
            mpv.SetPropertyAsync(0, "audio-files",
                request.AudioFile is null ? Array.Empty<string>() : new[] { request.AudioFile });
            mpv.LoadFileAsync(request.Path, "replace",
                LoadOptions(request.StartPosition, paused, request.NoVideo));
        });
        _loaded = loaded;
        _currentUsesAudioFile = request.AudioFile is not null;
        _pauseRequested = paused;
        if (loaded && _nextRequest is not null)
            AppendNext();
        return loaded;
    }

    private bool AppendNext()
    {
        if (_nextRequest is not LoadRequest request)
            return false;
        var appended = TryDo(mpv =>
        {
            mpv.CommandAsync(0, "playlist-clear");
            mpv.LoadFileAsync(request.Path, "append",
                LoadOptions(request.StartPosition, paused: false, request.NoVideo));
        });
        _queuedNext = appended;
        return appended;
    }

    private Dictionary<string, object?> LoadOptions(double? startPosition, bool paused, bool noVideo)
    {
        var options = new Dictionary<string, object?>
        {
            // Track IDs do not carry across files, and the user opts subtitles in per file through the menu.
            ["sid"] = "no",
            ["pause"] = paused ? "yes" : "no",
            // A YouTube video opened as audio can still resolve to a stream containing a picture. Leave it
            // undecoded for that item without affecting the video choice on the items around it.
            ["vid"] = noVideo ? "no" : "auto",
            // A one-shot selection sets this dynamically. Never let that boundary leak into another file.
            ["end"] = "none",
            ["keep-open"] = _endBehavior == EndBehavior.None ? "yes" : "no",
            ["loop-file"] = _endBehavior == EndBehavior.Loop ? "inf" : "no",
        };
        if (startPosition.HasValue)
            options["start"] = Precision.Normalize(startPosition.Value);
        return options;
    }

    private bool ShouldFadeTransitions => _fadeFileTransitions && _fadeIntervalMilliseconds > 0;

    private sealed record LoadRequest(string Path, double? StartPosition, string? AudioFile, bool NoVideo);

    public void Stop()
    {
        _loaded = false;
        _currentUsesAudioFile = false;
        _awaitingLoadRestart = false;
        _loadingPath = null;
        _pauseRequested = null;
        _resumeFadePending = false;
        _resumeFadePosition = null;
        _seekFadeActive = false;
        _fadeInOnRestart = false;
        _endFadeActive = false;
        _gaplessAdvancePending = false;
        ClearQueuedNext();
        _fader.Set(1);
        CancelSelectionSeek();
        _endBehaviorRestorePending = false;
        _releasedSelectionEndPending = false;
        _releasedSelectionWasPlaying = false;
        _releasedSelectionWasCompleted = false;
        _releasedSelectionPassed = false;
        TryDo(static mpv => mpv.Command("stop"));
    }

}
