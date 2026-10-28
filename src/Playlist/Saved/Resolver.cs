using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.Playlist;

internal sealed record Resolution(IReadOnlyList<M3uEntry> Files, IReadOnlyList<string> Unavailable);
internal static class DocumentResolver
{
    internal static Resolution Resolve(IEnumerable<SavedEntry> entries)
    {
        var files = new List<M3uEntry>();
        var unavailable = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.Kind == EntryKind.Folder)
            {
                var children = MediaLibrary.CollectFiles(entry.Location, recursive: false);
                files.AddRange(children.Select(path => new M3uEntry(path, null)));
                if (children.Count == 0) unavailable.Add(entry.Location);
            }
            else if (entry.Kind == EntryKind.Stream && LinkValidator.IsHttpUrl(entry.Location)
                || entry.Kind == EntryKind.File && File.Exists(entry.Location))
                files.Add(new(entry.Location, entry.Title));
            else unavailable.Add(entry.Location);
        }
        return new(files, unavailable);
    }

}
