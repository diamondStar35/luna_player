namespace LunaPlayer.Actions;

internal static class CurrentServerActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Tools > Music servers command for adding the currently playing server song to favorites.
        new(ActionId.CurrentServerFavorite, Tr("Add to favorites")),
        // Translators: Tools > Music servers command for removing the currently playing server song from favorites.
        new(ActionId.CurrentServerUnfavorite, Tr("Remove from favorites")),
        // Translators: Tools > Music servers command opening the rating dialog for the current song.
        new(ActionId.CurrentServerRate, Tr("Set rating...")),
        // Translators: Tools > Music servers command showing the lyrics of the current song.
        new(ActionId.CurrentServerLyrics, Tr("Song lyrics...")),
        // Translators: Tools > Music servers command showing the current song's server metadata and audio quality.
        new(ActionId.CurrentServerDetails, Tr("Song details...")),
    ];
}
