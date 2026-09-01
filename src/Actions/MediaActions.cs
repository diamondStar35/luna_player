namespace LunaPlayer.Actions;

internal readonly record struct BookmarkSlotAction(ActionId Id, int Slot);

internal static class MediaActionDefinitions
{
    internal static IReadOnlyList<BookmarkSlotAction> BookmarkSlots { get; } =
    [
        new(ActionId.JumpBookmark1, 1), new(ActionId.JumpBookmark2, 2), new(ActionId.JumpBookmark3, 3),
        new(ActionId.JumpBookmark4, 4), new(ActionId.JumpBookmark5, 5), new(ActionId.JumpBookmark6, 6),
        new(ActionId.JumpBookmark7, 7), new(ActionId.JumpBookmark8, 8), new(ActionId.JumpBookmark9, 9),
        new(ActionId.JumpBookmark10, 10),
    ];

    internal static IReadOnlyList<ActionDefinition> All { get; } = Build();

    private static IReadOnlyList<ActionDefinition> Build()
    {
        var actions = new List<ActionDefinition>
        {
            // Translators: Name of the command that opens one or more media files, in the File menu and the shortcuts list.
            new(ActionId.OpenFile, Tr("Open File"), new("o", ShortcutModifiers.Control)),
            // Translators: Name of the command that plays a network stream from a link, in the File menu and the shortcuts list.
            new(ActionId.OpenLink, Tr("Open Link"), new("l", ShortcutModifiers.Control)),
            // Translators: Name of the command that opens every media file in a folder, in the File menu and the shortcuts list.
            new(ActionId.OpenFolder, Tr("Open Folder"), new("o", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that opens the folder holding the current file in Windows Explorer.
            new(ActionId.OpenContainingFolder, Tr("Open Containing Folder"), new("f", ShortcutModifiers.Control)),
            // Translators: Name of the command that shows the Windows properties window for the current file.
            new(ActionId.OpenFileProperties, Tr("File Properties"), new("enter", ShortcutModifiers.Alt)),
            // Translators: Name of the command that lists the files currently loaded in the player.
            new(ActionId.OpenedFiles, Tr("Opened Files"), new("f2")),
            // Translators: Name of the command that removes the current file from the player.
            new(ActionId.CloseFile, Tr("Close File"), new("w", ShortcutModifiers.Control)),
            // Translators: Name of the command that removes every loaded file from the player.
            new(ActionId.CloseAllFiles, Tr("Close All Files"), new("w", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that opens the player's settings window.
            new(ActionId.OpenPreferences, Tr("Settings"), new("p", ShortcutModifiers.Control)),
            // Translators: Name of the command that quits the player.
            new(ActionId.Exit, Tr("Exit")),
            // Translators: Name of the command that speaks the details of the current file.
            new(ActionId.AnnounceFileInfo, Tr("Announce File Info"), new("f")),
            // Translators: Name of the command that gives the current file a new name.
            new(ActionId.RenameFile, Tr("Rename File"), new("f2", ShortcutModifiers.Shift)),
            // Translators: Name of the command that deletes the current file from the disk.
            new(ActionId.DeleteFile, Tr("Delete File"), new("delete", ShortcutModifiers.Shift)),
            // Translators: Name of the command that copies the current file to the clipboard.
            new(ActionId.CopyFile, Tr("Copy Current File"), new("c", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that pastes files copied from Windows Explorer into the player.
            new(ActionId.PasteFile, Tr("Paste"), new("v", ShortcutModifiers.Control)),
            // Translators: Name of the command that marks or unmarks the current file. Marked files can then be copied, moved or deleted together.
            new(ActionId.ToggleMarkCurrent, Tr("Mark Current File"), new("k", ShortcutModifiers.Control)),
            // Translators: Name of the command that marks every loaded file.
            new(ActionId.ToggleMarkAll, Tr("Mark All Files"), new("a", ShortcutModifiers.Control)),
            // Translators: Name of the command that unmarks all marked files.
            new(ActionId.ClearMarks, Tr("Clear Marked Files"), new("k", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that speaks how many files are marked.
            new(ActionId.AnnounceMarkedCount, Tr("Announce Marked Files Count"), new("k")),
            // Translators: Name of the command that copies the marked files into a folder the user chooses.
            new(ActionId.MarkedCopyToFolder, Tr("Copy to Folder")),
            // Translators: Name of the command that moves the marked files into a folder the user chooses.
            new(ActionId.MarkedMoveToFolder, Tr("Move to Folder")),
            // Translators: Name of the command that copies the marked files to the clipboard.
            new(ActionId.MarkedCopyToClipboard, Tr("Copy to Clipboard")),
            // Translators: Name of the command that deletes the marked files from the disk.
            new(ActionId.MarkedDelete, Tr("Delete Marked Files")),
            // Translators: Name of the command that saves the current position in the file as a bookmark.
            new(ActionId.AddBookmark, Tr("Add Bookmark"), new("m", ShortcutModifiers.Shift)),
            // Translators: Name of the command that opens the window for renaming and deleting bookmarks.
            new(ActionId.ManageBookmarks, Tr("Manage Bookmarks"), new("m", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that plays the file before the current one.
            new(ActionId.PreviousTrack, Tr("Previous Track"), new("tab", ShortcutModifiers.Shift), new("page_up")),
            // Translators: Name of the command that plays the file after the current one.
            new(ActionId.NextTrack, Tr("Next Track"), new("tab"), new("page_down")),
            // Translators: Name of the command that plays the first file in the list.
            new(ActionId.FirstTrack, Tr("Go to First File"), new("home", ShortcutModifiers.Control)),
            // Translators: Name of the command that asks for a file number and jumps to it.
            new(ActionId.GoToFile, Tr("Go To File"), new("g", ShortcutModifiers.Control)),
            // Translators: Name of the command that plays the last file in the list.
            new(ActionId.LastTrack, Tr("Go to Last File"), new("end", ShortcutModifiers.Control)),
            // Translators: Name of the command that turns playing files in random order on or off.
            new(ActionId.ToggleShuffle, Tr("Shuffle"), new("z", ShortcutModifiers.Control)),
            // Translators: Name of the command that turns repeating the current file on or off.
            new(ActionId.ToggleRepeatFile, Tr("Repeat File"), new("r", ShortcutModifiers.Control)),
        };
        // Translators: Name of the command that jumps to one of the ten numbered bookmark slots of the current file.
        // {number} is the slot, 1 to 10.
        actions.AddRange(BookmarkSlots.Select(slot => new ActionDefinition(slot.Id, TrFormat("Jump to Bookmark {number}", slot.Slot),
            new Shortcut(slot.Slot == 10 ? "0" : slot.Slot.ToString(), ShortcutModifiers.Alt))));
        return actions;
    }
}

internal static class BookmarkActionDefinitions
{
    internal static IReadOnlyList<BookmarkSlotAction> Slots => MediaActionDefinitions.BookmarkSlots;
}
