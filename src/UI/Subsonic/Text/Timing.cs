namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string BackgroundUpdates =>
        // Translators: Group heading on the music-server editor's Advanced tab for automatic server updates.
        Tr("Background updates");
    internal static string CacheAndBrowsing =>
        // Translators: Group heading on the music-server editor's Advanced tab for cached server information and browsing delays.
        Tr("Cache and browsing");
    internal static string RequestTimeouts =>
        // Translators: Group heading on the music-server editor's Advanced tab for how long network requests may take.
        Tr("Request timeouts");
    internal static string QueuePollInterval =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. How often Luna checks the server's saved playback queue.
        Tr("Queue polling interval (seconds)");
    internal static string QueueSaveInterval =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. How often Luna saves the playing position during automatic queue synchronization.
        Tr("Queue position save interval (seconds)");
    internal static string PlaybackReportInterval =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. How often Luna reports the current playback position to a server supporting playback reports.
        Tr("Playback reporting interval (seconds)");
    internal static string ScanPollInterval =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. How often an open scan-status window checks the server.
        Tr("Scan status polling interval (milliseconds)");
    internal static string SynchronizationRetryInterval =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. Maximum delay between retries after background synchronization errors.
        Tr("Maximum synchronization retry delay (seconds)");
    internal static string ConnectionCacheDuration =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. Zero prevents reusing completed connection inspections.
        Tr("Connection information cache (seconds; 0 disables)");
    internal static string StreamCacheDuration =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. How long negotiated stream information is reused. Zero disables this cache.
        Tr("Stream negotiation cache (seconds; 0 disables)");
    internal static string LibraryCacheDuration =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. Cache used to determine which albums belong to selected libraries. Zero disables it.
        Tr("Library membership cache (seconds; 0 disables)");
    internal static string BrowserRefreshInterval =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. Refresh on reopening after the browser has been hidden this long. Zero disables automatic refresh.
        Tr("Refresh browser after being closed for (seconds; 0 disables)");
    internal static string SearchDelay =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. Delay after typing before a browser or song-picker search starts.
        Tr("Search delay (milliseconds; 0 searches immediately)");
    internal static string RequestTimeout =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. Timeout for normal music-server requests, excluding an established audio stream.
        Tr("Music server request timeout (seconds)");
    internal static string AudioMuseSearchTimeout =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. Timeout for searches and recommendations from the optional AudioMuse service. Keep AudioMuse unchanged.
        Tr("AudioMuse search timeout (seconds)");
    internal static string AudioMuseGenerationTimeout =>
        // Translators: Integer spin control on the music-server editor's Advanced tab. Timeout for playlist generation by the optional AudioMuse service. Keep AudioMuse unchanged.
        Tr("AudioMuse playlist generation timeout (seconds)");

    internal static string HelpQueuePollInterval =>
        // Translators: F1 help for Queue polling interval on the music-server editor's Advanced tab.
        Tr("Choose the time between background checks of the server's playback queue. The default is 5 seconds. This applies only when automatic queue synchronization is enabled. Local changes and scheduled position saves can check the queue sooner to avoid overwriting another client's changes.");
    internal static string HelpQueueSaveInterval =>
        // Translators: F1 help for Queue position save interval on the music-server editor's Advanced tab.
        Tr("Choose how often Luna saves the current song position while playing and leading automatic queue synchronization. The default is 5 seconds. Changes to the queue, playback state or position are sent on the next app tick without waiting for this interval. Manual Save queue also works immediately.");
    internal static string HelpPlaybackReportInterval =>
        // Translators: F1 help for Playback reporting interval on the music-server editor's Advanced tab.
        Tr("Choose how often Luna reports the current position when Report listening activity is enabled and the server supports playback reports. The default is 1 second. Pause, seek and track changes are reported on the next app tick. This interval does not change the listening time required to submit a completed listen.");
    internal static string HelpScanPollInterval =>
        // Translators: F1 help for Scan status polling interval on the music-server editor's Advanced tab.
        Tr("Choose how often the open scan-status window checks whether a library scan has finished. The default is 1500 milliseconds. This does not start scans or change how quickly the server scans its library.");
    internal static string HelpSynchronizationRetryInterval =>
        // Translators: F1 help for Maximum synchronization retry delay on the music-server editor's Advanced tab.
        Tr("After a background synchronization error, Luna increases the delay between retries up to this limit. The default maximum is 30 seconds. A successful request restores the normal update intervals.");
    internal static string HelpConnectionCacheDuration =>
        // Translators: F1 help for Connection information cache on the music-server editor's Advanced tab.
        Tr("Choose how long to reuse the server's library list and account information before reading them again. The default is 300 seconds, or 5 minutes. Zero disables reuse of completed inspections. Refresh always requests updated information, and saving account changes clears the old information.");
    internal static string HelpStreamCacheDuration =>
        // Translators: F1 help for Stream negotiation cache on the music-server editor's Advanced tab.
        Tr("Choose how long to reuse the server's streaming decision when returning to a song. The default is 300 seconds, or 5 minutes. This caches stream information, not audio files. Zero negotiates again each time. A server may expire its stream tokens sooner than a long cache setting; Refresh renews them.");
    internal static string HelpLibraryCacheDuration =>
        // Translators: F1 help for Library membership cache on the music-server editor's Advanced tab.
        Tr("Choose how long to remember which albums belong to the selected libraries when filtering artists and recommendations. The default is 1800 seconds, or 30 minutes. Zero reads this information each time it is needed. Refresh clears the cache.");
    internal static string HelpBrowserRefreshInterval =>
        // Translators: F1 help for automatic browser refresh on the music-server editor's Advanced tab.
        Tr("Refresh the browser when you reopen it after it has been closed for at least this many seconds. The default is 1800 seconds, or 30 minutes. Zero disables this automatic refresh. You can still use Refresh whenever you want updated content.");
    internal static string HelpSearchDelay =>
        // Translators: F1 help for Search delay on the music-server editor's Advanced tab.
        Tr("Choose how long Luna waits after you stop typing before searching this server in the browser or a playlist's song picker. The default is 300 milliseconds. Zero starts searches immediately. A longer delay avoids requests for words you have not finished typing.");
    internal static string HelpRequestTimeout =>
        // Translators: F1 help for Music server request timeout on the music-server editor's Advanced tab.
        Tr("Choose how long browsing, account operations and establishing downloads may take before Luna reports a timeout. The default is 30 seconds. This does not change playback connection timeouts or limit the length of an established download.");
    internal static string HelpAudioMuseSearchTimeout =>
        // Translators: F1 help for AudioMuse search timeout on the music-server editor's Advanced tab. Keep AudioMuse unchanged.
        Tr("Choose how long to wait for searches and recommendations from AudioMuse. The default is 60 seconds. This applies only when an AudioMuse service is configured for this account.");
    internal static string HelpAudioMuseGenerationTimeout =>
        // Translators: F1 help for AudioMuse playlist generation timeout on the music-server editor's Advanced tab. Keep AudioMuse unchanged.
        Tr("Choose how long to wait while AudioMuse generates a playlist. The default is 300 seconds, or 5 minutes. Generation can take longer than a normal search. This applies only when an AudioMuse service is configured for this account.");
}
