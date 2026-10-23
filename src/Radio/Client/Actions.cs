namespace LunaPlayer.Radio.Client;

internal sealed partial class Browser
{
    /// <summary>Submits a new station. This mutation is never retried automatically.</summary>
    internal Task<CreationResult> AddStationAsync(StationSubmission station, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "add", Formats.Status), SubmissionParameters(station),
            Serialization.Default.CreationResult, token, mutation: true);

    /// <summary>Submits a station and returns the original JSON or XML response.</summary>
    internal Task<Content> AddStationAsync(ResponseFormat format, StationSubmission station,
        CancellationToken token = default)
        => SendAsync(Path(format, "add", Formats.Status), SubmissionParameters(station), token, mutation: true);

    /// <summary>Registers an explicit playback click and returns the resolved stream URL.
    /// Reading stations through other methods does not register a click.</summary>
    internal Task<ClickResult> CountClickAsync(Guid stationUuid, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, $"url/{stationUuid:D}", Formats.Click), null,
            Serialization.Default.ClickResult, token, mutation: true);

    /// <summary>Registers one click and returns JSON, XML, M3U or PLS. Never retried automatically.</summary>
    internal Task<Content> CountClickAsync(ResponseFormat format, Guid stationUuid, CancellationToken token = default)
        => SendAsync(Path(format, $"url/{stationUuid:D}", Formats.Click), null, token, mutation: true);

    /// <summary>Votes for a station. A server rejection is reported separately from network failure.</summary>
    internal Task<ActionResult> VoteAsync(Guid stationUuid, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, $"vote/{stationUuid:D}", Formats.Status), null,
            Serialization.Default.ActionResult, token, mutation: true);

    /// <summary>Votes once and returns the original JSON or XML response.</summary>
    internal Task<Content> VoteAsync(ResponseFormat format, Guid stationUuid, CancellationToken token = default)
        => SendAsync(Path(format, $"vote/{stationUuid:D}", Formats.Status), null, token, mutation: true);

    private static Dictionary<string, string> SubmissionParameters(StationSubmission station)
    {
        ArgumentNullException.ThrowIfNull(station);
        ArgumentException.ThrowIfNullOrWhiteSpace(station.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(station.Url);
        if (station.Name.Length > 400)
            throw new ArgumentOutOfRangeException(nameof(station));
        if (station.Latitude is double latitude && (!double.IsFinite(latitude) || latitude is < -90 or > 90)
            || station.Longitude is double longitude && (!double.IsFinite(longitude) || longitude is < -180 or > 180))
            throw new ArgumentOutOfRangeException(nameof(station));
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        Put(parameters, "name", station.Name);
        Put(parameters, "url", station.Url);
        Put(parameters, "homepage", station.Homepage);
        Put(parameters, "favicon", station.Favicon);
        Put(parameters, "countrycode", station.CountryCode);
        Put(parameters, "state", station.State);
        Put(parameters, "language", station.Language);
        Put(parameters, "languagecodes", station.LanguageCodes);
        Put(parameters, "tags", station.Tags is null ? null : string.Join(',', station.Tags));
        Put(parameters, "geo_lat", station.Latitude);
        Put(parameters, "geo_long", station.Longitude);
        return parameters;
    }
}
