using LunaPlayer.Media;
using LunaPlayer.Playlist;
using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

internal sealed class MediaPlayer : IDisposable
{
    private readonly IPlaybackEngine _engine;
    private readonly PositionStore? _positions;
    private readonly PlaylistState _playlist = new();
    private bool _disposed;
    private bool _normalizationEnabled;
    private bool _monoEnabled;
    private bool _silenceEnabled;
    private bool _running;
    private SilenceSettings _silence = new();
    private bool _trackPositions;

    internal MediaPlayer(IPlaybackEngine engine, PositionStore? positions = null)
    {
        _engine = engine;
        _positions = positions;
        _engine.Ended += OnEnded;
    }

    internal event Action<PlaybackEndReason>? Ended;
    internal event Action? CurrentChanged;
    internal event Action? StateChanged;

    internal string? CurrentPath => _playlist.CurrentPath;
    internal int Count => _playlist.Count;
    internal int CurrentIndex => _playlist.CurrentIndex;
    internal IReadOnlyList<string> Files => _playlist.Files;
    internal bool IsPaused => _engine.IsPaused;

    /// <summary>Whether mpv has the current entry open, and so can report and change a position in it -
    /// whether or not it is playing. False before anything has been loaded and after <see cref="Stop"/>,
    /// which unloads what mpv was playing while leaving it in the playlist, so <see cref="CurrentPath"/> is
    /// not on its own proof of this.</summary>
    internal bool IsLoaded => _running;

    /// <summary>Whether a file is open and running: <see cref="IsLoaded"/> and not held where it is.
    /// </summary>
    internal bool IsPlaying => _running && !_engine.IsPaused;
    internal double? Duration => _engine.Duration;
    internal double? Elapsed => _engine.Elapsed;
    internal double? Remaining => _engine.Remaining
        ?? (Duration is double duration && Elapsed is double elapsed ? duration - elapsed : null);
    internal double Volume => _engine.Volume;
    internal double Speed => _engine.Speed;

    internal bool OpenFile(string path, double? startPosition = null)
    {
        SavePosition();
        return _playlist.OpenFile(path, startPosition) && LoadCurrent();
    }

    internal bool OpenFiles(IEnumerable<string> files, string? preferredPath = null, double? startPosition = null)
    {
        SavePosition();
        return _playlist.OpenFiles(files, preferredPath, startPosition) && LoadCurrent();
    }

    /// <summary>Opens a network stream. Unlike a file it is appended to the playlist rather than replacing
    /// it, matching the Python player's open_stream.</summary>
    internal bool OpenStream(string url)
    {
        SavePosition();
        return _playlist.Append(url, jump: true) && LoadCurrent();
    }

    internal bool OpenFolder(string folderPath)
    {
        var files = MediaLibrary.CollectFiles(folderPath);
        return files.Count > 0 && OpenFiles(files, _playlist.CurrentPath);
    }

    internal bool OpenFileWithFolder(string path, bool recursive = false, double? startPosition = null)
    {
        var folder = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(folder))
            return false;
        var files = MediaLibrary.CollectFiles(folder, recursive);
        return files.Count > 0 && OpenFiles(files, path, startPosition);
    }

    internal bool Next(bool wrap)
    {
        SavePosition();
        return _playlist.MoveNext(wrap) && LoadCurrent();
    }

    internal bool Previous(bool wrap)
    {
        SavePosition();
        return _playlist.MovePrevious(wrap) && LoadCurrent();
    }

    internal bool First()
    {
        SavePosition();
        return _playlist.GoToFirst() && LoadCurrent();
    }

    internal bool Last()
    {
        SavePosition();
        return _playlist.GoToLast() && LoadCurrent();
    }

    internal bool GoToIndex(int index)
    {
        SavePosition();
        return _playlist.GoToIndex(index) && LoadCurrent();
    }

    internal bool Reload() => LoadCurrent();

    /// <summary>Reloads the current file from the very beginning, ignoring any remembered position. This is
    /// what repeating and looping need: the file has just finished, so its saved position is the end.</summary>
    internal bool RestartCurrent() => LoadCurrent(0, paused: false);

    internal void SetEndBehavior(EndBehavior behavior) => _engine.SetEndBehavior(behavior);

    // Pausing, resuming and stopping are state changes like any other, and they are announced like any other.
    // Nothing about them is visible in a property that raises an event of its own, so a listener that has to
    // follow whether the player is running - the play button and the Windows overlay both do - would
    // otherwise have no way of hearing about it.
    internal bool TogglePause() => Changed(_engine.TogglePause());
    internal void Play() { _engine.Play(); StateChanged?.Invoke(); }
    internal void Seek(double seconds) => _engine.SeekRelative(seconds);
    internal void SeekAbsolute(double seconds) => _engine.SeekAbsolute(seconds);
    internal bool SeekToEnd()
    {
        if (Duration is not double duration || duration <= 0)
            return false;
        SeekAbsolute(Math.Max(0, duration - 0.2));
        return true;
    }

    internal void Stop() { StopEngine(); StateChanged?.Invoke(); }
    internal double SetVolume(double volume) => _engine.SetVolume(volume);
    internal double ChangeVolume(double delta) => SetVolume(Volume + delta);
    internal double SetSpeed(double speed) => _engine.SetSpeed(speed);
    internal double ChangeSpeed(double delta) => SetSpeed(Speed + delta);
    internal bool SetLoopStart(double seconds) => _engine.SetLoopStart(seconds);
    internal bool SetLoopEnd(double seconds) => _engine.SetLoopEnd(seconds);
    internal bool ClearLoop() => _engine.ClearLoop();
    internal bool IsShuffleEnabled => _playlist.IsShuffleEnabled;
    internal bool IsRepeatFileEnabled => _playlist.IsRepeatFileEnabled;
    internal bool IsSilenceRemovalEnabled => _silenceEnabled;
    internal bool IsNormalizationEnabled => _normalizationEnabled;
    internal bool IsMonoEnabled => _monoEnabled;
    internal bool ToggleShuffle() => Changed(_playlist.ToggleShuffle());
    internal bool ToggleRepeatFile() => Changed(_playlist.ToggleRepeatFile());
    internal bool? ToggleCurrentMarked()
    {
        var value = _playlist.ToggleCurrentMarked();
        if (value.HasValue) StateChanged?.Invoke();
        return value;
    }
    internal bool ToggleAllMarked() => Changed(_playlist.ToggleAllMarked());
    internal bool ClearMarked()
    {
        var value = _playlist.ClearMarked();
        if (value) StateChanged?.Invoke();
        return value;
    }
    internal int MarkedCount => _playlist.MarkedCount;
    internal bool IsCurrentMarked => _playlist.IsCurrentMarked;
    internal bool AreAllMarked => _playlist.AreAllMarked;
    internal IReadOnlyList<string> MarkedFiles => _playlist.MarkedFiles;
    internal IReadOnlyList<AudioDevice> GetAudioDevices() => _engine.GetAudioDevices();
    internal string CurrentAudioDevice => _engine.CurrentAudioDevice;
    internal bool SetAudioDevice(string name) => _engine.SetAudioDevice(name);
    internal void TrackPositions(bool enabled) => _trackPositions = enabled;

    internal void SavePosition()
    {
        if (!_trackPositions || _positions is null || CurrentPath is not string path || !File.Exists(path)
            || Elapsed is not double elapsed) return;
        _positions.Set(path, elapsed);
    }
    internal bool SetNormalization(bool enabled)
    {
        var previous = _normalizationEnabled;
        _normalizationEnabled = enabled;
        if (_engine.SetNormalization(enabled)) return true;
        _normalizationEnabled = previous;
        _engine.SetNormalization(previous);
        return false;
    }

    internal bool SetMono(bool enabled)
    {
        var previous = _monoEnabled;
        _monoEnabled = enabled;
        if (_engine.SetMono(enabled)) return true;
        _monoEnabled = previous;
        _engine.SetMono(previous);
        return false;
    }

    internal bool ConfigureSilence(SilenceSettings settings)
    {
        var previous = _silence;
        _silence = settings.Copy();
        if (!_silenceEnabled || _engine.SetSilenceRemoval(true, AudioFilters.SilenceGraph(_silence))) return true;
        _silence = previous;
        _engine.SetSilenceRemoval(true, AudioFilters.SilenceGraph(_silence));
        return false;
    }

    internal bool SetSilenceRemoval(bool enabled)
    {
        var previous = _silenceEnabled;
        _silenceEnabled = enabled;
        if (_engine.SetSilenceRemoval(enabled, AudioFilters.SilenceGraph(_silence)))
        {
            StateChanged?.Invoke();
            return true;
        }
        _silenceEnabled = previous;
        _engine.SetSilenceRemoval(previous, AudioFilters.SilenceGraph(_silence));
        return false;
    }

    internal bool RenameCurrent(string newPath)
    {
        var oldPath = CurrentPath;
        if (oldPath is null || !File.Exists(oldPath))
            return false;
        var elapsed = Elapsed;
        StopEngine();
        try
        {
            File.Move(oldPath, newPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _engine.Load(oldPath, elapsed, paused: false);
            return false;
        }
        _playlist.ReplaceCurrent(newPath);
        return LoadCurrent(elapsed, paused: false);
    }

    internal bool DeleteCurrent()
    {
        var path = CurrentPath;
        if (path is null || !File.Exists(path))
            return false;
        var elapsed = Elapsed;
        StopEngine();
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _engine.Load(path, elapsed, paused: false);
            return false;
        }
        _playlist.RemoveCurrent();
        if (CurrentPath is null)
        {
            CurrentChanged?.Invoke();
            StateChanged?.Invoke();
            return true;
        }
        return LoadCurrent();
    }

    internal bool CloseCurrent()
    {
        var path = CurrentPath;
        if (path is null) return false;
        SavePosition();
        StopEngine();
        _playlist.RemoveCurrent();
        if (CurrentPath is null)
        {
            CurrentChanged?.Invoke();
            StateChanged?.Invoke();
            return true;
        }
        return LoadCurrent();
    }

    internal bool CloseAll()
    {
        SavePosition();
        if (!_playlist.ClearAll()) return false;
        StopEngine();
        CurrentChanged?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    internal bool RemovePaths(IEnumerable<string> paths)
    {
        var remove = paths.ToArray();
        var currentRemoved = CurrentPath is string current && remove.Any(path => Paths.AreSame(path, current));
        if (currentRemoved) StopEngine();
        var result = _playlist.RemovePaths(remove);
        if (!result.Changed) return false;
        if (result.CurrentChanged && CurrentPath is not null) return LoadCurrent();
        if (result.CurrentChanged) CurrentChanged?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _engine.Ended -= OnEnded;
        _engine.Dispose();
    }

    private bool LoadCurrent()
        => LoadCurrent(_playlist.TakePendingStart(), paused: false);

    private bool LoadCurrent(double? startPosition, bool paused)
    {
        var path = _playlist.CurrentPath;
        if (path is null)
            return false;
        if (!startPosition.HasValue && _trackPositions && _positions is not null && File.Exists(path))
            startPosition = _positions.Get(path);
        _engine.ClearLoop();
        if (!_engine.Load(path, startPosition, paused))
            return false;
        _engine.SetNormalization(_normalizationEnabled);
        _engine.SetMono(_monoEnabled);
        _engine.SetSilenceRemoval(_silenceEnabled, AudioFilters.SilenceGraph(_silence));
        _running = true;
        CurrentChanged?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>The name to show for a file: its media title when it declares one, otherwise the file name.
    /// Mirrors the Python player, which shows a title where it knows one and the file name otherwise.</summary>
    internal string DisplayName(string path)
    {
        // mpv loads asynchronously, so the title is not there yet when the file is opened. Re-read it for
        // whatever is playing whenever a name is asked for, and keep it: once a file has been played its
        // title stays available for the playlist, which names entries that are not playing.
        if (string.Equals(path, CurrentPath, StringComparison.Ordinal))
            RememberTitle(path);
        return _playlist.GetTitle(path) ?? MediaLibrary.DisplayName(path);
    }

    /// <summary>The display name of whatever is playing, or null when nothing is.</summary>
    internal string? CurrentDisplayName => CurrentPath is string path ? DisplayName(path) : null;

    // mpv only knows a title once the file is loaded, so it is read here and kept per path: the playlist
    // dialog names every entry, not just the one playing.
    private void RememberTitle(string path)
    {
        var title = _engine.MediaTitle;
        if (title is not null && string.Equals(title, MediaLibrary.DisplayName(path), StringComparison.OrdinalIgnoreCase))
            title = null;
        _playlist.SetTitle(path, title);
    }

    private void OnEnded(PlaybackEndReason reason)
    {
        if (!_disposed)
            Ended?.Invoke(reason);
    }

    private void StopEngine()
    {
        _engine.Stop();
        _running = false;
    }

    private bool Changed(bool value)
    {
        StateChanged?.Invoke();
        return value;
    }
}
