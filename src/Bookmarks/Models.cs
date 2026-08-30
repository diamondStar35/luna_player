using System.Text.Json.Serialization;

namespace LunaPlayer.Bookmarks;

internal sealed class Bookmark
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("pos")] public double Position { get; set; }
    [JsonPropertyName("created")] public long Created { get; set; }
}

internal sealed class BookmarkDocument
{
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("files")] public Dictionary<string, List<Bookmark>> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(BookmarkDocument))]
internal partial class BookmarkJsonContext : JsonSerializerContext;
