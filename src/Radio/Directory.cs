using System.Text.Json.Serialization;

namespace LunaPlayer.Radio;

/// <summary>A tag, codec, or deprecated country-code entry.</summary>
internal sealed record DirectoryEntry
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("stationcount")]
    public uint StationCount { get; init; }
}

/// <summary>A country and its ISO 3166-1 alpha-2 code.</summary>
internal sealed record Country
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("iso_3166_1")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("stationcount")]
    public uint StationCount { get; init; }
}

/// <summary>A language and its optional ISO 639 code.</summary>
internal sealed record Language
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("iso_639")]
    public string? Code { get; init; }

    [JsonPropertyName("stationcount")]
    public uint StationCount { get; init; }
}

/// <summary>A state or province, including its country.</summary>
internal sealed record State
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; init; } = string.Empty;

    [JsonPropertyName("stationcount")]
    public uint StationCount { get; init; }
}

/// <summary>An API mirror returned by the servers endpoint.</summary>
internal sealed record Mirror
{
    [JsonPropertyName("ip")]
    public string Ip { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

/// <summary>A broadcaster's streaming server, distinct from an API mirror.</summary>
internal sealed record StreamingServer
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("statusurl")]
    public string? StatusUrl { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
