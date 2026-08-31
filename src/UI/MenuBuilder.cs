using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed record MainMenuComponents(
    MenuBar MenuBar,
    int BookmarksMenuIndex,
    int MarkedMenuIndex,
    IReadOnlyList<MenuItem> PlaybackItems,
    IReadOnlyList<MenuItem> MediaFileItems,
    IReadOnlyList<MenuItem> LocalFileItems,
    IReadOnlyList<MenuItem> MarkedItems,
    IReadOnlyList<MenuItem> LocalEditItems,
    IReadOnlyList<MenuItem> BookmarkItems,
    MenuItem MarkCurrentItem,
    MenuItem MarkAllItem,
    MenuItem ShuffleItem,
    MenuItem RepeatFileItem,
    MenuItem SilenceRemovalItem);

internal static class MainMenuBuilder
{
    internal static MainMenuComponents Build(
        Frame frame,
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts)
    {
        var fileMenu = new Menu();
        fileMenu.Append(commandIds[ActionId.OpenFile], Label("Open File...", ActionId.OpenFile, shortcuts));
        fileMenu.Append(commandIds[ActionId.OpenLink], Label("Open Link...", ActionId.OpenLink, shortcuts));
        fileMenu.Append(commandIds[ActionId.OpenFolder], Label("Open Folder...", ActionId.OpenFolder, shortcuts));
        var localFileItems = new List<MenuItem>();
        var mediaFileItems = new List<MenuItem>();
        Add(fileMenu, localFileItems, commandIds, shortcuts, ActionId.OpenContainingFolder, "Open Containing Folder");
        Add(fileMenu, localFileItems, commandIds, shortcuts, ActionId.OpenFileProperties, "File properties...");
        Add(fileMenu, mediaFileItems, commandIds, shortcuts, ActionId.OpenedFiles, "Opened Files...");
        Add(fileMenu, mediaFileItems, commandIds, shortcuts, ActionId.CloseFile, "Close File");
        Add(fileMenu, mediaFileItems, commandIds, shortcuts, ActionId.CloseAllFiles, "Close all files");
        fileMenu.Append(commandIds[ActionId.OpenPreferences], Label("Preferences...", ActionId.OpenPreferences, shortcuts));
        fileMenu.AppendSeparator();
        fileMenu.Append(commandIds[ActionId.Exit], "Exit");

        var localEditItems = new List<MenuItem>();
        var editMenu = new Menu();
        Add(editMenu, localEditItems, commandIds, shortcuts, ActionId.RenameFile, "Rename...");
        Add(editMenu, localEditItems, commandIds, shortcuts, ActionId.DeleteFile, "Delete");
        Add(editMenu, localEditItems, commandIds, shortcuts, ActionId.CopyFile, "Copy");
        editMenu.Append(commandIds[ActionId.PasteFile], Label("Paste", ActionId.PasteFile, shortcuts));
        editMenu.AppendSeparator();
        var markCurrentItem = editMenu.AppendCheckItem(commandIds[ActionId.ToggleMarkCurrent], Label("Mark Current File", ActionId.ToggleMarkCurrent, shortcuts));
        var markAllItem = editMenu.AppendCheckItem(commandIds[ActionId.ToggleMarkAll], Label("Mark All Files", ActionId.ToggleMarkAll, shortcuts));
        var clearMarksItem = editMenu.Append(commandIds[ActionId.ClearMarks], Label("Clear Marked Files", ActionId.ClearMarks, shortcuts));
        localEditItems.Add(clearMarksItem);

        var bookmarkItems = new List<MenuItem>();
        var bookmarksMenu = new Menu();
        Add(bookmarksMenu, bookmarkItems, commandIds, shortcuts, ActionId.AddBookmark, "Add a new bookmark");
        Add(bookmarksMenu, bookmarkItems, commandIds, shortcuts, ActionId.ManageBookmarks, "Manage bookmarks");
        var bookmarkJumps = new Menu();
        foreach (var slot in BookmarkActionDefinitions.Slots)
            Add(bookmarkJumps, bookmarkItems, commandIds, shortcuts, slot.Id, $"Bookmark {slot.Slot}");
        bookmarksMenu.AppendSubMenu(bookmarkJumps, "Jump to bookmark");

        var markedItems = new List<MenuItem>();
        var markedMenu = new Menu();
        Add(markedMenu, markedItems, commandIds, shortcuts, ActionId.MarkedCopyToFolder, "&Copy to folder...");
        Add(markedMenu, markedItems, commandIds, shortcuts, ActionId.MarkedMoveToFolder, "&Move to folder...");
        Add(markedMenu, markedItems, commandIds, shortcuts, ActionId.MarkedCopyToClipboard, "Copy to &clipboard");
        Add(markedMenu, markedItems, commandIds, shortcuts, ActionId.MarkedDelete, "&Delete");

        var playbackItems = new List<MenuItem>();
        var playerMenu = new Menu();
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.PlayPause, "Play/Pause");

        var speedMenu = new Menu();
        Add(speedMenu, playbackItems, commandIds, shortcuts, ActionId.SpeedUp, "Increase Speed");
        Add(speedMenu, playbackItems, commandIds, shortcuts, ActionId.SpeedDown, "Decrease Speed");
        Add(speedMenu, playbackItems, commandIds, shortcuts, ActionId.ResetSpeed, "Reset Speed");
        playerMenu.AppendSubMenu(speedMenu, "Speed");
        playerMenu.AppendSeparator();

        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.PreviousTrack, "Previous");
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.NextTrack, "Next");
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.FirstTrack, "First File");
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.GoToFile, "Go to file...");
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.LastTrack, "Last File");
        var shuffleItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleShuffle],
            Label("Shuffle", ActionId.ToggleShuffle, shortcuts));
        playbackItems.Add(shuffleItem);
        var repeatItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleRepeatFile],
            Label("Repeat File", ActionId.ToggleRepeatFile, shortcuts));
        playbackItems.Add(repeatItem);
        var silenceItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleSilenceRemoval],
            Label("Enable silence removal filter", ActionId.ToggleSilenceRemoval, shortcuts));
        playbackItems.Add(silenceItem);

        var loopMenu = new Menu();
        Add(loopMenu, playbackItems, commandIds, shortcuts, ActionId.StartSelection, "Set A (loop start)");
        Add(loopMenu, playbackItems, commandIds, shortcuts, ActionId.EndSelection, "Set B (loop end)");
        Add(loopMenu, playbackItems, commandIds, shortcuts, ActionId.ClearSelection, "Clear A-B loop");
        playerMenu.AppendSubMenu(loopMenu, "A-B loop");
        playerMenu.AppendSeparator();

        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekBackward, "Rewind");
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekForward, "Forward");
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekStart, "Beginning");
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekEnd, "End");
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.GoToTime, "Go to time...");
        playerMenu.AppendSeparator();

        var jumpMenu = new Menu();
        foreach (var jump in PlaybackActionDefinitions.PercentJumps)
            Add(jumpMenu, playbackItems, commandIds, shortcuts, jump.Id, $"{jump.Percent}%");
        playerMenu.AppendSubMenu(jumpMenu, "Jump to Percentage");

        var movementMenu = new Menu();
        foreach (var step in PlaybackActionDefinitions.SeekSteps)
            Add(movementMenu, playbackItems, commandIds, shortcuts, step.Id, step.Label);
        playerMenu.AppendSubMenu(movementMenu, "Control the clicks movement value");
        playerMenu.AppendSeparator();
        playerMenu.Append(commandIds[ActionId.SoundCards], Label("Sound Cards...", ActionId.SoundCards, shortcuts));

        var menuBar = new MenuBar();
        menuBar.Append(fileMenu, "File");
        menuBar.Append(editMenu, "Edit");
        menuBar.Append(bookmarksMenu, "Bookmarks");
        var markedMenuIndex = 3;
        menuBar.Append(markedMenu, "Actions for marked files");
        menuBar.Append(playerMenu, "Player");
        frame.SetMenuBar(menuBar);
        return new MainMenuComponents(menuBar, 2, markedMenuIndex, playbackItems, mediaFileItems, localFileItems, markedItems, localEditItems, bookmarkItems, markCurrentItem, markAllItem, shuffleItem, repeatItem, silenceItem);
    }

    private static void Add(
        Menu menu,
        ICollection<MenuItem> playbackItems,
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts,
        ActionId action,
        string label)
        => playbackItems.Add(menu.Append(commandIds[action], Label(label, action, shortcuts)));

    private static string Label(string label, ActionId action, ShortcutManager shortcuts)
        => shortcuts.Get(action) is Shortcut shortcut
            ? $"{label}\t{shortcut.ToDisplayString()}"
            : label;
}
