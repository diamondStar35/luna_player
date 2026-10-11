namespace LunaPlayer.Actions;

/// <summary>The commands for the sleep timer: setting one, cancelling it, and hearing the time left.</summary>
///
/// <remarks>
/// Kept apart from the other definitions like the IPTV and YouTube commands are (see
/// <see cref="IptvActions"/>): the sleep timer is a self-contained feature. Setting the timer
/// and hearing the time left carry default shortcuts because they are the two things reached mid-listening;
/// cancelling is reached from the submenu and carries no default key, so nothing is spent on it.
/// </remarks>
internal static class SleepTimerActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that opens the window where a sleep timer is set.
        new(ActionId.OpenSleepTimer, Tr("Set sleep timer..."),
            new("s", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
        // Translators: Name of the command that cancels a sleep timer that is counting down.
        new(ActionId.CancelSleepTimer, Tr("Cancel sleep timer")),
        // Translators: Name of the command that speaks how long is left before the sleep timer fires.
        new(ActionId.AnnounceSleepTimerRemaining, Tr("Sleep timer remaining"),
            new("t", ShortcutModifiers.Control)),
    ];
}
