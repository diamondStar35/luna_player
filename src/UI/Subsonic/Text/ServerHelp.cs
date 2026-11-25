namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string HelpSaveServer =>
        // Translators: F1 help for Save in the saved music-server editor.
        Tr("Save the settings on every tab for this account and close the editor. You do not need a successful connection test to save. Invalid fields are reported before anything is saved.");
    internal static string HelpCancelServer =>
        // Translators: F1 help for Cancel in the saved music-server editor.
        Tr("Close the editor without saving these changes. Luna asks before discarding a changed account. Testing the connection does not save changes.");
    internal static string HelpConnection =>
        // Translators: F1 help for the Connection tab in the saved music-server editor.
        Tr("Enter a name, the server address and your login details. Test connection checks them without saving. Move to a setting and press F1 to hear help for that setting.");
    internal static string HelpPlayback =>
        // Translators: F1 help for the Playback tab in the saved music-server editor.
        Tr("Choose how this account streams music and whether Luna reports listening activity or synchronizes its playback queue. These settings do not change downloaded files.");
    internal static string HelpBrowsing =>
        // Translators: F1 help for the Browsing tab in the saved music-server editor.
        Tr("Choose which sections and music libraries appear when browsing this account. The checklists use Space to check or uncheck an entry. These choices do not change permissions on the server.");
    internal static string HelpDownloads =>
        // Translators: F1 help for the Downloads tab in the saved music-server editor.
        Tr("Choose where original files from this account are downloaded and whether to organize them by artist and album. Playback transcoding does not affect these downloads.");
    internal static string HelpAdvanced =>
        // Translators: F1 help for the Advanced tab in the saved music-server editor.
        Tr("Adjust background updates, cache lifetimes, search delays and request timeouts for this account. The defaults suit most connections. Custom HTTP headers are only needed when your server or proxy requires them.");
    internal static string HelpAudioMuse =>
        // Translators: F1 help for the AudioMuse tab in the saved music-server editor. Keep AudioMuse unchanged.
        Tr("AudioMuse is an optional, separate recommendation service. Configure it only if you have access to one. Leave its address empty to use the music server without AudioMuse.");
    internal static string HelpServerName =>
        // Translators: F1 help for Name on the server editor's Connection tab.
        Tr("Enter a name that helps you recognize this account in Luna's server list. It does not rename the server or change your login name.");
    internal static string HelpServerAddress =>
        // Translators: F1 help for Server address on the server editor's Connection tab. HTTPS is a protocol name.
        Tr("Enter the base address supplied by your server administrator, including any path used to reach it. Do not enter a song or stream link. An address without a protocol uses HTTPS.");
    internal static string HelpAuthentication =>
        // Translators: F1 help for Authentication on the server editor's Connection tab. API means application programming interface.
        Tr("Username and password uses normal token authentication. Choose Legacy password authentication only when your server requires it. API key requires a key supplied by a server that supports this login method.");
    internal static string HelpUsername =>
        // Translators: F1 help for Username on the server editor's Connection tab.
        Tr("Enter your account's login name on this music server. It may be different from your email address or the name you gave this saved connection.");
    internal static string HelpPassword =>
        // Translators: F1 help for Password on the server editor's Connection tab.
        Tr("Enter this account's password. Luna protects the saved password for your Windows account. Testing the connection does not save it.");
    internal static string HelpApiKey =>
        // Translators: F1 help for API key on the server editor's Connection tab.
        Tr("Enter the API key issued for this music-server account. This replaces username and password authentication and requires server support. Luna protects the saved key for your Windows account.");
    internal static string HelpTestConnection =>
        // Translators: F1 help for Test connection on the server editor's Connection tab.
        Tr("Check the address and credentials currently entered here without saving. A success message confirms access and loads available libraries into the Browsing tab. You can also save an account while the server is offline.");
    internal static string HelpStreamingMode =>
        // Translators: F1 help for Streaming mode on the server editor's Playback tab.
        Tr("Original requests the source file. Server default lets the server choose whether to transcode. Transcode requests a particular format and bitrate, which can reduce bandwidth and may change audio quality.");
    internal static string HelpStreamingFormat =>
        // Translators: F1 help for Format on the server editor's Playback tab.
        Tr("Choose the format to request when transcoding. The server must be able to produce that format. Use Original streaming mode if you want the source file without conversion.");
    internal static string HelpStreamingBitrate =>
        // Translators: F1 help for the maximum bitrate control on the server editor's Playback tab.
        Tr("Set the maximum requested playback bitrate in kilobits per second. Zero leaves the bitrate unrestricted by Luna. The server decides which conversion settings it supports.");
    internal static string HelpListening =>
        // Translators: F1 help for Report listening activity on the server editor's Playback tab.
        Tr("Report which song is playing and submit a completed listen after half the song or four minutes of listening, whichever comes first. Seeking forward does not count as listening. The server decides how to use these reports.");
    internal static string HelpQueueSync =>
        // Translators: F1 help for automatic queue synchronization on the server editor's Playback tab.
        Tr("Automatically exchange this account's songs, current item and position with its server queue. Other clients' changes can replace this account's entries in Luna. Local files and other accounts are kept. Leave this off to use Save queue and Load queue manually.");
    internal static string HelpRestrictLibraries =>
        // Translators: F1 help for Only include selected libraries on the server editor's Browsing tab.
        Tr("Limit library browsing and searches to the checked libraries. Load libraries first, then check at least one. Turning this off includes all libraries that your account can access.");
    internal static string HelpSelectLibraries =>
        // Translators: F1 help for the Libraries checklist on the server editor's Browsing tab.
        Tr("Use the arrow keys to select a library and Space to check or uncheck it. These checks apply when Only include selected libraries is enabled. They do not create libraries or change server access.");
    internal static string HelpLoadLibraries =>
        // Translators: F1 help for Load libraries on the server editor's Browsing tab.
        Tr("Read available libraries using the address and login details currently entered in Connection. This does not save the account. Test connection also loads these libraries.");
    internal static string HelpVisibleSections =>
        // Translators: F1 help for Visible browser sections on the server editor's Browsing tab.
        Tr("Choose which categories appear in the browser for this account. Use Space to check or uncheck a section and leave at least one checked. Hiding a section does not remove its content from the server.");
    internal static string HelpStartupMetadata =>
        // Translators: F1 help for startup metadata refresh on the server editor's Browsing tab.
        Tr("Refresh names and other metadata for this account's songs when Luna restores its current playlist. The refresh runs in the background and requires access to the server.");
    internal static string HelpDownloadFolder =>
        // Translators: F1 help for Download folder on the server editor's Downloads tab.
        Tr("Choose the default folder for original files downloaded from this account. An empty value uses Downloads, Luna Player. A download's options can override this destination for one operation.");
    internal static string HelpBrowseDownloads =>
        // Translators: F1 help for Browse on the server editor's Downloads tab.
        Tr("Open the folder picker to choose this account's default download destination. This chooses a folder on your computer, not a library on the server.");
    internal static string HelpAlbumFolders =>
        // Translators: F1 help for artist and album folder organization on the server editor's Downloads tab.
        Tr("Place downloaded files in artist and album subfolders beneath the download destination. Clear this to keep them directly in the chosen destination.");
    internal static string HelpHeaders =>
        // Translators: F1 help for Custom HTTP headers on the server editor's Advanced tab. Name: value is the required line format.
        Tr("Enter one HTTP header per line in the form Name: value. Use only headers required by this server or its proxy. Header values are protected with the account's saved credentials.");
    internal static string HelpAudioMuseAddress =>
        // Translators: F1 help for AudioMuse address on the server editor's AudioMuse tab.
        Tr("Enter the address of your AudioMuse service, including its protocol and any required path. This is separate from the music-server address. Leave it empty to disable AudioMuse for this account.");
    internal static string HelpAudioMuseToken =>
        // Translators: F1 help for the access token on the server editor's AudioMuse tab.
        Tr("Enter the access token supplied for your AudioMuse service. It is separate from the music-server password or API key and is protected for your Windows account.");
    internal static string HelpAudioMuseServer =>
        // Translators: F1 help for the optional music-server name on the server editor's AudioMuse tab.
        Tr("Enter the music-server name expected by your AudioMuse service if it manages several libraries. Leave this empty when the service does not require a name. It does not rename your saved connection.");
    internal static string HelpAudioMuseCount =>
        // Translators: F1 help for Number of results on the server editor's AudioMuse tab.
        Tr("Choose the default number of recommendation results, from one to five hundred. You can change this again when running a search or generating a playlist.");
}
