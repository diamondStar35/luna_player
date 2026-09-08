namespace LunaPlayer.Actions;

/// <summary>The documentation and application information commands in the Help menu.</summary>
internal static class HelpActionDefinitions
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
