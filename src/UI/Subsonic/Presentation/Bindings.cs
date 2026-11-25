using LunaPlayer.Actions;
using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.UI.Subsonic;

internal static class Bindings
{
    internal static bool Collections(Row[] rows) => rows.Any(row => row.Kind is RowKind.Artist or RowKind.Album or RowKind.Playlist);
    internal static Command Inspect(Row[] rows) => Collections(rows) ? Command.Play : Command.Details;
    internal static ActionId? ForMenu(Command command, Row[] rows) => command switch
    {
        Command.Play => Collections(rows) ? ActionId.ServerDetails : ActionId.ServerPlay,
        Command.Details when rows.Length == 1 && rows[0].Playable => ActionId.ServerDetails,
        Command.Rate => ActionId.ServerRate, Command.Lyrics => ActionId.ServerLyrics,
        Command.Bookmark => ActionId.ServerBookmark, Command.CreatePlaylist => ActionId.ServerCreatePlaylist,
        Command.Append => ActionId.ServerAppend, Command.Direct => ActionId.ServerDirectDownload,
        Command.Download => ActionId.ServerDownload, Command.Enqueue => ActionId.ServerEnqueue,
        _ => null
    };
}
