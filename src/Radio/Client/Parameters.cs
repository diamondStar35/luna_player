using System.Globalization;

namespace LunaPlayer.Radio.Client;

internal sealed partial class Browser
{
    [Flags]
    private enum Formats
    {
        Json = 1, Xml = 2, Csv = 4, M3u = 8, Pls = 16, Xspf = 32, Turtle = 64, Html = 128,
        Lists = Json | Xml | Csv,
        Stations = Lists | M3u | Pls | Xspf | Turtle,
        Status = Json | Xml,
        Click = Status | M3u | Pls,
        Statistics = Status | Html,
    }

    private static string Path(ResponseFormat format, string route, Formats allowed)
    {
        var (flag, prefix) = format switch
        {
            ResponseFormat.Json => (Formats.Json, "json"),
            ResponseFormat.Xml => (Formats.Xml, "xml"),
            ResponseFormat.Csv => (Formats.Csv, "csv"),
            ResponseFormat.M3u => (Formats.M3u, "m3u"),
            ResponseFormat.Pls => (Formats.Pls, "pls"),
            ResponseFormat.Xspf => (Formats.Xspf, "xspf"),
            ResponseFormat.Turtle => (Formats.Turtle, "ttl"),
            ResponseFormat.Html => (Formats.Html, "html"),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
        if ((allowed & flag) == 0)
            throw new ArgumentOutOfRangeException(nameof(format));
        return $"{prefix}/{route}";
    }

    private static string Segment(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Uri.EscapeDataString(value);
    }

    private static string Ids(IEnumerable<Guid> uuids)
    {
        ArgumentNullException.ThrowIfNull(uuids);
        var values = uuids.Select(uuid => uuid.ToString("D")).ToArray();
        if (values.Length == 0)
            throw new ArgumentException(nameof(uuids));
        return string.Join(',', values);
    }

    private static void Put(Dictionary<string, string> parameters, string key, object? value)
    {
        if (value is null)
            return;
        // Only the typed scalars passed below reach this switch; this does not inspect properties or
        // invoke reflection to build query strings. In particular, false and zero are not omitted.
        parameters[key] = value switch
        {
            string text => text,
            bool flag => flag ? "true" : "false",
            Guid uuid => uuid.ToString("D"),
            IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            _ => throw new ArgumentException(nameof(value)),
        };
    }

    private static Dictionary<string, string> Parameters(PageOptions? options)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        if (options is null)
            return parameters;
        Put(parameters, "offset", options.Offset);
        Put(parameters, "limit", options.Limit);
        if (options is StationPageOptions stations)
            Put(parameters, "hidebroken", stations.HideBroken);
        if (options is ListOptions list)
        {
            Put(parameters, "order", list.Order);
            Put(parameters, "reverse", list.Reverse);
        }
        if (options is StreamingServerOptions servers)
        {
            Put(parameters, "order", servers.Order);
            Put(parameters, "reverse", servers.Reverse);
        }
        return parameters;
    }

    private static Dictionary<string, string> SearchParameters(SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var parameters = Parameters(options);
        Put(parameters, "name", options.Name);
        Put(parameters, "nameExact", options.NameExact);
        Put(parameters, "country", options.Country);
        Put(parameters, "countryExact", options.CountryExact);
        Put(parameters, "countrycode", options.CountryCode);
        Put(parameters, "state", options.State);
        Put(parameters, "stateExact", options.StateExact);
        Put(parameters, "language", options.Language);
        Put(parameters, "languageExact", options.LanguageExact);
        Put(parameters, "tag", options.Tag);
        Put(parameters, "tagExact", options.TagExact);
        Put(parameters, "tagList", options.Tags is null ? null : string.Join(',', options.Tags));
        Put(parameters, "codec", options.Codec);
        Put(parameters, "bitrateMin", options.BitrateMin);
        Put(parameters, "bitrateMax", options.BitrateMax);
        Put(parameters, "has_geo_info", options.HasGeoInfo);
        Put(parameters, "has_extended_info", options.HasExtendedInfo);
        Put(parameters, "is_https", options.IsHttps);
        return parameters;
    }

    private static Dictionary<string, string> ChangeParameters(ChangeOptions? options, string afterKey)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        Put(parameters, afterKey, options?.After);
        Put(parameters, "limit", options?.Limit);
        if (options is CheckOptions checks)
            Put(parameters, "seconds", checks.Seconds);
        return parameters;
    }

    private static string Encode(Dictionary<string, string> parameters)
        => string.Join('&', parameters.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
}
