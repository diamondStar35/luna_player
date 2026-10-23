namespace LunaPlayer.Radio.Client;

internal sealed partial class Browser
{
    /// <summary>Lists a single page of stations.</summary>
    internal Task<Station[]> GetStationsAsync(ListOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "stations", Formats.Stations), Parameters(options),
            Serialization.Default.StationArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetStationsAsync(
        ResponseFormat format, ListOptions? options = null, CancellationToken token = default)
        => SendAsync(Path(format, "stations", Formats.Stations), Parameters(options), token);

    /// <summary>Looks up a station attribute. UUID and country-code searches are always exact.</summary>
    internal Task<Station[]> FindStationsAsync(
        StationField field, string value, bool exact = false, ListOptions? options = null,
        CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, StationRoute(field, value, exact), Formats.Stations), Parameters(options),
            Serialization.Default.StationArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> FindStationsAsync(
        ResponseFormat format, StationField field, string value, bool exact = false, ListOptions? options = null,
        CancellationToken token = default)
        => SendAsync(Path(format, StationRoute(field, value, exact), Formats.Stations), Parameters(options), token);

    /// <summary>Applies all supplied advanced-search filters.</summary>
    internal Task<Station[]> SearchStationsAsync(SearchOptions options, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "stations/search", Formats.Stations), SearchParameters(options),
            Serialization.Default.StationArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> SearchStationsAsync(
        ResponseFormat format, SearchOptions options, CancellationToken token = default)
        => SendAsync(Path(format, "stations/search", Formats.Stations), SearchParameters(options), token);

    /// <summary>Finds stations whose submitted URL exactly matches the supplied URL.</summary>
    internal Task<Station[]> FindStationsByUrlAsync(
        string url, ListOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "stations/byurl", Formats.Stations), UrlParameters(url, options),
            Serialization.Default.StationArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> FindStationsByUrlAsync(
        ResponseFormat format, string url, ListOptions? options = null, CancellationToken token = default)
        => SendAsync(Path(format, "stations/byurl", Formats.Stations), UrlParameters(url, options), token);

    /// <summary>Looks up multiple stations by their globally stable UUIDs.</summary>
    internal Task<Station[]> FindStationsByUuidsAsync(IEnumerable<Guid> uuids, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "stations/byuuid", Formats.Stations), UuidParameters(uuids),
            Serialization.Default.StationArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> FindStationsByUuidsAsync(
        ResponseFormat format, IEnumerable<Guid> uuids, CancellationToken token = default)
        => SendAsync(Path(format, "stations/byuuid", Formats.Stations), UuidParameters(uuids), token);

    /// <summary>Lists stations associated with the supplied broadcaster streaming servers.</summary>
    internal Task<Station[]> FindStationsByServerUuidsAsync(
        IEnumerable<Guid> uuids, ListOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, "stations/byserveruuid", Formats.Stations), UuidParameters(uuids, options),
            Serialization.Default.StationArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> FindStationsByServerUuidsAsync(
        ResponseFormat format, IEnumerable<Guid> uuids, ListOptions? options = null, CancellationToken token = default)
        => SendAsync(Path(format, "stations/byserveruuid", Formats.Stations), UuidParameters(uuids, options), token);

    /// <summary>Lists popular, recently clicked, recently changed, or broken stations.</summary>
    internal Task<Station[]> GetRankedStationsAsync(
        StationRanking ranking, StationPageOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, RankingRoute(ranking), Formats.Stations), Parameters(options),
            Serialization.Default.StationArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetRankedStationsAsync(
        ResponseFormat format, StationRanking ranking, StationPageOptions? options = null,
        CancellationToken token = default)
        => SendAsync(Path(format, RankingRoute(ranking), Formats.Stations), Parameters(options), token);

    private static string StationRoute(StationField field, string value, bool exact)
    {
        var route = field switch
        {
            StationField.Uuid => "byuuid",
            StationField.Name => exact ? "bynameexact" : "byname",
            StationField.Codec => exact ? "bycodecexact" : "bycodec",
            StationField.Country => exact ? "bycountryexact" : "bycountry",
            StationField.CountryCode => "bycountrycodeexact",
            StationField.State => exact ? "bystateexact" : "bystate",
            StationField.Language => exact ? "bylanguageexact" : "bylanguage",
            StationField.Tag => exact ? "bytagexact" : "bytag",
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
        return $"stations/{route}/{Segment(value)}";
    }

    private static string RankingRoute(StationRanking ranking) => ranking switch
    {
        StationRanking.MostClicked => "stations/topclick",
        StationRanking.RecentlyClicked => "stations/lastclick",
        StationRanking.RecentlyChanged => "stations/lastchange",
        StationRanking.HighestVoted => "stations/topvote",
        StationRanking.Broken => "stations/broken",
        _ => throw new ArgumentOutOfRangeException(nameof(ranking)),
    };

    private static Dictionary<string, string> UuidParameters(IEnumerable<Guid> uuids, PageOptions? options = null)
    {
        var parameters = Parameters(options);
        parameters["uuids"] = Ids(uuids);
        return parameters;
    }

    private static Dictionary<string, string> UrlParameters(string url, ListOptions? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        var parameters = Parameters(options);
        parameters["url"] = url;
        return parameters;
    }
}
