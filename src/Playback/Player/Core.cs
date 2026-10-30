using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
using LunaPlayer.Media;
using LunaPlayer.Playlist;

namespace LunaPlayer.Playback;

internal sealed partial class Player : IDisposable
{
    private readonly PositionStore? _positions;
    private readonly IEngine _engine;
    private PlaylistState _playlist = new();

    /// <summary>The playlist a session was put in front of, or null when there is no session.</summary>
    private SuspendedPlaylist? _suspendedPlaylist;

    private long _playbackRevision;
    internal long PlaybackRevision => Interlocked.Read(ref _playbackRevision);
    private bool _disposed;
    private bool _normalizationEnabled;
    private bool _monoEnabled;
    private bool _silenceEnabled;
    private bool _loaded;
    private SilenceSettings _silence = new();
    private Preset? _equalizer;
    private bool _trackPositions;
    private EndBehavior _endBehavior;
    private GaplessPlaybackMode _gaplessPlayback;
    private bool _crossfade;
    private bool _holdAutomaticAdvance;
    private bool _wrapPlaylist;
    private PendingSessionStream? _queuedSessionStream;

    internal Player(IEngine engine, PositionStore? positions = null)
    {
        _engine = engine;
        _positions = positions;
        _engine.Ended += OnEnded;
        _engine.SelectionEnded += OnSelectionEnded;
        _engine.SelectionSeekCompleted += OnSelectionSeekCompleted;
        _engine.VideoAvailabilityChanged += OnVideoAvailabilityChanged;
        _engine.MetadataChanged += OnMetadataChanged;
        _engine.ChaptersChanged += OnChaptersChanged;
        _engine.AudioTracksChanged += OnAudioTracksChanged;
        _engine.SubtitleTracksChanged += OnSubtitleTracksChanged;
        _engine.SubtitleTextChanged += OnSubtitleTextChanged;
    }

    internal event Action<EndReason>? Ended;
    internal event Action<double>? SelectionEnded;
    internal event Action? CurrentChanged;
    internal event Action? StateChanged;
    internal event Action? NextPreparationRequested;
    internal event Action? SelectionSeekCompleted;

    /// <summary>Raised when <see cref="HasVideo"/> may have changed, a moment after a file loads.</summary>
    internal event Action? VideoAvailabilityChanged;

    internal event Action? MetadataChanged;
    internal event Action? ChaptersChanged;

    /// <summary>Raised when the audio tracks the current file offers may have changed, a moment after a file
    /// loads.</summary>
    internal event Action? AudioTracksChanged;

    /// <summary>Raised when the subtitle tracks the current file offers may have changed, a moment after a
    /// file loads.</summary>
    internal event Action? SubtitleTracksChanged;

    /// <summary>Raised with each subtitle line as it appears, while a subtitle track is being read.</summary>
    internal event Action<string>? SubtitleTextChanged;

    internal void RefreshState() => StateChanged?.Invoke();

    internal string? CurrentPath => _playlist.CurrentPath;
    internal int Count => _playlist.Count;
    internal int CurrentIndex => _playlist.CurrentIndex;
    internal IReadOnlyList<PlaylistItem> Items => _playlist.Items;
    internal string SourceIdentity => _playlist.SourceIdentity;
    internal IReadOnlyList<string> SourceFiles => _playlist.SourceFiles;
    internal string? CurrentItemId => _playlist.CurrentItem?.Id;
    internal bool IsPaused => _engine.IsPaused;
    internal bool SelectionRangeCompleted => _engine.SelectionRangeCompleted;

    /// <summary>Whether mpv has the current entry open, and so can report and change a position in it -
    /// whether or not it is playing. False before anything has been loaded and after <see cref="Stop"/>,
    /// which unloads what mpv was playing while leaving it in the playlist, so <see cref="CurrentPath"/> is
    /// not on its own proof of this.</summary>
    internal bool IsLoaded => _loaded;

    /// <summary>Whether a file is loaded and not paused.</summary>
    internal bool IsPlaying => _loaded && !_engine.IsPaused;

    /// <summary>Whether a real picture is playing, from mpv itself; false for audio. The full-screen command
    /// is gated on this. Settles shortly after a load - see <see cref="VideoAvailabilityChanged"/>.</summary>
    internal bool HasVideo => _loaded && _engine.HasVideo;

    /// <summary>What the current item is: mpv's runtime answer while something is loaded, falling back to the
    /// extension-based guess otherwise.</summary>
    internal MediaKind CurrentKind => CurrentPath is not { } path
        ? MediaKind.Audio
        : _loaded ? (_engine.HasVideo ? MediaKind.Video : MediaKind.Audio) : MediaLibrary.KindOf(path);

    internal double? Duration => _loaded ? _playlist.CurrentItem?.Chapter?.Duration ?? _engine.Duration : null;
    internal double? Elapsed => SourceElapsed is double elapsed ? ToPlaybackTime(elapsed) : null;
    internal double? Remaining => Precision.Normalize(_playlist.CurrentItem?.Chapter is not null
        ? Duration - Elapsed : _engine.Remaining ?? (Duration - Elapsed));
    internal double Volume => _engine.Volume;
    internal double Speed => _engine.Speed;
    internal double Pitch => _engine.Pitch;
    internal double Pan => _engine.Pan;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _engine.Ended -= OnEnded;
        _engine.SelectionEnded -= OnSelectionEnded;
        _engine.SelectionSeekCompleted -= OnSelectionSeekCompleted;
        _engine.VideoAvailabilityChanged -= OnVideoAvailabilityChanged;
        _engine.MetadataChanged -= OnMetadataChanged;
        _engine.ChaptersChanged -= OnChaptersChanged;
        _engine.AudioTracksChanged -= OnAudioTracksChanged;
        _engine.SubtitleTracksChanged -= OnSubtitleTracksChanged;
        _engine.SubtitleTextChanged -= OnSubtitleTextChanged;
        _engine.Dispose();
    }

    private void OnEnded(EndReason reason)
    {
        if (!_disposed)
            Ended?.Invoke(reason);
    }

    private void OnSelectionEnded(double end)
    {
        if (!_disposed)
            SelectionEnded?.Invoke(ToPlaybackTime(end));
    }

    private void OnSelectionSeekCompleted()
    {
        if (!_disposed)
            SelectionSeekCompleted?.Invoke();
    }

    private void OnVideoAvailabilityChanged()
    {
        if (!_disposed)
            VideoAvailabilityChanged?.Invoke();
    }

    private void OnChaptersChanged()
    {
        if (!_disposed) ChaptersChanged?.Invoke();
    }

    private void OnMetadataChanged()
    {
        if (!_disposed) MetadataChanged?.Invoke();
    }

    private void OnAudioTracksChanged()
    {
        if (!_disposed)
            AudioTracksChanged?.Invoke();
    }

    private void OnSubtitleTracksChanged()
    {
        if (!_disposed)
            SubtitleTracksChanged?.Invoke();
    }

    private void OnSubtitleTextChanged(string text)
    {
        if (!_disposed)
            SubtitleTextChanged?.Invoke(text);
    }

    private void StopEngine()
    {
        Interlocked.Increment(ref _playbackRevision);
        _queuedSessionStream = null;
        _chapterStopHeld = false;
        _engine.Stop();
        _loaded = false;
    }

    private bool NotifyStateChanged(bool value)
    {
        StateChanged?.Invoke();
        return value;
    }
}
