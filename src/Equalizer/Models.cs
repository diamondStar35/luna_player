using System.Text.Json.Serialization;

namespace LunaPlayer.Equalizer;

/// <summary>One band of a preset the user made, as it is written to disk.</summary>
/// <remarks>
/// Separate from <see cref="Band"/> on purpose. That one is a value the player works in and is
/// free to change shape; this one is a file format, and a file written by one release has to be readable
/// by the next.
/// </remarks>
internal sealed class StoredBand
{
    [JsonPropertyName("frequency")] public double Frequency { get; set; }
    [JsonPropertyName("q")] public double Q { get; set; }
    [JsonPropertyName("gain")] public double Gain { get; set; }
    [JsonPropertyName("type")] public BandType Type { get; set; }
}

/// <summary>One preset the user made.</summary>
internal sealed class StoredPreset
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("bands")] public List<StoredBand> Bands { get; set; } = [];

    /// <summary>When it was made, in seconds since the Unix epoch, so the list keeps the order they were
    /// added in rather than an order that depends on how the file happened to be written.</summary>
    [JsonPropertyName("created")] public double Created { get; set; }
}

internal sealed class PresetDocument
{
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("presets")] public List<StoredPreset> Presets { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PresetDocument))]
internal partial class PresetJsonContext : JsonSerializerContext;
