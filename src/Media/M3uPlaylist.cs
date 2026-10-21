using System.Net;
using System.Text;
using LunaPlayer.Iptv;

namespace LunaPlayer.Media;

/// <summary>One playable location, the optional name supplied for it by an extended M3U file, and the IPTV
/// attributes (<c>tvg-id</c>, <c>group-title</c> and the rest) carried on its <c>#EXTINF</c> line when it is
/// an IPTV playlist. <see cref="Attributes"/> is null for a plain playlist that carries none, so an ordinary
/// music playlist is untouched by the IPTV reader.</summary>
internal readonly record struct M3uEntry(string Location, string? Title, IptvAttributes? Attributes = null);

/// <summary>The result of reading an M3U file. HLS manifests are handed intact to mpv rather than treating
/// their media segments as separate songs. <see cref="TvgUrl"/> is the programme-guide address declared in the
/// <c>#EXTM3U</c> header (<c>url-tvg</c> / <c>x-tvg-url</c>), or null when the header carries none.</summary>
internal sealed record M3uPlaylistResult(
    IReadOnlyList<M3uEntry> Entries, bool IsHls, string? Error, string? TvgUrl = null)
{
    internal static M3uPlaylistResult Failure(string error) => new([], false, error);
}

internal static class M3uPlaylist
{
    // Provider IPTV playlists routinely run to tens of megabytes - a single flat list can carry a hundred
    // thousand channels - so the ceiling is well above the few megabytes an ordinary music playlist needs.
    // Shared with the other playlist formats so every reader stops at the same size.
    internal const int MaximumBytes = 96 * 1024 * 1024;
    private static readonly HttpClient Client = CreateClient();

    internal static M3uPlaylistResult ReadLocal(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length > MaximumBytes)
                // Translators: Shown when a local M3U playlist exceeds the safe size limit.
                return M3uPlaylistResult.Failure(Tr("The playlist file is too large."));
            var root = new Uri(Path.GetFullPath(path));
            return ParseText(Decode(bytes), root, local: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException)
        {
            return M3uPlaylistResult.Failure(exception.Message);
        }
    }

    internal static M3uPlaylistResult ReadNetwork(string address, CancellationToken cancellationToken)
    {
        try
        {
            using var response = Client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > MaximumBytes)
                // Translators: Shown when an M3U playlist download exceeds the safe size limit.
                return M3uPlaylistResult.Failure(Tr("The network playlist is too large."));
            using var stream = response.Content.ReadAsStream(cancellationToken);
            using var memory = new MemoryStream();
            var buffer = new byte[16 * 1024];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                if (memory.Length + read > MaximumBytes)
                    // Translators: Shown when an M3U playlist download grows beyond the safe size limit.
                    return M3uPlaylistResult.Failure(Tr("The network playlist is too large."));
                memory.Write(buffer, 0, read);
            }
            return ParseText(Decode(memory.ToArray()), new Uri(address), local: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Translators: Shown when downloading an M3U playlist takes too long.
            return M3uPlaylistResult.Failure(Tr("The playlist request timed out."));
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException
            or UriFormatException or ArgumentException)
        {
            return M3uPlaylistResult.Failure(exception.Message);
        }
    }

    /// <summary>Parses already-decoded playlist text into entries. Exposed so the format dispatcher can hand
    /// M3U text through the same door as the other formats; <paramref name="source"/> is the file or web
    /// address it came from, used to resolve relative locations, and <paramref name="local"/> says which.</summary>
    internal static M3uPlaylistResult ParseText(string text, Uri source, bool local)
    {
        var entries = new List<M3uEntry>();
        string? title = null;
        IptvAttributes? attributes = null;
        string? tvgUrl = null;
        var isHls = false;
        using var reader = new StringReader(text);
        while (reader.ReadLine() is string rawLine)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;
            if (line.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase))
            {
                tvgUrl ??= ReadHeaderTvgUrl(line);
                continue;
            }
            if (line.StartsWith("#EXT-X-", StringComparison.OrdinalIgnoreCase))
            {
                isHls = true;
                continue;
            }
            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                var comma = line.IndexOf(',');
                title = comma >= 0 && comma + 1 < line.Length ? line[(comma + 1)..].Trim() : null;
                // Attributes live in the "#EXTINF:<duration> key="value"..." run before the comma.
                var head = comma >= 0 ? line[..comma] : line;
                attributes = ReadAttributes(head);
                continue;
            }
            if (line[0] == '#')
                continue;

            var location = Resolve(line, source, local);
            if (location is not null)
                entries.Add(new(
                    location,
                    string.IsNullOrWhiteSpace(title) ? null : title,
                    attributes is { IsEmpty: false } filled ? filled : null));
            title = null;
            attributes = null;
        }
        return new(entries, isHls, null, tvgUrl);
    }

    /// <summary>The programme-guide address a <c>#EXTM3U</c> header advertises, under either the
    /// <c>url-tvg</c> or the older <c>x-tvg-url</c> key, or null when it names none.</summary>
    private static string? ReadHeaderTvgUrl(string header)
    {
        foreach (var (key, value) in Tokenize(header.AsMemory()))
            if (key.Equals("url-tvg", StringComparison.OrdinalIgnoreCase)
                || key.Equals("x-tvg-url", StringComparison.OrdinalIgnoreCase))
            {
                // A header may list several comma-separated guides; the first is enough for now.
                var first = value.Split(',', 2)[0].Trim();
                if (first.Length > 0)
                    return first;
            }
        return null;
    }

    /// <summary>The IPTV attributes on an <c>#EXTINF</c> line, read from its <c>key="value"</c> run. Unknown
    /// keys are ignored; a line with no recognised keys yields an empty record the caller drops.</summary>
    private static IptvAttributes ReadAttributes(string head)
    {
        string? tvgId = null, tvgName = null, tvgLogo = null, groupTitle = null, tvgChno = null, catchup = null;
        foreach (var (key, value) in Tokenize(head.AsMemory()))
        {
            if (key.Equals("tvg-id", StringComparison.OrdinalIgnoreCase)) tvgId = value;
            else if (key.Equals("tvg-name", StringComparison.OrdinalIgnoreCase)) tvgName = value;
            else if (key.Equals("tvg-logo", StringComparison.OrdinalIgnoreCase)) tvgLogo = value;
            else if (key.Equals("group-title", StringComparison.OrdinalIgnoreCase)) groupTitle = value;
            else if (key.Equals("tvg-chno", StringComparison.OrdinalIgnoreCase)) tvgChno = value;
            else if (key.Equals("catchup", StringComparison.OrdinalIgnoreCase)
                || key.Equals("catchup-type", StringComparison.OrdinalIgnoreCase)) catchup ??= value;
        }
        return new(tvgId, tvgName, tvgLogo, groupTitle, tvgChno, catchup);
    }

    /// <summary>Walks a run of <c>key="value"</c> (or bare <c>key=value</c>) pairs, yielding each once. Written
    /// to tolerate the dirty lines real providers ship: missing quotes, stray spaces, and empty values. A key
    /// whose value is blank is skipped so it does not overwrite a good value with an empty string.</summary>
    private static IEnumerable<(string Key, string Value)> Tokenize(ReadOnlyMemory<char> lineMemory)
    {
        var line = lineMemory;
        var index = 0;
        while (index < line.Length)
        {
            var span = line.Span;
            while (index < line.Length && span[index] != '=')
                index++;
            if (index >= line.Length)
                break;

            // Walk back over the key: letters, digits and dashes, stopping at the space before it.
            var keyEnd = index;
            var keyStart = index;
            while (keyStart > 0 && !char.IsWhiteSpace(span[keyStart - 1]))
                keyStart--;
            index++; // step past '='

            string value;
            if (index < line.Length && span[index] == '"')
            {
                index++;
                var valueStart = index;
                while (index < line.Length && span[index] != '"')
                    index++;
                value = line.Slice(valueStart, index - valueStart).ToString();
                if (index < line.Length)
                    index++; // step past the closing quote
            }
            else
            {
                var valueStart = index;
                while (index < line.Length && !char.IsWhiteSpace(span[index]))
                    index++;
                value = line.Slice(valueStart, index - valueStart).ToString();
            }

            var key = line.Slice(keyStart, keyEnd - keyStart).ToString();
            if (key.Length > 0 && value.Length > 0)
                yield return (key, value);
        }
    }

    /// <summary>Turns one line of a playlist - a URL, an absolute path, or a path relative to the playlist -
    /// into a playable location, or null when it names nothing this player can reach. Shared with the other
    /// playlist formats so every format agrees on what a location means.</summary>
    internal static string? Resolve(string value, Uri source, bool local)
    {
        if (LinkValidator.TryGetHttpUrl(value, out var remote))
            return remote.AbsoluteUri;
        if (Uri.TryCreate(value, UriKind.Absolute, out var fileUri) && fileUri.IsFile)
            return File.Exists(fileUri.LocalPath) ? Path.GetFullPath(fileUri.LocalPath) : null;
        if (!local)
            return Uri.TryCreate(source, value, out var relative)
                && relative.Scheme is "http" or "https" ? relative.AbsoluteUri : null;

        try
        {
            var folder = Path.GetDirectoryName(source.LocalPath) ?? string.Empty;
            var path = Path.IsPathFullyQualified(value) ? value : Path.Combine(folder, value);
            return File.Exists(path) ? Path.GetFullPath(path) : null;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException
            or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>Decodes playlist bytes to text, honouring a UTF-8/UTF-16 byte-order mark and falling back from
    /// strict UTF-8 to Latin-1 for the odd list that is neither. Shared with the other playlist formats.</summary>
    internal static string Decode(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble))
            return Encoding.UTF8.GetString(bytes, Encoding.UTF8.Preamble.Length, bytes.Length - Encoding.UTF8.Preamble.Length);
        if (bytes.AsSpan().StartsWith(Encoding.Unicode.Preamble))
            return Encoding.Unicode.GetString(bytes, Encoding.Unicode.Preamble.Length, bytes.Length - Encoding.Unicode.Preamble.Length);
        if (bytes.AsSpan().StartsWith(Encoding.BigEndianUnicode.Preamble))
            return Encoding.BigEndianUnicode.GetString(bytes, Encoding.BigEndianUnicode.Preamble.Length, bytes.Length - Encoding.BigEndianUnicode.Preamble.Length);
        try { return new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { return Encoding.Latin1.GetString(bytes); }
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
    }
}
