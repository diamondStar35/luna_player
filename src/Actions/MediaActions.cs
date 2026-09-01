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
            // Translators: Name of the command that opens one or more media files, in the shortcuts list.
            new(ActionId.OpenFile, Tr("Open files"), new("o", ShortcutModifiers.Control)),
            // Translators: Name of the command that plays a network stream from a link.
            new(ActionId.OpenLink, Tr("Open a link to a network stream"), new("l", ShortcutModifiers.Control)),
            // Translators: Name of the command that opens every media file in a folder.
            new(ActionId.OpenFolder, Tr("Open all files in a folder"), new("o", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that opens the folder holding the current file in Windows Explorer.
            new(ActionId.OpenContainingFolder, Tr("Show the current file in Windows Explorer"), new("f", ShortcutModifiers.Control)),
            // Translators: Name of the command that shows the Windows properties window for the current file.
            new(ActionId.OpenFileProperties, Tr("File properties dialog"), new("enter", ShortcutModifiers.Alt)),
            // Translators: Name of the command that opens the window listing the files currently loaded in the player.
            new(ActionId.OpenedFiles, Tr("Opened files dialog"), new("f2")),
            // Translators: Name of the command that removes the current file from the player.
            new(ActionId.CloseFile, Tr("Close the current file"), new("w", ShortcutModifiers.Control)),
            // Translators: Name of the command that removes every loaded file from the player.
            new(ActionId.CloseAllFiles, Tr("Close all files"), new("w", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that opens the player's settings window.
            new(ActionId.OpenPreferences, Tr("Settings dialog"), new("p", ShortcutModifiers.Control)),
            // Translators: Name of the command that quits the player.
            new(ActionId.Exit, Tr("Exit the player")),
            // Translators: Name of the command that speaks the details of the current file, such as its name and length.
            new(ActionId.AnnounceFileInfo, Tr("Speak information about the current file"), new("f")),
            // Translators: Name of the command that gives the current file a new name on the disk.
            new(ActionId.RenameFile, Tr("Rename the current file"), new("f2", ShortcutModifiers.Shift)),
            // Translators: Name of the command that deletes the current file from the disk.
            new(ActionId.DeleteFile, Tr("Delete the current file from the disk"), new("delete", ShortcutModifiers.Shift)),
            // Translators: Name of the command that copies the current file to the clipboard.
            new(ActionId.CopyFile, Tr("Copy the current file to the clipboard"), new("c", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that pastes files copied from Windows Explorer into the player.
            new(ActionId.PasteFile, Tr("Paste files from the clipboard"), new("v", ShortcutModifiers.Control)),
            // Translators: Name of the command that marks or unmarks the current file. Marked files can then be copied, moved or deleted together.
            new(ActionId.ToggleMarkCurrent, Tr("Mark or unmark the current file"), new("k", ShortcutModifiers.Control)),
            // Translators: Name of the command that marks or unmarks every loaded file at once.
            new(ActionId.ToggleMarkAll, Tr("Mark or unmark all files"), new("a", ShortcutModifiers.Control)),
            // Translators: Name of the command that unmarks all marked files.
            new(ActionId.ClearMarks, Tr("Unmark all marked files"), new("k", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that speaks how many files are marked.
            new(ActionId.AnnounceMarkedCount, Tr("Speak the number of marked files"), new("k")),
            // Translators: Name of the command that copies the marked files into a folder the user chooses.
            new(ActionId.MarkedCopyToFolder, Tr("Copy the marked files to a folder")),
            // Translators: Name of the command that moves the marked files into a folder the user chooses.
            new(ActionId.MarkedMoveToFolder, Tr("Move the marked files to a folder")),
            // Translators: Name of the command that copies the marked files to the clipboard.
            new(ActionId.MarkedCopyToClipboard, Tr("Copy the marked files to the clipboard")),
            // Translators: Name of the command that deletes the marked files from the disk.
            new(ActionId.MarkedDelete, Tr("Delete the marked files from the disk")),
            // Translators: Name of the command that saves the position playing has reached as a bookmark.
            new(ActionId.AddBookmark, Tr("Add a bookmark at the current position"), new("m", ShortcutModifiers.Shift)),
            // Translators: Name of the command that opens the window for renaming and deleting bookmarks.
            new(ActionId.ManageBookmarks, Tr("Manage bookmarks dialog"), new("m", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that plays the file before the current one.
            new(ActionId.PreviousTrack, Tr("Play the previous file"), new("tab", ShortcutModifiers.Shift), new("page_up")),
            // Translators: Name of the command that plays the file after the current one.
            new(ActionId.NextTrack, Tr("Play the next file"), new("tab"), new("page_down")),
            // Translators: Name of the command that plays the first file in the list.
            new(ActionId.FirstTrack, Tr("Play the first file in the list"), new("home", ShortcutModifiers.Control)),
            // Translators: Name of the command that opens the window asking for a file number to jump to.
            new(ActionId.GoToFile, Tr("Go to file dialog"), new("g", ShortcutModifiers.Control)),
            // Translators: Name of the command that plays the last file in the list.
            new(ActionId.LastTrack, Tr("Play the last file in the list"), new("end", ShortcutModifiers.Control)),
            // Translators: Name of the command that turns playing the files in random order on or off.
            new(ActionId.ToggleShuffle, Tr("Turn shuffle on or off"), new("z", ShortcutModifiers.Control)),
            // Translators: Name of the command that turns repeating the current file on or off.
            new(ActionId.ToggleRepeatFile, Tr("Turn repeat file on or off"), new("r", ShortcutModifiers.Control)),
        };
        // Translators: Name of the command that jumps to one of the ten numbered bookmark slots of the current file.
        // {number} is the slot, 1 to 10.
        actions.AddRange(BookmarkSlots.Select(slot => new ActionDefinition(slot.Id, TrFormat("Jump to bookmark {number}", slot.Slot),
            new Shortcut(slot.Slot == 10 ? "0" : slot.Slot.ToString(), ShortcutModifiers.Alt))));
        return actions;
    }
}

internal static class BookmarkActionDefinitions
{
    internal static IReadOnlyList<BookmarkSlotAction> Slots => MediaActionDefinitions.BookmarkSlots;
}
