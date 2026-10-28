using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
using LunaPlayer.Media;
using MpvNet;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator : IEngine
{
    private readonly MpvSession _session;
    private readonly AudioProcessor _audio;
    private readonly TrackDiscovery _tracks;
    private readonly MetadataDiscovery _metadata;
    private bool _loadingFileSeen;
    private long? _loadingEntryId;
    private bool _heldEndNotified;
    private PlaybackRange? _playbackRange;
    private double? _chapterStopEnd;
    private bool _chapterStopCompleted;

    // Cached from the video-params observer; mpv learns it asynchronously after a load, not on demand.
    private bool _hasVideo;


    private EndBehavior _endBehavior;
    private GaplessPlaybackMode _gaplessPlayback;
    private int _fadeIntervalMilliseconds;
    private bool _fadeSeeking;
    private int _seekFadeIntervalMilliseconds = 100;
    private bool _fadeFileTransitions;
    private bool _fadePauseResume;
    private bool? _loadingPaused;
    private bool _fadeInOnRestart;
    private bool _endFadeActive;

    private bool _loaded;
    private bool _repeatFile;
    private bool _currentUsesAudioFile;
    private LoadRequest? _nextRequest;
    private bool _queuedNext;
    private long? _queuedSourceEntryId;
    private bool _awaitingLoadRestart;
    private string? _loadingPath;
    private readonly Lock _loadSync = new();
    private Probe? _probe;
    private long _loadGeneration;
    private long _queueGeneration;
    private long? _loadingSourceEntryId;
    private long? _activeEntryId;
    private string? _activePath;
    private double? _lastPosition;
    private LoadRequest? _pendingAdvance;
    private double? _pendingSourcePosition;
    private QueuedAdvance? _completedAdvance;
    private bool _crossfade;
    private bool _crossfadeManual;
    private int _crossfadeMilliseconds = 3000;
    private bool _crossfadeEqualPower = true;
    private bool _holdAutomaticAdvance;
    private LoadRequest? _manualRequest;

    private bool _selectionRangeActive;
    private bool _selectionRangeRepeats;
    private bool _selectionRangeCompleted;
    private double _selectionRangeEnd;
    private long? _selectionRangeEntryId;
    private bool _releasedSelectionEndPending;
    private double _releasedSelectionEnd;
    private long? _releasedSelectionEntryId;
    private bool _releasedSelectionWasPlaying;
    private bool _releasedSelectionWasCompleted;
    private bool _releasedSelectionPassed;
    private bool _endBehaviorRestorePending;
    private bool _selectionSeekPending;
    private bool _playAfterSelectionSeek;
    private bool _restoreEndBehaviorAfterSelectionSeek;
    private double _selectionSeekTarget;


    // The last subtitle line handed out, so mpv re-reporting the same cue is not announced twice. Cleared
    // when the cue goes away, so a line that genuinely returns is read again.
    private string? _lastSubText;
    private bool _disposed;

    internal PlaybackCoordinator(nint windowHandle, IReadOnlyDictionary<string, object?>? options = null)
    {
        _session = new MpvSession(windowHandle, _loadSync, options);
        _audio = new AudioProcessor(_session);
        _tracks = new TrackDiscovery(_session);
        _metadata = new MetadataDiscovery(_session, HandleMetadataChange);
        _session.SetPropertySafely("network-timeout", 10);
        _session.OnEvent(HandleEndFile, MpvEventId.EndFile);
        _session.OnEvent(HandlePlaybackRestart, MpvEventId.PlaybackRestart);
        _session.OnEvent(HandleFileLoaded, MpvEventId.FileLoaded);
        _session.ObserveProperty("time-pos", HandlePositionChange);
        // The equalizer is left out of the chain entirely until a preset asks for it, and which bands it
        // then carries depends on the file's sample rate: a band at the Nyquist frequency rings rather
        // than filters, so the graph is rebuilt whenever that rate changes under it. Off is nothing at
        // all, not a row of flat bands.
        _session.ObserveProperty("audio-params/samplerate", (name, value) => { if (!_disposed) _audio.HandleSampleRateChange(name, value); });
        // Video parameters are not known when the load returns, so the width is watched rather than read.
        _session.ObserveProperty("video-params/w", HandleVideoParamsChange);
        // Tracks are learned after the load returns as well, so the count is watched to tell when the audio
        // tracks a file offers have changed under the menu that gates on them.
        _session.ObserveProperty("track-list/count", HandleTrackListChange);
        // The text of the subtitle currently on screen. Watching it turns each cue into a line to announce as
        // it comes up; mpv leaves it empty between cues and whenever no subtitle track is selected.
        _session.ObserveProperty("sub-text", HandleSubTextChange);
        _session.ObserveProperty("chapter-list", HandleChapterChange);
        _session.ObserveProperty("chapter", HandleChapterChange);
        _session.ObserveProperty("duration", HandleChapterChange);
        _session.ObserveProperty("eof-reached", HandleHeldEnd);
    }

    public event Action<EndReason>? Ended;

    public event Action<double>? SelectionEnded;

    public event Action? SelectionSeekCompleted;

    public event Action? VideoAvailabilityChanged;

    public event Action? MetadataChanged;

    public event Action? ChaptersChanged;

    public event Action? AudioTracksChanged;

    public event Action? SubtitleTracksChanged;

    public event Action<string>? SubtitleTextChanged;

    public bool HasVideo => _hasVideo;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Interlocked.Increment(ref _loadGeneration);
        Interlocked.Increment(ref _queueGeneration);
        if (_probe is Probe probe)
            _ = Task.Run(probe.Dispose);
        if (_spotifyAudio is { } streams)
            foreach (var pending in streams)
                if (streams.TryRemove(pending.Key, out var unused)) unused.Dispose();
        _session.Dispose();
    }

    private void FadeVolume(double target, int milliseconds)
        => _session.TryDo(mpv => mpv.FadeVolume(target, milliseconds / 1000.0));

}
