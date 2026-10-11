namespace LunaPlayer.Actions;

/// <summary>The commands for watching IPTV: managing saved sources and browsing the channels one holds.</summary>
///
/// <remarks>
/// Kept apart from the other definitions for the same reason the YouTube commands are (see
/// <see cref="YouTubeActions"/>): IPTV is a feature that stands on its own and can grow, change or
/// be taken away as a whole. The two commands are the two doors into it - the source manager, where sources
/// are added and edited, and the channel browser, which opens the currently chosen source. Browsing carries
/// no default shortcut because it is reached through the manager; only the manager takes a global key.
/// </remarks>
internal static class IptvActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that opens the window where IPTV sources are added and edited.
        new(ActionId.OpenIptvSources, Tr("Manage IPTV sources"),
            new("i", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
        // Translators: Name of the command that opens the list of channels the chosen IPTV source holds.
        new(ActionId.OpenIptvChannels, Tr("Browse IPTV channels")),
    ];
}
