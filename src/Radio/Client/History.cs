namespace LunaPlayer.Radio.Client;

internal sealed partial class Browser
{
    /// <summary>Lists station revisions, optionally continuing after a change UUID.</summary>
    internal Task<StationChange[]> GetChangesAsync(
        Guid? stationUuid = null, ChangeOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, HistoryRoute("stations/changed", stationUuid), Formats.Lists),
            ChangeParameters(options, "lastchangeuuid"),
            Serialization.Default.StationChangeArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetChangesAsync(
        ResponseFormat format, Guid? stationUuid = null, ChangeOptions? options = null,
        CancellationToken token = default)
        => SendAsync(Path(format, HistoryRoute("stations/changed", stationUuid), Formats.Lists),
            ChangeParameters(options, "lastchangeuuid"), token);

    /// <summary>Lists recent checks, or the check history of one station.</summary>
    internal Task<StationCheck[]> GetChecksAsync(
        Guid? stationUuid = null, CheckOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, HistoryRoute("checks", stationUuid), Formats.Lists),
            ChangeParameters(options, "lastcheckuuid"),
            Serialization.Default.StationCheckArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetChecksAsync(
        ResponseFormat format, Guid? stationUuid = null, CheckOptions? options = null,
        CancellationToken token = default)
        => SendAsync(Path(format, HistoryRoute("checks", stationUuid), Formats.Lists), ChangeParameters(options, "lastcheckuuid"), token);

    /// <summary>Lists redirect and playlist steps for checks of the supplied stations.</summary>
    internal Task<CheckStep[]> GetCheckStepsAsync(IEnumerable<Guid> stationUuids, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "checksteps", Formats.Lists), UuidParameters(stationUuids),
            Serialization.Default.CheckStepArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetCheckStepsAsync(
        ResponseFormat format, IEnumerable<Guid> stationUuids, CancellationToken token = default)
        => SendAsync(Path(format, "checksteps", Formats.Lists), UuidParameters(stationUuids), token);

    /// <summary>Lists playback-click history. The server chunks results; After continues from a click UUID.</summary>
    internal Task<StationClick[]> GetClicksAsync(
        Guid? stationUuid = null, ClickOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, HistoryRoute("clicks", stationUuid), Formats.Lists), ClickParameters(options),
            Serialization.Default.StationClickArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetClicksAsync(
        ResponseFormat format, Guid? stationUuid = null, ClickOptions? options = null,
        CancellationToken token = default)
        => SendAsync(Path(format, HistoryRoute("clicks", stationUuid), Formats.Lists), ClickParameters(options), token);

    private static string HistoryRoute(string route, Guid? stationUuid)
        => stationUuid.HasValue ? $"{route}/{stationUuid.Value:D}" : route;

    private static Dictionary<string, string> ClickParameters(ClickOptions? options)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        Put(parameters, "lastclickuuid", options?.After);
        Put(parameters, "seconds", options?.Seconds);
        return parameters;
    }
}
