using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.Playlist;

internal sealed partial class PlaylistState
{
    internal IReadOnlyList<Chapter> ChaptersFor(string? path)
        => path is not null && _chapters.TryGetValue(Paths.Key(path), out var chapters) ? chapters : [];

    internal bool SetChapters(string path, IReadOnlyList<Chapter> chapters, double position)
    {
        var key = Paths.Key(path);
        if (ChaptersFor(path).SequenceEqual(chapters))
            return false;
        _chapters[key] = chapters;
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
    private void RebuildItems(double position)
    {
        var current = CurrentItem;
        var previousOrder = Items.ToArray();
        _items.Clear();
        foreach (var source in _sources)
        {
            var chapters = ChaptersFor(source.Path);
            if (_audiobookMode && chapters.Count > 0)
                _items.AddRange(chapters.Select(chapter => source with { Chapter = chapter }));
            else
                _items.Add(source);
        }
        var chapterIndex = current is null ? null : Chapters.At(ChaptersFor(current.Path), position)?.Index;
        _currentIndex = _items.FindIndex(item => item.SourceId == current?.SourceId
            && (!_audiobookMode || item.Chapter is null || item.Chapter.Index == chapterIndex));
        if (_currentIndex < 0 && _items.Count > 0)
            _currentIndex = Math.Max(0, _items.FindIndex(item => item.SourceId == current?.SourceId));
        if (!IsShuffleEnabled)
            return;
        var order = new List<int>();
        foreach (var old in previousOrder)
        {
            var index = _items.FindIndex(item => item.Id == old.Id);
            if (index < 0)
                index = _items.FindIndex(item => item.SourceId == old.SourceId
                    && (item.Chapter is null || item == CurrentItem || item.Chapter.Index == 0));
            if (index >= 0 && !order.Contains(index)) order.Add(index);
        }
        for (var index = 0; index < _items.Count; index++)
            if (!order.Contains(index)) order.Insert(Random.Shared.Next(order.Count + 1), index);
        _shuffle.Replace(order);
        _shuffle.Position = _shuffle.IndexOf(_currentIndex);
    }
}
