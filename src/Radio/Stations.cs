using System.Text.Json.Serialization;

namespace LunaPlayer.Radio;

/// <summary>A station's editable metadata, also returned by the change-history endpoint.</summary>
internal record StationChange
{
    [JsonPropertyName("changeuuid")]
    public Guid ChangeUuid { get; init; }

    [JsonPropertyName("stationuuid")]
    public Guid StationUuid { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("homepage")]
    public string Homepage { get; init; } = string.Empty;

    [JsonPropertyName("favicon")]
    public string Favicon { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public string Tags { get; init; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; init; } = string.Empty;

    [JsonPropertyName("countrycode")]
    public string CountryCode { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; init; } = string.Empty;

    [JsonPropertyName("languagecodes")]
    public string? LanguageCodes { get; init; }

    [JsonPropertyName("votes")]
    public int Votes { get; init; }

    [JsonPropertyName("lastchangetime")]
    public string LastChangeTime { get; init; } = string.Empty;

    [JsonPropertyName("lastchangetime_iso8601")]
    public DateTimeOffset? LastChangedAt { get; init; }

    [JsonPropertyName("geo_lat")]
    public double? Latitude { get; init; }

    [JsonPropertyName("geo_long")]
    public double? Longitude { get; init; }
}

/// <summary>A directory entry. Attached metadata is preserved; URL strings are not assumed to be valid URIs.</summary>
internal sealed record Station : StationChange
{
    [JsonPropertyName("serveruuid")]
    public Guid? ServerUuid { get; init; }

    [JsonPropertyName("url_resolved")]
    public string ResolvedUrl { get; init; } = string.Empty;

    [JsonPropertyName("iso_3166_2")]
    public string? CountrySubdivisionCode { get; init; }

    [JsonPropertyName("codec")]
    public string Codec { get; init; } = string.Empty;

    [JsonPropertyName("bitrate")]
    public uint Bitrate { get; init; }

    [JsonPropertyName("hls")]
    public bool Hls { get; init; }

    [JsonPropertyName("lastcheckok")]
    public bool LastCheckOk { get; init; }

    [JsonPropertyName("lastchecktime")]
    public string LastCheckTime { get; init; } = string.Empty;

    [JsonPropertyName("lastchecktime_iso8601")]
    public DateTimeOffset? LastCheckedAt { get; init; }

    [JsonPropertyName("lastcheckoktime")]
    public string LastCheckOkTime { get; init; } = string.Empty;

    [JsonPropertyName("lastcheckoktime_iso8601")]
    public DateTimeOffset? LastSuccessfulCheckAt { get; init; }

    [JsonPropertyName("lastlocalchecktime")]
    public string LastLocalCheckTime { get; init; } = string.Empty;

    [JsonPropertyName("lastlocalchecktime_iso8601")]
    public DateTimeOffset? LastLocalCheckAt { get; init; }

    [JsonPropertyName("clicktimestamp")]
    public string ClickTimestamp { get; init; } = string.Empty;

    [JsonPropertyName("clicktimestamp_iso8601")]
    public DateTimeOffset? LastClickedAt { get; init; }

    [JsonPropertyName("clickcount")]
    public uint ClickCount { get; init; }

    [JsonPropertyName("clicktrend")]
    public int ClickTrend { get; init; }

    [JsonPropertyName("ssl_error")]
    public bool? SslError { get; init; }

    [JsonPropertyName("has_extended_info")]
    public bool? HasExtendedInfo { get; init; }
}

/// <summary>The result of an explicit directory mutation.</summary>
internal record ActionResult
{
    [JsonPropertyName("ok")]
    [JsonRequired]
    public bool Ok { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

/// <summary>The identifier assigned when a station is added.</summary>
internal sealed record CreationResult : ActionResult
{
    [JsonPropertyName("uuid")]
    public Guid? Uuid { get; init; }
}

/// <summary>The resolved stream returned when a playback click is registered.</summary>
internal sealed record ClickResult : ActionResult
{
    [JsonPropertyName("stationuuid")]
    public Guid? StationUuid { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
