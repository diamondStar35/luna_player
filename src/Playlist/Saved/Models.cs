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
