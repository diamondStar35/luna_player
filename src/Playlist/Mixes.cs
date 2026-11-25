namespace LunaPlayer.Playlist;

internal sealed partial class PlaylistState
{
    internal PlaylistItem[] QueueMix(IReadOnlyList<string> paths, double position)
    {
        if (paths.Count == 0) return [];
        var sources = _sources.ToArray(); var active = Items.ToArray();
        var current = CurrentItem;
        var batch = paths.Distinct(StringComparer.Ordinal).Select(path =>
            (current?.Path == path ? sources.FirstOrDefault(item => item.SourceId == current.SourceId) : null)
            ?? sources.FirstOrDefault(item => item.Path == path) ?? new PlaylistItem(Guid.NewGuid(), path)).ToArray();
        var moved = batch.Select(item => item.SourceId).ToHashSet();
        var seed = batch[0].SourceId;
        var remaining = sources.Where(item => !moved.Contains(item.SourceId)).ToList();
        var insertion = sources.TakeWhile(item => item.SourceId != seed).Count(item => !moved.Contains(item.SourceId));
        remaining.InsertRange(insertion, batch); _sources.Replace(remaining);
        if (_sources.SavedSnapshot is { } saved)
        {
            var logical = saved.Where(entry => !moved.Contains(entry.Id)).ToList();
            var at = saved.TakeWhile(entry => entry.Id != seed).Count(entry => !moved.Contains(entry.Id));
            logical.InsertRange(at, batch.Select(item => saved.FirstOrDefault(entry => entry.Id == item.SourceId)
                ?? SavedEntry.FromPath(item.Path) with { Id = item.SourceId, Title = GetTitle(item.Path) }));
            _sources.ReplaceSaved(logical);
        }
        RebuildItems(position, randomizeNew: false);
        if (IsShuffleEnabled)
        {
            // A mix follows its seed even in a shuffled queue. Keep every unrelated item's
            // existing order and identity, including the source already playing.
            var indexes = _items.Select((item, index) => (item.Id, Index: index)).ToDictionary(item => item.Id, item => item.Index);
            var order = active.Where(item => !moved.Contains(item.SourceId) && indexes.ContainsKey(item.Id)).Select(item => indexes[item.Id]).ToList();
            var at = active.TakeWhile(item => item.SourceId != seed).Count(item => !moved.Contains(item.SourceId));
            order.InsertRange(at, _items.Select((item, index) => (item, index)).Where(entry => moved.Contains(entry.item.SourceId)).Select(entry => entry.index));
            _shuffle.Replace(order); _shuffle.Position = _shuffle.IndexOf(_currentIndex);
        }
        return batch;
    }
}
