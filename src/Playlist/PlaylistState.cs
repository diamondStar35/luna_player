using LunaPlayer.Configuration;

namespace LunaPlayer.Playlist;

/// <summary>What the player knows about one entry beyond its path.</summary>
///
/// <remarks>
/// A side table rather than fields on a list of entries, because the list of paths is what almost every
/// caller wants and what the playlist window is built on. All three values are optional and all three are
/// forgotten together, which is the property that matters: a stale <see cref="Source"/> would be worse than
/// a stale title, because it would match a video no longer in the list.
/// </remarks>
/// <param name="Title">The media title, as read from the media itself.</param>
/// <param name="Source">The address the entry came from, when its path is not somewhere a user could point
/// at. For a YouTube video this is its watch page, which outlives the stream URL in the path.</param>
/// <param name="AudioFile">A second stream carrying the sound, played alongside the path. YouTube serves
/// picture and sound separately above 360p, so a video entry has both.</param>
internal readonly record struct EntryInfo(string? Title, string? Source, string? AudioFile)
{
    internal bool IsEmpty => Title is null && Source is null && AudioFile is null;
}

internal sealed class PlaylistState
{
    private readonly List<string> _files = [];
    private readonly List<int> _shuffleOrder = [];
    private readonly MarkedFileSet _marks = new();
    private readonly Dictionary<string, EntryInfo> _info = new(StringComparer.OrdinalIgnoreCase);
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
        _info.Clear();
        _currentIndex = selectedIndex;
        _pendingStart = selectedIndex == 0 && preferredPath is null ? null : startPosition;
        if (IsShuffleEnabled)
            RebuildShuffleOrder();
        return true;
    }

    /// <summary>Adds one entry to the end of the list, keeping the current entry and the marks. Used for a
    /// network stream, which is opened on top of whatever is already loaded rather than replacing it.
    /// </summary>
    /// <param name="jump">Whether the new entry becomes the current one.</param>
    internal bool Append(string path, bool jump)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        _files.Add(path);
        if (jump)
        {
            _currentIndex = _files.Count - 1;
            _pendingStart = null;
        }
        if (IsShuffleEnabled)
            RebuildShuffleOrder();
        else
            SyncShufflePosition();
        return true;
    }

    /// <summary>Remembers the media title for a file, as the player reads it from the media itself. An
    /// empty or whitespace title is treated as no title, so callers fall back to the file name.</summary>
    internal void SetTitle(string path, string? title)
        => Update(path, info => info with { Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim() });

    /// <summary>The remembered media title for a file, or null when none is known.</summary>
    internal string? GetTitle(string? path) => Info(path).Title;

    /// <summary>Records where an entry came from and, for a video, the second stream carrying its sound.
    /// </summary>
    internal void SetSource(string path, string? source, string? audioFile = null)
        => Update(path, info => info with
        {
            Source = string.IsNullOrWhiteSpace(source) ? null : source,
            AudioFile = string.IsNullOrWhiteSpace(audioFile) ? null : audioFile,
        });

    /// <summary>The address an entry came from, or null when its path is the address.</summary>
    internal string? GetSource(string? path) => Info(path).Source;

    /// <summary>The stream carrying the sound of a video whose path carries only the picture.</summary>
    internal string? GetAudioFile(string? path) => Info(path).AudioFile;

    internal string? CurrentSource => GetSource(CurrentPath);

    /// <summary>Where in the list the entry that came from <paramref name="source"/> is, or -1.</summary>
    /// <remarks>
    /// Comparison is case-sensitive: these are addresses, and the part of a YouTube address that names a
    /// video is. It is also what answers "is this video still in the list", so the caller that removes a
    /// session's entries needs no separate record of what it queued.
    /// </remarks>
    internal int IndexOfSource(string source)
        => _files.FindIndex(path => string.Equals(GetSource(path), source, StringComparison.Ordinal));

    private EntryInfo Info(string? path)
        => path is not null && _info.TryGetValue(path, out var info) ? info : default;

    private void Update(string path, Func<EntryInfo, EntryInfo> change)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var updated = change(Info(path));
        if (updated.IsEmpty) _info.Remove(path);
        else _info[path] = updated;
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
        if (_info.Remove(oldPath, out var info))
            _info[path] = info;
        return true;
    }

    internal string? RemoveCurrent()
    {
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
            return null;
        var removed = _files[_currentIndex];
        _files.RemoveAt(_currentIndex);
        _marks.Remove([removed]);
        _info.Remove(removed);
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
        var keys = paths.Select(Paths.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (keys.Count == 0) return (false, false);
        var oldCurrent = CurrentPath;
        var oldIndex = _currentIndex;
        var removed = _files.Where(path => keys.Contains(Paths.Key(path))).ToList();
        if (removed.Count == 0) return (false, false);
        _files.RemoveAll(path => keys.Contains(Paths.Key(path)));
        _marks.Remove(paths);
        foreach (var path in removed)
            _info.Remove(path);
        if (_files.Count == 0)
            _currentIndex = -1;
        else if (oldCurrent is not null && !keys.Contains(Paths.Key(oldCurrent)))
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
        _info.Clear();
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
}
