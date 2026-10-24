namespace LunaPlayer.Actions;

internal static class SpotifyPreviewActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Window title or action label for Spotify. Keep the service name unchanged.
        new(ActionId.OpenSpotifyPreview, Tr("Spotify"), new("s", ShortcutModifiers.Alt)),
        // Translators: Window title or action label for Spotify. Keep the service name unchanged.
        new(ActionId.OpenSpotifyCredentials, Tr("Spotify")),
    ];
}
