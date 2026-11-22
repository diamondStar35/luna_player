namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string Playlists =>
        // Translators: Server playlists. Used in the server playlist editor and queue commands.
        Tr("Server playlists");
    internal static string AddRemotePlaylist =>
        // Translators: Add to server playlist. Used in the server playlist editor and queue commands.
        Tr("Add to server playlist...");
    internal static string CreatePlaylist =>
        // Translators: Create server playlist. Used in the server playlist editor and queue commands.
        Tr("Create server playlist...");
    internal static string Rename =>
        // Translators: Rename. Used in the server playlist editor and queue commands.
        Tr("Rename...");
    internal static string Description =>
        // Translators: Description. Used in the server playlist editor and queue commands.
        Tr("Description");
    internal static string Public =>
        // Translators: Public playlist. Used in the server playlist editor and queue commands.
        Tr("Public playlist");
    internal static string Up =>
        // Translators: Move up. Used in the server playlist editor and queue commands.
        Tr("Move up");
    internal static string Down =>
        // Translators: Move down. Used in the server playlist editor and queue commands.
        Tr("Move down");
    internal static string AddSelection =>
        // Translators: Button in the server playlist editor that opens a searchable song picker.
        Tr("Add songs...");
    internal static string ServerSaveQueue =>
        // Translators: Save queue to server. Used in the server playlist editor and queue commands.
        Tr("Save queue to server");
    internal static string ServerLoadQueue =>
        // Translators: Load queue from server. Used in the server playlist editor and queue commands.
        Tr("Load queue from server");
    internal static string SelectPlaylist =>
        // Translators: Choose a server playlist. Used in the server playlist editor and queue commands.
        Tr("Choose a server playlist");
    internal static string RemovePlaylist =>
        // Translators: Would you like to remove this server playlist? Used in the server playlist editor and queue commands.
        Tr("Would you like to remove this server playlist?");
    internal static string Owner =>
        // Translators: Owner. Used in the server playlist editor and queue commands.
        Tr("Owner");
    internal static string PlaylistChanged =>
        // Translators: Save error in the server playlist editor when another client changed the same playlist.
        Tr("This playlist changed on the server. Reopen it before saving your changes.");
    internal static string QueueSubset =>
        // Translators: Confirmation before saving a mixed Luna playlist to one server's play queue.
        Tr("Only songs from the selected server can be saved in its queue. Continue?");
    internal static string PlaylistHelp =>
        // Translators: F1 help for editing a playlist stored on a music server.
        Tr("Give the playlist a name and optional description. Public playlists can be visible to other users on the server. Add songs opens a searchable song picker with standard multiple selection. Shift+Up and Shift+Down select a range. To select separate songs, hold Ctrl while moving through the list and use Ctrl+Space to toggle each song. Add selected songs adds the selection from the current results to this draft. To choose songs from another search, add this selection and open Add songs again. In the playlist itself, select one or more items to remove them or move them using Shift+Up and Shift+Down. Removing an item does not delete the media file. Save writes the playlist to the server; Cancel discards this draft. Reopen the playlist if another client changed it while you were editing.");
}
