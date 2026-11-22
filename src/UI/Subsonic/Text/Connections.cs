namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string Server =>
        // Translators: Heading for the saved music-server connection editor and label of the account selector in the browser.
        Tr("Server");
    internal static string Manage =>
        // Translators: Button in the music-server browser that opens the list of saved server connections.
        Tr("Manage servers...");
    internal static string Address =>
        // Translators: Field label for the Subsonic server base URL, including any reverse-proxy path.
        Tr("Server address");
    internal static string Username =>
        // Translators: Account username field in the music-server connection editor.
        Tr("Username");
    internal static string Password =>
        // Translators: Password field in the music-server connection editor.
        Tr("Password");
    internal static string ApiKey =>
        // Translators: Secret API key field in the music-server connection editor. API is the protocol abbreviation.
        Tr("API key");
    internal static string Auth =>
        // Translators: Authentication method selector in the music-server connection editor.
        Tr("Authentication");
    internal static string TokenAuth =>
        // Translators: Authentication choice that sends a salted password hash instead of the password.
        Tr("Username and password");
    internal static string KeyAuth =>
        // Translators: Authentication choice for a server-issued API key.
        Tr("API key");
    internal static string LegacyAuth =>
        // Translators: Compatibility authentication choice for servers that require password authentication.
        Tr("Legacy password authentication");
    internal static string Connection =>
        // Translators: Tab containing the server address and credentials.
        Tr("Connection");
    internal static string Playback =>
        // Translators: Tab containing stream format, listening reports and queue synchronization settings.
        Tr("Playback");
    internal static string Downloads =>
        // Translators: Tab containing the default destination and folder organization for original-file downloads.
        Tr("Downloads");
    internal static string Advanced =>
        // Translators: Tab containing custom HTTP headers for a music-server connection.
        Tr("Advanced");
    internal static string Test =>
        // Translators: Button that checks connection credentials without saving the server profile.
        Tr("Test connection");
    internal static string Connected =>
        // Translators: Status shown when testing the music-server connection succeeds.
        Tr("Connection successful.");
    internal static string Original =>
        // Translators: Playback format choice that requests the original media without server transcoding.
        Tr("Original");
    internal static string ServerDefault =>
        // Translators: Playback format choice that leaves transcoding decisions to the music server.
        Tr("Server default");
    internal static string Transcode =>
        // Translators: Playback format choice that requests conversion on the music server.
        Tr("Transcode");
    internal static string Mode =>
        // Translators: Label for the streaming mode selector in the server playback settings.
        Tr("Streaming mode");
    internal static string Format =>
        // Translators: Label for the requested output codec when server transcoding is selected.
        Tr("Format");
    internal static string Bitrate =>
        // Translators: Maximum transcoding bitrate in kilobits per second; zero means no requested limit.
        Tr("Maximum bitrate (kbps; 0 means server default)");
    internal static string Report =>
        // Translators: Checkbox enabling playback reports and scrobbles to this music-server account.
        Tr("Report listening activity");
    internal static string Sync =>
        // Translators: Checkbox enabling automatic two-way play-queue synchronization with this server.
        Tr("Synchronize queue and position automatically");
    internal static string Headers =>
        // Translators: Multiline field for HTTP request headers, one header per line in Name: value format.
        Tr("Custom HTTP headers (one Name: value per line)");
    internal static string AudioMuseAddress =>
        // Translators: Optional URL of the separate AudioMuse recommendation service.
        Tr("AudioMuse address (optional)");
    internal static string AudioMuseToken =>
        // Translators: Secret access token for the optional AudioMuse service.
        Tr("AudioMuse token");
    internal static string AudioMuseServer =>
        // Translators: Optional server name used by AudioMuse to identify the music library.
        Tr("AudioMuse music server (optional)");
    internal static string ConfirmRemove =>
        // Translators: Confirmation before removing an account; only that server’s playback entries are removed and existing downloads keep running.
        Tr("Would you like to remove this server? Its current playlist entries will be removed and its playback will stop. Existing downloads will continue.");
    internal static string MissingServer =>
        // Translators: Error when a saved playlist references an account that has been removed.
        Tr("This item belongs to a server that is no longer configured.");
    internal static string Invalid =>
        // Translators: Error for invalid music-server connection fields.
        Tr("Check the server address, credentials and numeric settings.");
    internal static string InvalidAddress =>
        // Translators: Validation error for the saved music-server address field.
        Tr("Enter a valid HTTP or HTTPS server address.");
    internal static string ConnectionSuccessful =>
        // Translators: Success message after Test connection in the music-server profile editor.
        Tr("The connection was successful.");
    internal static string NameRequired =>
        // Translators: Validation error for the name field when saving a music-server profile.
        Tr("Enter a name for this server.");
    internal static string ConnectionHelp =>
        // Translators: F1 help for saved music-server settings. OpenSubsonic, AudioMuse, API and HTTPS are service/protocol names.
        Tr("Connection: enter a name, the server's base address, and your credentials. An address without a scheme uses HTTPS. Use legacy password authentication only for servers that require it. API key authentication requires server support. Test connection checks your details without saving. Playback: Original requests the original stream; Server default lets the server choose; Transcode requests a format and maximum bitrate. Downloads always request original files. Report listening sends playback information and scrobbles to this account. Synchronize queue automatically exchanges this server's songs, current item and position with the server queue, including other clients' changes. Browsing: choose visible sections and optionally restrict browsing to selected libraries. Load libraries uses the connection details entered here. Startup metadata refresh updates this server's songs in Luna's restored current playlist. Advanced headers are sent only to this connection. Configure AudioMuse only if you have access to that separate service. Secrets are protected for your Windows account. You can save a connection while the server is offline.");
}
