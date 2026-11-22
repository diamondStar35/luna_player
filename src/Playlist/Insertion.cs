namespace LunaPlayer.Playlist;

internal sealed partial class PlaylistState
{
    internal IReadOnlyList<PlaylistItem> Insert(IEnumerable<string> paths, bool afterCurrent, double position = 0)
    {
        var added = paths.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => new PlaylistItem(Guid.NewGuid(), x)).ToArray();
        if (added.Length == 0) return added;
        var previous = IsShuffleEnabled ? Items.ToArray() : [];
        var source = CurrentItem?.SourceId;
        _sources.Insert(added, afterCurrent ? source : null);
        RebuildItems(position, randomizeNew: false);
        if (IsShuffleEnabled)
        {
            var indexes = _items.Select((item, index) => (item.Id, index)).ToDictionary(x => x.Id, x => x.index);
            var order = previous.Where(x => indexes.ContainsKey(x.Id)).Select(x => indexes[x.Id]).ToList();
            var addedSources = added.Select(x => x.SourceId).ToHashSet();
            var inserted = _items.Select((item, index) => (item, index)).Where(x => addedSources.Contains(x.item.SourceId)).Select(x => x.index);
            order.InsertRange(afterCurrent ? order.IndexOf(_currentIndex) + 1 : order.Count, inserted);
            _shuffle.Replace(order); _shuffle.Position = _shuffle.IndexOf(_currentIndex);
        }
        return added;
    }

    internal void ReconcileServer(Guid server, IReadOnlyList<string> addresses, double position)
    {
        var old = _sources.Where(x => x.ServerMedia?.ServerId == server && x.ServerMedia.Kind == Subsonic.Playback.MediaKind.Song)
            .GroupBy(x => x.Path, StringComparer.Ordinal).ToDictionary(x => x.Key, x => new Queue<PlaylistItem>(x), StringComparer.Ordinal);
        var incoming = new Queue<PlaylistItem>(addresses.Select(path => old.TryGetValue(path, out var matches) && matches.Count > 0
            ? matches.Dequeue() : new PlaylistItem(Guid.NewGuid(), path)));
        var merged = new List<PlaylistItem>();
        var lastSlot = -1;
        foreach (var source in _sources)
        {
            if (source.ServerMedia is { Kind: Subsonic.Playback.MediaKind.Song } reference && reference.ServerId == server)
            { if (incoming.Count > 0) merged.Add(incoming.Dequeue()); lastSlot = merged.Count; }
            else merged.Add(source);
        }
        merged.InsertRange(lastSlot < 0 ? merged.Count : lastSlot, incoming);
        _sources.Replace(merged);
        if (_sources.SavedSnapshot is { } saved)
        {
            var replacements = new Queue<SavedEntry>(merged.Where(x => x.ServerMedia is { Kind: Subsonic.Playback.MediaKind.Song } reference && reference.ServerId == server)
                .Select(x => SavedEntry.FromPath(x.Path) with { Id = x.SourceId, Title = GetTitle(x.Path) }));
            var logical = new List<SavedEntry>(); var last = -1;
            foreach (var entry in saved)
                if (entry.ServerMedia is { Kind: Subsonic.Playback.MediaKind.Song } reference && reference.ServerId == server)
                { if (replacements.Count > 0) logical.Add(replacements.Dequeue()); last = logical.Count; }
                else logical.Add(entry);
            logical.InsertRange(last < 0 ? logical.Count : last, replacements);
            _sources.ReplaceSaved(logical);
        }
        RebuildItems(position);
    }
}
