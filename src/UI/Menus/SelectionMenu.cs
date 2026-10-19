using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed record SelectionMenuParts(
    Menu Menu,
    IReadOnlyDictionary<ActionId, MenuItem> Items,
    MenuItem RepeatItem);

internal static partial class MainMenuBuilder
{
    internal static SelectionMenuParts BuildSelectionMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        var menu = new Menu();
        var items = new Dictionary<ActionId, MenuItem>();

        // Translators: Selection menu item that marks the current playback position as a new part's beginning.
        AddSelection(ActionId.SetSelectionStartMarker, Tr("Set Start Marker"));
        // Translators: Selection menu item that asks for the exact time where a new part begins.
        AddSelection(ActionId.SetSelectionStartTime, Tr("Set Start Time..."));
        // Translators: Selection menu item that marks the current playback position as the unfinished part's end.
        AddSelection(ActionId.SetSelectionEndMarker, Tr("Set End Marker"));
        // Translators: Selection menu item that asks for the exact time where the unfinished part ends.
        AddSelection(ActionId.SetSelectionEndTime, Tr("Set End Time..."));
        menu.AppendSeparator();
        // Translators: Selection menu item that moves to and plays the previous saved part.
        AddSelection(ActionId.PreviousSelection, Tr("Previous Selection Point"));
        // Translators: Selection menu item that moves to and plays the next saved part or its free slot.
        AddSelection(ActionId.NextSelection, Tr("Next Selection Point"));
        menu.AppendSeparator();
        // Translators: Selection menu item that removes the part in the current selection slot.
        AddSelection(ActionId.ClearSelectionPoint, Tr("Clear Selection Point"));
        // Translators: Selection menu item that removes every saved part from the current file.
        AddSelection(ActionId.ClearAllSelections, Tr("Clear All Selection Points"));
        menu.AppendSeparator();
        // Translators: Selection menu item that writes the current selected part as a new media file.
        AddSelection(ActionId.SaveSelection, Tr("Save Selected Time to a File..."));
        menu.AppendSeparator();
        var repeat = menu.AppendCheckItem(
            commandIds[ActionId.ToggleRepeatSelections],
            // Translators: Selection menu item that repeatedly plays the chosen part. It is ticked while enabled.
            Label(Tr("Repeat Selections"), ActionId.ToggleRepeatSelections, shortcuts));
        items[ActionId.ToggleRepeatSelections] = repeat;
        return new SelectionMenuParts(menu, items, repeat);

        void AddSelection(ActionId action, string label)
            => items[action] = menu.Append(commandIds[action], Label(label, action, shortcuts));
    }
}
