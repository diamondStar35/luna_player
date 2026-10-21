using System.Net;
using LunaPlayer.Media;

namespace LunaPlayer.Iptv;

/// <summary>The one door every playlist goes through, whatever its format.</summary>
///
/// <remarks>
/// The player used to read only M3U. This dispatches on a playlist's extension to the reader that understands
/// it - M3U/M3U8 to <see cref="M3uPlaylist"/>, and PLS, XSPF, ASX and CUE to the readers beside this one - and
/// hands every one of them back as the same <see cref="M3uPlaylistResult"/>, so the rest of the player neither
/// knows nor cares which format a file was. An unrecognised extension is read as M3U, the most forgiving of
/// the formats and the one a mislabelled list is most often really in.
///
/// Fetching (a size-capped download, or a file read) lives here for every format except M3U, which fetches
/// itself; the bytes are decoded through <see cref="M3uPlaylist.Decode"/> so text handling stays identical.
/// </remarks>
internal static class PlaylistReader
{
    private delegate M3uPlaylistResult ParseFormat(string text, Uri source, bool local);

    private static readonly HttpClient Client = CreateClient();

    internal static M3uPlaylistResult ReadLocal(string path)
    {
        // M3U reads itself, so relative paths inside it resolve exactly as they always did.
        if (IsM3u(path))
            return M3uPlaylist.ReadLocal(path);
        var parse = ParserFor(path);
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length > M3uPlaylist.MaximumBytes)
                // Translators: Shown when a local IPTV playlist exceeds the safe size limit.
                return M3uPlaylistResult.Failure(Tr("The playlist file is too large."));
            var root = new Uri(Path.GetFullPath(path));
            return parse(M3uPlaylist.Decode(bytes), root, local: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException)
        {
            return M3uPlaylistResult.Failure(exception.Message);
        }
    }

    internal static M3uPlaylistResult ReadNetwork(string address, CancellationToken cancellationToken)
    {
        if (IsM3u(address))
            return M3uPlaylist.ReadNetwork(address, cancellationToken);
        var parse = ParserFor(address);
        try
        {
            using var response = Client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > M3uPlaylist.MaximumBytes)
                // Translators: Shown when an IPTV playlist download exceeds the safe size limit.
                return M3uPlaylistResult.Failure(Tr("The network playlist is too large."));
            using var stream = response.Content.ReadAsStream(cancellationToken);
            using var memory = new MemoryStream();
            var buffer = new byte[16 * 1024];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                if (memory.Length + read > M3uPlaylist.MaximumBytes)
                    // Translators: Shown when an IPTV playlist download grows beyond the safe size limit.
                    return M3uPlaylistResult.Failure(Tr("The network playlist is too large."));
                memory.Write(buffer, 0, read);
            }
            return parse(M3uPlaylist.Decode(memory.ToArray()), new Uri(address), local: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Translators: Shown when downloading an IPTV playlist takes too long.
            return M3uPlaylistResult.Failure(Tr("The playlist request timed out."));
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException
            or UriFormatException or ArgumentException)
        {
            return M3uPlaylistResult.Failure(exception.Message);
        }
    }

    private static bool IsM3u(string pathOrUrl)
    {
        var extension = ExtensionOf(pathOrUrl);
        return extension is ".m3u" or ".m3u8";
    }

    private static ParseFormat ParserFor(string pathOrUrl) => ExtensionOf(pathOrUrl) switch
    {
        ".pls" => PlsPlaylist.Parse,
        ".xspf" => XspfPlaylist.Parse,
        ".asx" => AsxPlaylist.Parse,
        ".cue" => CuePlaylist.Parse,
        _ => M3uPlaylist.ParseText,
    };

    private static string ExtensionOf(string pathOrUrl)
    {
        if (LinkValidator.TryGetHttpUrl(pathOrUrl, out var uri))
            return Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        return Path.GetExtension(pathOrUrl).ToLowerInvariant();
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
    }
}
