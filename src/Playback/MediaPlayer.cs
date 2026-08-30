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
    internal bool TogglePause() => _engine.TogglePause();
    internal void Play() => _engine.Play();
    internal void Seek(double seconds) => _engine.SeekRelative(seconds);
    internal void SeekAbsolute(double seconds) => _engine.SeekAbsolute(seconds);
    internal bool SeekToEnd()
    {
        if (Duration is not double duration || duration <= 0)
            return false;
        SeekAbsolute(Math.Max(0, duration - 0.2));
        return true;
    }

    internal void Stop() => _engine.Stop();
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
        _engine.Stop();
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
        _engine.Stop();
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
        _engine.Stop();
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
        _engine.Stop();
        CurrentChanged?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    internal bool RemovePaths(IEnumerable<string> paths)
    {
        var remove = paths.ToArray();
        var currentRemoved = CurrentPath is string current
            && remove.Any(path => string.Equals(PathKey(path), PathKey(current), StringComparison.OrdinalIgnoreCase));
        if (currentRemoved) _engine.Stop();
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
        CurrentChanged?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    private void OnEnded(PlaybackEndReason reason)
    {
        if (!_disposed)
            Ended?.Invoke(reason);
    }

    private bool Changed(bool value)
    {
        StateChanged?.Invoke();
        return value;
    }

    private static string PathKey(string path)
    {
        try { return Path.GetFullPath(path); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException) { return path; }
    }
}
