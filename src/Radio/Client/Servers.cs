namespace LunaPlayer.Radio.Client;

internal sealed partial class Browser
{
    /// <summary>Reads the responding API mirror's public configuration.</summary>
    internal Task<ServerConfiguration> GetConfigurationAsync(CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "config", Formats.Status), null,
            Serialization.Default.ServerConfiguration, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetConfigurationAsync(ResponseFormat format, CancellationToken token = default)
        => SendAsync(Path(format, "config", Formats.Status), null, token);

    /// <summary>Reads directory statistics. HTML is also available through the raw overload.</summary>
    internal Task<ServerStatistics> GetStatisticsAsync(CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "stats", Formats.Statistics), null,
            Serialization.Default.ServerStatistics, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetStatisticsAsync(ResponseFormat format, CancellationToken token = default)
        => SendAsync(Path(format, "stats", Formats.Statistics), null, token);

    /// <summary>Reads the server's mirror list. Normal client discovery uses local DNS instead.</summary>
    internal Task<Mirror[]> GetMirrorsAsync(CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "servers", Formats.Json), null,
            Serialization.Default.MirrorArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetMirrorsAsync(ResponseFormat format, CancellationToken token = default)
        => SendAsync(Path(format, "servers", Formats.Json), null, token);

    /// <summary>Lists broadcaster streaming servers, rather than Radio Browser API mirrors.</summary>
    internal Task<StreamingServer[]> GetStreamingServersAsync(
        StreamingServerOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "streamingservers", Formats.Status), Parameters(options),
            Serialization.Default.StreamingServerArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetStreamingServersAsync(
        ResponseFormat format, StreamingServerOptions? options = null, CancellationToken token = default)
        => SendAsync(Path(format, "streamingservers", Formats.Status), Parameters(options), token);

    /// <summary>Looks up broadcaster servers by their UUIDs.</summary>
    internal Task<StreamingServer[]> FindStreamingServersByUuidsAsync(
        IEnumerable<Guid> serverUuids, StreamingServerOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "streamingservers/byserveruuid", Formats.Status), UuidParameters(serverUuids, options),
            Serialization.Default.StreamingServerArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> FindStreamingServersByUuidsAsync(
        ResponseFormat format, IEnumerable<Guid> serverUuids, StreamingServerOptions? options = null,
        CancellationToken token = default)
        => SendAsync(Path(format, "streamingservers/byserveruuid", Formats.Status), UuidParameters(serverUuids, options), token);

    /// <summary>Looks up the broadcaster servers associated with the supplied stations.</summary>
    internal Task<StreamingServer[]> FindStreamingServersByStationUuidsAsync(
        IEnumerable<Guid> stationUuids, StreamingServerOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "streamingservers/bystationuuid", Formats.Status), UuidParameters(stationUuids, options),
            Serialization.Default.StreamingServerArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> FindStreamingServersByStationUuidsAsync(
        ResponseFormat format, IEnumerable<Guid> stationUuids, StreamingServerOptions? options = null,
        CancellationToken token = default)
        => SendAsync(Path(format, "streamingservers/bystationuuid", Formats.Status), UuidParameters(stationUuids, options), token);

    /// <summary>Returns the optional Prometheus export unchanged, including its media type.</summary>
    internal Task<Content> GetMetricsAsync(CancellationToken token = default)
        => SendAsync("metrics", null, token);
}
