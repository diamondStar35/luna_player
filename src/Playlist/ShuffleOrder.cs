namespace LunaPlayer.Playlist;

/// <summary>Maintains one stable random permutation of playlist indexes and the current position within
/// that permutation. Adding or removing entries repairs the existing order without reshuffling survivors.
/// </summary>
internal sealed class ShuffleOrder
{
    private readonly List<int> _indexes = [];

    internal int Position { get; set; } = -1;
    internal int Count => _indexes.Count;
    internal int this[int position] => _indexes[position];

    internal IReadOnlyList<string> Arrange(IReadOnlyList<string> files)
        => _indexes.Select(index => files[index]).ToArray();

    internal int IndexOf(int fileIndex) => _indexes.IndexOf(fileIndex);
    internal int[] Snapshot() => _indexes.ToArray();

    internal void Insert(int fileIndex)
        => _indexes.Insert(Random.Shared.Next(_indexes.Count + 1), fileIndex);

    internal void Replace(IEnumerable<int> indexes)
    {
        _indexes.Clear();
        _indexes.AddRange(indexes);
    }

    internal void Create(int fileCount, int currentIndex)
    {
        Clear();
        if (currentIndex < 0 || currentIndex >= fileCount)
            return;
        for (var index = 0; index < fileCount; index++)
            _indexes.Add(index);
        for (var index = _indexes.Count - 1; index > 0; index--)
        {
            var target = Random.Shared.Next(index + 1);
            (_indexes[index], _indexes[target]) = (_indexes[target], _indexes[index]);
        }
        Position = _indexes.IndexOf(currentIndex);
    }

    internal void Sync(int fileCount, int currentIndex)
    {
        Repair(fileCount);
        Position = _indexes.IndexOf(currentIndex);
    }

    internal void RemoveFile(int removedIndex)
    {
        _indexes.Remove(removedIndex);
        for (var index = 0; index < _indexes.Count; index++)
        {
            if (_indexes[index] > removedIndex)
                _indexes[index]--;
        }
    }

    internal void Clear()
    {
        _indexes.Clear();
        Position = -1;
    }

    private void Repair(int fileCount)
    {
        var seen = new HashSet<int>();
        _indexes.RemoveAll(index => index < 0 || index >= fileCount || !seen.Add(index));
        for (var index = 0; index < fileCount; index++)
        {
            if (!seen.Contains(index))
                _indexes.Insert(Random.Shared.Next(_indexes.Count + 1), index);
        }
    }
}
