using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;

namespace LunaPlayer.Playlist;

internal sealed partial class PlaylistState
{
    private readonly List<PlaylistItem> _items = [];
    internal List<SavedEntry>? SavedEntries { get; set; }
    internal IReadOnlyList<PlaylistItem> Sources => _sources.AsReadOnly();
    private readonly List<PlaylistItem> _sources = [];
    private readonly Dictionary<string, IReadOnlyList<Chapter>> _chapters = new(StringComparer.OrdinalIgnoreCase);
    private bool _audiobookMode;
    private readonly ShuffleOrder _shuffle = new();
    private readonly MarkedFileSet _marks = new();
    private readonly PlaylistEntries _entries = new();
    private int _currentIndex = -1;
    private double? _pendingStart;

    internal PlaylistItem? CurrentItem => _currentIndex >= 0 && _currentIndex < _items.Count
        ? _items[_currentIndex]
        : null;
    internal string? CurrentPath => CurrentItem?.Path;

    /// <summary>The current entry's position in the order the user is navigating.</summary>
    internal int CurrentIndex => IsShuffleEnabled ? _shuffle.Position : _currentIndex;
    internal int Count => _items.Count;
    /// <summary>The files in their active order: shuffled while shuffle is enabled, source order otherwise.</summary>
    internal IReadOnlyList<PlaylistItem> Items => IsShuffleEnabled
        ? _shuffle.Arrange(_items)
        : _items.AsReadOnly();
    internal string SourceIdentity => string.Join(";", _sources.Select(item => item.Id));
    internal IReadOnlyList<string> SourceFiles => _sources.Select(item => item.Path).DistinctBy(Paths.Key, StringComparer.OrdinalIgnoreCase).ToArray();
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

        SavedEntries = null;
        _items.Clear();
        _sources.Clear();
        _sources.AddRange(files.Select(path => new PlaylistItem(Guid.NewGuid(), path)));
        _chapters.Clear();
        _items.AddRange(_sources);
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
        SavedEntries?.Add(SavedEntry.FromPath(path));
        var addedIndex = _items.Count;
        var source = new PlaylistItem(Guid.NewGuid(), path);
        _sources.Add(source);
        _items.Add(source);
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
    internal PlaylistItem? PeekNext(bool wrap)
    {
        if (_items.Count < 2)
            return null;
        if (IsShuffleEnabled)
        {
            SyncShufflePosition();
            if (_shuffle.Position < 0)
                return _items[_shuffle[0]];
            var position = _shuffle.Position + 1;
            if (position >= _shuffle.Count)
            {
                if (!wrap)
                    return null;
                position = 0;
            }
            return _items[_shuffle[position]];
        }
        var index = _currentIndex + 1;
        if (index >= _items.Count)
        {
            if (!wrap)
                return null;
            index = 0;
        }
        return index >= 0 && index < _items.Count ? _items[index] : null;
    }

    internal bool GoToFirst() => GoToIndex(0);

    internal bool GoToLast() => GoToIndex(_items.Count - 1);

    internal bool GoToIndex(int index)
    {
        if (index < 0 || index >= _items.Count)
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
        if (CurrentPath is not string oldPath)
            return false;
        for (var index = 0; index < _sources.Count; index++)
            if (Paths.AreSame(_sources[index].Path, oldPath))
                _sources[index] = _sources[index] with { Path = path };
        for (var index = 0; index < _items.Count; index++)
            if (Paths.AreSame(_items[index].Path, oldPath))
                _items[index] = _items[index] with { Path = path };
        if (_chapters.Remove(Paths.Key(oldPath), out var chapters))
            _chapters[Paths.Key(path)] = chapters;
        _marks.Replace(oldPath, path);
        _entries.Replace(oldPath, path);
        return true;
    }

    internal string? RemoveCurrent()
    {
        var path = CurrentPath;
        if (path is not null)
            RemovePaths([path]);
        return path;
    }

    internal bool? ToggleCurrentMarked()
    {
        return _marks.Toggle(CurrentPath);
    }

    internal bool ToggleAllMarked()
    {
        return _marks.ToggleAll(SourceFiles);
    }

    internal bool ClearMarked()
    {
        return _marks.Clear();
    }
    internal int MarkedCount => _marks.Count(SourceFiles);
    internal bool IsCurrentMarked => _marks.Contains(CurrentPath);
    internal bool AreAllMarked => _marks.AreAll(SourceFiles);
    internal IReadOnlyList<string> MarkedFiles
        => _marks.Files(SourceFiles);

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
        var removedIndexes = Enumerable.Range(0, _items.Count)
            .Where(index => keys.Contains(Paths.Key(_items[index].Path)))
            .ToHashSet();
        if (removedIndexes.Count == 0) return (false, false);
        var removed = removedIndexes.Select(index => _items[index]).ToList();
        var newIndexes = new int[_items.Count];
        var kept = new List<PlaylistItem>(_items.Count - removedIndexes.Count);
        for (var index = 0; index < _items.Count; index++)
        {
            if (removedIndexes.Contains(index))
            {
                newIndexes[index] = -1;
                continue;
            }
            newIndexes[index] = kept.Count;
            kept.Add(_items[index]);
        }
        _items.Clear();
        _items.AddRange(kept);
        _marks.Remove(paths);
        _entries.Remove(removed.Select(item => item.Path));
        _sources.RemoveAll(item => keys.Contains(Paths.Key(item.Path)));
        foreach (var path in removed.Select(item => Paths.Key(item.Path)))
            _chapters.Remove(path);
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
        else if (_items.Count == 0)
            _currentIndex = -1;
        else if (oldCurrent is not null && !keys.Contains(Paths.Key(oldCurrent)))
            _currentIndex = newIndexes[oldIndex];
        else
            _currentIndex = Math.Min(
                Enumerable.Range(0, Math.Max(oldIndex, 0)).Count(index => !removedIndexes.Contains(index)),
                _items.Count - 1);
        _pendingStart = null;
        return (true, !string.Equals(oldCurrent, CurrentPath, StringComparison.Ordinal));
    }

    internal bool ClearAll()
    {
        SavedEntries = null;
        if (_items.Count == 0) return false;
        _items.Clear();
        _sources.Clear();
        _chapters.Clear();
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
        if (_items.Count == 0)
            return false;
        if (_currentIndex < 0 || _currentIndex >= _items.Count)
        {
            _currentIndex = 0;
            _pendingStart = null;
            return true;
        }
        if (_items.Count == 1)
            return false;
        if (_currentIndex + 1 >= _items.Count)
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
        if (_items.Count == 0)
            return false;
        if (_currentIndex < 0 || _currentIndex >= _items.Count)
        {
            _currentIndex = _items.Count - 1;
            _pendingStart = null;
            return true;
        }
        if (_items.Count == 1)
            return false;
        if (_currentIndex <= 0)
        {
            if (!wrap)
                return false;
            _currentIndex = _items.Count - 1;
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
        if (_items.Count == 0)
            return false;
        SyncShufflePosition();
        if (_shuffle.Position < 0)
        {
            _shuffle.Position = 0;
            _currentIndex = _shuffle[0];
            _pendingStart = null;
            return true;
        }
        if (_items.Count == 1)
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
        if (_items.Count == 0)
            return false;
        SyncShufflePosition();
        if (_shuffle.Position < 0)
        {
            _shuffle.Position = _shuffle.Count - 1;
            _currentIndex = _shuffle[_shuffle.Position];
            _pendingStart = null;
            return true;
        }
        if (_items.Count == 1)
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
        _shuffle.Sync(_items.Count, _currentIndex);
    }

    /// <summary>Creates the one random order used until shuffle is turned off or a new playlist is opened.
    /// The current item is shuffled like every other item; keeping its old numeric position is what made
    /// the first and last entries remain at the edges.</summary>
    private void CreateShuffleOrder()
    {
        _shuffle.Create(_items.Count, _currentIndex);
    }

    private void ClearShuffleOrder()
    {
        _shuffle.Clear();
    }
}
