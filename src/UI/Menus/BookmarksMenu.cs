using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal static partial class Builder
{
    private sealed record BookmarksMenuParts(Menu Menu, IReadOnlyList<MenuItem> Items);

    private static BookmarksMenuParts BuildBookmarksMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        var bookmarkItems = new List<MenuItem>();
        var bookmarksMenu = new Menu();
        // Translators: Bookmarks menu item that saves the current position in the file as a bookmark.
        Add(bookmarksMenu, bookmarkItems, commandIds, shortcuts, ActionId.AddBookmark, Tr("Add a new bookmark"));
        // Translators: Bookmarks menu item that opens the window for renaming and deleting bookmarks.
        Add(bookmarksMenu, bookmarkItems, commandIds, shortcuts, ActionId.ManageBookmarks, Tr("Manage bookmarks"));
        var bookmarkJumps = new Menu();
        foreach (var slot in BookmarkActions.Slots)
            // Translators: Item in the "Jump to bookmark" submenu, one for each of the ten bookmark slots.
            // {slot} is the slot number, 1 to 10.
            Add(bookmarkJumps, bookmarkItems, commandIds, shortcuts, slot.Id, TrFormat("Bookmark {slot}", slot.Slot));
        // Translators: Bookmarks submenu holding one item per numbered bookmark slot.
        bookmarksMenu.AppendSubMenu(bookmarkJumps, Tr("Jump to bookmark"));
        return new BookmarksMenuParts(bookmarksMenu, bookmarkItems);
    }
}
