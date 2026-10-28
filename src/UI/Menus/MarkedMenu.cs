using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal static partial class Builder
{
    private sealed record MarkedMenuParts(Menu Menu, IReadOnlyList<MenuItem> Items);

    private static MarkedMenuParts BuildMarkedMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        var markedItems = new List<MenuItem>();
        var markedMenu = new Menu();
        // Translators: Item in the marked files menu: copy the marked files into a folder the user chooses.
        // The ampersand marks the letter used to reach the item from the keyboard; put it before a letter that suits your language.
        Add(markedMenu, markedItems, commandIds, shortcuts, ActionId.MarkedCopyToFolder, Tr("&Copy to folder..."));
        // Translators: Item in the marked files menu: move the marked files into a folder the user chooses.
        // The ampersand marks the letter used to reach the item from the keyboard; put it before a letter that suits your language.
        Add(markedMenu, markedItems, commandIds, shortcuts, ActionId.MarkedMoveToFolder, Tr("&Move to folder..."));
        // Translators: Item in the marked files menu: copy the marked files to the clipboard.
        // The ampersand marks the letter used to reach the item from the keyboard; put it before a letter that suits your language.
        Add(markedMenu, markedItems, commandIds, shortcuts, ActionId.MarkedCopyToClipboard, Tr("Copy to &clipboard"));
        // Translators: Item in the marked files menu: delete the marked files from the disk.
        // The ampersand marks the letter used to reach the item from the keyboard; put it before a letter that suits your language.
        Add(markedMenu, markedItems, commandIds, shortcuts, ActionId.MarkedDelete, Tr("&Delete"));
        return new MarkedMenuParts(markedMenu, markedItems);
    }
}
