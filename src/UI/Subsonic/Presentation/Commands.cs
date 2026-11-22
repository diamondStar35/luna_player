using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.UI.Subsonic;

internal enum Command
{
    Play, Append, Direct, Download, Enqueue, Details, Lyrics, Similar, SimilarArtists, Top, Sonic, Path,
    Favorite, Unfavorite, Rate, CreatePlaylist, EditPlaylist, Remove, AddPlaylist, Rename, SendPlaylist,
    RemovePlaylistItems, Bookmark, SaveQueue, LoadQueue, Scan, FullScan, ScanStatus, Info, NewRadio,
    Subscribe, RefreshPodcasts, EpisodeDownload, EpisodeDelete, AiSearch, AiGenerate, Alchemy
}

internal sealed record CommandContext(Row[] Rows, Location Location, User? User, Capabilities? Capabilities,
    string Username, bool HasCurrentSong, bool HasPlaylist, bool HasAudioMuse);

internal static class CommandPolicy
{
    internal static bool Owns(Row row, User? user, string username)
        => row.Owner.Length == 0 || row.Owner == username || user?.AdminRole == true;

    internal static bool Allows(Command command, CommandContext context)
    {
        var rows = context.Rows; var user = context.User;
        var single = rows.Length == 1 ? rows[0] : null;
        var songs = rows.Length > 0 && rows.All(row => row.Kind is RowKind.Song or RowKind.Bookmark && row.Available);
        var expandable = rows.Length > 0 && rows.All(row => row.Playable
            || row.Available && row.Kind is RowKind.Artist or RowKind.Album or RowKind.Playlist);
        var favorite = rows.Length > 0 && rows.All(row => row.Available && row.Kind is RowKind.Song or RowKind.Bookmark or RowKind.Artist or RowKind.Album);
        var playlist = user?.AdminRole == true || user?.PlaylistRole != false;
        var sonic = context.Capabilities?.Extension("sonicSimilarity") == Support.Supported;
        return command switch
        {
            Command.Play or Command.Append => expandable && user?.StreamRole != false,
            Command.Direct or Command.Download or Command.Enqueue => expandable && rows.All(row => row.Kind != RowKind.Radio) && user?.DownloadRole != false,
            Command.Favorite => favorite && rows.Any(row => !row.Favorite),
            Command.Unfavorite => favorite && rows.Any(row => row.Favorite),
            Command.Rate => songs,
            Command.Details => single is not null && single.Kind is not RowKind.Unavailable,
            Command.Lyrics => single?.Kind is RowKind.Song or RowKind.Bookmark,
            Command.Top or Command.SimilarArtists => single?.Kind == RowKind.Artist,
            Command.Similar => single?.Kind is RowKind.Song or RowKind.Bookmark or RowKind.Album or RowKind.Artist,
            Command.Sonic => single?.Kind is RowKind.Song or RowKind.Bookmark && sonic,
            Command.Path => rows.Length == 2 && songs && sonic,
            Command.CreatePlaylist => playlist,
            Command.SendPlaylist => playlist && context.HasPlaylist,
            Command.AddPlaylist => playlist && expandable && rows.All(row => row.Kind is RowKind.Song or RowKind.Bookmark or RowKind.Artist or RowKind.Album or RowKind.Playlist),
            Command.EditPlaylist => single?.Kind == RowKind.Playlist && Owns(single, user, context.Username) && playlist,
            Command.RemovePlaylistItems => songs && rows.All(row => row.PlaylistIndex.HasValue)
                && context.Location.Container == RowKind.Playlist && playlist,
            Command.Rename or Command.Remove => single is not null && (single.Kind switch
            {
                RowKind.Playlist => playlist && Owns(single, user, context.Username),
                RowKind.Bookmark => true, RowKind.Radio => user?.AdminRole != false,
                RowKind.PodcastChannel => command == Command.Remove && user?.PodcastRole != false,
                _ => false
            }),
            Command.Bookmark => context.HasCurrentSong,
            Command.SaveQueue => context.HasPlaylist,
            Command.LoadQueue or Command.Info or Command.ScanStatus => true,
            Command.FullScan => user?.AdminRole != false && string.Equals(context.Capabilities?.Server.Type, "navidrome", StringComparison.OrdinalIgnoreCase),
            Command.Scan or Command.NewRadio => user?.AdminRole != false,
            Command.Subscribe or Command.RefreshPodcasts => user?.PodcastRole != false,
            Command.EpisodeDownload or Command.EpisodeDelete => single?.Kind == RowKind.Podcast && user?.PodcastRole != false,
            Command.AiSearch or Command.AiGenerate => context.HasAudioMuse,
            Command.Alchemy => context.HasAudioMuse && rows.Any(row => row.Kind is RowKind.Song or RowKind.Artist),
            _ => false
        };
    }
}
