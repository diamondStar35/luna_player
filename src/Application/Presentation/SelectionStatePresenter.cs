using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Presentation;

/// <summary>Builds the Selection menu's availability from the current file and slot state. Both the
/// application controller and the playback commands change that state, so keeping the mapping here prevents
/// their copies from drifting apart when another selection action is added.</summary>
internal static class SelectionStatePresenter
{
    internal static void Sync(IMainView view, MediaPlayer player, SelectionSlots selections)
    {
        var available = player.IsLoaded;
        var canSave = available
            && selections.Count > 0
            && player.CurrentPath is string path
            && File.Exists(path);
        view.SetSelectionState(new SelectionMenuState(
            available,
            available && selections.CanMovePrevious,
            available && selections.CanMoveNext,
            available && (selections.Current.HasValue || selections.PendingStart.HasValue),
            available && selections.HasAny,
            canSave,
            canSave,
            selections.Repeat));
    }
}
