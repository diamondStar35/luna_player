namespace LunaPlayer.Actions;

/// <summary>The actions that can be driven by a system-wide hot key, with the combinations they answer to by
/// default. A deliberately small set: every one of them is something a user wants while another application
/// has the focus, which is the only reason to take a combination away from the rest of the system.</summary>
internal static class GlobalActionDefinitions
{
    private const ShortcutModifiers WinAlt = ShortcutModifiers.Win | ShortcutModifiers.Alt;

    /// <summary>Global actions carry a primary shortcut only; there is no secondary slot.</summary>
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that starts or pauses playing, here in the list of shortcuts that work while another program is in front.
        new(ActionId.PlayPause, Tr("Play or pause"), new("space", WinAlt)),
        // Translators: Name of the command that moves back in the file, here in the list of shortcuts that work while another program is in front.
        new(ActionId.SeekBackward, Tr("Rewind by one step"), new("left", WinAlt)),
        // Translators: Name of the command that moves forward in the file, here in the list of shortcuts that work while another program is in front.
        new(ActionId.SeekForward, Tr("Fast forward by one step"), new("right", WinAlt)),
        // Translators: Name of the command that makes the sound louder, here in the list of shortcuts that work while another program is in front.
        new(ActionId.VolumeUp, Tr("Increase volume"), new("up", WinAlt)),
        // Translators: Name of the command that makes the sound quieter, here in the list of shortcuts that work while another program is in front.
        new(ActionId.VolumeDown, Tr("Decrease volume"), new("down", WinAlt)),
        // Translators: Name of the command that plays the next file, here in the list of shortcuts that work while another program is in front.
        new(ActionId.NextTrack, Tr("Play the next file"), new("page_down", WinAlt)),
        // Translators: Name of the command that plays the previous file, here in the list of shortcuts that work while another program is in front.
        new(ActionId.PreviousTrack, Tr("Play the previous file"), new("page_up", WinAlt)),
    ];
}
