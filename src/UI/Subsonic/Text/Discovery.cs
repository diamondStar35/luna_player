namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string SimilarArtists =>
        // Translators: Context-menu action that lists related artists for the selected music-server artist.
        Tr("Similar artists");
    internal static string RecentlyAdded =>
        // Translators: Browser category listing albums most recently added to a music server.
        Tr("Recently added");
    internal static string InstantMix =>
        // Translators: Context-menu action that creates a list of similar songs from a song, album or artist.
        Tr("Instant mix");
    internal static string SendPlaylist =>
        // Translators: Server-menu action to create a saved server playlist from Luna's current opened songs.
        Tr("Send current playlist to server...");
    internal static string CurrentPlaylist =>
        // Translators: Initial editable name when sending Luna's current playlist to a music server.
        Tr("Current playlist");
    internal static string PlaylistSubset =>
        // Translators: Confirmation when a current playlist contains local media or songs from other servers.
        Tr("Only songs from the selected server can be included in its playlist. Continue?");
    internal static string RemoveFromPlaylist =>
        // Translators: Context-menu action removing selected occurrences from the displayed server playlist.
        Tr("Remove from playlist");
    internal static string ConfirmRemoveEntries =>
        // Translators: Confirmation before removing selected song occurrences from a server playlist.
        Tr("Would you like to remove the selected items from this playlist?");
    internal static string EmptyExpansion =>
        // Translators: Error after a selected music-server collection no longer resolves to any songs.
        Tr("No playable songs were found. The server library may have changed. Refresh the list and select the items again.");
    internal static string InformationLink =>
        // Translators: Button in music-server artist or album information that opens its source web page.
        Tr("Open information page");
    internal static string BrowseSettings =>
        // Translators: Tab of a music-server profile containing visible sections and library selection settings.
        Tr("Browsing");
    internal static string VisibleSections =>
        // Translators: Checklist of categories to show in the Music servers browser.
        Tr("Visible browser sections");
    internal static string RestrictLibraries =>
        // Translators: Checkbox that restricts music-server browsing to the checked libraries.
        Tr("Only include selected libraries");
    internal static string SelectLibraries =>
        // Translators: Label of the music-server library checklist in server browsing settings.
        Tr("Libraries");
    internal static string LoadLibraries =>
        // Translators: Button loading the library checklist using the connection details currently entered.
        Tr("Load libraries");
    internal static string LibrariesLoaded(int count) =>
        // Translators: Success message after loading the library checklist in music-server settings. {count} is how many libraries were returned.
        TrFormat("Loaded {count} libraries.", count);
    internal static string NeedLibraries =>
        // Translators: Validation error when library restriction is enabled without any checked libraries.
        Tr("Select at least one library, or turn off the library restriction.");
    internal static string NeedSections =>
        // Translators: Validation error when all browser categories have been unchecked.
        Tr("Select at least one browser section.");
    internal static string RefreshMetadata =>
        // Translators: Per-server preference to refresh metadata for its songs in Luna's restored current playlist.
        Tr("Refresh current playlist metadata on startup");
}
