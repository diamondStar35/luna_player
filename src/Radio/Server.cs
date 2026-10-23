using System.Text.Json.Serialization;

namespace LunaPlayer.Radio;

/// <summary>The configuration fields exposed by an API mirror.</summary>
internal sealed record ServerConfiguration
{
    [JsonPropertyName("check_enabled")]
    public bool CheckEnabled { get; init; }

    [JsonPropertyName("prometheus_exporter_enabled")]
    public bool PrometheusExporterEnabled { get; init; }

    [JsonPropertyName("pull_servers")]
    public string[] PullServers { get; init; } = [];

    [JsonPropertyName("tcp_timeout_seconds")]
    public ulong TcpTimeoutSeconds { get; init; }

    [JsonPropertyName("broken_stations_never_working_timeout_seconds")]
    public ulong BrokenStationsNeverWorkingTimeoutSeconds { get; init; }

    [JsonPropertyName("broken_stations_timeout_seconds")]
    public ulong BrokenStationsTimeoutSeconds { get; init; }

    [JsonPropertyName("checks_timeout_seconds")]
    public ulong ChecksTimeoutSeconds { get; init; }

    [JsonPropertyName("click_valid_timeout_seconds")]
    public ulong ClickValidTimeoutSeconds { get; init; }

    [JsonPropertyName("clicks_timeout_seconds")]
    public ulong ClicksTimeoutSeconds { get; init; }

    [JsonPropertyName("mirror_pull_interval_seconds")]
    public ulong MirrorPullIntervalSeconds { get; init; }

    [JsonPropertyName("update_caches_interval_seconds")]
    public ulong UpdateCachesIntervalSeconds { get; init; }

    [JsonPropertyName("server_name")]
    public string ServerName { get; init; } = string.Empty;

    [JsonPropertyName("server_location")]
    public string ServerLocation { get; init; } = string.Empty;

    [JsonPropertyName("server_country_code")]
    public string ServerCountryCode { get; init; } = string.Empty;

    [JsonPropertyName("check_retries")]
    public byte CheckRetries { get; init; }

    [JsonPropertyName("check_batchsize")]
    public uint CheckBatchSize { get; init; }

    [JsonPropertyName("check_pause_seconds")]
    public ulong CheckPauseSeconds { get; init; }

    [JsonPropertyName("api_threads")]
    public ulong ApiThreads { get; init; }

    [JsonPropertyName("cache_type")]
    public string CacheType { get; init; } = string.Empty;

    [JsonPropertyName("cache_ttl")]
    public ulong CacheTtl { get; init; }

    [JsonPropertyName("language_replace_filepath")]
    public string LanguageReplaceFilePath { get; init; } = string.Empty;

    [JsonPropertyName("language_to_code_filepath")]
    public string LanguageToCodeFilePath { get; init; } = string.Empty;
}

/// <summary>Statistics for the responding API mirror.</summary>
internal sealed record ServerStatistics
{
    [JsonPropertyName("supported_version")]
    public uint SupportedVersion { get; init; }

    [JsonPropertyName("software_version")]
    public string? SoftwareVersion { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("stations")]
    public ulong Stations { get; init; }

    [JsonPropertyName("stations_broken")]
    public ulong StationsBroken { get; init; }

    [JsonPropertyName("tags")]
    public ulong Tags { get; init; }

    [JsonPropertyName("clicks_last_hour")]
    public ulong ClicksLastHour { get; init; }

    [JsonPropertyName("clicks_last_day")]
    public ulong ClicksLastDay { get; init; }

    [JsonPropertyName("languages")]
    public ulong Languages { get; init; }

    [JsonPropertyName("countries")]
    public ulong Countries { get; init; }
}
