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
        new(ActionId.PlayPause, Tr("Play/Pause"), new("space", WinAlt)),
        // Translators: Name of the command that moves back in the file, here in the list of shortcuts that work while another program is in front.
        new(ActionId.SeekBackward, Tr("Seek Backward"), new("left", WinAlt)),
        // Translators: Name of the command that moves forward in the file, here in the list of shortcuts that work while another program is in front.
        new(ActionId.SeekForward, Tr("Seek Forward"), new("right", WinAlt)),
        // Translators: Name of the command that makes the sound louder, here in the list of shortcuts that work while another program is in front.
        new(ActionId.VolumeUp, Tr("Volume Up"), new("up", WinAlt)),
        // Translators: Name of the command that makes the sound quieter, here in the list of shortcuts that work while another program is in front.
        new(ActionId.VolumeDown, Tr("Volume Down"), new("down", WinAlt)),
        // Translators: Name of the command that plays the next file, here in the list of shortcuts that work while another program is in front.
        new(ActionId.NextTrack, Tr("Next Track"), new("page_down", WinAlt)),
        // Translators: Name of the command that plays the previous file, here in the list of shortcuts that work while another program is in front.
        new(ActionId.PreviousTrack, Tr("Previous Track"), new("page_up", WinAlt)),
    ];
}
