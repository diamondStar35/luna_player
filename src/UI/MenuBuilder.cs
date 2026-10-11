using System.Globalization;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Equalizer;
using LunaPlayer.Playback;
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
    IReadOnlyList<MenuItem> AudioTrackItems,
    IReadOnlyDictionary<int, string> SubtitleCommands,
    IReadOnlyDictionary<string, MenuItem> SubtitleItems,
    Menu SubtitleMenu,
    MenuItem SubtitleMenuItem,
    MenuItem SubtitleRememberItem,
    IReadOnlyDictionary<int, RecentCommand> RecentCommands,
    Menu RecentFilesMenu,
    Menu RecentFoldersMenu,
    Menu RecentPlaylistsMenu,
    IReadOnlyDictionary<int, string> EqualizerCommands,
    IReadOnlyDictionary<string, MenuItem> EqualizerItems,
    Menu EqualizerMenu,
    MenuItem MarkCurrentItem,
    MenuItem MarkAllItem,
    MenuItem ShuffleItem,
    MenuItem RepeatFileItem,
    MenuItem SilenceRemovalItem,
    MenuItem FullScreenItem,
    MenuItem StartRecordingItem,
    MenuItem PauseRecordingItem,
    MenuItem StopRecordingItem);

internal static class MainMenuBuilder
{
    internal static MainMenuComponents Build(
        Frame frame,
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts,
        IReadOnlyList<PresetEntry> presets)
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
        // Translators: File menu item that lists the YouTube links and streams the user has saved.
        fileMenu.Append(commandIds[ActionId.OpenFavorites], Label(Tr("Favorite videos..."), ActionId.OpenFavorites, shortcuts));
        fileMenu.AppendSeparator();
        // The recently opened files, folders and playlists, each kind its own submenu. Filled from
        // recents.json and rebuilt as things open; entries carry no fixed ActionId (which recents exist
        // changes), so they are routed by command id like the equalizer and subtitle entries, and rebuilt
        // with fresh ids so no reserved id is ever deleted and reused.
        var recentCommands = new Dictionary<int, RecentCommand>();
        var recentMenu = new Menu();
        var recentFilesMenu = new Menu();
        var recentFoldersMenu = new Menu();
        var recentPlaylistsMenu = new Menu();
        // Translators: Submenu of the File menu's Recents submenu listing the files opened recently.
        recentMenu.AppendSubMenu(recentFilesMenu, Tr("Recent files"));
        // Translators: Submenu of the File menu's Recents submenu listing the folders opened recently.
        recentMenu.AppendSubMenu(recentFoldersMenu, Tr("Recent folders"));
        // Translators: Submenu of the File menu's Recents submenu listing the playlists opened recently.
        recentMenu.AppendSubMenu(recentPlaylistsMenu, Tr("Recent playlists"));
        // Translators: File menu submenu holding the recently opened files, folders and playlists.
        fileMenu.AppendSubMenu(recentMenu, Tr("Recents"));
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
        foreach (var slot in BookmarkActions.Slots)
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

        var pitchMenu = new Menu();
        // Translators: Item in the Pitch submenu: raise audio pitch without changing playback speed.
        Add(pitchMenu, playbackItems, commandIds, shortcuts, ActionId.PitchUp, Tr("Raise Pitch"));
        // Translators: Item in the Pitch submenu: lower audio pitch without changing playback speed.
        Add(pitchMenu, playbackItems, commandIds, shortcuts, ActionId.PitchDown, Tr("Lower Pitch"));
        // Translators: Item in the Pitch submenu: restore the audio's original pitch.
        Add(pitchMenu, playbackItems, commandIds, shortcuts, ActionId.ResetPitch, Tr("Reset Pitch"));
        // Translators: Item in the Pitch submenu: speak the current pitch adjustment.
        Add(pitchMenu, playbackItems, commandIds, shortcuts, ActionId.AnnouncePitch, Tr("Speak Pitch"));
        // Translators: Player submenu holding the controls that raise or lower audio pitch.
        playerMenu.AppendSubMenu(pitchMenu, Tr("Pitch"));

        var panMenu = new Menu();
        // Translators: Item in the Pan submenu: move the sound toward the left speaker.
        Add(panMenu, playbackItems, commandIds, shortcuts, ActionId.PanLeft, Tr("Pan Left"));
        // Translators: Item in the Pan submenu: move the sound toward the right speaker.
        Add(panMenu, playbackItems, commandIds, shortcuts, ActionId.PanRight, Tr("Pan Right"));
        // Translators: Item in the Pan submenu: speak the current left/right audio balance.
        Add(panMenu, playbackItems, commandIds, shortcuts, ActionId.AnnouncePan, Tr("Speak Pan"));
        // Translators: Player submenu holding the left/right audio balance controls.
        playerMenu.AppendSubMenu(panMenu, Tr("Pan"));

        var equalizerCommands = new Dictionary<int, string>();
        var equalizerItems = new Dictionary<string, MenuItem>(StringComparer.Ordinal);
        var equalizerMenu = new Menu();
        FillEqualizerMenu(equalizerMenu, presets, commandIds, shortcuts, equalizerCommands, equalizerItems);
        // Translators: Player submenu holding the equalizer presets and the commands for editing them.
        playerMenu.AppendSubMenu(equalizerMenu, Tr("Equalizer"));
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
        foreach (var jump in PlaybackActions.PercentJumps)
            Add(jumpMenu, playbackItems, commandIds, shortcuts, jump.Id, $"{jump.Percent}%");
        // Translators: Player submenu holding items that jump to a position given as a percentage, from 10% to 100%.
        playerMenu.AppendSubMenu(jumpMenu, Tr("Jump to Percentage"));

        var movementMenu = new Menu();
        foreach (var step in PlaybackActions.SeekSteps)
            Add(movementMenu, playbackItems, commandIds, shortcuts, step.Id, step.Label);
        // Translators: Player submenu for choosing how far one press of the seek keys moves.
        playerMenu.AppendSubMenu(movementMenu, Tr("Seek amount"));
        playerMenu.AppendSeparator();
        // Translators: Player menu item that opens the list of audio output devices to play through.
        playerMenu.Append(commandIds[ActionId.SoundCards], Label(Tr("Sound Cards..."), ActionId.SoundCards, shortcuts));
        // Switching between a file's audio tracks. Gated on there being more than one to switch between, not
        // on a file being loaded, so the three stay out of playbackItems and travel in their own list.
        var audioTrackItems = new List<MenuItem>();
        var audioTrackMenu = new Menu();
        // Translators: Item in the Audio track submenu that switches to the next audio track in the file.
        Add(audioTrackMenu, audioTrackItems, commandIds, shortcuts, ActionId.NextAudioTrack, Tr("Next"));
        // Translators: Item in the Audio track submenu that switches to the previous audio track in the file.
        Add(audioTrackMenu, audioTrackItems, commandIds, shortcuts, ActionId.PreviousAudioTrack, Tr("Previous"));
        audioTrackMenu.AppendSeparator();
        // Translators: Item in the Audio track submenu that opens the window listing the file's audio tracks to choose from.
        Add(audioTrackMenu, audioTrackItems, commandIds, shortcuts, ActionId.OpenAudioTracks, Tr("Track list..."));
        // Translators: Player submenu holding the commands that switch between a file's audio tracks.
        playerMenu.AppendSubMenu(audioTrackMenu, Tr("Audio track"));
        // The subtitles to read aloud. The dynamic region - Off, then one radio item per track - is rebuilt
        // per file; the fixed tail below it (load from file, load from URL, the remember tick box) is built
        // once here and never deleted, so its permanent action ids stay reserved. Off and the tracks carry no
        // ActionId, like the equalizer presets. The quick on/off key (ActionId.ToggleSubtitles) is a bare
        // accelerator, not shown here.
        var subtitleCommands = new Dictionary<int, string>();
        var subtitleItems = new Dictionary<string, MenuItem>(StringComparer.Ordinal);
        var subtitleMenu = new Menu();
        FillSubtitleTracks(subtitleMenu, [], null, subtitleCommands, subtitleItems);
        var subtitleRememberItem = AppendSubtitleCommands(subtitleMenu, commandIds, shortcuts);
        // Translators: Player submenu listing the file's subtitles to read aloud, with Off at the top.
        var subtitleMenuItem = playerMenu.AppendSubMenu(subtitleMenu, Tr("Subtitles"));
        // Full screen is gated on a picture playing, not on a file being loaded, so it stays out of playbackItems.
        var fullScreenItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleFullScreen],
            // Translators: Player menu item that fills the whole screen with the video, or leaves that mode. It is ticked while full screen.
            Label(Tr("Full screen"), ActionId.ToggleFullScreen, shortcuts));

        var videoItems = new List<MenuItem>();
        var videoMenu = new Menu();
        // Translators: Item in the Video options menu that saves the video being played to a folder on this computer.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoDownload, Tr("Download..."));
        // Translators: Item in the Video options menu that shows the text the uploader wrote under the video.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoDescription, Tr("Video description..."));
        // Translators: Item in the Video options menu that copies the address of the video being played.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoCopyLink, Tr("Copy video link"));
        // Translators: Item in the Video options menu that opens the video being played in the web browser.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoOpenInBrowser, Tr("Open video in browser"));
        // Translators: Item in the Video options menu that opens the channel of the video being played in the web browser.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoOpenChannelInBrowser, Tr("Open channel in browser"));
        // Translators: Item in the Video options menu that opens the channel of the video being played inside the player.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoGoToChannel, Tr("Go to channel"));

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

        var updatesMenu = new Menu();
        // Translators: Help menu item that checks whether a newer Luna Player release is available.
        updatesMenu.Append(commandIds[ActionId.CheckAppUpdates], Label(Tr("Check for app updates"), ActionId.CheckAppUpdates, shortcuts));
        // Translators: Help menu item that fetches a newer yt-dlp. "YouTube" is a service name.
        updatesMenu.Append(commandIds[ActionId.UpdateYouTubeComponents], Label(Tr("Update YouTube components"), ActionId.UpdateYouTubeComponents, shortcuts));
        var helpMenu = new Menu();
        // Translators: Help menu item that opens the user guide matching the application language.
        helpMenu.Append(commandIds[ActionId.UserGuide], Label(Tr("User guide"), ActionId.UserGuide, shortcuts));
        // Translators: Help menu item that shows information about Luna Player.
        helpMenu.Append(commandIds[ActionId.About], Label(Tr("About"), ActionId.About, shortcuts));
        // Translators: Help menu item that opens the GitHub page for the installed release.
        helpMenu.Append(commandIds[ActionId.ReleaseNotes], Label(Tr("Release notes"), ActionId.ReleaseNotes, shortcuts));
        helpMenu.AppendSeparator();
        // Translators: Help submenu containing the commands that update Luna Player and its YouTube tools.
        helpMenu.AppendSubMenu(updatesMenu, Tr("Updates"));

        // Appended after all the indexed menus below, between Recording and Help, which nothing counts by
        // position - so the Tools menu can hold whatever it likes without disturbing the returned indices.
        var toolsMenu = new Menu();
        // Translators: Tools menu item that opens the window for converting files to another audio format.
        toolsMenu.Append(commandIds[ActionId.OpenMediaConverter], Label(Tr("Media converter..."), ActionId.OpenMediaConverter, shortcuts));

        // IPTV is a submenu of Tools rather than a top-level menu: two items do not earn a place on the menu
        // bar, and the source manager and channel browser are tools like the converter above them.
        var iptvMenu = new Menu();
        // Translators: IPTV menu item that opens the window where IPTV sources are added and edited.
        iptvMenu.Append(commandIds[ActionId.OpenIptvSources], Label(Tr("Manage IPTV sources..."), ActionId.OpenIptvSources, shortcuts));
        // Translators: IPTV menu item that opens the list of channels the chosen IPTV source holds.
        iptvMenu.Append(commandIds[ActionId.OpenIptvChannels], Label(Tr("Browse channels..."), ActionId.OpenIptvChannels, shortcuts));
        // Translators: Tools submenu holding the IPTV source manager and channel browser.
        toolsMenu.AppendSubMenu(iptvMenu, Tr("IPTV"));

        // A submenu of Tools for the same reason IPTV is: setting, cancelling and querying the timer are three
        // small commands that belong together rather than each on the menu bar.
        var sleepMenu = new Menu();
        // Translators: Sleep-timer menu item that opens the window where a timer is set.
        sleepMenu.Append(commandIds[ActionId.OpenSleepTimer], Label(Tr("Set timer..."), ActionId.OpenSleepTimer, shortcuts));
        // Translators: Sleep-timer menu item that turns off a timer that is counting down.
        sleepMenu.Append(commandIds[ActionId.CancelSleepTimer], Label(Tr("Cancel timer"), ActionId.CancelSleepTimer, shortcuts));
        // Translators: Sleep-timer menu item that speaks how long is left before the timer fires.
        sleepMenu.Append(commandIds[ActionId.AnnounceSleepTimerRemaining], Label(Tr("Time remaining"), ActionId.AnnounceSleepTimerRemaining, shortcuts));
        // Translators: Tools submenu holding the sleep-timer commands.
        toolsMenu.AppendSubMenu(sleepMenu, Tr("Sleep timer"));

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
        // The index returned below is written out as a number, so a menu inserted before the Video menu would
        // leave it pointing at the wrong one.
        var videoMenuIndex = 5;
        // Translators: Name of the menu bar menu holding what can be done with the YouTube video being played.
        menuBar.Append(videoMenu, Tr("Video options"));
        // Last, and it has to be: the three indices returned below are written out as numbers, so a menu
        // put in front of any of them would leave those pointing at the wrong one.
        // Translators: Name of the menu bar menu holding what can be recorded and how.
        menuBar.Append(recordingMenu, Tr("Recording"));
        // Safe after the indexed menus above: nothing counts the tools or help menus by position.
        // Translators: Name of the Tools menu in the menu bar, holding the media converter.
        menuBar.Append(toolsMenu, Tr("Tools"));
        // Translators: Name of the Help menu in the menu bar.
        menuBar.Append(helpMenu, Tr("Help"));
        frame.SetMenuBar(menuBar);
        return new MainMenuComponents(menuBar, 2, markedMenuIndex, videoMenuIndex, playbackItems, mediaFileItems, localFileItems, markedItems, localEditItems, bookmarkItems, videoItems, audioTrackItems, subtitleCommands, subtitleItems, subtitleMenu, subtitleMenuItem, subtitleRememberItem, recentCommands, recentFilesMenu, recentFoldersMenu, recentPlaylistsMenu, equalizerCommands, equalizerItems, equalizerMenu, markCurrentItem, markAllItem, shuffleItem, repeatItem, silenceItem, fullScreenItem, startRecordingItem, pauseRecordingItem, stopRecordingItem);
    }

    /// <summary>What <see cref="MainMenuComponents.EqualizerCommands"/> holds for the item that switches
    /// the equalizer off, in place of a preset name. Not a name any preset can have, because a preset
    /// whose name was empty could not be stored.</summary>
    internal const string EqualizerOffKey = "";

    /// <summary>The key the Subtitles menu holds for the Off item, in place of a track id.</summary>
    internal const string SubtitleOffKey = "";

    /// <summary>Fills the Subtitles submenu's dynamic region: Off and one radio item per track, ticking the
    /// active one. Items are inserted at the top so they sit above the fixed command tail that
    /// <see cref="AppendSubtitleCommands"/> added once - see <see cref="MainFrame.RebuildSubtitleMenu"/> for
    /// why the tail is never part of the rebuild.</summary>
    ///
    /// <remarks>
    /// Off and the tracks are one unbroken run of radio items, for the same reason the equalizer's are - see
    /// <see cref="FillEqualizerMenu"/>. They get a fresh auto id each time, which deletion safely frees; the
    /// fixed tail keeps its permanent action ids precisely because it is not rebuilt.
    /// </remarks>
    internal static void FillSubtitleTracks(
        Menu menu,
        IReadOnlyList<SubtitleMenuEntry> entries,
        int? activeId,
        IDictionary<int, string> commands,
        IDictionary<string, MenuItem> items)
    {
        var position = 0;
        // Translators: Item at the top of the Subtitles menu that reads no subtitles. Ticked by default.
        InsertSubtitleRadio(menu, position++, commands, items, SubtitleOffKey, Tr("Off"));
        foreach (var entry in entries)
            InsertSubtitleRadio(menu, position++, commands, items, entry.Id.ToString(CultureInfo.InvariantCulture), entry.Label);
        var key = activeId?.ToString(CultureInfo.InvariantCulture) ?? SubtitleOffKey;
        if (items.TryGetValue(key, out var item))
            item.Checked = true;
    }

    /// <summary>Adds the fixed tail of the Subtitles submenu once, below a separator: load from a file, load
    /// from a URL, and the session-only remember tick box. Returns the remember item so its tick can be set
    /// later. These carry an <see cref="ActionId"/> apiece, so they take a shortcut and go through the
    /// ordinary action path; they are built once and never deleted, so their permanent ids are not freed and
    /// re-used - which wxWidgets asserts on.</summary>
    internal static MenuItem AppendSubtitleCommands(
        Menu menu,
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts)
    {
        menu.AppendSeparator();
        // Translators: Subtitles submenu item that loads a subtitle file from this computer.
        menu.Append(commandIds[ActionId.LoadSubtitleFile],
            Label(Tr("Load from file..."), ActionId.LoadSubtitleFile, shortcuts));
        // Translators: Subtitles submenu item that loads a subtitle from a web address.
        menu.Append(commandIds[ActionId.LoadSubtitleUrl],
            Label(Tr("Load from URL..."), ActionId.LoadSubtitleUrl, shortcuts));
        // Translators: Subtitles submenu tick box that keeps the chosen subtitle selected across the playlist until the player closes.
        return menu.AppendCheckItem(commandIds[ActionId.RememberSubtitle],
            Label(Tr("Remember subtitle for this session"), ActionId.RememberSubtitle, shortcuts));
    }

    /// <remarks>Carries no shortcut and no <see cref="ActionId"/>, like the equalizer presets: which subtitle
    /// tracks exist changes with the file, so there is no fixed action for a key to be bound to.</remarks>
    private static void InsertSubtitleRadio(
        Menu menu,
        int position,
        IDictionary<int, string> commands,
        IDictionary<string, MenuItem> items,
        string trackKey,
        string label)
    {
        var id = IdManager.NewId();
        commands[id] = trackKey;
        items[trackKey] = menu.Insert(position, id, label, kind: MenuItemKind.Radio);
    }

    /// <summary>Fills one Recents submenu: a numbered item per entry (routed by command id), then a
    /// separator and the kind's Clear command - or a single disabled placeholder when the kind is empty.
    /// Everything uses a fresh id each rebuild, so no reserved action id is deleted and reused.</summary>
    internal static void FillRecentsSubmenu(
        Menu menu,
        RecentKind kind,
        IReadOnlyList<RecentMenuEntry> entries,
        string emptyLabel,
        string clearLabel,
        IDictionary<int, RecentCommand> commands)
    {
        if (entries.Count == 0)
        {
            menu.Append(IdManager.NewId(), emptyLabel).Enabled = false;
            return;
        }
        foreach (var entry in entries)
        {
            var id = IdManager.NewId();
            commands[id] = new RecentCommand(RecentAction.Open, kind, entry.Path);
            menu.Append(id, entry.Label);
        }
        menu.AppendSeparator();
        var clearId = IdManager.NewId();
        commands[clearId] = new RecentCommand(RecentAction.Clear, kind, string.Empty);
        menu.Append(clearId, clearLabel);
    }

    /// <summary>Puts Off, every preset and the two editing commands into the equalizer submenu.</summary>
    ///
    /// <remarks>
    /// Called again whenever the user adds or removes a preset, so it fills a menu rather than making
    /// one: the submenu itself is already attached to the menu bar and outlives its contents.
    ///
    /// Off and the presets are one unbroken run of radio items on purpose. wxWidgets ends a radio group at
    /// the first item that is not one, a separator included, and two groups would let Off stay ticked
    /// beside a ticked preset. The two commands below the separator are ordinary items with an
    /// <see cref="ActionId"/> apiece, so they can be given a shortcut; the presets cannot, because which
    /// presets exist is not something the fixed action table can describe.
    /// </remarks>
    internal static void FillEqualizerMenu(
        Menu menu,
        IReadOnlyList<PresetEntry> presets,
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts,
        IDictionary<int, string> commands,
        IDictionary<string, MenuItem> items)
    {
        // Translators: Item in the Equalizer submenu that leaves the sound unequalized. It is ticked while no preset is in use.
        AddEqualizerItem(menu, commands, items, EqualizerOffKey, Tr("Off"));
        foreach (var preset in presets)
            AddEqualizerItem(menu, commands, items, preset.Id, preset.Name);
        menu.AppendSeparator();
        // Translators: Item in the Equalizer submenu that opens the preset in use for editing, band by band.
        menu.Append(commandIds[ActionId.EditEqualizerPreset],
            Label(Tr("Edit current preset..."), ActionId.EditEqualizerPreset, shortcuts));
        // Translators: Item in the Equalizer submenu that opens the window listing every preset.
        menu.Append(commandIds[ActionId.ManageEqualizerPresets],
            Label(Tr("Manage presets..."), ActionId.ManageEqualizerPresets, shortcuts));
    }

    /// <remarks>
    /// These carry no shortcut and no <see cref="ActionId"/>: which presets exist changes while the player
    /// runs, so there is no fixed action for a key to be bound to.
    /// </remarks>
    private static void AddEqualizerItem(
        Menu menu,
        IDictionary<int, string> commands,
        IDictionary<string, MenuItem> items,
        string presetId,
        string label)
    {
        var id = IdManager.NewId();
        commands[id] = presetId;
        items[presetId] = menu.AppendRadioItem(id, label);
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
