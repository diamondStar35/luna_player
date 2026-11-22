using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.Playlist;

internal sealed partial class PlaylistState
{
    internal IReadOnlyList<Chapter> ChaptersFor(string? path)
        => _projection.For(path);

    internal bool SetChapters(string path, IReadOnlyList<Chapter> chapters, double position)
    {
        var key = Paths.Key(path);
        if (ChaptersFor(path).SequenceEqual(chapters))
            return false;
        _projection.Set(path, chapters);
        if (_audiobookMode)
            RebuildItems(position);
        return true;
    }

    internal void SetAudiobookMode(bool enabled, double position)
    {
        _audiobookMode = enabled;
        RebuildItems(position);
    }

    internal bool GoToItem(string id)
    {
        var items = Items;
        for (var index = 0; index < items.Count; index++)
            if (items[index].Id == id)
                return items[index].Id == CurrentItem?.Id || GoToIndex(index);
        return false;
    }

    internal bool GoToChapter(int index)
    {
        if (CurrentItem is not PlaylistItem current)
            return false;
        var item = Items.FirstOrDefault(item => item.SourceId == current.SourceId && item.Chapter?.Index == index);
        return item is not null && GoToItem(item.Id);
    }

    /// <summary>Preserves the existing shuffled order when metadata expands a source. New chapters are
    /// inserted into the existing order without reshuffling surviving items.</summary>
    private void RebuildItems(double position, bool randomizeNew = true)
    {
        var current = CurrentItem;
        var previousOrder = Items.ToArray();
        _items.Clear();
        _items.AddRange(_projection.Expand(_sources));
        var chapterIndex = current is null ? null : Chapters.At(ChaptersFor(current.Path), position)?.Index;
        _currentIndex = _items.FindIndex(item => item.SourceId == current?.SourceId
            && (!_audiobookMode || item.Chapter is null || item.Chapter.Index == chapterIndex));
        if (_currentIndex < 0 && _items.Count > 0)
            _currentIndex = Math.Max(0, _items.FindIndex(item => item.SourceId == current?.SourceId));
        if (!IsShuffleEnabled)
            return;
        var indexes = _items.Select((item, index) => (item, index)).ToDictionary(x => x.item.Id, x => x.index);
        var fallback = _items.Select((item, index) => (item, index))
            .Where(x => x.item.Chapter is null || x.item == CurrentItem || x.item.Chapter.Index == 0)
            .GroupBy(x => x.item.SourceId).ToDictionary(x => x.Key, x => x.First().index);
        var existing = new List<int>(); var seen = new HashSet<int>();
        foreach (var old in previousOrder)
        {
            if (!indexes.TryGetValue(old.Id, out var index) && !fallback.TryGetValue(old.SourceId, out index)) continue;
            if (seen.Add(index)) existing.Add(index);
        }
        var added = Enumerable.Range(0, _items.Count).Where(index => !seen.Contains(index)).ToArray();
        var order = new List<int>(_items.Count);
        if (randomizeNew)
        {
            Random.Shared.Shuffle(added);
            // Merge a shuffled set of new items without repeatedly shifting a large playlist.
            var oldIndex = 0; var newIndex = 0;
            while (oldIndex < existing.Count || newIndex < added.Length)
                if (Random.Shared.Next(existing.Count - oldIndex + added.Length - newIndex) < existing.Count - oldIndex)
                    order.Add(existing[oldIndex++]);
                else order.Add(added[newIndex++]);
        }
        else { order.AddRange(existing); order.AddRange(added); }
        _shuffle.Replace(order);
        _shuffle.Position = _shuffle.IndexOf(_currentIndex);
    }
}
