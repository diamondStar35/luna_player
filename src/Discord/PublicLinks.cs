using System.Net;
using System.Net.Sockets;

namespace LunaPlayer.Discord;

/// <summary>Filters non-catalog addresses conservatively before publishing them to Discord.</summary>
internal static class PublicLinks
{
    internal sealed record Link(string Url, string? VideoId = null);
    internal static Link? Catalog(string? source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || uri.UserInfo.Length > 0 || !uri.IsDefaultPort) return null;
        var host = uri.IdnHost.ToLowerInvariant();
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? id = null;
        if (host is "youtu.be" or "www.youtu.be") id = parts.FirstOrDefault();
        else if (host is "youtube.com" or "www.youtube.com" or "m.youtube.com" or "music.youtube.com")
        {
            if (parts.Length == 2 && parts[0] is "shorts" or "embed" or "v" or "live") id = parts[1];
            else if (uri.AbsolutePath == "/watch")
                id = uri.Query.TrimStart('?').Split('&').FirstOrDefault(p => p.StartsWith("v=", StringComparison.Ordinal))?[2..];
        }
        else if (host == "open.spotify.com" && parts.Length == 2 && parts[0] == "track"
            && parts[1].Length == 22 && parts[1].All(char.IsAsciiLetterOrDigit))
            return new($"https://open.spotify.com/track/{parts[1]}");
        return id is { Length: 11 } && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
            ? new($"https://www.youtube.com/watch?v={id}", id) : null;
    }

    // Public stream addresses are shared intact. Stripping an authentication parameter would create
    // a broken button, so omit an uncertain address instead. No DNS or HTTP requests run on the UI thread.
    internal static string? Stream(string? source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
            || uri.UserInfo.Length > 0 || uri.IsLoopback || uri.HostNameType == UriHostNameType.Unknown) return null;
        var host = uri.IdnHost.TrimEnd('.');
        if (uri.HostNameType == UriHostNameType.Dns && (!host.Contains('.') || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".home", StringComparison.OrdinalIgnoreCase))) return null;
        if (IPAddress.TryParse(host.Trim('[', ']'), out var address))
        {
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            var bytes = address.GetAddressBytes();
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal
                    || (bytes[0] & 0xfe) == 0xfc || bytes.All(b => b == 0)) return null;
            }
            else if (bytes[0] is 0 or 10 or 127 || bytes[0] >= 224
                || bytes[0] == 169 && bytes[1] == 254 || bytes[0] == 192 && bytes[1] == 168
                || bytes[0] == 172 && bytes[1] is >= 16 and <= 31
                || bytes[0] == 100 && bytes[1] is >= 64 and <= 127) return null;
        }
        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var key = Uri.UnescapeDataString(parameter.Split('=', 2)[0]).ToLowerInvariant();
            if (key is not ("id" or "station" or "mount" or "type" or "format" or "codec" or "bitrate" or "quality" or "channel" or "size"))
                return null;
        }
        var parts = Uri.UnescapeDataString(uri.AbsolutePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && parts[0].ToLowerInvariant() is "live" or "movie" or "series") return null;
        return uri.GetLeftPart(UriPartial.Path) + uri.Query;
    }
}
