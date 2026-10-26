namespace LunaPlayer.Playlist;

/// <summary>Source metadata shared by playlist items. Chapter titles live on individual items; the
/// source title and stream details are forgotten when their real path leaves the playlist.</summary>
internal sealed class PlaylistEntries
{
    private readonly Dictionary<string, EntryInfo> _items = new(StringComparer.OrdinalIgnoreCase);

    internal void SetTitle(string path, string? title)
        => Update(path, info => info with
        {
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
        });

    internal void SetSource(string path, string? source, string? audioFile, bool audioOnly)
        => Update(path, info => info with
        {
            Source = string.IsNullOrWhiteSpace(source) ? null : source,
            AudioFile = string.IsNullOrWhiteSpace(audioFile) ? null : audioFile,
            AudioOnly = audioOnly,
        });

    internal string? Title(string? path) => Get(path).Title;
    internal string? Source(string? path) => Get(path).Source;
    internal string? AudioFile(string? path) => Get(path).AudioFile;
    internal bool AudioOnly(string? path) => Get(path).AudioOnly;

    internal void Replace(string oldPath, string newPath)
    {
        if (_items.Remove(oldPath, out var info))
            _items[newPath] = info;
    }

    internal void Remove(string path) => _items.Remove(path);

    internal void Remove(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            _items.Remove(path);
    }

    internal void Clear() => _items.Clear();

    private EntryInfo Get(string? path)
        => path is not null && _items.TryGetValue(path, out var info) ? info : default;

    private void Update(string path, Func<EntryInfo, EntryInfo> change)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        var updated = change(Get(path));
        if (updated.IsEmpty)
            _items.Remove(path);
        else
            _items[path] = updated;
    }

    private readonly record struct EntryInfo(
        string? Title,
        string? Source,
        string? AudioFile,
        bool AudioOnly = false)
    {
        internal bool IsEmpty => Title is null && Source is null && AudioFile is null && !AudioOnly;
    }
}
