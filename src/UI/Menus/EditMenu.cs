using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal static partial class Builder
{
    private sealed record EditMenuParts(
        Menu Menu,
        IReadOnlyList<MenuItem> LocalEditItems,
        MenuItem MarkCurrentItem,
        MenuItem MarkAllItem);

    private static EditMenuParts BuildEditMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        var localEditItems = new List<MenuItem>();
        var editMenu = new Menu();
        // Translators: Edit menu item that gives the current file a new name.
        Add(editMenu, localEditItems, commandIds, shortcuts, ActionId.RenameFile, Tr("Rename..."));
        // Translators: Edit menu item that deletes the current file from the disk.
        Add(editMenu, localEditItems, commandIds, shortcuts, ActionId.DeleteFile, Tr("Delete"));
        // Translators: Edit menu item that copies the current file to the clipboard.
        Add(editMenu, localEditItems, commandIds, shortcuts, ActionId.CopyFile, Tr("Copy"));
        // Translators: Edit menu item that pastes files copied from Windows Explorer into the player.
        editMenu.Append(commandIds[ActionId.PasteFile], Label(Tr("Paste"), ActionId.PasteFile, shortcuts));
        editMenu.AppendSeparator();
        // Translators: Edit menu item that marks or unmarks the current file. It is ticked while the file is marked.
        var markCurrentItem = editMenu.AppendCheckItem(commandIds[ActionId.ToggleMarkCurrent], Label(Tr("Mark Current File"), ActionId.ToggleMarkCurrent, shortcuts));
        // Translators: Edit menu item that marks every loaded file. It is ticked while they all are.
        var markAllItem = editMenu.AppendCheckItem(commandIds[ActionId.ToggleMarkAll], Label(Tr("Mark All Files"), ActionId.ToggleMarkAll, shortcuts));
        // Translators: Edit menu item that unmarks all marked files.
        var clearMarksItem = editMenu.Append(commandIds[ActionId.ClearMarks], Label(Tr("Clear Marked Files"), ActionId.ClearMarks, shortcuts));
        localEditItems.Add(clearMarksItem);
        return new EditMenuParts(editMenu, localEditItems, markCurrentItem, markAllItem);
    }
}
