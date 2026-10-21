using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Playlist;

internal sealed class PlaylistState
{
    private readonly List<string> _files = [];
    private readonly ShuffleOrder _shuffle = new();
    private readonly MarkedFileSet _marks = new();
    private readonly PlaylistEntries _entries = new();
    private int _currentIndex = -1;
    private double? _pendingStart;

    internal string? CurrentPath => _currentIndex >= 0 && _currentIndex < _files.Count
        ? _files[_currentIndex]
        : null;

    /// <summary>The current entry's position in the order the user is navigating.</summary>
    internal int CurrentIndex => IsShuffleEnabled ? _shuffle.Position : _currentIndex;
    internal int Count => _files.Count;
    /// <summary>The files in their active order: shuffled while shuffle is enabled, source order otherwise.</summary>
    internal IReadOnlyList<string> Files => IsShuffleEnabled
        ? _shuffle.Arrange(_files)
        : _files.AsReadOnly();
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
        _entries.Clear();
        _currentIndex = selectedIndex;
        _pendingStart = selectedIndex == 0 && preferredPath is null ? null : Precision.Normalize(startPosition);
        if (IsShuffleEnabled)
            CreateShuffleOrder();
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
        var addedIndex = _files.Count;
        _files.Add(path);
        if (IsShuffleEnabled)
        {
            // A playlist is shuffled once. Adding a file gives only that file a random place; moving every
            // existing file again would make Previous and Next point somewhere different after each edit.
            _shuffle.Insert(addedIndex);
        }
        if (jump)
        {
            _currentIndex = addedIndex;
            _pendingStart = null;
        }
        SyncShufflePosition();
        return true;
    }

    /// <summary>Remembers the media title for a file, as the player reads it from the media itself. An
    /// empty or whitespace title is treated as no title, so callers fall back to the file name.</summary>
    internal void SetTitle(string path, string? title)
        => _entries.SetTitle(path, title);

    /// <summary>The remembered media title for a file, or null when none is known.</summary>
    internal string? GetTitle(string? path) => _entries.Title(path);

    /// <summary>Records where an entry came from and, for a video, the second stream carrying its sound.
    /// </summary>
    internal void SetSource(string path, string? source, string? audioFile = null, bool audioOnly = false)
        => _entries.SetSource(path, source, audioFile, audioOnly);

    /// <summary>The address an entry came from, or null when its path is the address.</summary>
    internal string? GetSource(string? path) => _entries.Source(path);

    /// <summary>The stream carrying the sound of a video whose path carries only the picture.</summary>
    internal string? GetAudioFile(string? path) => _entries.AudioFile(path);

    /// <summary>Whether an entry should be played as sound only, with any video track left undecoded.</summary>
    internal bool GetAudioOnly(string? path) => _entries.AudioOnly(path);

    internal string? CurrentSource => GetSource(CurrentPath);

    internal bool MoveNext(bool wrap)
        => IsShuffleEnabled ? MoveNextShuffled(wrap) : MoveNextSequential(wrap);

    internal bool MovePrevious(bool wrap)
        => IsShuffleEnabled ? MovePreviousShuffled(wrap) : MovePreviousSequential(wrap);

    /// <summary>The item Next would choose without changing the current item.</summary>
    internal string? PeekNext(bool wrap)
    {
        if (_files.Count < 2)
            return null;
        if (IsShuffleEnabled)
        {
            SyncShufflePosition();
            if (_shuffle.Position < 0)
                return _files[_shuffle[0]];
            var position = _shuffle.Position + 1;
            if (position >= _shuffle.Count)
            {
                if (!wrap)
                    return null;
                position = 0;
            }
            return _files[_shuffle[position]];
        }
        var index = _currentIndex + 1;
        if (index >= _files.Count)
        {
            if (!wrap)
                return null;
            index = 0;
        }
        return index >= 0 && index < _files.Count ? _files[index] : null;
    }

    internal bool GoToFirst() => GoToIndex(0);

    internal bool GoToLast() => GoToIndex(_files.Count - 1);

    internal bool GoToIndex(int index)
    {
        if (index < 0 || index >= _files.Count)
            return false;
        if (IsShuffleEnabled)
        {
            SyncShufflePosition();
            var fileIndex = _shuffle[index];
            if (fileIndex == _currentIndex)
                return false;
            _currentIndex = fileIndex;
            _shuffle.Position = index;
        }
        else
        {
            if (index == _currentIndex)
                return false;
            _currentIndex = index;
        }
        _pendingStart = null;
        return true;
    }

    internal bool ReplaceCurrent(string path)
    {
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
            return false;
        var oldPath = _files[_currentIndex];
        _files[_currentIndex] = path;
        _marks.Replace(oldPath, path);
        _entries.Replace(oldPath, path);
        return true;
    }

    internal string? RemoveCurrent()
    {
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
            return null;
        if (IsShuffleEnabled)
            SyncShufflePosition();
        var removedIndex = _currentIndex;
        var removedPosition = _shuffle.Position;
        var removed = _files[_currentIndex];
        _files.RemoveAt(_currentIndex);
        _marks.Remove([removed]);
        _entries.Remove(removed);
        _pendingStart = null;
        if (IsShuffleEnabled)
        {
            RemoveFromShuffleOrder(removedIndex);
            if (_shuffle.Count == 0)
            {
                _currentIndex = -1;
                _shuffle.Position = -1;
            }
            else
            {
                // Removing the current item advances to the item that followed it in the fixed shuffled
                // order. At the end there is no following item, so the preceding one remains current.
                _shuffle.Position = Math.Min(Math.Max(removedPosition, 0), _shuffle.Count - 1);
                _currentIndex = _shuffle[_shuffle.Position];
            }
        }
        else if (_files.Count == 0)
            _currentIndex = -1;
        else if (_currentIndex >= _files.Count)
            _currentIndex = _files.Count - 1;
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
        if (IsShuffleEnabled)
            SyncShufflePosition();
        var oldShufflePosition = _shuffle.Position;
        var oldShuffleOrder = _shuffle.Snapshot();
        var removedIndexes = Enumerable.Range(0, _files.Count)
            .Where(index => keys.Contains(Paths.Key(_files[index])))
            .ToHashSet();
        if (removedIndexes.Count == 0) return (false, false);
        var removed = removedIndexes.Select(index => _files[index]).ToList();
        var newIndexes = new int[_files.Count];
        var kept = new List<string>(_files.Count - removedIndexes.Count);
        for (var index = 0; index < _files.Count; index++)
        {
            if (removedIndexes.Contains(index))
            {
                newIndexes[index] = -1;
                continue;
            }
            newIndexes[index] = kept.Count;
            kept.Add(_files[index]);
        }
        _files.Clear();
        _files.AddRange(kept);
        _marks.Remove(paths);
        _entries.Remove(removed);
        if (IsShuffleEnabled)
        {
            var stableOrder = oldShuffleOrder
                .Where(index => !removedIndexes.Contains(index))
                .Select(index => newIndexes[index])
                .ToList();
            _shuffle.Replace(stableOrder);
            if (_shuffle.Count == 0)
            {
                _currentIndex = -1;
                _shuffle.Position = -1;
            }
            else if (oldIndex >= 0 && oldIndex < newIndexes.Length && !removedIndexes.Contains(oldIndex))
            {
                _currentIndex = newIndexes[oldIndex];
                _shuffle.Position = _shuffle.IndexOf(_currentIndex);
            }
            else if (oldIndex < 0 || oldIndex >= newIndexes.Length)
            {
                _shuffle.Position = 0;
                _currentIndex = _shuffle[0];
            }
            else
            {
                // Count the surviving entries that preceded the removed current one. That is the position
                // of its old successor after removal, or the final position when the current one was last.
                var nextPosition = oldShuffleOrder
                    .Take(Math.Max(0, oldShufflePosition))
                    .Count(index => !removedIndexes.Contains(index));
                _shuffle.Position = Math.Min(nextPosition, _shuffle.Count - 1);
                _currentIndex = _shuffle[_shuffle.Position];
            }
        }
        else if (_files.Count == 0)
            _currentIndex = -1;
        else if (oldCurrent is not null && !keys.Contains(Paths.Key(oldCurrent)))
            _currentIndex = newIndexes[oldIndex];
        else
            _currentIndex = Math.Min(Math.Max(oldIndex, 0), _files.Count - 1);
        _pendingStart = null;
        return (true, !string.Equals(oldCurrent, CurrentPath, StringComparison.Ordinal));
    }

    internal bool ClearAll()
    {
        if (_files.Count == 0) return false;
        _files.Clear();
        _marks.Clear();
        _entries.Clear();
        _currentIndex = -1;
        _pendingStart = null;
        ClearShuffleOrder();
        return true;
    }

    internal bool ToggleShuffle()
    {
        IsShuffleEnabled = !IsShuffleEnabled;
        if (IsShuffleEnabled)
            CreateShuffleOrder();
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
        if (_files.Count == 0)
            return false;
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
        {
            _currentIndex = 0;
            _pendingStart = null;
            return true;
        }
        if (_files.Count == 1)
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
        if (_files.Count == 0)
            return false;
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
        {
            _currentIndex = _files.Count - 1;
            _pendingStart = null;
            return true;
        }
        if (_files.Count == 1)
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
        if (_files.Count == 0)
            return false;
        SyncShufflePosition();
        if (_shuffle.Position < 0)
        {
            _shuffle.Position = 0;
            _currentIndex = _shuffle[0];
            _pendingStart = null;
            return true;
        }
        if (_files.Count == 1)
            return false;
        if (_shuffle.Position + 1 >= _shuffle.Count)
        {
            if (!wrap)
                return false;
            _shuffle.Position = 0;
        }
        else
        {
            _shuffle.Position++;
        }
        _currentIndex = _shuffle[_shuffle.Position];
        _pendingStart = null;
        return true;
    }

    private bool MovePreviousShuffled(bool wrap)
    {
        if (_files.Count == 0)
            return false;
        SyncShufflePosition();
        if (_shuffle.Position < 0)
        {
            _shuffle.Position = _shuffle.Count - 1;
            _currentIndex = _shuffle[_shuffle.Position];
            _pendingStart = null;
            return true;
        }
        if (_files.Count == 1)
            return false;
        if (_shuffle.Position <= 0)
        {
            if (!wrap)
                return false;
            _shuffle.Position = _shuffle.Count - 1;
        }
        else
        {
            _shuffle.Position--;
        }
        _currentIndex = _shuffle[_shuffle.Position];
        _pendingStart = null;
        return true;
    }

    private void SyncShufflePosition()
    {
        if (!IsShuffleEnabled)
            return;
        _shuffle.Sync(_files.Count, _currentIndex);
    }

    /// <summary>Creates the one random order used until shuffle is turned off or a new playlist is opened.
    /// The current item is shuffled like every other item; keeping its old numeric position is what made
    /// the first and last entries remain at the edges.</summary>
    private void CreateShuffleOrder()
    {
        _shuffle.Create(_files.Count, _currentIndex);
    }

    private void RemoveFromShuffleOrder(int removedIndex)
    {
        _shuffle.RemoveFile(removedIndex);
    }

    private void ClearShuffleOrder()
    {
        _shuffle.Clear();
    }
}
