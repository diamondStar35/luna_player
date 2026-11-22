using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record Lyrics
{
    public string Artist { get; set; } = "";
    public string Title { get; set; } = "";
    public string Value { get; set; } = "";
}

internal sealed record StructuredLyrics
{
    public string DisplayArtist { get; set; } = "";
    public string DisplayTitle { get; set; } = "";
    public string Lang { get; set; } = "";
    public bool Synced { get; set; }
    public double? Offset { get; set; }
    [JsonPropertyName("line")]
    [JsonConverter(typeof(Transport.OneOrMany<LyricLine>))]
    public LyricLine[] Lines { get; set; } = [];
}

internal sealed record LyricLine
{
    public long? Start { get; set; }
    public string Value { get; set; } = "";
}
