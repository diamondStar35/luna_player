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
        new(ActionId.PlayPause, "Play/Pause", new("space", WinAlt)),
        new(ActionId.SeekBackward, "Seek Backward", new("left", WinAlt)),
        new(ActionId.SeekForward, "Seek Forward", new("right", WinAlt)),
        new(ActionId.VolumeUp, "Volume Up", new("up", WinAlt)),
        new(ActionId.VolumeDown, "Volume Down", new("down", WinAlt)),
        new(ActionId.NextTrack, "Next Track", new("page_down", WinAlt)),
        new(ActionId.PreviousTrack, "Previous Track", new("page_up", WinAlt)),
    ];
}
