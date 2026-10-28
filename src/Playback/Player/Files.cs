using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    internal bool RenameCurrent(string newPath)
    {
        var oldPath = CurrentPath;
        if (oldPath is null || !File.Exists(oldPath))
            return false;
        var elapsed = SourceElapsed;
        StopEngine();
        try
        {
            File.Move(oldPath, newPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            LoadCurrent(elapsed, paused: false);
            return false;
        }
        RenameSavedPath(oldPath, newPath);
        _playlist.ReplaceCurrent(newPath);
        return LoadCurrent(elapsed, paused: false);
    }

    internal bool DeleteCurrent()
    {
        var path = CurrentPath;
        if (path is null || !File.Exists(path))
            return false;
        var elapsed = SourceElapsed;
        StopEngine();
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            LoadCurrent(elapsed, paused: false);
            return false;
        }
        ForgetSavedPath(path);
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
        if (path is null)
            return false;
        SavePosition();
        StopEngine();
        ForgetSavedPath(path);
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
        if (!_playlist.ClearAll())
            return false;
        _playlist.SavedEntries = null;
        StopEngine();
        CurrentChanged?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    internal bool RemovePaths(IEnumerable<string> paths)
    {
        var remove = paths.ToArray();
        var currentRemoved = CurrentPath is string current && remove.Any(path => Paths.AreSame(path, current));
        if (currentRemoved)
            StopEngine();
        foreach (var path in remove) ForgetSavedPath(path);
        var result = _playlist.RemovePaths(remove);
        if (!result.Changed)
            return false;
        if (result.CurrentChanged && CurrentPath is not null)
            return LoadCurrent();
        if (result.CurrentChanged)
            CurrentChanged?.Invoke();
        PrepareNext();
        StateChanged?.Invoke();
        return true;
    }
}
