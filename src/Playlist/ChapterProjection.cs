using LunaPlayer.Configuration;
using LunaPlayer.Media;
namespace LunaPlayer.Playlist;

internal sealed class ChapterProjection
{
    private readonly Dictionary<string, IReadOnlyList<Chapter>> _chapters = new(StringComparer.OrdinalIgnoreCase);
    internal bool Enabled { get; set; }
    internal IReadOnlyList<Chapter> For(string? path)
        => path is not null && _chapters.TryGetValue(Paths.Key(path), out var chapters) ? chapters : [];
    internal void Set(string path, IReadOnlyList<Chapter> chapters) => _chapters[Paths.Key(path)] = chapters;
    internal void Clear() => _chapters.Clear();
    internal void Remove(string path) => _chapters.Remove(Paths.Key(path));
    internal void Rename(string oldPath, string newPath)
    {
        if (_chapters.Remove(Paths.Key(oldPath), out var chapters)) _chapters[Paths.Key(newPath)] = chapters;
    }
    internal IEnumerable<PlaylistItem> Expand(IEnumerable<PlaylistItem> sources)
    {
        foreach (var source in sources)
        {
            var chapters = For(source.Path);
            if (Enabled && chapters.Count > 0)
            {
                foreach (var chapter in chapters) yield return source with { Chapter = chapter };
            }
            else yield return source;
        }
    }
}
