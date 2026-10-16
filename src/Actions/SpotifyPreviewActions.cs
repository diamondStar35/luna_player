namespace LunaPlayer.Actions;

internal static class SpotifyPreviewActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        new(ActionId.OpenSpotifyPreview, Tr("Spotify"), new("s", ShortcutModifiers.Control)),
        new(ActionId.OpenSpotifyCredentials, Tr("Spotify")),
    ];
}
