using System.Text.Json;
using System.Text.Json.Serialization;

namespace LunaPlayer.Configuration;

internal sealed class PositionStore
{
    private readonly string _path;

    internal PositionStore(string path) => _path = path;

    internal double? Get(string mediaPath)
    {
        var key = Paths.Key(mediaPath);
        if (key.Length == 0) return null;
        var document = Load();
        return document.Files.TryGetValue(key, out var entry) ? Math.Max(0, entry.Position) : null;
    }

    internal bool Set(string mediaPath, double position)
    {
        var key = Paths.Key(mediaPath);
        if (key.Length == 0) return false;
        var document = Load();
        document.Files[key] = new PositionEntry
        {
            Path = Paths.Absolute(mediaPath),
            Position = Math.Max(0, position),
            Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        return Save(document);
    }

    private PositionDocument Load()
    {
        if (!File.Exists(_path)) return new();
        try
        {
            using var stream = File.OpenRead(_path);
            return JsonSerializer.Deserialize(stream, PositionJsonContext.Default.PositionDocument) ?? new();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    private bool Save(PositionDocument document)
    {
        try
        {
            Paths.EnsureDirectoryFor(_path);
            var temporary = Paths.TemporaryFor(_path);
            using (var stream = File.Create(temporary))
                JsonSerializer.Serialize(stream, document, PositionJsonContext.Default.PositionDocument);
            File.Move(temporary, _path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}

internal sealed class PositionDocument
{
    public int Version { get; set; } = 1;
    public Dictionary<string, PositionEntry> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class PositionEntry
{
    public string Path { get; set; } = string.Empty;
    public double Position { get; set; }
    public long Updated { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PositionDocument))]
internal partial class PositionJsonContext : JsonSerializerContext;
