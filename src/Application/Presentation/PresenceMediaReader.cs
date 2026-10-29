using LunaPlayer.Discord;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.YouTube;

namespace LunaPlayer.Application.Presentation;

/// <summary>Maps application-thread playback state to a shareable snapshot. Stopped entries and raw
/// stream addresses are never mistaken for loaded media and display titles.</summary>
internal static class PresenceMediaReader
{
    internal static PresenceMedia? Read(Player player, MediaTagsCache tagsCache, Spotify.Playback.Sessions spotify,
        string untitled, bool details, YouTubeResult? youTube = null, Radio.Station? station = null)
    {
        if (!player.IsLoaded && !(spotify.IsActive && spotify.CurrentTrack is not null)) return null;
        var track = spotify.IsActive ? spotify.CurrentTrack : null;
        var source = player.CurrentSource ?? player.CurrentPath;
        var video = !spotify.IsActive && player.HasVideo;
        var kind = track is not null ? SourceKind.Spotify : player.InRadioSession ? SourceKind.Radio
            : PublicLinks.Catalog(source)?.VideoId is not null ? SourceKind.YouTube
            : LinkValidator.IsHttpUrl(player.CurrentPath) ? SourceKind.Stream
            : player.AudiobookMode && !video ? SourceKind.Book : SourceKind.Local;
        var media = new PresenceMedia(track?.Uri ?? player.CurrentItemId ?? string.Empty,
            string.Empty, string.Empty, null,
            spotify.IsActive ? spotify.IsPlaying : player.IsPlaying, video,
            spotify.IsActive ? spotify.Elapsed : player.Elapsed,
            spotify.IsActive ? spotify.Duration : player.Duration, player.Speed) { Kind = kind };
        if (!details) return media;
        if (track is not null)
            return media with { Title = track.Name, Artist = track.Artists, PublicUrl = spotify.CurrentLink,
                ArtworkUrl = track.ArtworkUrl };

        var tags = tagsCache.Original(player);
        var live = player.CurrentMetadata.Display;
        var title = kind switch
        {
            SourceKind.Radio => First(station?.Name, player.CurrentTitle),
            SourceKind.Book => First(tags.Album, live.Album, tags.Title, live.Title),
            SourceKind.Local => First(tags.Title, live.Title),
            _ => First(tags.Title, live.Title, player.CurrentTitle),
        };
        if (string.IsNullOrWhiteSpace(title))
            title = kind is SourceKind.Local or SourceKind.Book
                ? Path.GetFileNameWithoutExtension(player.CurrentPath) ?? string.Empty
                : untitled;
        var artist = First(tags.Artist, live.Artist);
        if (kind == SourceKind.YouTube && youTube is { } item && item.Url == player.CurrentSource)
        {
            title = First(item.Title, title);
            artist = First(item.Author, artist);
        }
        // mpv can use its input address as a title; sharing details must not publish a path or token.
        if (Uri.TryCreate(title, UriKind.Absolute, out _) || title.StartsWith(@"\\", StringComparison.Ordinal)) title = untitled;
        return media with
        {
            Title = title,
            Artist = kind == SourceKind.Book ? string.Empty : artist,
            Chapter = kind == SourceKind.Book && player.CurrentChapter is { } chapter ? Player.ChapterTitle(chapter) : null,
            PublicUrl = kind == SourceKind.Radio ? player.CurrentPath : source,
            ArtworkUrl = kind == SourceKind.Radio ? station?.Favicon : null,
        };
    }

    private static string First(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}
