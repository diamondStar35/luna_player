namespace LunaPlayer.Actions;

/// <summary>Browser bindings have their own scope, so familiar keys do not replace player shortcuts.</summary>
internal static class MusicServerActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Browser shortcut that opens selected collections or plays individual songs.
        new(ActionId.ServerPlay, Tr("Open collections or play selected songs"), new("enter")),
        // Translators: Browser shortcut showing details for a song, or playing an album or artist collection.
        new(ActionId.ServerDetails, Tr("Song details or play selected collections"), new("enter", ShortcutModifiers.Shift)),
        // Translators: Browser shortcut opening the rating dialog for selected server songs.
        new(ActionId.ServerRate, Tr("Set song rating"), new("r", ShortcutModifiers.Control)),
        // Translators: Browser shortcut showing lyrics for the selected song.
        new(ActionId.ServerLyrics, Tr("Song lyrics"), new("l", ShortcutModifiers.Control)),
        // Translators: Browser shortcut bookmarking the current playback position on the selected server.
        new(ActionId.ServerBookmark, Tr("Bookmark current position"), new("b", ShortcutModifiers.Control)),
        // Translators: Browser shortcut creating a saved playlist on the selected server.
        new(ActionId.ServerCreatePlaylist, Tr("Create server playlist"), new("n", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
        // Translators: Shortcut preference for appending selected server items to Luna's current playback list without starting playback.
        new(ActionId.ServerAppend, Tr("Add selected server items to current playlist"), new("enter", ShortcutModifiers.Control)),
        // Translators: Shortcut preference for downloading selected music-server files with saved options.
        new(ActionId.ServerDirectDownload, Tr("Direct download of selected server items"), new("d", ShortcutModifiers.Control)),
        // Translators: Shortcut preference for opening download options for selected server files.
        new(ActionId.ServerDownload, Tr("Download options for selected server items"), new("d", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
        // Translators: Shortcut preference for adding selected server files to Luna's shared download queue.
        new(ActionId.ServerEnqueue, Tr("Queue selected server downloads"), new("q", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
        // Translators: Shortcut preference for focusing the music-server search field.
        new(ActionId.ServerSearch, Tr("Focus server search"), new("f", ShortcutModifiers.Control)),
        // Translators: Shortcut preference for refreshing the current music-server browsing location.
        new(ActionId.ServerRefresh, Tr("Refresh server items"), new("f5")),
        // Translators: Shortcut preference for going back in the music-server browser.
        new(ActionId.ServerBack, Tr("Back in server browser"), new("left", ShortcutModifiers.Alt)),
        // Translators: Shortcut preference for opening Luna's shared download queue from the server browser.
        new(ActionId.ServerQueue, Tr("Open download queue from server browser"), new("q", ShortcutModifiers.Control)),
    ];
}
