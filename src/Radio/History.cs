using System.Text.Json.Serialization;

namespace LunaPlayer.Radio;

/// <summary>A stream check, including optional metadata supplied by the broadcaster.</summary>
internal sealed record StationCheck
{
    [JsonPropertyName("stationuuid")]
    public Guid StationUuid { get; init; }

    [JsonPropertyName("checkuuid")]
    public Guid CheckUuid { get; init; }

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("codec")]
    public string Codec { get; init; } = string.Empty;

    [JsonPropertyName("bitrate")]
    public uint Bitrate { get; init; }

    [JsonPropertyName("hls")]
    public bool Hls { get; init; }

    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("timestamp_iso8601")]
    public DateTimeOffset? CheckedAt { get; init; }

    [JsonPropertyName("urlcache")]
    public string CachedUrl { get; init; } = string.Empty;

    [JsonPropertyName("metainfo_overrides_database")]
    public bool? MetadataOverridesDatabase { get; init; }

    [JsonPropertyName("public")]
    public bool? Public { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("tags")]
    public string? Tags { get; init; }

    [JsonPropertyName("countrycode")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("homepage")]
    public string? Homepage { get; init; }

    [JsonPropertyName("favicon")]
    public string? Favicon { get; init; }

    [JsonPropertyName("loadbalancer")]
    public string? LoadBalancer { get; init; }

    [JsonPropertyName("do_not_index")]
    public bool? DoNotIndex { get; init; }

    [JsonPropertyName("countrysubdivisioncode")]
    public string? CountrySubdivisionCode { get; init; }

    [JsonPropertyName("server_software")]
    public string? ServerSoftware { get; init; }

    [JsonPropertyName("sampling")]
    public uint? SampleRate { get; init; }

    [JsonPropertyName("timing_ms")]
    public UInt128? TimingMilliseconds { get; init; }

    [JsonPropertyName("languagecodes")]
    public string? LanguageCodes { get; init; }

    [JsonPropertyName("ssl_error")]
    public bool? SslError { get; init; }

    [JsonPropertyName("geo_lat")]
    public double? Latitude { get; init; }

    [JsonPropertyName("geo_long")]
    public double? Longitude { get; init; }
}

/// <summary>One node in the redirect/playlist tree followed by a stream check.</summary>
internal sealed record CheckStep
{
    [JsonPropertyName("stepuuid")]
    public Guid StepUuid { get; init; }

    [JsonPropertyName("parent_stepuuid")]
    public Guid? ParentStepUuid { get; init; }

    [JsonPropertyName("checkuuid")]
    public Guid CheckUuid { get; init; }

    [JsonPropertyName("stationuuid")]
    public Guid StationUuid { get; init; }

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("urltype")]
    public string? UrlType { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("creation_iso8601")]
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>An entry in the directory's playback-click history.</summary>
internal sealed record StationClick
{
    [JsonPropertyName("stationuuid")]
    public Guid StationUuid { get; init; }

    [JsonPropertyName("clickuuid")]
    public Guid ClickUuid { get; init; }

    [JsonPropertyName("clicktimestamp")]
    public string ClickTimestamp { get; init; } = string.Empty;

    [JsonPropertyName("clicktimestamp_iso8601")]
    public DateTimeOffset? ClickedAt { get; init; }
}
