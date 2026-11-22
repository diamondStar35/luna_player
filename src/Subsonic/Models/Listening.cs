using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record Lyrics
{
    public string Artist { get; init; } = "";
    public string Title { get; init; } = "";
    public string Value { get; init; } = "";
}

internal sealed record StructuredLyrics
{
    public string DisplayArtist { get; init; } = "";
    public string DisplayTitle { get; init; } = "";
    public string Lang { get; init; } = "";
    public bool Synced { get; init; }
    public double? Offset { get; init; }
    [JsonPropertyName("line")]
    [JsonConverter(typeof(Transport.OneOrMany<LyricLine>))]
    public LyricLine[] Lines { get; init; } = [];
}

internal sealed record LyricLine
{
    public long? Start { get; init; }
    public string Value { get; init; } = "";
}
