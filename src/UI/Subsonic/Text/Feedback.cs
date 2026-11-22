namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string QueueLoaded =>
        // Translators: Success message after loading the selected music server's saved play queue into Luna.
        Tr("The server queue was loaded.");
    internal static string CurrentPlaylistItemsAdded(int count) =>
        // Translators: Announcement after appending server songs to Luna's current playback list. {count} is how many songs were added.
        TrPluralFormat("Added {count} song to Luna's current playlist.", "Added {count} songs to Luna's current playlist.", count, count);
    internal static string DraftItemsAdded(int count, string name) =>
        // Translators: Announcement after adding songs to an unsaved server playlist draft. {count} is how many songs were added; {name} is the playlist's name.
        TrPluralFormat("Added {count} song to the draft of \"{name}\". Save the playlist to keep this change.", "Added {count} songs to the draft of \"{name}\". Save the playlist to keep this change.", count, count, name);
    internal static string NewDraftItemsAdded(int count) =>
        // Translators: Announcement after adding songs to a new server playlist draft whose name is still blank. {count} is how many songs were added.
        TrPluralFormat("Added {count} song to the new playlist draft. Name and save the playlist to keep this change.", "Added {count} songs to the new playlist draft. Name and save the playlist to keep this change.", count, count);
    internal static string ServerPlaylistItemsAdded(int count, string name) =>
        // Translators: Success message after adding songs to an existing saved server playlist. {count} is how many songs were added; {name} is the destination playlist name.
        TrPluralFormat("Added {count} song to the server playlist \"{name}\".", "Added {count} songs to the server playlist \"{name}\".", count, count, name);
    internal static string ServerPlaylistSaved(string name) =>
        // Translators: Success message after saving a server playlist. {name} is the playlist name written to the server.
        TrFormat("The server playlist \"{name}\" was saved.", name);
    internal static string AddSelectedSongs =>
        // Translators: Button that accepts the current song-picker selection in a playlist or recommendation editor.
        Tr("Add selected songs");
    internal static string IncludedSeedsAdded(int count) =>
        // Translators: Announcement after adding songs as positive AudioMuse recommendation seeds. {count} is how many songs were added.
        TrPluralFormat("Added {count} song to the recommendation's include list.", "Added {count} songs to the recommendation's include list.", count, count);
    internal static string ExcludedSeedsAdded(int count) =>
        // Translators: Announcement after adding songs as negative AudioMuse recommendation seeds. {count} is how many songs were added.
        TrPluralFormat("Added {count} song to the recommendation's exclude list.", "Added {count} songs to the recommendation's exclude list.", count, count);
    internal static string ChooseSongs =>
        // Translators: Validation error when Add selected songs is pressed in the song picker without selecting any songs.
        Tr("Select at least one song.");
    internal static string ScanStarted =>
        // Translators: Success message after a music server accepts a library scan request; the scan may still be running.
        Tr("Library scan started.");
    internal static string ScanInProgress =>
        // Translators: Informational message from the music-server scan status command.
        Tr("The library scan is in progress.");
    internal static string ScanFinished =>
        // Translators: Informational message from the music-server scan status command when no scan is active.
        Tr("No library scan is currently running.");
    internal static string PodcastRefreshStarted =>
        // Translators: Success message after the server accepts a request to refresh its podcast subscriptions.
        Tr("The podcast refresh was requested. Refresh the podcast list to check for new episodes.");
    internal static string EpisodeDownloadStarted =>
        // Translators: Success message when the server accepts a podcast download request. This downloads to the server, not this computer.
        Tr("The episode download was requested on the server. Refresh the episode list to check its status.");
    internal static string EpisodeDeleted =>
        // Translators: Success message after deleting a downloaded podcast episode from the music server.
        Tr("The downloaded episode was removed from the server.");
}
