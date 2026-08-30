namespace LunaPlayer.Playlist;

internal sealed class PlaylistState
{
    private readonly List<string> _files = [];
    private readonly List<int> _shuffleOrder = [];
    private readonly MarkedFileSet _marks = new();
    private int _currentIndex = -1;
    private int _shufflePosition = -1;
    private double? _pendingStart;

    internal string? CurrentPath => _currentIndex >= 0 && _currentIndex < _files.Count
        ? _files[_currentIndex]
        : null;

    internal int CurrentIndex => _currentIndex;
    internal int Count => _files.Count;
    internal IReadOnlyList<string> Files => _files.AsReadOnly();
    internal bool IsShuffleEnabled { get; private set; }
    internal bool IsRepeatFileEnabled { get; private set; }

    internal bool OpenFile(string path, double? startPosition = null)
        => OpenFiles([path], path, startPosition);

    internal bool OpenFiles(IEnumerable<string> paths, string? preferredPath = null, double? startPosition = null)
    {
        var files = paths.Where(static path => !string.IsNullOrWhiteSpace(path)).ToList();
        if (files.Count == 0)
            return false;

        var selectedIndex = preferredPath is null
            ? 0
            : files.FindIndex(path => string.Equals(path, preferredPath, StringComparison.Ordinal));
        if (selectedIndex < 0)
            selectedIndex = 0;

        _files.Clear();
        _files.AddRange(files);
        _marks.Clear();
        _currentIndex = selectedIndex;
        _pendingStart = selectedIndex == 0 && preferredPath is null ? null : startPosition;
        if (IsShuffleEnabled)
            RebuildShuffleOrder();
        return true;
    }

    internal bool MoveNext(bool wrap)
        => IsShuffleEnabled ? MoveNextShuffled(wrap) : MoveNextSequential(wrap);

    internal bool MovePrevious(bool wrap)
        => IsShuffleEnabled ? MovePreviousShuffled(wrap) : MovePreviousSequential(wrap);

    internal bool GoToFirst() => GoToIndex(0);

    internal bool GoToLast() => GoToIndex(_files.Count - 1);

    internal bool GoToIndex(int index)
    {
        if (index < 0 || index >= _files.Count || index == _currentIndex)
            return false;
        _currentIndex = index;
        _pendingStart = null;
        SyncShufflePosition();
        return true;
    }

    internal bool ReplaceCurrent(string path)
    {
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
            return false;
        var oldPath = _files[_currentIndex];
        _files[_currentIndex] = path;
        _marks.Replace(oldPath, path);
        return true;
    }

    internal string? RemoveCurrent()
    {
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
            return null;
        var removed = _files[_currentIndex];
        _files.RemoveAt(_currentIndex);
        _marks.Remove([removed]);
        if (_files.Count == 0)
            _currentIndex = -1;
        else if (_currentIndex >= _files.Count)
            _currentIndex = _files.Count - 1;
        _pendingStart = null;
        if (IsShuffleEnabled)
            RebuildShuffleOrder();
        return removed;
    }

    internal bool? ToggleCurrentMarked()
    {
        return _marks.Toggle(CurrentPath);
    }

    internal bool ToggleAllMarked()
    {
        return _marks.ToggleAll(_files);
    }

    internal bool ClearMarked()
    {
        return _marks.Clear();
    }
    internal int MarkedCount => _marks.Count(_files);
    internal bool IsCurrentMarked => _marks.Contains(CurrentPath);
    internal bool AreAllMarked => _marks.AreAll(_files);
    internal IReadOnlyList<string> MarkedFiles
        => _marks.Files(_files);

    internal (bool Changed, bool CurrentChanged) RemovePaths(IEnumerable<string> paths)
    {
        var keys = paths.Select(NormalizeKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (keys.Count == 0) return (false, false);
        var oldCurrent = CurrentPath;
        var oldIndex = _currentIndex;
        var changed = _files.RemoveAll(path => keys.Contains(NormalizeKey(path))) > 0;
        if (!changed) return (false, false);
        _marks.Remove(paths);
        if (_files.Count == 0)
            _currentIndex = -1;
        else if (oldCurrent is not null && !keys.Contains(NormalizeKey(oldCurrent)))
            _currentIndex = _files.FindIndex(path => string.Equals(path, oldCurrent, StringComparison.Ordinal));
        else
            _currentIndex = Math.Min(Math.Max(oldIndex, 0), _files.Count - 1);
        _pendingStart = null;
        if (IsShuffleEnabled) RebuildShuffleOrder();
        return (true, !string.Equals(oldCurrent, CurrentPath, StringComparison.Ordinal));
    }

    internal bool ClearAll()
    {
        if (_files.Count == 0) return false;
        _files.Clear();
        _marks.Clear();
        _currentIndex = -1;
        _pendingStart = null;
        ClearShuffleOrder();
        return true;
    }

    internal bool ToggleShuffle()
    {
        IsShuffleEnabled = !IsShuffleEnabled;
        if (IsShuffleEnabled)
            RebuildShuffleOrder();
        else
            ClearShuffleOrder();
        return IsShuffleEnabled;
    }

    internal bool ToggleRepeatFile()
    {
        IsRepeatFileEnabled = !IsRepeatFileEnabled;
        return IsRepeatFileEnabled;
    }

    internal double? TakePendingStart()
    {
        var value = _pendingStart;
        _pendingStart = null;
        return value;
    }

    private bool MoveNextSequential(bool wrap)
    {
        if (_files.Count <= 1)
            return false;
        if (_currentIndex + 1 >= _files.Count)
        {
            if (!wrap)
                return false;
            _currentIndex = 0;
        }
        else
        {
            _currentIndex++;
        }
        _pendingStart = null;
        SyncShufflePosition();
        return true;
    }

    private bool MovePreviousSequential(bool wrap)
    {
        if (_files.Count <= 1)
            return false;
        if (_currentIndex <= 0)
        {
            if (!wrap)
                return false;
            _currentIndex = _files.Count - 1;
        }
        else
        {
            _currentIndex--;
        }
        _pendingStart = null;
        SyncShufflePosition();
        return true;
    }

    private bool MoveNextShuffled(bool wrap)
    {
        if (_files.Count <= 1)
            return false;
        SyncShufflePosition();
        if (_shufflePosition + 1 >= _shuffleOrder.Count)
        {
            if (!wrap)
                return false;
            _shufflePosition = 0;
        }
        else
        {
            _shufflePosition++;
        }
        _currentIndex = _shuffleOrder[_shufflePosition];
        _pendingStart = null;
        return true;
    }

    private bool MovePreviousShuffled(bool wrap)
    {
        if (_files.Count <= 1)
            return false;
        SyncShufflePosition();
        if (_shufflePosition <= 0)
        {
            if (!wrap)
                return false;
            _shufflePosition = _shuffleOrder.Count - 1;
        }
        else
        {
            _shufflePosition--;
        }
        _currentIndex = _shuffleOrder[_shufflePosition];
        _pendingStart = null;
        return true;
    }

    private void SyncShufflePosition()
    {
        if (!IsShuffleEnabled)
            return;
        if (_shuffleOrder.Count != _files.Count || !_shuffleOrder.Contains(_currentIndex))
        {
            RebuildShuffleOrder();
            return;
        }
        _shufflePosition = _shuffleOrder.IndexOf(_currentIndex);
    }

    private void RebuildShuffleOrder()
    {
        ClearShuffleOrder();
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
            return;
        for (var index = 0; index < _files.Count; index++)
        {
            if (index != _currentIndex)
                _shuffleOrder.Add(index);
        }
        for (var index = _shuffleOrder.Count - 1; index > 0; index--)
        {
            var target = Random.Shared.Next(index + 1);
            (_shuffleOrder[index], _shuffleOrder[target]) = (_shuffleOrder[target], _shuffleOrder[index]);
        }
        var insertPosition = Math.Min(Math.Max(_currentIndex, 0), _shuffleOrder.Count);
        _shuffleOrder.Insert(insertPosition, _currentIndex);
        _shufflePosition = insertPosition;
    }

    private void ClearShuffleOrder()
    {
        _shuffleOrder.Clear();
        _shufflePosition = -1;
    }

    private static string NormalizeKey(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return path;
        }
    }
}
