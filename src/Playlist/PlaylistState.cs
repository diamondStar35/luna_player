using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

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
/// <param name="AudioOnly">Whether this entry is to be played as sound only, with any video track ignored.
/// A YouTube video opened "as audio" can still resolve to a stream that carries a picture; this says to
/// leave that picture undecoded so the play is truly audio.</param>
internal readonly record struct EntryInfo(string? Title, string? Source, string? AudioFile, bool AudioOnly = false)
{
    internal bool IsEmpty => Title is null && Source is null && AudioFile is null && !AudioOnly;
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

    /// <summary>The current entry's position in the order the user is navigating.</summary>
    internal int CurrentIndex => IsShuffleEnabled ? _shufflePosition : _currentIndex;
    internal int Count => _files.Count;
    /// <summary>The files in their active order: shuffled while shuffle is enabled, source order otherwise.</summary>
    internal IReadOnlyList<string> Files => IsShuffleEnabled
        ? _shuffleOrder.Select(index => _files[index]).ToArray()
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
        _info.Clear();
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
            _shuffleOrder.Insert(Random.Shared.Next(_shuffleOrder.Count + 1), addedIndex);
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
        => Update(path, info => info with { Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim() });

    /// <summary>The remembered media title for a file, or null when none is known.</summary>
    internal string? GetTitle(string? path) => Info(path).Title;

    /// <summary>Records where an entry came from and, for a video, the second stream carrying its sound.
    /// </summary>
    internal void SetSource(string path, string? source, string? audioFile = null, bool audioOnly = false)
        => Update(path, info => info with
        {
            Source = string.IsNullOrWhiteSpace(source) ? null : source,
            AudioFile = string.IsNullOrWhiteSpace(audioFile) ? null : audioFile,
            AudioOnly = audioOnly,
        });

    /// <summary>The address an entry came from, or null when its path is the address.</summary>
    internal string? GetSource(string? path) => Info(path).Source;

    /// <summary>The stream carrying the sound of a video whose path carries only the picture.</summary>
    internal string? GetAudioFile(string? path) => Info(path).AudioFile;

    /// <summary>Whether an entry should be played as sound only, with any video track left undecoded.</summary>
    internal bool GetAudioOnly(string? path) => Info(path).AudioOnly;

    internal string? CurrentSource => GetSource(CurrentPath);

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
        if (index < 0 || index >= _files.Count)
            return false;
        if (IsShuffleEnabled)
        {
            SyncShufflePosition();
            var fileIndex = _shuffleOrder[index];
            if (fileIndex == _currentIndex)
                return false;
            _currentIndex = fileIndex;
            _shufflePosition = index;
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
        if (_info.Remove(oldPath, out var info))
            _info[path] = info;
        return true;
    }

    internal string? RemoveCurrent()
    {
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
            return null;
        if (IsShuffleEnabled)
            SyncShufflePosition();
        var removedIndex = _currentIndex;
        var removedPosition = _shufflePosition;
        var removed = _files[_currentIndex];
        _files.RemoveAt(_currentIndex);
        _marks.Remove([removed]);
        _info.Remove(removed);
        _pendingStart = null;
        if (IsShuffleEnabled)
        {
            RemoveFromShuffleOrder(removedIndex);
            if (_shuffleOrder.Count == 0)
            {
                _currentIndex = -1;
                _shufflePosition = -1;
            }
            else
            {
                // Removing the current item advances to the item that followed it in the fixed shuffled
                // order. At the end there is no following item, so the preceding one remains current.
                _shufflePosition = Math.Min(Math.Max(removedPosition, 0), _shuffleOrder.Count - 1);
                _currentIndex = _shuffleOrder[_shufflePosition];
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
        var oldShufflePosition = _shufflePosition;
        var oldShuffleOrder = _shuffleOrder.ToArray();
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
        foreach (var path in removed)
            _info.Remove(path);
        if (IsShuffleEnabled)
        {
            var stableOrder = oldShuffleOrder
                .Where(index => !removedIndexes.Contains(index))
                .Select(index => newIndexes[index])
                .ToList();
            _shuffleOrder.Clear();
            _shuffleOrder.AddRange(stableOrder);
            if (_shuffleOrder.Count == 0)
            {
                _currentIndex = -1;
                _shufflePosition = -1;
            }
            else if (oldIndex >= 0 && oldIndex < newIndexes.Length && !removedIndexes.Contains(oldIndex))
            {
                _currentIndex = newIndexes[oldIndex];
                _shufflePosition = _shuffleOrder.IndexOf(_currentIndex);
            }
            else if (oldIndex < 0 || oldIndex >= newIndexes.Length)
            {
                _shufflePosition = 0;
                _currentIndex = _shuffleOrder[0];
            }
            else
            {
                // Count the surviving entries that preceded the removed current one. That is the position
                // of its old successor after removal, or the final position when the current one was last.
                var nextPosition = oldShuffleOrder
                    .Take(Math.Max(0, oldShufflePosition))
                    .Count(index => !removedIndexes.Contains(index));
                _shufflePosition = Math.Min(nextPosition, _shuffleOrder.Count - 1);
                _currentIndex = _shuffleOrder[_shufflePosition];
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
        if (_shufflePosition < 0)
        {
            _shufflePosition = 0;
            _currentIndex = _shuffleOrder[0];
            _pendingStart = null;
            return true;
        }
        if (_files.Count == 1)
            return false;
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
        if (_files.Count == 0)
            return false;
        SyncShufflePosition();
        if (_shufflePosition < 0)
        {
            _shufflePosition = _shuffleOrder.Count - 1;
            _currentIndex = _shuffleOrder[_shufflePosition];
            _pendingStart = null;
            return true;
        }
        if (_files.Count == 1)
            return false;
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
        RepairShuffleOrder();
        _shufflePosition = _shuffleOrder.IndexOf(_currentIndex);
    }

    /// <summary>Creates the one random order used until shuffle is turned off or a new playlist is opened.
    /// The current item is shuffled like every other item; keeping its old numeric position is what made
    /// the first and last entries remain at the edges.</summary>
    private void CreateShuffleOrder()
    {
        ClearShuffleOrder();
        if (_currentIndex < 0 || _currentIndex >= _files.Count)
            return;
        for (var index = 0; index < _files.Count; index++)
            _shuffleOrder.Add(index);
        for (var index = _shuffleOrder.Count - 1; index > 0; index--)
        {
            var target = Random.Shared.Next(index + 1);
            (_shuffleOrder[index], _shuffleOrder[target]) = (_shuffleOrder[target], _shuffleOrder[index]);
        }
        _shufflePosition = _shuffleOrder.IndexOf(_currentIndex);
    }

    /// <summary>Repairs an invalid permutation without changing the relative order of valid entries.</summary>
    private void RepairShuffleOrder()
    {
        var seen = new HashSet<int>();
        _shuffleOrder.RemoveAll(index => index < 0 || index >= _files.Count || !seen.Add(index));
        for (var index = 0; index < _files.Count; index++)
        {
            if (!seen.Contains(index))
                _shuffleOrder.Insert(Random.Shared.Next(_shuffleOrder.Count + 1), index);
        }
    }

    private void RemoveFromShuffleOrder(int removedIndex)
    {
        _shuffleOrder.Remove(removedIndex);
        for (var index = 0; index < _shuffleOrder.Count; index++)
        {
            if (_shuffleOrder[index] > removedIndex)
                _shuffleOrder[index]--;
        }
    }

    private void ClearShuffleOrder()
    {
        _shuffleOrder.Clear();
        _shufflePosition = -1;
    }
}
