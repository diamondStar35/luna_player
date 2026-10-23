namespace LunaPlayer.Radio.Client;

internal sealed partial class Browser
{
    /// <summary>Lists countries, including their standardized country codes.</summary>
    internal Task<Country[]> GetCountriesAsync(
        string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, DirectoryRoute("countries", filter), Formats.Lists), Parameters(options),
            Serialization.Default.CountryArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetCountriesAsync(
        ResponseFormat format, string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => SendAsync(Path(format, DirectoryRoute("countries", filter), Formats.Lists), Parameters(options), token);

    /// <summary>Exposes the deprecated country-code list for API completeness. Prefer GetCountriesAsync.</summary>
    internal Task<DirectoryEntry[]> GetCountryCodesAsync(
        string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, DirectoryRoute("countrycodes", filter), Formats.Lists), Parameters(options),
            Serialization.Default.DirectoryEntryArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetCountryCodesAsync(
        ResponseFormat format, string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => SendAsync(Path(format, DirectoryRoute("countrycodes", filter), Formats.Lists), Parameters(options), token);

    /// <summary>Lists languages and their optional ISO codes.</summary>
    internal Task<Language[]> GetLanguagesAsync(
        string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, DirectoryRoute("languages", filter), Formats.Lists), Parameters(options),
            Serialization.Default.LanguageArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetLanguagesAsync(
        ResponseFormat format, string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => SendAsync(Path(format, DirectoryRoute("languages", filter), Formats.Lists), Parameters(options), token);

    /// <summary>Lists codecs observed by directory checks.</summary>
    internal Task<DirectoryEntry[]> GetCodecsAsync(
        string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, DirectoryRoute("codecs", filter), Formats.Lists), Parameters(options),
            Serialization.Default.DirectoryEntryArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetCodecsAsync(
        ResponseFormat format, string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => SendAsync(Path(format, DirectoryRoute("codecs", filter), Formats.Lists), Parameters(options), token);

    /// <summary>Lists tags used by stations.</summary>
    internal Task<DirectoryEntry[]> GetTagsAsync(
        string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, DirectoryRoute("tags", filter), Formats.Lists), Parameters(options),
            Serialization.Default.DirectoryEntryArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetTagsAsync(
        ResponseFormat format, string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => SendAsync(Path(format, DirectoryRoute("tags", filter), Formats.Lists), Parameters(options), token);

    /// <summary>Lists states or provinces, optionally restricted to a country.</summary>
    internal Task<State[]> GetStatesAsync(
        string? country = null, string? filter = null, ListOptions? options = null, CancellationToken token = default)
        => ReadAsync(Path(ResponseFormat.Json, StatesRoute(country, filter), Formats.Lists), Parameters(options),
            Serialization.Default.StateArray, token);

    /// <summary>Returns the server's original representation in a supported format.</summary>
    internal Task<Content> GetStatesAsync(
        ResponseFormat format, string? country = null, string? filter = null, ListOptions? options = null,
        CancellationToken token = default)
        => SendAsync(Path(format, StatesRoute(country, filter), Formats.Lists), Parameters(options), token);

    private static string DirectoryRoute(string route, string? filter)
        => filter is null ? route : $"{route}/{Segment(filter)}";

    private static string StatesRoute(string? country, string? filter)
    {
        // The one-segment states route filters names, not countries. A trailing empty segment is
        // required to select all states in a country when no state-name filter was supplied.
        if (country is null)
            return DirectoryRoute("states", filter);
        return $"states/{Segment(country)}/{(filter is null ? string.Empty : Segment(filter))}";
    }
}
