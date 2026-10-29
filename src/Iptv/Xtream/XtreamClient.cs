using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace LunaPlayer.Iptv;

/// <summary>Loads a catalog from a provider portal spoken to through the Xtream Codes API.</summary>
///
/// <remarks>
/// The panel is asked, in turn, to authenticate, then for its live categories and streams and its
/// video-on-demand categories and streams; those become one flat <see cref="IptvCatalog"/> the browser and the
/// player work in. Series are deliberately not expanded here: listing a series' episodes needs one
/// <c>get_series_info</c> call per series, which for a panel carrying thousands of series would be thousands of
/// requests at load time - a drill-in browser is the right home for that and is left as a later addition.
///
/// This runs on a worker thread behind a progress window, the same as <see cref="M3uPlaylist.ReadNetwork"/>,
/// and words its own failures the same way. Credentials travel in the request URL as the API requires; they are
/// never logged or spoken.
/// </remarks>
internal static class XtreamClient
{
    private static readonly HttpClient Client = CreateClient();

    internal static IptvLoadResult Load(IptvSource source, string uncategorized, CancellationToken token)
    {
        var server = NormalizeServer(source.Url);
        if (server is null)
            // Translators: Shown when an Xtream IPTV source's server address cannot be understood.
            return IptvLoadResult.Fail(Tr("The Xtream server address is not valid."));

        var user = Uri.EscapeDataString(source.Username);
        var password = Uri.EscapeDataString(source.Password);
        var apiBase = $"{server}/player_api.php?username={user}&password={password}";

        try
        {
            var auth = Fetch(apiBase, IptvJsonContext.Default.XtreamAuthResponse, token);
            if (auth?.UserInfo is not { Auth: 1 } info)
                // Translators: Shown when an Xtream panel rejects the saved username or password.
                return IptvLoadResult.Fail(Tr("The Xtream server rejected the username or password."));
            if (info.Status is { Length: > 0 } status
                && !status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                // Translators: Shown when an Xtream account is no longer active (expired, disabled or banned).
                return IptvLoadResult.Fail(Tr("This Xtream account is not active."));

            var categories = new List<IptvCategory>();
            var channels = new List<IptvChannel>();
            var known = new HashSet<string>(StringComparer.Ordinal);
            var needsUncategorized = false;

            Collect(apiBase, "get_live_categories", "get_live_streams", "live", StreamKind.Live,
                server, source, categories, channels, known, ref needsUncategorized, token);
            Collect(apiBase, "get_vod_categories", "get_vod_streams", "vod", StreamKind.Movie,
                server, source, categories, channels, known, ref needsUncategorized, token);

            if (needsUncategorized)
                categories.Add(new IptvCategory(string.Empty, uncategorized, StreamKind.Live));

            return IptvLoadResult.Ok(new IptvCatalog(categories, channels, XmltvUrl(apiBase)));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Translators: Shown when talking to an Xtream server took too long and was given up on.
            return IptvLoadResult.Fail(Tr("The Xtream server did not respond in time."));
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException
            or UriFormatException or JsonException)
        {
            return IptvLoadResult.Fail(exception.Message);
        }
    }
    // PLACEHOLDER
    /// <summary>Fetches one category list and its streams and adds them, as one kind, to the growing catalog.
    /// Category ids are prefixed with the kind (<c>live:</c>, <c>vod:</c>) so live and video categories that
    /// share a provider number stay apart in the browser's category filter.</summary>
    private static void Collect(
        string apiBase, string categoriesAction, string streamsAction, string prefix, StreamKind kind,
        string server, IptvSource source,
        List<IptvCategory> categories, List<IptvChannel> channels, HashSet<string> known,
        ref bool needsUncategorized, CancellationToken token)
    {
        var apiCategories = Fetch($"{apiBase}&action={categoriesAction}", IptvJsonContext.Default.XtreamCategoryArray, token) ?? [];
        foreach (var category in apiCategories)
        {
            var id = (category.CategoryId ?? string.Empty).Trim();
            if (id.Length == 0)
                continue;
            var key = $"{prefix}:{id}";
            if (known.Add(key))
                categories.Add(new IptvCategory(key, (category.CategoryName ?? key).Trim(), kind));
        }

        var streams = Fetch($"{apiBase}&action={streamsAction}", IptvJsonContext.Default.XtreamStreamArray, token) ?? [];
        var user = Uri.EscapeDataString(source.Username);
        var password = Uri.EscapeDataString(source.Password);
        foreach (var stream in streams)
        {
            token.ThrowIfCancellationRequested();
            var categoryId = (stream.CategoryId ?? string.Empty).Trim();
            var key = categoryId.Length == 0 ? string.Empty : $"{prefix}:{categoryId}";
            if (key.Length == 0 || !known.Contains(key))
            {
                key = string.Empty;
                needsUncategorized = true;
            }
            var url = kind == StreamKind.Live
                ? $"{server}/live/{user}/{password}/{stream.StreamId}.{(source.PreferHls ? "m3u8" : "ts")}"
                : $"{server}/movie/{user}/{password}/{stream.StreamId}.{Container(stream.ContainerExtension)}";
            channels.Add(new IptvChannel(
                Id: stream.StreamId.ToString(),
                Name: (stream.Name ?? string.Empty).Trim(),
                Number: stream.Num > 0 ? stream.Num.ToString() : null,
                CategoryId: key,
                Kind: kind,
                EpgId: Trimmed(stream.EpgChannelId),
                Logo: Trimmed(stream.StreamIcon),
                Url: url,
                StalkerCmd: null));
        }
    }

    private static T? Fetch<T>(string url, JsonTypeInfo<T> type, CancellationToken token) where T : class
    {
        using var response = Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using var stream = response.Content.ReadAsStream(token);
        return JsonSerializer.Deserialize(stream, type);
    }

    /// <summary>The XMLTV programme-guide address an Xtream panel serves, built from the same credentials.</summary>
    private static string XmltvUrl(string apiBase)
        => apiBase.Replace("/player_api.php?", "/xmltv.php?", StringComparison.Ordinal);

    /// <summary>The server as a bare scheme-and-host(-and-port) with no trailing slash or path, or null when
    /// the saved address is not a web address at all.</summary>
    private static string? NormalizeServer(string address)
    {
        if (!Media.LinkValidator.TryGetHttpUrl(address.Trim(), out var uri))
            return null;
        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        return $"{uri.Scheme}://{uri.Host}{port}";
    }

    private static string Container(string? extension)
    {
        var value = (extension ?? string.Empty).Trim().TrimStart('.');
        return value.Length == 0 ? "mp4" : value;
    }

    private static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
    }
}
