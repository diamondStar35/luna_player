using LunaPlayer.Configuration;
using LunaPlayer.Media;
namespace LunaPlayer.Playlist;
internal sealed partial class PlaylistState
{

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

        _sources.ReplaceSaved(null);
        _items.Clear();
        _sources.Clear();
        _sources.AddRange(files.Select(path => new PlaylistItem(Guid.NewGuid(), path)));
        _projection.Clear();
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

    internal bool ReplaceCurrent(string path)
    {
        if (CurrentPath is not string oldPath)
            return false;
        _sources.Rename(oldPath, path);
        for (var index = 0; index < _items.Count; index++)
            if (Paths.AreSame(_items[index].Path, oldPath))
                _items[index] = _items[index] with { Path = path };
        _projection.Rename(oldPath, path);
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
            _projection.Remove(path);
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
        _sources.ReplaceSaved(null);
        if (_items.Count == 0) return false;
        _items.Clear();
        _sources.Clear();
        _projection.Clear();
        _marks.Clear();
        _entries.Clear();
        _currentIndex = -1;
        _pendingStart = null;
        ClearShuffleOrder();
        return true;
    }

}
