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
            new(ActionId.OpenFile, "Open File", new("o", ShortcutModifiers.Control)),
            new(ActionId.OpenFolder, "Open Folder", new("o", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.OpenContainingFolder, "Open Containing Folder", new("f", ShortcutModifiers.Control)),
            new(ActionId.OpenFileProperties, "File Properties", new("enter", ShortcutModifiers.Alt)),
            new(ActionId.OpenedFiles, "Opened Files", new("f2")),
            new(ActionId.CloseFile, "Close File", new("w", ShortcutModifiers.Control)),
            new(ActionId.CloseAllFiles, "Close All Files", new("w", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.OpenPreferences, "Settings", new("p", ShortcutModifiers.Control)),
            new(ActionId.Exit, "Exit"), new(ActionId.AnnounceFileInfo, "Announce File Info", new("f")),
            new(ActionId.RenameFile, "Rename File", new("f2", ShortcutModifiers.Shift)),
            new(ActionId.DeleteFile, "Delete File", new("delete", ShortcutModifiers.Shift)),
            new(ActionId.CopyFile, "Copy Current File", new("c", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.PasteFile, "Paste", new("v", ShortcutModifiers.Control)),
            new(ActionId.ToggleMarkCurrent, "Mark Current File", new("k", ShortcutModifiers.Control)),
            new(ActionId.ToggleMarkAll, "Mark All Files", new("a", ShortcutModifiers.Control)),
            new(ActionId.ClearMarks, "Clear Marked Files", new("k", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.AnnounceMarkedCount, "Announce Marked Files Count", new("k")),
            new(ActionId.MarkedCopyToFolder, "Copy to Folder"),
            new(ActionId.MarkedMoveToFolder, "Move to Folder"),
            new(ActionId.MarkedCopyToClipboard, "Copy to Clipboard"),
            new(ActionId.MarkedDelete, "Delete Marked Files"),
            new(ActionId.AddBookmark, "Add Bookmark", new("m", ShortcutModifiers.Shift)),
            new(ActionId.ManageBookmarks, "Manage Bookmarks", new("m", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.PreviousTrack, "Previous Track", new("tab", ShortcutModifiers.Shift), new("page_up")),
            new(ActionId.NextTrack, "Next Track", new("tab"), new("page_down")),
            new(ActionId.FirstTrack, "Go to First File", new("home", ShortcutModifiers.Control)),
            new(ActionId.GoToFile, "Go To File", new("g", ShortcutModifiers.Control)),
            new(ActionId.LastTrack, "Go to Last File", new("end", ShortcutModifiers.Control)),
            new(ActionId.ToggleShuffle, "Shuffle", new("z", ShortcutModifiers.Control)),
            new(ActionId.ToggleRepeatFile, "Repeat File", new("r", ShortcutModifiers.Control)),
        };
        actions.AddRange(BookmarkSlots.Select(slot => new ActionDefinition(slot.Id, $"Jump to Bookmark {slot.Slot}",
            new Shortcut(slot.Slot == 10 ? "0" : slot.Slot.ToString(), ShortcutModifiers.Alt))));
        return actions;
    }
}

internal static class BookmarkActionDefinitions
{
    internal static IReadOnlyList<BookmarkSlotAction> Slots => MediaActionDefinitions.BookmarkSlots;
}
