using System.Collections;
using LunaPlayer.Configuration;
namespace LunaPlayer.Playlist;

internal sealed class SourceCollection : IReadOnlyList<PlaylistItem>
{
    private readonly List<PlaylistItem> _items = [];
    private List<SavedEntry>? _saved;
    public int Count => _items.Count;
    public PlaylistItem this[int index] => _items[index];
    internal IReadOnlyList<SavedEntry>? SavedSnapshot => _saved?.ToArray();
    internal IReadOnlyList<PlaylistItem> Snapshot() => _items.ToArray();
    internal void ReplaceSaved(IEnumerable<SavedEntry>? entries) => _saved = entries?.ToList();
    internal void Add(PlaylistItem item)
    {
        _items.Add(item);
        _saved?.Add(SavedEntry.FromPath(item.Path));
    }
    internal void AddRange(IEnumerable<PlaylistItem> items) { foreach (var item in items) Add(item); }
    internal void Clear() => _items.Clear();
    internal void RemoveAll(Predicate<PlaylistItem> match) => _items.RemoveAll(match);
    internal void Rename(string oldPath, string newPath)
    {
        for (var i = 0; i < _items.Count; i++)
            if (Paths.AreSame(_items[i].Path, oldPath)) _items[i] = _items[i] with { Path = newPath };
    }
    internal void ForgetSaved(string path)
        => _saved?.RemoveAll(entry => entry.Kind != EntryKind.Folder && Paths.AreSame(entry.Location, path));
    internal void RenameSaved(string oldPath, string newPath)
    {
        if (_saved is null) return;
        for (var i = 0; i < _saved.Count; i++)
            if (Paths.AreSame(_saved[i].Location, oldPath)) _saved[i] = _saved[i] with { Location = newPath };
    }
    public IEnumerator<PlaylistItem> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
