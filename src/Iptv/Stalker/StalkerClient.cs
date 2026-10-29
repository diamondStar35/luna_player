using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace LunaPlayer.Iptv;

/// <summary>Loads a catalog from, and resolves play addresses for, a Stalker (MAG set-top-box) portal.</summary>
///
/// <remarks>
/// FRAGILE AND NON-STANDARD, and isolated here on purpose so its quirks never reach the M3U or Xtream paths.
/// The Stalker protocol is undocumented, differs between panels, and authenticates a device by its MAC address
/// rather than a login. The sequence is: <c>handshake</c> for a token, then <c>get_profile</c> to bind it,
/// then <c>get_genres</c> and a paginated <c>get_ordered_list</c> for the channels. Each channel carries a
/// <c>cmd</c> rather than an address; the real address is short-lived and is fetched through
/// <see cref="ResolveLink"/> only at the moment of playing, and never saved.
///
/// Runs on a worker thread behind a progress window. The MAC and portal identify the device but are not
/// secrets in the way a password is; even so nothing here is logged or spoken.
/// </remarks>
internal static class StalkerClient
{
    private const string UserAgent =
        "Mozilla/5.0 (QtEmbedded; U; Linux; C) AppleWebKit/533.3 (KHTML, like Gecko) "
        + "MAG200 stbapp ver: 2 rev: 250 Safari/533.3";

    // A portal carrying more pages than this is treated as misbehaving rather than followed without end.
    private const int MaximumPages = 2000;

    private static readonly HttpClient Client = CreateClient();

    /// <summary>Loads every live channel the portal offers, each holding the <c>cmd</c> that resolves to an
    /// address at play time rather than an address of its own.</summary>
    internal static IptvLoadResult Load(IptvSource source, string uncategorized, CancellationToken token)
    {
        var root = NormalizePortal(source.Url);
        if (root is null)
            // Translators: Shown when a Stalker portal's address cannot be understood.
            return IptvLoadResult.Fail(Tr("The Stalker portal address is not valid."));
        var mac = source.MacAddress.Trim();

        try
        {
            var handshake = Send(root, mac, null, "type=stb&action=handshake&token=", IptvJsonContext.Default.StalkerHandshakeResponse, token);
            if (handshake?.Js?.Token is not { Length: > 0 } authToken)
                // Translators: Shown when a Stalker portal does not return a token, so it cannot be used.
                return IptvLoadResult.Fail(Tr("The Stalker portal did not accept this MAC address."));

            // Binds the token to the device; its body is not needed, only that it is asked for in order.
            _ = Send(root, mac, authToken, "type=stb&action=get_profile", IptvJsonContext.Default.StalkerHandshakeResponse, token);

            var categories = new List<IptvCategory>();
            var known = new HashSet<string>(StringComparer.Ordinal);
            var genres = Send(root, mac, authToken, "type=itv&action=get_genres", IptvJsonContext.Default.StalkerGenresResponse, token);
            foreach (var genre in genres?.Js ?? [])
            {
                var id = (genre.Id ?? string.Empty).Trim();
                if (id.Length > 0 && known.Add(id))
                    categories.Add(new IptvCategory(id, (genre.Title ?? id).Trim(), StreamKind.Live));
            }

            var channels = new List<IptvChannel>();
            var needsUncategorized = LoadChannels(root, mac, authToken, known, channels, token);
            if (needsUncategorized)
                categories.Add(new IptvCategory(string.Empty, uncategorized, StreamKind.Live));

            return IptvLoadResult.Ok(new IptvCatalog(categories, channels, null));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Translators: Shown when talking to a Stalker portal took too long and was given up on.
            return IptvLoadResult.Fail(Tr("The Stalker portal did not respond in time."));
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException
            or UriFormatException or JsonException)
        {
            return IptvLoadResult.Fail(exception.Message);
        }
    }
    // PLACEHOLDER
    /// <summary>Walks the paginated channel list, adding each channel. Returns whether any channel fell into no
    /// known genre, so the caller can add the catch-all category once.</summary>
    private static bool LoadChannels(
        string root, string mac, string token, HashSet<string> known, List<IptvChannel> channels,
        CancellationToken cancellationToken)
    {
        var needsUncategorized = false;
        var page = 1;
        var pages = 1;
        while (page <= pages && page <= MaximumPages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = Send(root, mac, token,
                $"type=itv&action=get_ordered_list&genre=*&p={page}",
                IptvJsonContext.Default.StalkerListResponse, cancellationToken);
            var list = response?.Js;
            if (list?.Data is not { Length: > 0 } data)
                break;
            if (page == 1 && list.TotalItems > 0 && list.MaxPageItems > 0)
                pages = (list.TotalItems + list.MaxPageItems - 1) / list.MaxPageItems;
            foreach (var channel in data)
            {
                var cmd = (channel.Cmd ?? string.Empty).Trim();
                if (cmd.Length == 0)
                    continue;
                var genreId = (channel.GenreId ?? string.Empty).Trim();
                if (genreId.Length == 0 || !known.Contains(genreId))
                {
                    genreId = string.Empty;
                    needsUncategorized = true;
                }
                channels.Add(new IptvChannel(
                    Id: (channel.Id ?? string.Empty).Trim(),
                    Name: (channel.Name ?? string.Empty).Trim(),
                    Number: Trimmed(channel.Number),
                    CategoryId: genreId,
                    Kind: StreamKind.Live,
                    EpgId: Trimmed(channel.XmltvId),
                    Logo: Trimmed(channel.Logo),
                    Url: null,
                    StalkerCmd: cmd));
            }
            page++;
        }
        return needsUncategorized;
    }

    /// <summary>Turns a channel's stored <c>cmd</c> into the short-lived address that plays it, fetched fresh
    /// each time because the portal expires it quickly. Null when the portal refused or nothing came back.</summary>
    internal static string? ResolveLink(IptvSource source, string cmd, CancellationToken token)
    {
        var root = NormalizePortal(source.Url);
        if (root is null)
            return null;
        var mac = source.MacAddress.Trim();
        var handshake = Send(root, mac, null, "type=stb&action=handshake&token=", IptvJsonContext.Default.StalkerHandshakeResponse, token);
        if (handshake?.Js?.Token is not { Length: > 0 } authToken)
            return null;
        _ = Send(root, mac, authToken, "type=stb&action=get_profile", IptvJsonContext.Default.StalkerHandshakeResponse, token);
        var link = Send(root, mac, authToken,
            $"type=itv&action=create_link&cmd={Uri.EscapeDataString(cmd)}",
            IptvJsonContext.Default.StalkerLinkResponse, token);
        return CleanLink(link?.Js?.Cmd);
    }

    /// <summary>A resolved link with the <c>ffmpeg </c> prefix some portals prepend stripped off, or null when
    /// there was nothing playable.</summary>
    private static string? CleanLink(string? command)
    {
        var value = (command ?? string.Empty).Trim();
        if (value.StartsWith("ffmpeg ", StringComparison.OrdinalIgnoreCase))
            value = value["ffmpeg ".Length..].Trim();
        // A link line can carry a trailing label after the address; the address itself is the first token.
        var space = value.IndexOf(' ');
        if (space > 0)
            value = value[..space];
        return value.Length > 0 ? value : null;
    }

    private static T? Send<T>(
        string root, string mac, string? token, string query, JsonTypeInfo<T> type,
        CancellationToken cancellationToken) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{root}/portal.php?{query}&JsHttpRequest=1-xml");
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Headers.TryAddWithoutValidation("Referer", $"{root}/c/");
        request.Headers.TryAddWithoutValidation("X-User-Agent", "Model: MAG250; Link: WiFi");
        request.Headers.TryAddWithoutValidation("Cookie",
            $"mac={Uri.EscapeDataString(mac)}; stb_lang=en; timezone=Europe/London");
        if (token is { Length: > 0 })
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        using var response = Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using var stream = response.Content.ReadAsStream(cancellationToken);
        return JsonSerializer.Deserialize(stream, type);
    }

    /// <summary>The portal as a bare scheme-and-host(-and-port) with no trailing slash or path, or null when
    /// the saved address is not a web address.</summary>
    private static string? NormalizePortal(string address)
    {
        if (!Media.LinkValidator.TryGetHttpUrl(address.Trim(), out var uri))
            return null;
        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        return $"{uri.Scheme}://{uri.Host}{port}";
    }

    private static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
    }
}
