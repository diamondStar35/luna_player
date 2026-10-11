namespace LunaPlayer.Actions;

/// <summary>Every command the player offers, gathered from the per-feature definition tables. The router
/// is validated against this at startup, so a command with a definition but no handler is caught then.</summary>
internal static class ActionRegistry
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [.. MediaActions.All, .. PlaybackActions.All, .. YouTubeActions.All,
        .. HelpActions.All, .. UpdateActions.All,
        .. RecordingActions.All, .. ToolsActions.All, .. IptvActions.All,
        .. SleepTimerActions.All];
}
