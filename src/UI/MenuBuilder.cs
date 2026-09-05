using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed record MainMenuComponents(
    MenuBar MenuBar,
    int BookmarksMenuIndex,
    int MarkedMenuIndex,
    int VideoMenuIndex,
    IReadOnlyList<MenuItem> PlaybackItems,
    IReadOnlyList<MenuItem> MediaFileItems,
    IReadOnlyList<MenuItem> LocalFileItems,
    IReadOnlyList<MenuItem> MarkedItems,
    IReadOnlyList<MenuItem> LocalEditItems,
    IReadOnlyList<MenuItem> BookmarkItems,
    IReadOnlyList<MenuItem> VideoItems,
    MenuItem MarkCurrentItem,
    MenuItem MarkAllItem,
    MenuItem ShuffleItem,
    MenuItem RepeatFileItem,
    MenuItem SilenceRemovalItem,
    MenuItem StartRecordingItem,
    MenuItem PauseRecordingItem,
    MenuItem StopRecordingItem);

internal static class MainMenuBuilder
{
    internal static MainMenuComponents Build(
        Frame frame,
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts)
    {
        var fileMenu = new Menu();
        // Translators: File menu item that opens one or more media files. The three dots mean it opens a window to choose them.
        fileMenu.Append(commandIds[ActionId.OpenFile], Label(Tr("Open File..."), ActionId.OpenFile, shortcuts));
        // Translators: File menu item that asks for a link to a network stream and plays it.
        fileMenu.Append(commandIds[ActionId.OpenLink], Label(Tr("Open Link..."), ActionId.OpenLink, shortcuts));
        // Translators: File menu item that opens every media file in a folder.
        fileMenu.Append(commandIds[ActionId.OpenFolder], Label(Tr("Open Folder..."), ActionId.OpenFolder, shortcuts));
        // Translators: File menu item that asks for a YouTube address and plays the video or playlist it names.
        fileMenu.Append(commandIds[ActionId.OpenYouTubeLink], Label(Tr("Open YouTube Link..."), ActionId.OpenYouTubeLink, shortcuts));
        // Translators: File menu item that asks what to look for on YouTube and lists what it finds.
        fileMenu.Append(commandIds[ActionId.SearchYouTube], Label(Tr("Search YouTube..."), ActionId.SearchYouTube, shortcuts));
        // Translators: File menu item that fetches a newer yt-dlp. Its home in the original player is a
        // submenu of the Help menu, which this player does not have yet.
        fileMenu.Append(commandIds[ActionId.UpdateYouTubeComponents], Label(Tr("Update YouTube components"), ActionId.UpdateYouTubeComponents, shortcuts));
        // Translators: File menu item that lists the YouTube links and streams the user has saved.
        fileMenu.Append(commandIds[ActionId.OpenFavorites], Label(Tr("Favorite videos..."), ActionId.OpenFavorites, shortcuts));
        var localFileItems = new List<MenuItem>();
        var mediaFileItems = new List<MenuItem>();
        // Translators: File menu item that shows the folder holding the current file in Windows Explorer.
        Add(fileMenu, localFileItems, commandIds, shortcuts, ActionId.OpenContainingFolder, Tr("Open Containing Folder"));
        // Translators: File menu item that shows the Windows properties window for the current file.
        Add(fileMenu, localFileItems, commandIds, shortcuts, ActionId.OpenFileProperties, Tr("File properties..."));
        // Translators: File menu item that lists the files currently loaded in the player.
        Add(fileMenu, mediaFileItems, commandIds, shortcuts, ActionId.OpenedFiles, Tr("Opened Files..."));
        // Translators: File menu item that removes the current file from the player.
        Add(fileMenu, mediaFileItems, commandIds, shortcuts, ActionId.CloseFile, Tr("Close File"));
        // Translators: File menu item that removes every loaded file from the player.
        Add(fileMenu, mediaFileItems, commandIds, shortcuts, ActionId.CloseAllFiles, Tr("Close all files"));
        // Translators: File menu item that opens the player's settings window.
        fileMenu.Append(commandIds[ActionId.OpenPreferences], Label(Tr("Preferences..."), ActionId.OpenPreferences, shortcuts));
        fileMenu.AppendSeparator();
        // Translators: File menu item that quits the player.
        fileMenu.Append(commandIds[ActionId.Exit], Tr("Exit"));

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

        var bookmarkItems = new List<MenuItem>();
        var bookmarksMenu = new Menu();
        // Translators: Bookmarks menu item that saves the current position in the file as a bookmark.
        Add(bookmarksMenu, bookmarkItems, commandIds, shortcuts, ActionId.AddBookmark, Tr("Add a new bookmark"));
        // Translators: Bookmarks menu item that opens the window for renaming and deleting bookmarks.
        Add(bookmarksMenu, bookmarkItems, commandIds, shortcuts, ActionId.ManageBookmarks, Tr("Manage bookmarks"));
        var bookmarkJumps = new Menu();
        foreach (var slot in BookmarkActionDefinitions.Slots)
            // Translators: Item in the "Jump to bookmark" submenu, one for each of the ten bookmark slots.
            // {slot} is the slot number, 1 to 10.
            Add(bookmarkJumps, bookmarkItems, commandIds, shortcuts, slot.Id, TrFormat("Bookmark {slot}", slot.Slot));
        // Translators: Bookmarks submenu holding one item per numbered bookmark slot.
        bookmarksMenu.AppendSubMenu(bookmarkJumps, Tr("Jump to bookmark"));

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

        var playbackItems = new List<MenuItem>();
        var playerMenu = new Menu();
        // Translators: Player menu item that starts playing, or pauses playing.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.PlayPause, Tr("Play/Pause"));

        var speedMenu = new Menu();
        // Translators: Item in the Speed submenu: play the file faster.
        Add(speedMenu, playbackItems, commandIds, shortcuts, ActionId.SpeedUp, Tr("Increase Speed"));
        // Translators: Item in the Speed submenu: play the file slower.
        Add(speedMenu, playbackItems, commandIds, shortcuts, ActionId.SpeedDown, Tr("Decrease Speed"));
        // Translators: Item in the Speed submenu: return the playing speed to normal.
        Add(speedMenu, playbackItems, commandIds, shortcuts, ActionId.ResetSpeed, Tr("Reset Speed"));
        // Translators: Player submenu holding the items that change how fast the file plays.
        playerMenu.AppendSubMenu(speedMenu, Tr("Speed"));
        playerMenu.AppendSeparator();

        // Translators: Player menu item that plays the file before the current one.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.PreviousTrack, Tr("Previous"));
        // Translators: Player menu item that plays the file after the current one.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.NextTrack, Tr("Next"));
        // Translators: Player menu item that plays the first file in the list.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.FirstTrack, Tr("First File"));
        // Translators: Player menu item that asks for a file number and jumps to it.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.GoToFile, Tr("Go to file..."));
        // Translators: Player menu item that plays the last file in the list.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.LastTrack, Tr("Last File"));
        var shuffleItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleShuffle],
            // Translators: Player menu item that turns playing files in random order on or off. It is ticked while it is on.
            Label(Tr("Shuffle"), ActionId.ToggleShuffle, shortcuts));
        playbackItems.Add(shuffleItem);
        var repeatItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleRepeatFile],
            // Translators: Player menu item that turns repeating the current file on or off. It is ticked while it is on.
            Label(Tr("Repeat File"), ActionId.ToggleRepeatFile, shortcuts));
        playbackItems.Add(repeatItem);
        var silenceItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleSilenceRemoval],
            // Translators: Player menu item that turns skipping silent parts of the file on or off. It is ticked while it is on.
            Label(Tr("Enable silence removal filter"), ActionId.ToggleSilenceRemoval, shortcuts));
        playbackItems.Add(silenceItem);

        var loopMenu = new Menu();
        // Translators: Item in the A-B loop submenu: mark the start of the part to repeat.
        // A and B name the two ends of the loop and are usually left as they are.
        Add(loopMenu, playbackItems, commandIds, shortcuts, ActionId.StartSelection, Tr("Set A (loop start)"));
        // Translators: Item in the A-B loop submenu: mark the end of the part to repeat.
        // A and B name the two ends of the loop and are usually left as they are.
        Add(loopMenu, playbackItems, commandIds, shortcuts, ActionId.EndSelection, Tr("Set B (loop end)"));
        // Translators: Item in the A-B loop submenu: forget the marked part and play the whole file again.
        // A and B name the two ends of the loop and are usually left as they are.
        Add(loopMenu, playbackItems, commandIds, shortcuts, ActionId.ClearSelection, Tr("Clear A-B loop"));
        // Translators: Player submenu holding the items that repeat one part of the file.
        // A and B name the two ends of the loop and are usually left as they are.
        playerMenu.AppendSubMenu(loopMenu, Tr("A-B loop"));
        playerMenu.AppendSeparator();

        // Translators: Player menu item that moves back in the file by one seek step.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekBackward, Tr("Rewind"));
        // Translators: Player menu item that moves forward in the file by one seek step.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekForward, Tr("Forward"));
        // Translators: Player menu item that jumps to the beginning of the file.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekStart, Tr("Beginning"));
        // Translators: Player menu item that jumps to the end of the file.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekEnd, Tr("End"));
        // Translators: Player menu item that asks for a time and jumps to it.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.GoToTime, Tr("Go to time..."));
        playerMenu.AppendSeparator();

        var jumpMenu = new Menu();
        foreach (var jump in PlaybackActionDefinitions.PercentJumps)
            Add(jumpMenu, playbackItems, commandIds, shortcuts, jump.Id, $"{jump.Percent}%");
        // Translators: Player submenu holding items that jump to a position given as a percentage, from 10% to 100%.
        playerMenu.AppendSubMenu(jumpMenu, Tr("Jump to Percentage"));

        var movementMenu = new Menu();
        foreach (var step in PlaybackActionDefinitions.SeekSteps)
            Add(movementMenu, playbackItems, commandIds, shortcuts, step.Id, step.Label);
        // Translators: Player submenu for choosing how far one press of the seek keys moves.
        playerMenu.AppendSubMenu(movementMenu, Tr("Control the clicks movement value"));
        playerMenu.AppendSeparator();
        // Translators: Player menu item that opens the list of audio output devices to play through.
        playerMenu.Append(commandIds[ActionId.SoundCards], Label(Tr("Sound Cards..."), ActionId.SoundCards, shortcuts));

        var videoItems = new List<MenuItem>();
        var videoMenu = new Menu();
        // Translators: Item in the Video options menu that saves the video being played to a folder on this computer.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoDownload, Tr("Download..."));
        // Translators: Item in the Video options menu that shows the text the uploader wrote under the video.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoDescription, Tr("Video description..."));
        // Translators: Item in the Video options menu that copies the address of the video being played.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoCopyLink, Tr("Copy video link"));

        // Recording needs nothing loaded and nothing playing, so the window and the folder are always
        // available. The three that run a recording are not: they follow the recorder itself, which is why
        // they are kept rather than added to one of the enable lists above - each has its own condition.
        var recordingMenu = new Menu();
        // Translators: Recording menu item that opens the window where recording is set up and run.
        recordingMenu.Append(commandIds[ActionId.OpenRecordingInterface], Label(Tr("Open the recording interface..."), ActionId.OpenRecordingInterface, shortcuts));
        recordingMenu.AppendSeparator();
        // Translators: Recording menu item that begins recording.
        var startRecordingItem = recordingMenu.Append(commandIds[ActionId.StartRecording], Label(Tr("Start recording"), ActionId.StartRecording, shortcuts));
        // Translators: Recording menu item that holds a recording where it is, or starts it again.
        var pauseRecordingItem = recordingMenu.Append(commandIds[ActionId.PauseRecording], Label(Tr("Pause"), ActionId.PauseRecording, shortcuts));
        // Translators: Recording menu item that ends a recording and closes the file.
        var stopRecordingItem = recordingMenu.Append(commandIds[ActionId.StopRecording], Label(Tr("Stop"), ActionId.StopRecording, shortcuts));
        recordingMenu.AppendSeparator();
        // Translators: Recording menu item that opens the folder recordings are saved into.
        recordingMenu.Append(commandIds[ActionId.OpenRecordingsFolder], Label(Tr("Open recordings folder"), ActionId.OpenRecordingsFolder, shortcuts));

        var menuBar = new MenuBar();
        // Translators: Name of the File menu in the menu bar.
        menuBar.Append(fileMenu, Tr("File"));
        // Translators: Name of the Edit menu in the menu bar.
        menuBar.Append(editMenu, Tr("Edit"));
        // Translators: Name of the Bookmarks menu in the menu bar.
        menuBar.Append(bookmarksMenu, Tr("Bookmarks"));
        var markedMenuIndex = 3;
        // Translators: Name of the menu bar menu holding the things that can be done to the marked files at once.
        menuBar.Append(markedMenu, Tr("Actions for marked files"));
        // Translators: Name of the Player menu in the menu bar, holding the playing, seeking and volume items.
        menuBar.Append(playerMenu, Tr("Player"));
        // Appended last on purpose: the two indices returned below are written out as numbers, so a menu
        // inserted before them would leave the bookmark and marked-file updates pointing at the wrong one.
        var videoMenuIndex = 5;
        // Translators: Name of the menu bar menu holding what can be done with the YouTube video being played.
        menuBar.Append(videoMenu, Tr("Video options"));
        // Last, and it has to be: the three indices returned below are written out as numbers, so a menu
        // put in front of any of them would leave those pointing at the wrong one.
        // Translators: Name of the menu bar menu holding what can be recorded and how.
        menuBar.Append(recordingMenu, Tr("Recording"));
        frame.SetMenuBar(menuBar);
        return new MainMenuComponents(menuBar, 2, markedMenuIndex, videoMenuIndex, playbackItems, mediaFileItems, localFileItems, markedItems, localEditItems, bookmarkItems, videoItems, markCurrentItem, markAllItem, shuffleItem, repeatItem, silenceItem, startRecordingItem, pauseRecordingItem, stopRecordingItem);
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
