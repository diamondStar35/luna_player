using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.Playlist;

internal enum EntryKind { File, Folder, Stream }
internal sealed record SavedEntry(Guid Id, EntryKind Kind, string Location, string? Title = null)
{
    internal static SavedEntry FromPath(string path) => new(Guid.NewGuid(),
        Directory.Exists(path) ? EntryKind.Folder : LinkValidator.IsHttpUrl(path) ? EntryKind.Stream : EntryKind.File, path);
    internal string Name => Title ?? (Kind == EntryKind.Folder ? new DirectoryInfo(Location).Name : MediaLibrary.DisplayName(Location));
}

internal sealed class Document
{
    public int Version { get; set; } = 1;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SavedEntry> Entries { get; set; } = [];
    internal Document Copy() => new() { Id = Id, Name = Name, Description = Description, Entries = [.. Entries] };
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Document))]
internal partial class DocumentJsonContext : JsonSerializerContext;

internal sealed class DocumentStore
{
    internal string DirectoryPath { get; } = Path.Combine(Paths.RootDirectory, "Playlists");
    private string FilePath(Guid id) => Path.Combine(DirectoryPath, $"{id:N}.json");
    internal static Document Read(string path)
    {
        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize(stream, DocumentJsonContext.Default.Document);
        if (document is null || document.Version != 1 || document.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(document.Name) || document.Description is null || document.Entries is null
            || document.Entries.Select(entry => entry?.Id).Distinct().Count() != document.Entries.Count
            || document.Entries.Any(entry => entry is null || entry.Id == Guid.Empty
                || !Enum.IsDefined(entry.Kind) || string.IsNullOrWhiteSpace(entry.Location)))
            throw new InvalidDataException("Invalid playlist document.");
        return document;
    }
    internal IReadOnlyList<Document> List()
        => System.IO.Directory.Exists(DirectoryPath)
            ? System.IO.Directory.EnumerateFiles(DirectoryPath, "*.json").Select(Read)
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToArray() : [];
    internal void Save(Document document)
    {
        document.Name = document.Name.Trim();
        if (document.Name.Length == 0) throw new InvalidDataException("A playlist name is required.");
        if (List().Any(item => item.Id != document.Id && string.Equals(item.Name, document.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("A playlist with that name already exists.");
        Write(document, FilePath(document.Id));
    }
    internal static void Write(Document document, string path)
    {
        if (!AtomicFile.WriteJson(path, document, DocumentJsonContext.Default.Document, out var error))
            throw new IOException(error);
    }
    internal void Remove(Guid id) => File.Delete(FilePath(id));
}

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
    internal static void Export(Document document, string path, Resolution? resolved = null)
    {
        var text = new StringBuilder("#EXTM3U\n");
        foreach (var entry in (resolved ?? Resolve(document.Entries)).Files)
        {
            var title = (entry.Title ?? MediaLibrary.DisplayName(entry.Location)).Replace('\r', ' ').Replace('\n', ' ');
            if (entry.Location.Contains('\n') || entry.Location.Contains('\r'))
                throw new InvalidDataException("A playlist location contains a line break.");
            text.Append("#EXTINF:-1,").Append(title).Append('\n').Append(entry.Location).Append('\n');
        }
        if (!AtomicFile.WriteBytes(path, Encoding.UTF8.GetBytes(text.ToString()), out var error)) throw new IOException(error);
    }
}
