using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.Playlist;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Document))]
internal partial class DocumentJsonContext : JsonSerializerContext;

internal sealed class DocumentStore
{
    internal string DirectoryPath { get; }
    internal DocumentStore(string? directory = null)
        => DirectoryPath = directory ?? Path.Combine(Paths.RootDirectory, "Playlists");
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
