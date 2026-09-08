namespace LunaPlayer.Actions;

/// <summary>The commands in Help that update the player and its optional components.</summary>
internal static class UpdateActionDefinitions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that checks whether a newer Luna Player release is available.
        new(ActionId.CheckAppUpdates, Tr("Check for app updates")),
    ];
}
