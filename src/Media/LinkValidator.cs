using System.Net;

namespace LunaPlayer.Media;

/// <summary>What a link points at, as far as the player can tell from the link alone.</summary>
internal enum LinkKind
{
    /// <summary>Not a link the player can do anything with: not http, or a YouTube address that names
    /// neither a video, a playlist nor a channel.</summary>
    Invalid,
    /// <summary>An http address that is not YouTube, played as a network stream.</summary>
    Stream,
    /// <summary>A YouTube address naming a single video.</summary>
    Video,
    /// <summary>A YouTube address naming a playlist.</summary>
    Playlist,
    /// <summary>A YouTube address naming a channel.</summary>
    Channel,
}

/// <summary>What <see cref="LinkValidator.Parse"/> worked out about a link.</summary>
/// <param name="Raw">The link as the user gave it, trimmed.</param>
/// <param name="IsHttp">Whether it is an absolute http or https address with a host.</param>
/// <param name="IsYouTube">Whether that host is one of YouTube's.</param>
/// <param name="Kind">The single kind the link is treated as when the user has not said otherwise.</param>
/// <param name="HasVideo">Whether a video can be played from it. A link can carry both a video and a
/// playlist, which is why this is separate from <paramref name="Kind"/>: the caller has to be able to ask
/// what is available before choosing which to use.</param>
/// <param name="HasPlaylist">Whether a playlist can be opened from it.</param>
internal readonly record struct LinkInfo(
    string Raw,
    bool IsHttp,
    bool IsYouTube,
    LinkKind Kind,
    bool HasVideo,
    bool HasPlaylist);

/// <summary>Decides what a link the user typed or pasted actually is, without going near the network.</summary>
///
/// <remarks>
/// A port of the Python player's <c>youtube/link_validator.py</c>, kept deliberately faithful to it: the
/// YouTube rules below are the ones the original arrived at against real addresses, and a link the two
/// players disagree about is a link one of them gets wrong. It also replaces that project's second, looser
/// classifier in <c>youtube/ui_utils.py</c>, which guessed the same thing from substrings of the address.
///
/// Nothing here confirms that a video exists or is playable. That needs yt-dlp and a request; this only
/// rules out the links that cannot work, so the player can refuse them without making the user wait.
/// </remarks>
internal static class LinkValidator
{
    /// <summary>The path segments YouTube puts a video id after.</summary>
    private static readonly string[] VideoSegments = ["shorts", "embed", "live", "v"];

    /// <summary>The path segments YouTube puts a channel name or id after.</summary>
    private static readonly string[] ChannelSegments = ["channel", "c", "user"];

    /// <summary>Everything the player can tell about <paramref name="value"/> from the text of it.</summary>
    internal static LinkInfo Parse(string? value)
    {
        var raw = (value ?? string.Empty).Trim();
        if (!TryGetHttpUrl(raw, out var uri))
            return new LinkInfo(raw, IsHttp: false, IsYouTube: false, LinkKind.Invalid, HasVideo: false, HasPlaylist: false);
        if (!IsYouTubeHost(Host(uri)))
            return new LinkInfo(raw, IsHttp: true, IsYouTube: false, LinkKind.Stream, HasVideo: false, HasPlaylist: false);
        var (kind, hasVideo, hasPlaylist) = YouTubeMeta(uri);
        return new LinkInfo(raw, IsHttp: true, IsYouTube: true, kind, hasVideo, hasPlaylist);
    }

    /// <summary>Whether a string is an absolute http or https address with a host. This is the only test the
    /// player applies to a plain network stream, which it hands to mpv to make sense of.</summary>
    internal static bool IsHttpUrl(string? value) => TryGetHttpUrl(value, out _);

    /// <summary>Whether a string is an address on one of YouTube's hosts, whatever it names there.</summary>
    internal static bool IsYouTubeUrl(string? value) => Parse(value).IsYouTube;

    /// <summary>The parsed form of an http or https address, for a caller that needs its parts rather than
    /// just a yes or no.</summary>
    internal static bool TryGetHttpUrl(string? value, out Uri uri)
    {
        if (Uri.TryCreate((value ?? string.Empty).Trim(), UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps)
            && parsed.Authority.Length > 0)
        {
            uri = parsed;
            return true;
        }
        uri = null!;
        return false;
    }

    /// <summary>The host, lower-cased and without the trailing dot a fully qualified name may carry. Uri
    /// already drops the user information and the port, which the Python original had to strip by hand.
    /// </summary>
    private static string Host(Uri uri) => uri.Host.TrimEnd('.');

    /// <summary>Whether a host belongs to YouTube. Matching the suffix rather than a list covers
    /// <c>www</c>, <c>m</c> and <c>music</c> without naming them, and a host such as
    /// <c>youtube.com.example.com</c> does not end in <c>.youtube.com</c>, so it is not mistaken for one.
    /// </summary>
    private static bool IsYouTubeHost(string host)
        => host is "youtu.be" or "www.youtu.be" or "youtube.com"
            || host.EndsWith(".youtube.com", StringComparison.Ordinal);

    /// <summary>Reads a YouTube address. The order of these tests is the order the Python player uses, and it
    /// matters: a link carrying both a video and a playlist reports <see cref="LinkKind.Video"/>, so a caller
    /// that has not been told which the user wants plays the video rather than opening the whole list.
    /// </summary>
    private static (LinkKind Kind, bool HasVideo, bool HasPlaylist) YouTubeMeta(Uri uri)
    {
        var parts = PathSegments(uri);
        var hasPlaylist = QueryValue(uri, "list").Length > 0;

        // A youtu.be address holds the video id in the path and nothing else, so whether it names a video is
        // simply whether it has a path at all.
        if (Host(uri) is "youtu.be" or "www.youtu.be")
            return parts.Length > 0
                ? (LinkKind.Video, true, hasPlaylist)
                : (LinkKind.Invalid, false, hasPlaylist);

        var hasVideo = QueryValue(uri, "v").Length > 0 || HasSegmentValue(parts, VideoSegments);
        if (hasVideo)
            return (LinkKind.Video, true, hasPlaylist);
        if (HasSegmentValue(parts, ChannelSegments) || (parts.Length > 0 && parts[0].StartsWith('@')))
            return (LinkKind.Channel, false, hasPlaylist);
        // The Python player tests /playlist with a list id before testing a list id on its own. Both answer
        // the same, so one test does for both.
        if (hasPlaylist)
            return (LinkKind.Playlist, false, true);
        // A /watch that carries no v= names nothing to play.
        if (parts.Length > 0 && parts[0] == "watch")
            return (LinkKind.Invalid, false, false);
        // Anything else on a YouTube host - the bare domain among them - is left as a video with nothing to
        // play, as the Python player leaves it. Callers decide on HasVideo and HasPlaylist rather than on
        // Kind alone, so this reports "no video and no playlist" to every one of them.
        return (LinkKind.Video, false, false);
    }

    /// <summary>Whether the first path segment is one of <paramref name="segments"/> and is followed by
    /// something. Matched exactly, as the Python player matches it: YouTube's own addresses are lower case.
    /// </summary>
    private static bool HasSegmentValue(string[] parts, string[] segments)
        => parts.Length >= 2
            && Array.IndexOf(segments, parts[0]) >= 0
            && parts[1].Trim().Length > 0;

    /// <summary>The non-empty path segments, still percent-encoded, as the Python original leaves them.
    /// </summary>
    private static string[] PathSegments(Uri uri)
        => uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The first non-blank value given for a query parameter, decoded and trimmed, or an empty
    /// string when there is none. Blank values are skipped rather than returned, which is what Python's
    /// parse_qs does with them by default.</summary>
    private static string QueryValue(Uri uri, string name)
    {
        var query = uri.Query;
        if (query.Length <= 1)
            return string.Empty;
        foreach (var pair in query[1..].Split('&'))
        {
            var separator = pair.IndexOf('=');
            if (separator < 0 || !pair.AsSpan(0, separator).SequenceEqual(name))
                continue;
            var value = WebUtility.UrlDecode(pair[(separator + 1)..]).Trim();
            if (value.Length > 0)
                return value;
        }
        return string.Empty;
    }
}
