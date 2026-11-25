using System.Text;

namespace LunaPlayer.Subsonic.Playback;

internal enum MediaKind { Song, Podcast, Radio }
internal sealed record Reference(Guid ServerId, string Id, MediaKind Kind = MediaKind.Song)
{
    private const string Prefix = "luna-subsonic://";
    // Hex keeps opaque IDs distinct even in legacy path collections that compare without case.
    internal string Address => Prefix + ServerId.ToString("N") + "/" + Kind.ToString().ToLowerInvariant()
        + "/" + Convert.ToHexStringLower(Encoding.UTF8.GetBytes(Id));
    internal static bool IsAddress(string? value) => value?.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == true;
    internal static Reference? Parse(string? address)
    {
        if (!IsAddress(address)) return null;
        var parts = address![Prefix.Length..].Split('/');
        if (parts.Length != 3 || !Guid.TryParseExact(parts[0], "N", out var server) || server == Guid.Empty
            || !Enum.TryParse<MediaKind>(parts[1], true, out var kind) || !Enum.IsDefined(kind)) return null;
        try
        {
            var id = new UTF8Encoding(false, true).GetString(Convert.FromHexString(parts[2]));
            return string.IsNullOrWhiteSpace(id) ? null : new(server, id, kind);
        }
        catch (Exception error) when (error is FormatException or DecoderFallbackException) { return null; }
    }
}
