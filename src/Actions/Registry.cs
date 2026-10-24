namespace LunaPlayer.Actions;

/// <summary>One command the player offers: what it is called and which keys invoke it.</summary>
internal sealed record ActionDefinition(
    ActionId Id,
    string Label,
    Shortcut? PrimaryShortcut = null,
    Shortcut? SecondaryShortcut = null);

/// <summary>The documentation and application information commands in the Help menu.</summary>
internal static class HelpActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that opens the Luna Player user guide.
        new(ActionId.UserGuide, Tr("User guide"), new Shortcut("f1")),
        // Translators: Name of the command that shows information about Luna Player.
        new(ActionId.About, Tr("About")),
        // Translators: Name of the command that opens the notes for the installed Luna Player release.
        new(ActionId.ReleaseNotes, Tr("Release notes")),
    ];
}

/// <summary>The application update commands in the Help menu.</summary>
internal static class UpdateActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that checks whether a newer Luna Player release is available.
        new(ActionId.CheckAppUpdates, Tr("Check for app updates")),
    ];
}

/// <summary>Commands for tools that work independently of current playback.</summary>
internal static class ToolsActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that opens the window for converting files to another audio format.
        new(ActionId.OpenMediaConverter, Tr("Open the media converter")),
    ];
}

/// <summary>Every command the player offers, gathered from the per-feature definition tables. The router
/// is validated against this at startup, so a command with a definition but no handler is caught then.</summary>
internal static class ActionRegistry
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [.. MediaActions.All, .. PlaybackActions.All, .. YouTubeActions.All,
        .. HelpActions.All, .. UpdateActions.All,
        .. RecordingActions.All, .. ToolsActions.All, .. IptvActions.All,
        .. SleepTimerActions.All, .. SpotifyPreviewActions.All];
}
