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
        _engine.AudioTracksChanged += OnAudioTracksChanged;
        _engine.SubtitleTracksChanged += OnSubtitleTracksChanged;
        _engine.SubtitleTextChanged += OnSubtitleTextChanged;
    }

    internal event Action<EndReason>? Ended;
    internal event Action<double>? SelectionEnded;
    internal event Action? CurrentChanged;
    internal event Action? StateChanged;
    internal event Action? SelectionSeekCompleted;

    /// <summary>Raised when <see cref="HasVideo"/> may have changed, a moment after a file loads.</summary>
    internal event Action? VideoAvailabilityChanged;

    /// <summary>Raised when the audio tracks the current file offers may have changed, a moment after a file
    /// loads.</summary>
    internal event Action? AudioTracksChanged;

    /// <summary>Raised when the subtitle tracks the current file offers may have changed, a moment after a
    /// file loads.</summary>
    internal event Action? SubtitleTracksChanged;

    /// <summary>Raised with each subtitle line as it appears, while a subtitle track is being read.</summary>
    internal event Action<string>? SubtitleTextChanged;

    internal string? CurrentPath => _playlist.CurrentPath;
    internal int Count => _playlist.Count;
    internal int CurrentIndex => _playlist.CurrentIndex;
    internal IReadOnlyList<string> Files => _playlist.Files;
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

    internal double? Duration => _engine.Duration;
    internal double? Elapsed => _engine.Elapsed;
    internal double? Remaining => Precision.Normalize(_engine.Remaining
        ?? (Duration is double duration && Elapsed is double elapsed ? duration - elapsed : null));
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
            SelectionEnded?.Invoke(end);
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
        _queuedSessionStream = null;
        _engine.Stop();
        _loaded = false;
    }

    private bool NotifyStateChanged(bool value)
    {
        StateChanged?.Invoke();
        return value;
    }
}
