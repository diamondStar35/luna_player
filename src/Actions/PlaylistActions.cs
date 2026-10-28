namespace LunaPlayer.Actions;

internal static class PlaylistActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Command opening the saved playlist manager.
        new(ActionId.MyPlaylists, Tr("My Playlists...")),
        // Translators: Command creating an empty named playlist.
        new(ActionId.CreatePlaylist, Tr("Create Playlist...")),
        // Translators: Command saving current entries with a name and description into My Playlists.
        new(ActionId.SaveCurrentPlaylist, Tr("Save Current Playlist...")),
        // Translators: Command exporting current entries to JSON, M3U or M3U8.
        new(ActionId.ExportPlaylist, Tr("Save Playlist As...")),
        // Translators: Command appending multiple files to the playback list.
        new(ActionId.AddPlaylistFiles, Tr("Add Files..."), new("f", ShortcutModifiers.Control | ShortcutModifiers.Alt)),
        // Translators: Command appending a folder reference to the playback list.
        new(ActionId.AddPlaylistFolder, Tr("Add Folder..."), new("d", ShortcutModifiers.Control | ShortcutModifiers.Alt)),
    ];
}
