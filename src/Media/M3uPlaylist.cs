using System.Net;
using System.Text;

namespace LunaPlayer.Media;

/// <summary>One playable location and the optional name supplied for it by an extended M3U file.</summary>
internal readonly record struct M3uEntry(string Location, string? Title);

/// <summary>The result of reading an M3U file. HLS manifests are handed intact to mpv rather than treating
/// their media segments as separate songs.</summary>
internal sealed record M3uPlaylistResult(IReadOnlyList<M3uEntry> Entries, bool IsHls, string? Error)
{
    internal static M3uPlaylistResult Failure(string error) => new([], false, error);
}

internal static class M3uPlaylist
{
    private const int MaximumBytes = 8 * 1024 * 1024;
    private static readonly HttpClient Client = CreateClient();

    internal static M3uPlaylistResult ReadLocal(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length > MaximumBytes)
                return M3uPlaylistResult.Failure(Tr("The playlist file is too large."));
            var root = new Uri(Path.GetFullPath(path));
            return Parse(Decode(bytes), root, local: true);
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
                    return M3uPlaylistResult.Failure(Tr("The network playlist is too large."));
                memory.Write(buffer, 0, read);
            }
            return Parse(Decode(memory.ToArray()), new Uri(address), local: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return M3uPlaylistResult.Failure(Tr("The playlist request timed out."));
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException
            or UriFormatException or ArgumentException)
        {
            return M3uPlaylistResult.Failure(exception.Message);
        }
    }

    private static M3uPlaylistResult Parse(string text, Uri source, bool local)
    {
        var entries = new List<M3uEntry>();
        string? title = null;
        var isHls = false;
        using var reader = new StringReader(text);
        while (reader.ReadLine() is string rawLine)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;
            if (line.StartsWith("#EXT-X-", StringComparison.OrdinalIgnoreCase))
            {
                isHls = true;
                continue;
            }
            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                var comma = line.IndexOf(',');
                title = comma >= 0 && comma + 1 < line.Length ? line[(comma + 1)..].Trim() : null;
                continue;
            }
            if (line[0] == '#')
                continue;

            var location = Resolve(line, source, local);
            if (location is not null)
                entries.Add(new(location, string.IsNullOrWhiteSpace(title) ? null : title));
            title = null;
        }
        return new(entries, isHls, null);
    }

    private static string? Resolve(string value, Uri source, bool local)
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

    private static string Decode(byte[] bytes)
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
