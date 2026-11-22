namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string Name =>
        // Translators: Name. Used in the music-server browser and its child dialogs.
        Tr("Name");
    internal static string Artist =>
        // Translators: Artist. Used in the music-server browser and its child dialogs.
        Tr("Artist");
    internal static string Album =>
        // Translators: Album. Used in the music-server browser and its child dialogs.
        Tr("Album");
    internal static string Duration =>
        // Translators: Duration. Used in the music-server browser and its child dialogs.
        Tr("Duration");
    internal static string Items =>
        // Translators: Items. Used in the music-server browser and its child dialogs.
        Tr("Items");
    internal static string Play =>
        // Translators: Play. Used in the music-server browser and its child dialogs.
        Tr("Play");
    internal static string Append =>
        // Translators: Music-server browser action that appends songs to Luna's current playback list, without saving a server playlist.
        Tr("Add to current playlist");
    internal static string Refresh =>
        // Translators: Refresh. Used in the music-server browser and its child dialogs.
        Tr("Refresh");
    internal static string Close =>
        // Translators: Close. Used in the music-server browser and its child dialogs.
        Tr("Close");
    internal static string New =>
        // Translators: Add. Used in the music-server browser and its child dialogs.
        Tr("Add...");
    internal static string Edit =>
        // Translators: Edit. Used in the music-server browser and its child dialogs.
        Tr("Edit...");
    internal static string Remove =>
        // Translators: Remove. Used in the music-server browser and its child dialogs.
        Tr("Remove");
    internal static string Save =>
        // Translators: Save. Used in the music-server browser and its child dialogs.
        Tr("Save");
    internal static string Cancel =>
        // Translators: Cancel. Used in the music-server browser and its child dialogs.
        Tr("Cancel");
    internal static string Loading =>
        // Translators: Loading. Used in the music-server browser and its child dialogs.
        Tr("Loading...");
    internal static string Saved =>
        // Translators: Saved successfully. Used in the music-server browser and its child dialogs.
        Tr("Saved successfully.");
    internal static string Unavailable =>
        // Translators: This feature is not supported by this server. Used in the music-server browser and its child dialogs.
        Tr("This feature is not supported by this server.");
    internal static string Failed =>
        // Translators: The operation could not be completed. Used in the music-server browser and its child dialogs.
        Tr("The operation could not be completed.");
    internal static string Unsaved =>
        // Translators: Discard unsaved changes? Used in the music-server browser and its child dialogs.
        Tr("Discard unsaved changes?");
    internal static string Status =>
        // Translators: Status. Used in the music-server browser and its child dialogs.
        Tr("Status");
    internal static string CurrentPosition =>
        // Translators: Current playback position. Used in the music-server browser and its child dialogs.
        Tr("Current playback position");
    internal static string Ready =>
        // Translators: Ready. Used in the music-server browser and its child dialogs.
        Tr("Ready");
    internal static string Open =>
        // Translators: Open. Used in the music-server browser and its child dialogs.
        Tr("Open");
    internal static string Enabled =>
        // Translators: Enabled. Used in the music-server browser and its child dialogs.
        Tr("Enabled");
    internal static string NoSelection =>
        // Translators: Select one or more items first. Used in the music-server browser and its child dialogs.
        Tr("Select one or more items first.");
    internal static string Help =>
        // Translators: Help. Used in the music-server browser and its child dialogs.
        Tr("Help");
    internal static string Error(Exception error) => error is LunaPlayer.Subsonic.RequestException request
        ? request.Kind == LunaPlayer.Subsonic.FailureKind.Unsupported ? Unavailable
        : Failed + "\n" + (request.ServerMessage ?? request.Kind.ToString())
        : error is KeyNotFoundException ? MissingServer : Failed + "\n" + error.Message;
}
