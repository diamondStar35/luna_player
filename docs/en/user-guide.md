# Luna Player User Guide

Luna Player is a keyboard-friendly audio and video player for Windows. It plays local media, DVD-Video and DVD-Audio discs, network streams, YouTube, Spotify, Subsonic music servers, internet radio and IPTV; reads subtitles aloud; downloads and converts media; records audio; and provides file management, bookmarks, speech announcements, and configurable local and system-wide shortcuts.

This guide describes the current version of Luna Player. Menu names and shortcuts below use the English interface and the default shortcut configuration.

[TOC]

## 1. Before you begin

### System requirements

Luna Player requires 64-bit Windows 10 version 1809 or later. Capturing the sound of one specific program requires Windows 10 version 2004 or later. Other playback and recording features remain available on older supported versions.

### Installed and portable editions

Luna Player releases are available as an installer and as a portable ZIP archive.

- To install Luna Player, run the release installer and follow its pages. The installer can create Start menu and desktop shortcuts and can register supported media types.
- To use the portable edition, extract the entire ZIP archive to a folder and run `LunaPlayer.exe`. Keep all extracted files and folders together.

The two editions use the same user settings folder. The built-in updater recognizes which edition is running and downloads the corresponding installer or portable archive.

### Starting Luna Player

You can start Luna Player directly, open a supported file with it from Windows, or pass files and folders to it on the command line. By default, opening more media while Luna is already running sends that media to the existing window. Clear **Open files in the existing window when Luna is already running** on the General Preferences page if each file opened from Windows should use a separate Luna window.

If **Remember last file position** is enabled, starting Luna without another file restores the last local file and its last playback position when that file is still available.

## 2. Interface and accessibility

### Main window

The main window contains a menu bar and five buttons:

- **Previous** moves to the previous item.
- **Rewind** moves backward by the selected seek amount.
- **Play** or **Pause** changes playback state.
- **Forward** moves forward by the selected seek amount.
- **Next** moves to the next item.

Every command is also available from a menu or shortcut. Items that cannot apply to the current media are disabled. For example, file properties are available for a local file but not for a web stream, and the **Video options** menu is enabled only for an active YouTube video.

### Keyboard navigation

Use standard Windows navigation throughout the application:

- Press <kbd>Alt</kbd> to reach the menu bar, then use the arrow keys and <kbd>Enter</kbd>.
- Press <kbd>Tab</kbd> and <kbd>Shift</kbd>+<kbd>Tab</kbd> to move between controls in dialogs.
- Use the arrow keys to move through lists and choices.
- Press <kbd>Enter</kbd> to activate the selected item where a dialog supports it.
- Press <kbd>Escape</kbd> to close most dialogs without applying a change.
- Press <kbd>F1</kbd> in the main window to open this guide.

On a Preferences control, <kbd>F1</kbd> has a different purpose: Luna speaks context-sensitive help for that control. If the category tree has focus, it explains how to move between settings pages.

### Speech announcements

Luna reports playback state, time, volume, navigation, file operations, and other changes through a supported screen reader. Two verbosity levels are available:

- **Beginner** uses complete, explanatory messages.
- **Advanced** uses shorter confirmations.

Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> to switch verbosity immediately, or choose it on the **General** Preferences page. Enable **Speak file name when navigating (Previous/Next)** if you want Luna to announce each item reached with Previous or Next.

### Windows media controls

Luna integrates with the Windows media overlay and compatible hardware media keys. The overlay can show the current title, artist, album, artwork and timeline when available. Play, pause, previous, next, rewind, and fast-forward buttons invoke the corresponding Luna commands.

If a keyboard, headset, or another program sends media commands to the wrong application or sends them twice, enable **Disable system media controls** on the General Preferences page. This removes Luna from the Windows media overlay and stops it from handling hardware media keys.

## 3. Quick start

To play a local file:

1. Press <kbd>Ctrl</kbd>+<kbd>O</kbd>.
2. Choose a media file, an M3U/M3U8 playlist, or a Luna JSON playlist.
3. Press <kbd>Space</kbd> to pause or resume.
4. Press <kbd>Left</kbd> or <kbd>Right</kbd> to seek.
5. Press <kbd>Up</kbd> or <kbd>Down</kbd> to change the volume.

To play every supported file in a folder, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd> and choose the folder. Use <kbd>Page Down</kbd> and <kbd>Page Up</kbd> to move between loaded items.

To play a YouTube video, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd> and enter its address. To search instead, press <kbd>Ctrl</kbd>+<kbd>Y</kbd>.

To find an internet radio station, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>R</kbd>. To open Spotify, press <kbd>Alt</kbd>+<kbd>S</kbd>; follow the account setup in the [Spotify section](#13-spotify) before playing your first track.

To browse your Navidrome, Subsonic or OpenSubsonic library, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>M</kbd>, then use **Manage servers...** to add your account. See [Music servers](#14-music-servers) for connection, browsing and playlist instructions.

## 4. Opening media

### Opening a file

Choose **File > Open File...** or press <kbd>Ctrl</kbd>+<kbd>O</kbd>. The dialog accepts supported audio, video, M3U, M3U8, and Luna JSON playlist files.

The **What would you like to open with files?** setting controls what happens when one media file is opened from Windows or from the file dialog:

- **Open the file only** loads only the selected file.
- **Open the file and the main folder files** loads the supported files in the same folder and selects the requested file.
- **Open the file with the main and subfolder files** scans the folder and all folders below it, loads every supported media file found, and selects the requested file.

A recursive scan has a cancellable progress dialog and reports how many media files it has found. Files are placed in Windows natural sort order.

When several files are sent to Luna together and **Open the file only** is selected, Luna loads all of those explicitly selected files rather than discarding all but one.

### Opening a folder

Choose **File > Open Folder...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd>.

With either of the first two file-opening modes, Luna loads supported media directly inside the chosen folder. With the subfolder mode, it scans recursively. Inaccessible and system folders are skipped during a recursive scan.

### Opening a DVD

Choose **File > Open DVD...**. In the folder picker, select the DVD drive or a folder containing a disc's `VIDEO_TS` or `AUDIO_TS` directory. You can also select that directory itself. Use **Disc type** to choose **DVD-Video** or **DVD-Audio**. DVD-Video plays a video's soundtrack as well as its picture; DVD-Audio is the separate audio-disc format, not another name for the soundtrack of a video DVD.

Luna reads the disc's playable titles in the background. A title is a separate programme on the disc, such as a film, an extra, or a group of music tracks. Leave **Title** at **Automatic** to use the longest playable title, or select a particular title when several are available. Choose **Open** to begin playback. If inspection has already finished, playback opens without an opening progress window. Otherwise, **Opening DVD** shows the progress of the inspection already in progress; it does not start reading the disc again.

When a disc has several titles, Previous and Next move between titles. When there is only one playable title, they move between its chapters instead. You can also use **Player > Chapters** and the chapter shortcuts to navigate within a title. Disc menus are not displayed; use the title and chapter lists to reach the content directly.

Luna reports an error if the location does not contain the selected disc format or has no playable titles. If DVD-Audio produces a message about missing audio titles, try DVD-Video only if the disc is a video DVD. Some DVD-Audio discs also contain a separate DVD-Video area, whose content can differ from the audio area.

The installer and **Register file extensions** command also register DVD AutoPlay actions. Windows can offer Luna when a DVD is inserted and as **Play DVD with Luna Player** in Explorer. Choose Luna in Windows' AutoPlay settings if you want it to handle disc insertion automatically. Opening a disc from Explorer detects its type, preferring the DVD-Audio area when both formats are present, and shows **Opening DVD** while reading it. These registrations apply to DVDs; Blu-ray discs and audio CDs are not covered by this feature.

### Opening a network stream

Choose **File > Open Link...** or press <kbd>Ctrl</kbd>+<kbd>L</kbd>. Enter an `http` or `https` address.

Use this command for direct media streams and remote M3U or M3U8 playlists. Use **Open YouTube Link...** for YouTube video and playlist addresses.

### Opening playlists

Luna supports local and remote M3U and M3U8 playlists. Relative entries in a local playlist are resolved from the playlist's folder; relative entries in a network playlist are resolved from its address.

An HLS M3U8 manifest is treated as one stream and passed to the playback engine. It is not expanded into a list of media segments. Other playlists become an opened-files list containing their playable local paths and web links.

### Adding to the current list

Choose **File > Add Files...** or press <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>F</kbd> to select one or more files. Luna adds the selected files to the end of the opened-files list. The current track keeps playing, and the files already in the list keep their order.

To add a folder, choose **File > Add Folder...** or press <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>D</kbd>. Luna adds the supported media files directly inside that folder. It does not scan subfolders, regardless of the file-opening mode selected in Preferences. If you save the list as a named playlist or a JSON file, Luna remembers the folder itself, so changes to its contents are picked up when you open the playlist again.

Adding files and saving the current list are unavailable while you are in an online browsing session. Leave that session before using these commands.

### Saved playlists

Use **File > Playlists** to keep collections you can open again without choosing their files each time. Each saved playlist has a name and an optional description. You can create an empty playlist, build one by selecting files and folders, or save the list you already have open.

You can leave My Playlists open while using the main player window. Choosing **My Playlists...** again brings the existing window forward. The list updates when you create, save, or remove a playlist, and opening a playlist leaves the manager open so you can choose another one later.

Choosing **New** or **Edit** opens an editor belonging to My Playlists. The editor temporarily disables My Playlists, while the main player window remains usable and playback continues. Finish or cancel editing to return control to My Playlists.

- **My Playlists...** opens your saved playlists. The **Items** list shows their names and descriptions. Select a playlist to make its Open, Edit, Remove, and Export commands available.
- **Create Playlist...** opens the playlist editor with an empty list. Enter a name, add the files or folders you want, and choose **Save**. The playlist then appears in My Playlists.
- **Save Current Playlist...** uses the current opened-files list as the starting point. Enter a name and, if you want, a description, then choose **Save** to add it to My Playlists. You do not need to create an empty playlist and add the same files again. To change its contents later, select it in My Playlists and choose **Edit**.
- **Save Playlist As...** saves the current list to a file outside My Playlists. Choose a file name, destination, and M3U8, M3U, or JSON format. If you choose JSON, Luna also asks for the playlist's name and optional description. Cancelling either dialog leaves the destination unchanged.

In My Playlists, **Open** replaces the opened-files list and plays the first available track. **Edit** lets you change the saved playlist's name, description, contents, and order without interrupting playback. These changes take effect in playback the next time you open that playlist. **Remove** asks “Would you like to remove this playlist?” before deleting the saved playlist; its media files and folders remain on disk. **Export** saves a copy of the selected playlist in one of the supported playlist formats.

After a playlist is saved or exported, Luna shows a **Success** message on the main window saying “Playlist saved.” Removing a saved playlist shows “Playlist removed.” in a Success message. Choose **OK** to dismiss the confirmation. These messages confirm completed changes; cancelling an editor or save dialog does not show a success message.

If some files or folders are unavailable, Luna reports them when opening the playlist. If no playable files remain, Luna keeps the current list and playback state. The unavailable entries remain in the saved playlist so you can make them available again or remove them in the editor.

Files saved with Save Playlist As or Export do not automatically appear in My Playlists. To open a Luna JSON playlist file, choose **File > Open File...** and select the JSON playlists filter. My Playlists stores its own JSON files in the `Playlists` folder under Luna's user settings folder; see **Settings and user data** for its location.

### Editing a playlist

Enter a playlist name and an optional description. The name is required when you save, and playlists in My Playlists must have different names. The description is a place to explain what the playlist contains or when you use it. You can save a playlist before adding any files and return to it later through My Playlists.

Choose **Add > Files...** to select one or more individual files in the file dialog. Choose **Add > Folder...** to include a folder. The **Items** list shows each file or folder in **Name** and **Full Path** columns, so you can distinguish files with the same name in different locations. You can also press <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>F</kbd> to add files or <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>D</kbd> to add a folder.

Select the entries you want to change. With the entry list focused, the following keys are available:

| Key | Action |
| --- | --- |
| <kbd>Delete</kbd> | Remove selected entries from the playlist only |
| <kbd>Shift</kbd>+<kbd>Up</kbd> / <kbd>Shift</kbd>+<kbd>Down</kbd> | Move selected entries up / down, preserving their relative order |
| <kbd>Ctrl</kbd>+<kbd>A</kbd> | Select all entries |

The **Remove**, **Move Up**, and **Move Down** buttons perform the same operations. Remove is available when at least one entry is selected. Move Up is available when the selection can move without passing the first entry; Move Down is available when it can move without passing the last entry. Removing an entry changes the playlist only and does not delete the file or folder from disk.

Choose **Save** to keep the name, description, and entry changes. If you close the editor after making changes, including with **Cancel**, <kbd>Escape</kbd>, or <kbd>Alt</kbd>+<kbd>F4</kbd>, Luna asks whether to save them. Choose **Yes** to save, **No** to discard the changes, or **Cancel** to keep editing. Closing without saving leaves the previously saved playlist unchanged.

If saving fails, the editor stays open with your changes so you can correct the problem or try again. When you quit Luna with playlist editors still open, Luna checks their unsaved changes before exiting. Choosing **Cancel** keeps Luna and the editor open.

### Playlist folders and file formats

A folder remains one entry when you save a playlist in My Playlists or as a JSON file. Each time you open that playlist, Luna reads the supported files directly inside the folder and sorts them by name. Adding or removing files in the folder therefore changes what Luna loads the next time you open it. Subfolders are not included. Missing files and empty or inaccessible folders are reported, and their entries stay in the saved playlist.

Choose **JSON** when you want to keep the playlist's name, description, and folder entries. Choose **M3U** or **M3U8** when you want a list of individual media paths and titles that you can open in another player. Both M3U formats use UTF-8. When you export a folder to one of these formats, Luna writes the supported files currently inside it rather than the folder itself. Later changes to the folder are not reflected in that exported file; export the playlist again to include them.

The order of the playlist is preserved when it is saved or exported, including any repeated entries. Files from a folder appear together at that folder's position in the list. Saving or exporting a playlist never moves, copies, or deletes the media files it contains.

### Opening media from the clipboard

Copy a file or folder in File Explorer, return to Luna, and press <kbd>Ctrl</kbd>+<kbd>V</kbd>. Luna opens the first existing path on the clipboard according to the configured file-opening mode.

### Reopening recent media

The **File > Recents** submenu keeps separate lists of recently opened files, folders, and playlists. Choose an entry to open it again. Each list has its own **Clear** command, and an empty list displays a disabled message instead of a command.

The history is stored in Luna's user settings folder. Clearing a recent list removes only its history; it does not delete any media.

### File associations

The installer can register Luna for supported file types. You can also use **Preferences > General > Register file extensions** or **Unregister file extensions**.

Registration adds Luna to Windows' list of applications for supported media. Windows may still require you to select Luna in **Settings > Apps > Default apps** before it becomes the default player.

## 5. Playback controls

### Play and pause

Press <kbd>Space</kbd> or <kbd>Enter</kbd>, choose **Player > Play/Pause**, or use the main Play/Pause button.

If a finished or unloaded item can be reopened, Play/Pause reloads it. In Beginner verbosity, Luna announces Play or Pause; Advanced verbosity avoids these longer state messages.

### Seeking

Press <kbd>Left</kbd> or <kbd>Right</kbd> to move by one seek step. Larger movements are available without changing the selected step:

- <kbd>Shift</kbd>+<kbd>Left</kbd> or <kbd>Shift</kbd>+<kbd>Right</kbd> moves by two steps.
- <kbd>Home</kbd> moves to the beginning.
- <kbd>End</kbd> moves to the end.

The default seek amount is 5 seconds. Choose another value from **Player > Seek amount**, or press <kbd>Shift</kbd> plus a number:

| Shortcut | Seek amount |
| --- | --- |
| <kbd>Shift</kbd>+<kbd>1</kbd> | 1 second |
| <kbd>Shift</kbd>+<kbd>2</kbd> | 5 seconds |
| <kbd>Shift</kbd>+<kbd>3</kbd> | 10 seconds |
| <kbd>Shift</kbd>+<kbd>4</kbd> | 20 seconds |
| <kbd>Shift</kbd>+<kbd>5</kbd> | 30 seconds |
| <kbd>Shift</kbd>+<kbd>6</kbd> | 1 minute |
| <kbd>Shift</kbd>+<kbd>7</kbd> | 2 minutes |
| <kbd>Shift</kbd>+<kbd>8</kbd> | 3 minutes |
| <kbd>Shift</kbd>+<kbd>9</kbd> | 5 minutes |
| <kbd>Shift</kbd>+<kbd>0</kbd> | 10 minutes |
| <kbd>Shift</kbd>+<kbd>-</kbd> | Custom value from Audio Preferences |

The selected amount is saved immediately. Changing **Custom seek value (seconds)** does not select the custom value automatically; select it from the menu or with <kbd>Shift</kbd>+<kbd>-</kbd> when you want to use it.

### Going to a time

Choose **Player > Go to time...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>G</kbd>. Enter the hours, minutes, and seconds that are relevant to the current duration. Luna prevents the selected position from exceeding the end of the file.

### Jumping by percentage

Choose **Player > Jump to Percentage** or use the default percentage shortcuts:

- <kbd>Ctrl</kbd>+<kbd>1</kbd> through <kbd>Ctrl</kbd>+<kbd>9</kbd> jump to 10% through 90%.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>1</kbd> through <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>9</kbd> jump to 15% through 95%.
- <kbd>Ctrl</kbd>+<kbd>0</kbd> or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>0</kbd> jumps to 100%.

Percentage jumps require a known duration and are therefore unavailable for some live streams.

### Getting playback information

Luna can speak playback information without moving focus:

- <kbd>E</kbd> speaks elapsed time.
- <kbd>R</kbd> speaks remaining time.
- <kbd>T</kbd> speaks total duration.
- <kbd>P</kbd> speaks the current percentage.
- <kbd>V</kbd> speaks the current volume.
- <kbd>S</kbd> speaks the current speed.
- <kbd>Shift</kbd>+<kbd>P</kbd> speaks the pitch adjustment.
- <kbd>B</kbd> speaks the left/right pan value.

### File name, path, and media title

Press <kbd>F</kbd> repeatedly to get progressively more detailed information about the current item:

1. The first press speaks the file name, stream name, or YouTube video title.
2. A second press made quickly speaks the complete local path or stream address.
3. A third quick press copies that path or address to the clipboard.

If you pause for more than a short moment, the sequence begins again with the displayed name.

Press <kbd>I</kbd> to speak the title stored in the media's metadata. This is separate from its file name; a local file can have no embedded title or can have a title that differs from its name.

### Volume

Press <kbd>Up</kbd> or <kbd>Down</kbd> to change volume by the configured volume step. The default step is 5 percentage points.

- <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Up</kbd> sets volume to Luna's maximum.
- <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Down</kbd> sets volume to 5%.
- <kbd>V</kbd> speaks the current volume.

Luna permits amplification above 100%, up to 2000%. High amplification can raise background noise and distortion in the source. Dynamic normalization and the limiter can help restrain peaks, but start at a moderate level to protect your hearing and equipment.

### Playback speed

Press <kbd>Ctrl</kbd>+<kbd>Up</kbd> or <kbd>Ctrl</kbd>+<kbd>Down</kbd> to change playback speed. Press <kbd>Alt</kbd>+<kbd>Y</kbd> to return to 1x. Speed is limited to 0.5x through 4x, and the step is configurable on the Audio Preferences page.

### Pitch

Pitch changes do not change playback speed.

- <kbd>Shift</kbd>+<kbd>Up</kbd> raises pitch.
- <kbd>Shift</kbd>+<kbd>Down</kbd> lowers pitch.
- <kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>P</kbd> restores the original pitch.
- <kbd>Shift</kbd>+<kbd>P</kbd> speaks the current adjustment in semitones.

Pitch is limited to 12 semitones above or below the original. The step is configurable from 0.001 through 12 semitones.

### Stereo pan

- <kbd>Ctrl</kbd>+<kbd>Left</kbd> moves the sound toward the left.
- <kbd>Ctrl</kbd>+<kbd>Right</kbd> moves the sound toward the right.
- <kbd>B</kbd> speaks the current pan percentage.

Zero is centered, negative values are left, and positive values are right. The pan step is configurable from 1 through 100 percentage points.

### Choosing an output device

Choose **Player > Sound Cards...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>A</kbd>. Select a Windows audio output device and press OK. The selected device is saved immediately.

### Full-screen video

Choose **Player > Full screen** or press <kbd>F11</kbd> to enter or leave full-screen mode. This command is available only while the current item has video. Pressing <kbd>Escape</kbd> also leaves full-screen mode.

### Choosing an audio track

Files can contain several audio tracks, such as different languages or commentary. Use **Player > Audio track**:

- Press <kbd>Shift</kbd>+<kbd>Page Down</kbd> for the next track.
- Press <kbd>Shift</kbd>+<kbd>Page Up</kbd> for the previous track.
- Choose **Track list...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> to select from a list.

Luna describes each track using its title, channel layout, and language when that information is available. Moving past the first or last audio track does not wrap around.

### Reading subtitles aloud

The **Player > Subtitles** submenu lists the subtitle tracks in the current file. Choose a track to have each subtitle line spoken as it appears, or choose **Off** to stop reading subtitles. Press <kbd>Ctrl</kbd>+<kbd>U</kbd> to switch the last selected track on or off.

You can also load subtitles that are not embedded in the video:

- Choose **Load from file...** or press <kbd>Ctrl</kbd>+<kbd>J</kbd> for a subtitle file on this computer.
- Choose **Load from URL...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>J</kbd> for an `http` or `https` address.

Enable **Remember subtitle for this session**, or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>U</kbd>, to carry the selected subtitle language across the current playlist. This choice lasts only until Luna closes. If the next file has no subtitle in that language, Luna leaves its subtitle selection unchanged.

By default, Luna finishes speaking a subtitle line before reading the next one. Enable **Interrupt the current line when a new subtitle appears** on the General Preferences page if keeping pace with the picture is more important than hearing every line in full.

### Chapters and audiobooks

M4B audiobooks open like other audio files. **Player > Chapters** lists the current file's chapters as **number. title: duration**. The duration is the whole chapter's length. This menu is unavailable when the file has no chapters.

- Press <kbd>Ctrl</kbd>+<kbd>Page Up</kbd> for the previous chapter or <kbd>Ctrl</kbd>+<kbd>Page Down</kbd> for the next chapter. These commands stay within the current book and announce the destination chapter.
- Press <kbd>C</kbd> to hear the current chapter name.
- Select a chapter in the menu to jump to its beginning. Ordinary playback does not announce chapter names as they change naturally.
- Check **Stop playback at the end of current chapter** to pause once at that chapter's end. If you move to another chapter while it is checked, the stop follows that chapter. It takes priority over repeat and automatic advance, then clears itself. Press Play to continue to the following chapter.

Check **Player > Chapters > Audiobook mode** to treat chapters as individual playlist items. Luna remembers this choice. Enabling or disabling it keeps the same listening position and pause state.

Files without chapters use ordinary playback even when audiobook mode is enabled. You do not need to turn the mode off before opening music or another file without chapters.

In audiobook mode, the opened-files list expands each book into its chapters; files without chapters remain single items. Previous/next file commands, shuffle, repeat, end-of-file behavior, and end-of-track sleep timers apply to the active chapter. Elapsed, remaining, and total time refer to that chapter. Seeking backward stops at its beginning; reaching its end, including by seeking, follows the configured end behavior. A-B loops and selection points stay within the chapter, and selecting a different chapter clears them. Automatic chapter announcements follow **Speak file name when navigating (Previous/Next)**.

Bookmarks and remembered listening positions belong to the whole book. A bookmark in another chapter selects that chapter and its saved position. Copying, renaming, deleting, marking, or closing a chapter acts on its complete source book, including all its chapter entries.

## 6. Working with several files

### Previous, next, first, and last

- <kbd>Page Up</kbd> plays the previous item. <kbd>Shift</kbd>+<kbd>Tab</kbd> is its secondary shortcut.
- <kbd>Page Down</kbd> plays the next item. <kbd>Tab</kbd> is its secondary shortcut.
- <kbd>Ctrl</kbd>+<kbd>Home</kbd> plays the first item.
- <kbd>Ctrl</kbd>+<kbd>End</kbd> plays the last item.

If **Wrap to top for multiple files** is enabled, Next on the last item returns to the first and Previous on the first returns to the last. Automatic advance follows the same rule.

You can turn off secondary shortcuts in **Preferences > Keyboard Shortcuts**. This lets <kbd>Tab</kbd> and <kbd>Shift</kbd>+<kbd>Tab</kbd> move between controls in the main window while <kbd>Page Down</kbd> and <kbd>Page Up</kbd> continue to change tracks.

### Going to a file number

Choose **Player > Go to file...** or press <kbd>Ctrl</kbd>+<kbd>G</kbd>. Enter a number from 1 through the number of loaded items.

### Opened Files dialog

Choose **File > Opened Files...** or press <kbd>F2</kbd>. The current item is selected in the list.

- Choose an item and activate **Jump to selected**, or press <kbd>Enter</kbd> on it, to play it.
- Activate **Playlist info** to calculate the number of files, total size, total duration, elapsed time across the playlist, and remaining time. The scan is cancellable and the result opens in a read-only text window.

For a music-server song, the context menu also offers **Set rating...** and the applicable **Add to favorites** or **Remove from favorites** command. Changes are saved to that song's server account.

The list is designed to remain responsive even with a very large number of loaded files.

### Shuffle

Choose **Player > Shuffle** or press <kbd>Ctrl</kbd>+<kbd>Z</kbd>. Enabling shuffle builds a random playback order while keeping the current item selected.

While shuffle is on, Previous, Next, First File, Last File, Go to file, and the Opened Files dialog operate on the shuffled order. Turning shuffle off restores the normal listed order while retaining the current item.

### Repeat and end-of-file behavior

Choose **Player > Repeat File** or press <kbd>Ctrl</kbd>+<kbd>R</kbd> to repeat the current item. This runtime toggle takes precedence while enabled.

The Audio Preferences page also controls the normal action at the end of a file:

- **Advance to the next file** starts the next item.
- **Loop the file** starts the same item again.
- **Do nothing** leaves playback at the end.

### Remembering positions

Two settings serve different purposes:

- **Remember last file position** restores the active local file and playback time the next time Luna starts.
- **Save current position for each file** remembers a separate position for every local item and returns to it when that item is revisited.

If both are disabled, files normally begin at the start.

## 7. A-B loops

An A-B loop repeats one part of the current item.

1. Seek to the desired beginning and press <kbd>[</kbd>. This sets point A.
2. Seek to a later position and press <kbd>]</kbd>. This sets point B and begins looping from A to B.
3. Press <kbd>Backspace</kbd> to clear the loop and continue from point B.

While a loop is active, normal seeking is constrained to the selected range. <kbd>Home</kbd> goes to point A and <kbd>End</kbd> goes to point B. Point B must be later than point A.

The selection applies to the current item and is cleared when playback moves to another item.

## 8. Silence removal and audio processing

### Turning silence removal on or off

Choose **Player > Enable silence removal filter** or press <kbd>Ctrl</kbd>+<kbd>M</kbd>. Luna saves the on/off state immediately.

Silence removal shortens quiet sections while media is playing. It does not modify the original file. Results depend heavily on the source: an aggressive threshold can treat quiet speech or music as silence.

### Basic silence settings

Open **Preferences > Silence removal**.

- **Minimum silence duration (seconds)** controls how long a quiet section must last before it can be shortened. Raising it preserves more short pauses.
- **Silence threshold** is the level, in decibels, below which audio counts as silence. A less negative value, such as -20, treats louder audio as silence; a more negative value, such as -50, limits removal to quieter audio.

The defaults are 0.5 seconds and -30 dB.

### Advanced silence settings

Enable **Show advanced settings** to configure the underlying silence-removal filter more precisely:

- **Leading silent parts to trim**: 0 preserves the beginning; 1 removes initial silence until sustained sound is found. Higher values continue through more sound sections.
- **Sound required before leading trim stops**: how long audio must remain above the threshold before Luna keeps it.
- **Silent parts to trim after audio starts**: -1 shortens every qualifying later pause, 0 preserves later silence, and a positive value limits the number shortened.
- **Minimum inner silence length**: the minimum qualifying pause after sound has begun.
- **Pause to keep after trimmed silence**: retains part of each pause so words do not run together.
- **Detection window size**: the span used for each loudness measurement. Larger windows are steadier; smaller windows react faster.
- **Detection mode**: Peak reacts to the loudest sample and brief sounds; RMS uses average energy for smoother detection.

Use small changes and test them on representative material. The filter affects playback immediately after Preferences is accepted.

### Normalization, limiting, and mono

The **Preferences > Audio > Playback** page contains two additional filters:

- **Enable dynamic normalize and limiter** evens out changing loudness and restrains peaks to reduce clipping.
- **Play audio as Mono** combines the left and right channels into one mono output.

Neither option changes the source file.

### Headphone spatialization

Headphone spatialization makes the audio channels sound like virtual speakers around your head. It uses a head-related transfer function, or HRTF: measurements of how sound from different directions reaches a listener's ears. Listen with headphones to hear the effect. This changes the sound during playback and does not alter the original media file.

#### Downloading an HRTF dataset

**Luna Player does not ship with any HRTF datasets.** Download a compatible `.sofa` file separately and keep it in a folder where it will remain available. SOFA is the file format used to store the measurements; choose a file containing `SimpleFreeFieldHRIR` responses for headphone listening.

SADIE D1 is a useful starting point and is also provided in Steam Audio's HRTF files. The following links download SOFA files directly:

| Dataset | Download and use |
| --- | --- |
| SADIE D1 | [Download sadie_d1.sofa from Steam Audio](https://raw.githubusercontent.com/ValveSoftware/steam-audio/master/core/data/hrtf/sadie_d1.sofa). Start here if you do not already have a preferred HRTF. |
| SADIE H12 | [Download sadie_h12.sofa from Steam Audio](https://raw.githubusercontent.com/ValveSoftware/steam-audio/master/core/data/hrtf/sadie_h12.sofa). An alternative set of measurements to compare with D1. |
| SADIE D2 | [Download D2 from the SOFA database](https://sofacoustics.org/data/database/sadie/D2_48K_24bit_256tap_FIR_SOFA.sofa). Another alternative if D1 does not give you convincing directions. |

You can find more measurements in the [SADIE database](https://www.york.ac.uk/sadie-project/database.html) and the [SOFA dataset collection](https://sofacoustics.org/data/database/). Download the `.sofa` file itself rather than saving the web page describing it. If your browser opens the file instead of saving it, use its save command or the link's **Save link as** command and keep the `.sofa` extension.

There is no single dataset that sounds best for everyone. Compare a few with the same familiar recording and similar volume, listening for clear left/right, front/back and above/below positions. A larger download or a higher measurement sample rate does not by itself make a dataset a better match for your ears.

#### Setting up spatialization

1. Open **Preferences > Audio > Headphone spatialization**.
2. Select **Enable headphone spatialization** to reveal the other controls.
3. Choose **Browse...** and select the downloaded `.sofa` file.
4. Set the gain, starting angles and shortcut steps as needed, then choose **OK**.

Spatialization is off by default. If you want it off at startup but available from the Player menu, select a file and configure the defaults, then clear **Enable headphone spatialization** before choosing OK. The other fields become hidden while the checkbox is cleared, but their values are kept.

| Setting | Purpose |
| --- | --- |
| Gain (dB) | Output adjustment from -20 to 40 dB, initially 0. Lower it if the result is too loud or distorted. Decimal values are accepted. |
| Rotation (degrees) | Starting horizontal angle, from -360 to 360, initially 0. Positive values turn right; negative values turn left. |
| Elevation (degrees) | Starting vertical angle, from -90 to 90, initially 0. Positive values raise the sound; negative values lower it. |
| Rotation step (degrees) | Change made by each rotation command, from 1 to 90 degrees. The default is 5. |
| Elevation step (degrees) | Change made by each elevation command, from 1 to 90 degrees. The default is 5. |

Rotation, elevation and their steps use whole-degree spin controls. Gain uses a text field, so you can enter a decimal adjustment.

#### Changing direction during playback

Use **Player > Headphone Spatialization** or these default shortcuts:

| Command | Default shortcut | Effect |
| --- | --- | --- |
| Enable Headphone Spatialization | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> | Turns the effect on or off for this session. Unavailable until a SOFA path is configured in Preferences. |
| Rotate Left | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Left</kbd> | Subtracts the rotation step. |
| Rotate Right | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Right</kbd> | Adds the rotation step. |
| Raise Elevation | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Up</kbd> | Adds the elevation step. |
| Lower Elevation | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Down</kbd> | Subtracts the elevation step. |

Direction commands are available while spatialization is enabled. Rotation wraps around a full circle: one default step left from 0 becomes 355 degrees. Elevation stops at -90 degrees, directly below you, and 90 degrees, directly above you. Near those vertical positions, changing rotation may make little audible difference. To hear movement around your head more clearly, return elevation towards 0.

Luna announces each change. For example, Beginner verbosity says “Rotation 270 degrees” and Advanced says “270 degrees.” These changes last for the current session and do not replace the defaults in Preferences. Saving unrelated preferences keeps the temporary enabled state and angles. Changing the spatialization defaults applies the edited settings; switching the effect off and on from the Player menu keeps the current session angles.

Pausing, seeking and moving between files preserve your spatialization settings. Enabling or disabling the effect can cause a brief interruption while the audio processing changes, and loading a large dataset can take longer. Direction changes do not require choosing the file again.

Leave **Play audio as Mono** off to preserve the stereo cues created by the effect. Turn spatialization off when listening through speakers or to audio that is already binaural. Keep the SOFA file at the selected path: Luna does not copy it into its settings folder or include it in a settings backup. If it is moved or deleted, choose its new location in Preferences. If the effect cannot be applied, Luna reports an error and turns it off for the current session.

### Equalizer

The equalizer lifts and cuts individual frequency ranges. Choose **Player > Equalizer** and pick an entry from the list. The change takes effect at once and is remembered between sessions.

The list opens with **Off**, followed by six presets for common listening problems:

- **Bass Boost** and **Bass Reduce** raise or lower the bass. The effect is strongest below 100 Hz, still substantial at 170 Hz, and fades out by 600 Hz, so it covers the range a kick drum and a bass guitar occupy without thickening voices.
- **Treble Boost** raises everything from about 4 kHz upward.
- **Headphones** adds bass weight, eases the 3.5 kHz range where headphones sound shouty, and opens up the top.
- **Voice** removes rumble and boxiness and raises the presence range, which is what makes consonants distinct. It suits audiobooks, podcasts and lectures rather than music.
- **Loudness** raises both ends and leaves the middle alone, for listening at low volume where the ear is least sensitive to them.

Then come the genre presets: Classical, Club, Dance, Live, Pop, Reggae, Rock, Ska, Soft, Soft Rock, and Techno. **Custom** is last. It changes nothing until you edit it, and is the starting point for presets of your own.

A preset that raises a range makes the sound louder rather than only different, and no range is quietened to make the change seem larger. Material already mastered close to the limit can therefore clip. **Enable dynamic normalize and limiter** on the Audio Preferences page prevents this and is on by default; with it turned off, lower the volume instead.

**Off** flattens every band rather than removing the equalizer. The preset you used last is remembered while the equalizer is off, so selecting it again returns to it.

### Editing a preset

Choose **Player > Equalizer > Edit current preset** to open the preset the equalizer is set to. Every preset can be edited, including the ones Luna supplies.

The dialog holds one group per band: a low shelf, fifteen bands from 25 Hz to 16 kHz, and a high shelf. Each group contains three fields.

- **Frequency** is the centre of the band, or the point a shelf turns at.
- **Gain** is how far the band is raised or lowered, in decibels, from -20 to 20.
- **Q** is how wide the band is, from 0.1 to 10. Larger values are narrower.

While the preset being edited is the one in use, each change is applied as you type, so you can judge it by ear before keeping it. **Save** keeps the result; **Cancel** restores the preset as it was.

**Base preset** fills every band from another preset. Use it to start from something close to what you want. **Reset to defaults** returns the bands to the way the preset chosen there was supplied.

Editing a preset Luna supplies records only the values you changed, so bands you leave alone continue to follow Luna and later improvements to that preset still reach you. Resetting discards those changes. The name of a supplied preset cannot be changed.

### Managing presets

Choose **Player > Equalizer > Manage presets** for the full list.

- **New** creates a preset, starting from the base preset you choose in the editor.
- **Edit** opens the selected preset.
- **Delete** removes it after confirmation.
- **Close** leaves the list.

Edit and Delete apply to your own presets and are hidden while a preset Luna supplies is selected; use **Edit current preset** for those. To copy a preset, create a new one and choose the original as its base.

Presets you create appear in the Equalizer menu alongside the rest and are stored in `equalizer.json`, next to the settings file.

## 9. Local file management

The rename, delete, containing-folder, and Windows properties commands work only with local files. They are unavailable for network streams and YouTube media.

### Showing the file in Windows

- Press <kbd>Ctrl</kbd>+<kbd>F</kbd> or choose **File > Open Containing Folder** to open File Explorer with the current file selected.
- Press <kbd>Alt</kbd>+<kbd>Enter</kbd> or choose **File > File properties...** to open the Windows properties dialog.

### Renaming a file

Choose **Edit > Rename...** or press <kbd>Shift</kbd>+<kbd>F2</kbd>. Type the new file name. If you omit an extension, Luna retains the current extension. Luna refuses an empty name or a name already used in that folder.

### Deleting a file

Choose **Edit > Delete** or press <kbd>Shift</kbd>+<kbd>Delete</kbd>. Confirm the prompt to permanently delete the current file from disk and remove it from the loaded list. Luna does not move the file to the Recycle Bin.

Deletion is not the same as **Close File**. Closing removes an item only from Luna; deleting removes the local file from disk.

### Copying a file

Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> to place the current item on the Windows file clipboard. Paste it into File Explorer or another program that accepts copied files.

### Closing files

- Press <kbd>Ctrl</kbd>+<kbd>W</kbd> to remove the current item from Luna without deleting it.
- Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>W</kbd> to remove all loaded items.

## 10. Marked files and batch operations

Marks let you collect local files while navigating and then operate on them together.

- Press <kbd>Ctrl</kbd>+<kbd>K</kbd> to mark or unmark the current file.
- Press <kbd>Ctrl</kbd>+<kbd>A</kbd> to mark all loaded files or unmark them all when they are already all marked.
- Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>K</kbd> to clear all marks.
- Press <kbd>K</kbd> to hear the number of marked files.

When at least one file is marked, the **Actions for marked files** menu becomes available:

- **Copy to folder...** copies all marked files to a chosen folder.
- **Move to folder...** moves them and removes the successfully moved entries from Luna.
- **Copy to clipboard** places them on the Windows file clipboard.
- **Delete** asks for confirmation, permanently deletes the files from disk without using the Recycle Bin, and removes successful entries from Luna.

Copy and move use a cancellable progress dialog. Luna reports complete success, partial success, cancellation, or failure. If some files fail, successfully processed files remain processed.

## 11. Bookmarks

Bookmarks save a listening position that you can return to later. Local-file bookmarks are stored by Luna; bookmarks for Subsonic songs are saved on the song's server account. **Add a new bookmark** chooses the appropriate kind from the media playing. YouTube, Spotify, live radio and ordinary network streams do not use this bookmark feature.

### Adding a local bookmark

1. Play or seek to the desired position in a local file.
2. Choose **Bookmarks > Add a new bookmark** or press <kbd>Ctrl</kbd>+<kbd>B</kbd>.
3. Accept the generated time-based name or type a descriptive name.

Local bookmarks are ordered by playback position. The first ten in that order occupy the slots exposed by direct shortcuts. In audiobook mode, a bookmark retains its position in the whole book and can return you to another chapter.

### Jumping to a numbered bookmark

For local files, press <kbd>Alt</kbd>+<kbd>1</kbd> through <kbd>Alt</kbd>+<kbd>9</kbd> for slots 1 through 9. Press <kbd>Alt</kbd>+<kbd>0</kbd> for slot 10. Luna announces when the requested slot is empty.

### Managing local bookmarks

Choose **Bookmarks > Manage bookmarks** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>B</kbd>. The dialog lists bookmark names and positions for the current local file.

- **Jump** moves playback to the selected bookmark.
- **Edit** changes its name.
- **Delete** removes it after confirmation.
- Activating a bookmark in the list jumps to it directly.

Use the Backup and restore Preferences page to export or import the local bookmark collection.

### Bookmarking a music-server song

While a Subsonic song plays, use the same **Bookmarks > Add a new bookmark** command or <kbd>Ctrl</kbd>+<kbd>B</kbd>. Enter an optional comment and Save. The dialog closes, Luna sends the position to the server in the background, and a spoken confirmation reports success.

To resume or manage those positions, open **Tools > Music servers > Browse servers...**, select the account, and choose **Server bookmarks** in the category tree. These entries belong to the remote account. The local manager and numbered shortcuts do not operate on them. See [Server bookmarks](#server-bookmarks) for details.

## 12. YouTube

YouTube features require an internet connection. Playback, channel browsing, and downloads also require the YouTube components available from the YouTube Preferences page. Availability can change when YouTube changes its service.

### Opening a YouTube link

Choose **File > Open YouTube Link...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd>. Enter a video or playlist address. Channel addresses are not accepted by this command.

Some YouTube addresses identify both a video and a playlist. Luna can ask whether to play the video or open the playlist, or follow the fixed behavior selected in YouTube Preferences.

### Searching YouTube

Choose **File > Search YouTube...** or press <kbd>Ctrl</kbd>+<kbd>Y</kbd>. Enter search text, then move through the results list. By default, Luna offers search suggestions while you type. You can turn these off in YouTube Preferences if you do not want background requests while entering a query.

- Press <kbd>Enter</kbd> on a selected result or activate **Play** to play it with video.
- Press <kbd>Ctrl</kbd>+<kbd>Enter</kbd> or activate **Play as audio** to play only its sound.
- Activate **Download** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd> to choose how to save it.
- Press <kbd>Ctrl</kbd>+<kbd>D</kbd> to download it immediately with the saved defaults.
- Press <kbd>Ctrl</kbd>+<kbd>Q</kbd> to add it to the download queue.
- Press <kbd>Ctrl</kbd>+<kbd>Space</kbd> to add it to or remove it from Favorites.
- Press <kbd>Ctrl</kbd>+<kbd>C</kbd> to copy the selected result's link.
- Press <kbd>Ctrl</kbd>+<kbd>B</kbd> to open it in the default browser.
- Press <kbd>Ctrl</kbd>+<kbd>N</kbd> to open the uploader's channel in the default browser.
- Press <kbd>Ctrl</kbd>+<kbd>G</kbd> to browse that channel inside Luna.
- Open the context menu for the same favorite, copy, browser, channel, download, and queue actions.

When selection reaches the end of the current result page, Luna requests more results automatically until YouTube has no more to return.

Playlist and channel result windows work in the same way. Next moves forward through the active YouTube result session, fetching the next video when necessary. Pressing <kbd>Escape</kbd> in the main window after playback begins can return to the related result list when that session is still available.

Channel results are organized into tabs. A tab is loaded when you first open it and is then kept available while the channel window remains open.

### Playback quality

Open **Preferences > YouTube**.

- **Video quality** selects a preferred picture height for playback started with <kbd>Enter</kbd>.
- **Audio quality** selects a preferred bitrate for audio-only playback started with <kbd>Ctrl</kbd>+<kbd>Enter</kbd>.

These are playback settings. Download quality and format have their own defaults on the same Preferences page.

### Video options

While a YouTube video is active, the **Video options** menu provides:

- **Video description...** or <kbd>Alt</kbd>+<kbd>D</kbd>: fetch the title and uploader description and show them in a read-only text window.
- **Copy video link**: copy the stable YouTube watch address rather than the temporary media stream address.
- **Open video in browser** and **Open channel in browser**: open the corresponding YouTube page in the default browser.
- **Go to channel**: browse the uploader's channel inside Luna.

### YouTube download options

For the current video, choose **File > Download...**, or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd>. **File > Direct download** (<kbd>Ctrl</kbd>+<kbd>D</kbd>) starts with saved defaults; **File > Add to download queue** (<kbd>Ctrl</kbd>+<kbd>Q</kbd>) queues it with those defaults. The same choices are available for selected search results.

The Download dialog starts with the defaults from YouTube Preferences and lets you change them for one item. You can choose:

- A file name and destination folder.
- Video or audio-only output.
- Video quality and MP4 or MKV for video.
- MP3, M4A, Opus, FLAC, or WAV and an audio bitrate for audio-only output.
- How many fragments yt-dlp downloads at once.
- Whether to embed metadata, a thumbnail, chapters, or subtitles.
- Whether SponsorBlock sections should be removed.

Choose **Download** to start immediately, or **Add to queue** to retain the choices in a queued job. Progress, finishing work and the shared queue are described in [Downloading online media](#15-downloading-online-media).

### Favorite videos and streams

Favorite videos and streams save links in Luna. Music-server favorites are stored separately on their server, and bookmarks save listening positions. Open favorites with **File > Favorite videos...** or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd>.

The Favorites window can open, add, edit, or remove entries. You can keep using the main player while it is open. Adding or editing an entry temporarily disables Favorites until you finish that dialog, while the main player remains usable. Each entry has a name, link, and type:

- **Video** requires a YouTube video link.
- **Playlist** requires a YouTube playlist link.
- **Combined link** requires an address containing both video and playlist identifiers and opens its video.
- **Generic stream** accepts any `http` or `https` stream address.

Luna validates the address format when an entry is saved. It checks whether the remote content is still available only when you open it.

### YouTube components and cookies

Luna uses yt-dlp and a JavaScript runtime for YouTube playback, channel browsing, and downloads. Search, playlist listings, descriptions, and search suggestions use Luna's metadata client, but a result cannot be played until the components are installed. Use **Download YouTube components** in Preferences to install or refresh them.

The component page offers three update channels:

- **Stable** changes least often and is recommended for normal use.
- **Nightly** follows nightly yt-dlp builds.
- **Master** follows the newest source and can be less reliable.

Use **Help > Updates > Update YouTube components** to update an installed yt-dlp. Enable **Check for yt-dlp updates on startup** for a background check that prompts only when an update is available.

Some age-restricted, members-only, or verification-protected videos require browser cookies. You can choose a Netscape-format cookie file or use **Import from Firefox** so Luna reads the current Firefox cookies for each request. These choices are mutually exclusive, and the read-only cookie status reports which source is active. Treat exported cookie files as private account data.

## 13. Spotify

Choose **Tools > Spotify...** or press <kbd>Alt</kbd>+<kbd>S</kbd> to browse Spotify. You can search for songs, artists and playlists, open your library, and play a selected track. **Browsing Spotify in Luna Player requires a Spotify Premium account.** You also need an internet connection and Spotify authorization.

### Configuring your Spotify app

Spotify's browsing features require the Client ID of your own developer app. If it has not been configured, opening Spotify offers **Go to Preferences**.

1. Open the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard) and sign in.
2. Create an app, enter its name and description, and select **Web API**.
3. Add `http://127.0.0.1:43891/callback/` to its **Redirect URIs**. Copy it exactly, including the port and final slash.
4. Save the app and copy its **Client ID**.
5. In Luna, open **Preferences > Spotify**, paste the Client ID, and choose **OK**. Do not paste the Client Secret.

Spotify sets the account and access requirements for developer apps. Its current [development-mode requirements](https://developer.spotify.com/documentation/web-api/concepts/quota-modes) require the app owner to have Spotify Premium. If you use a different account to sign in, that account must also be allowed to use the app in the dashboard. Some playlists and other content may remain unavailable to development-mode apps.

### Signing in and playing a track

Open the Spotify browser and choose **Sign in to Spotify**. Your default web browser opens Spotify's authorization page. Sign in to the account you want to browse, approve access, and return to Luna when the browser confirms that you can do so.

The Spotify browser has three tabs:

- **Home** contains **Your top tracks**, **Recently played**, and available mixes or radio collections from your library. It is not a copy of every recommendation shown in Spotify's own application.
- **Search** lets you enter a query and choose **All**, **Songs**, **Artists**, or **Playlists** in **Filter by**. Activate **Search**, or choose a previous query from **Recent searches**.
- **Library** lists your playlists and **Liked songs**. Select a collection and choose **Open selection** to see its tracks.

In search results, select a song to play it, or open an artist or playlist to browse its tracks. In a track list, select a song and choose **Play** or press <kbd>Enter</kbd>. More results or tracks load as you reach the end of a list. **Back** or <kbd>Escape</kbd> returns from a results or track window to the browser.

The first playback may open a second browser sign-in. This authorizes the audio connection separately from the Home, Search and Library connection. Complete it and return to Luna; Luna keeps the saved authorization for later sessions. You do not need to start Spotify Desktop to play the selected track.

Choosing a track returns you to the main player. Previous and Next move through the selected collection or song results, loading further tracks when available. Open Spotify again with <kbd>Alt</kbd>+<kbd>S</kbd> to choose something else. Use **Refresh** to reload your library after changing it in Spotify. **Sign out** disconnects the account and its playback authorization and stops an active Spotify session.

### Audio quality and transitions

In **Preferences > Spotify > Audio quality**, choose **Normal (96 kbps)**, **High (160 kbps)**, or **Very high (320 kbps)**. Very high is the default. Changing quality during playback reloads the current track at its current position.

Spotify supports Luna's gapless and cross-fading settings under **Preferences > Audio > Playback**. Enable **Enable cross-fading for audio files** to overlap tracks, and enable **Enable cross-fading during manual navigation** if you also want overlaps when choosing Previous or Next. The next track must be ready in time; a slow connection or unavailable track can prevent a seamless transition. See [Cross-fading](#cross-fading) for duration and curve settings.

Spotify collections belong to Spotify. Browsing them does not copy them into **My Playlists**, and Luna's local playlist editor does not edit your Spotify library.

## 14. Music servers

Luna can browse and play music from Navidrome and other servers that support Subsonic or OpenSubsonic. You need the server address and login details from its administrator. You can save more than one account, including accounts on different servers.

Choose **Tools > Music servers > Browse servers...**, or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>M</kbd>. The browser opens in its own window. You can leave it open and return to the main player to pause, seek or use other playback controls.

### Adding a server

1. In the music-server browser, choose **Manage servers...**, then **Add...**.
2. On the **Connection** tab, enter a **Name** that will help you recognize the account in Luna. This name is only for the saved connection; it does not change your username on the server.
3. Enter the **Server address** supplied by the administrator. Use the server's base address, including a path such as `/music/` if one is required. Do not enter a link to an individual song. If you leave out `http://` or `https://`, Luna uses HTTPS.
4. Choose an **Authentication** method and enter the corresponding login details, as described below.
5. Choose **Test connection**. A success message confirms that Luna can connect, and the available music libraries are loaded into the Browsing tab. If the test fails, Luna shows an error explaining the problem.
6. Review the remaining tabs, then choose **Save**. Testing does not save the connection. You can also save while the server is offline and test it later.

The authentication choices are:

- **Username and password:** use this for a normal server account.
- **Legacy password authentication:** use this only if the server requires the older authentication method.
- **API key:** use this if your server supports API-key authentication and you have been given a key for your account.

Press <kbd>F1</kbd> on a setting to hear help for that control. With a tab selected, F1 explains the tab. Help is spoken in the same way as help in Preferences.

### Switching, editing and removing accounts

The **Server** list in the browser selects the account you are browsing. Luna remembers the last selected account when you close the player and uses it the next time you open the browser. Recently loaded connection information is reused when you switch accounts, but the contents of the selected view are read from the server.

To change an account's settings, choose **Manage servers...**, select the account and choose Edit. Choose Remove to remove a saved connection.

Removing an account stops playback from that account and removes its songs from Luna's current playback list. It does not delete music on the server or delete saved playlist documents. Local files and other accounts' songs remain in the playback list. Downloads already running or queued for the removed account can finish.

Passwords, API keys, custom HTTP header values and AudioMuse tokens are protected for your current Windows account. Saved server connections are separate from ordinary player preferences and are not included in a settings backup. Copying the protected connection data to another Windows account does not make the credentials usable there.

### Choosing the playback format

The account editor's **Playback** tab has three **Streaming mode** choices:

- **Original** requests the source file without asking the server to convert it.
- **Server default** lets the server decide whether to transcode the file.
- **Transcode** requests a particular **Format** and **Maximum bitrate**. These fields appear when Transcode is selected. A bitrate of zero leaves the requested bitrate unrestricted; the server still determines which conversions it supports.

Transcoding can reduce the amount of data needed for playback. Converting to a compressed format can also change audio quality. Choose Original if you want the original stream. Downloading uses the original file regardless of the playback mode.

The Playback tab also contains **Report listening activity** and **Synchronize queue and position automatically**. These are explained under [Listening activity](#listening-activity) and [Saving and synchronizing the playback queue](#saving-and-synchronizing-the-playback-queue).

### Choosing browser sections and libraries

The **Browsing** tab controls what appears when you browse this account.

**Visible browser sections** is a checklist of categories. Use the arrow keys to select an entry and <kbd>Space</kbd> to check or uncheck it. Leave at least one section checked. Hiding a section does not remove anything from the server.

To browse only particular music libraries:

1. Choose **Load libraries**. This uses the address and login details currently entered on the Connection tab, even if you have not saved them. Test connection also loads the libraries.
2. Select **Only include selected libraries**.
3. In the libraries checklist, use <kbd>Space</kbd> to check each library you want to include. At least one must be checked while this restriction is enabled.

Clear Only include selected libraries to browse all libraries available to your account. These choices limit Luna's library browsing and searches; they do not change your permissions or create libraries on the server.

**Refresh current playlist metadata on startup** updates names and other information for this account's songs when Luna restores its current playback list. It runs in the background and needs access to the server.

### Other connection settings

The remaining tabs provide settings you may need for a particular account:

- **Downloads:** choose the default folder for original-file downloads and whether to put them in artist and album subfolders. You can override the destination for an individual download.
- **Advanced:** enter custom HTTP headers only if the server or its proxy requires them. Enter one header per line in the form `Name: value`. These headers apply to requests made through this saved connection.
- **AudioMuse:** configure an optional recommendation service. Leave it unconfigured if you do not use AudioMuse. See [Finding related music](#finding-related-music).

### Browsing the library

Use the **Categories** tree to choose what to browse. Depending on the server and your settings, it can include artists, albums, songs, genres, favorites, recent or frequently played music, random music, server playlists, bookmarks, now-playing information, radio and podcasts. Some sections and actions require server support or particular account permissions.

Choose **Artists**, select an artist in the list and press <kbd>Enter</kbd> to open their albums. Select an album and press Enter again to open its songs. Enter also opens collections such as genres, server playlists and podcast channels. The list's label changes to describe its contents, for example **Albums** or **Songs**.

Opening a collection from the list announces the new view and updates the category tree. Selecting a category in the tree does not add a second announcement of the same view. **Back**, or <kbd>Alt</kbd>+<kbd>Left</kbd>, returns to the previous browsing view without changing playback.

Use **Library** to narrow the current view to one of the available music libraries.

The list has columns appropriate to the view. For songs, these can include title, artist, album, duration, quality, rating, favorite status, track number, year and play count. The **Quality** column describes the original server file, using its format, bitrate, sample rate, bit depth and channel information when available. It does not describe the format of a transcoded playback stream.

Other views show information such as an artist's album count, a playlist's owner and description, a bookmark's position or a podcast episode's availability. The server may not supply every field.

### Searching, sorting and filtering

Type in **Search** to search after a short pause. **Result type** selects artists, albums, songs or all three. Clear the search text to return to the current browsing location. Searches and view loading run in the background; choosing another view or changing the query cancels the previous request. A large Songs view can take time to load.

**Sort by** and **Sort order** rearrange the rows already loaded into the browser. Changing the sort does not make another network request or fetch additional metadata. The chosen sort remains in effect when you open another collection, go Back or switch accounts. Newly loaded results use that same sort.

You can sort by name, artist, album, track, duration, year, date added, play count, rating, format, source bitrate, sample rate or bit depth. Numbers are sorted by value, and missing values come last. Choose **Default** to use the order supplied by the server, such as a playlist's saved song order. Sorting the browser does not change the order saved in a server playlist.

Choose **Filters...** to set a year range. Zero leaves that end of the range unrestricted.

Choose **Refresh**, or press <kbd>F5</kbd>, to reload the current view. This reads the server's existing catalogue. To make the server look for new or changed music files, use a library scan as described under [Refreshing and scanning the library](#refreshing-and-scanning-the-library).

### Playing songs and collections

Select a song and press <kbd>Enter</kbd> to play it. To play an album, artist or server playlist without opening it first, use **Play** in its context menu or press <kbd>Shift</kbd>+<kbd>Enter</kbd>. Enter on those collections opens them for browsing instead.

You can select several rows before playing or adding them. Use <kbd>Shift</kbd> with the arrow keys to select a range. For separate rows, hold <kbd>Ctrl</kbd> while moving with the arrow keys and press <kbd>Ctrl</kbd>+<kbd>Space</kbd> to select or clear each row.

Playing a selection adds new songs at the end of Luna's current playback list and starts the first selected song. If a song is already in that list, Luna plays the existing entry rather than adding a second copy. **Add to current playlist**, or <kbd>Ctrl</kbd>+<kbd>Enter</kbd>, adds missing songs without starting playback. Local files and songs from different server accounts can share this list.

The browser stays open and keeps its selection when you start playback. Previous, Next, pause, seeking, speed changes and audio processing work through the main player. A-B loops and selection ranges also work with server songs. For transcoded streams, some seeks require a new response from the server; Luna opens it in the background while preserving the song's position, loop and selection boundaries. Server response time and connection speed can affect how quickly playback or seeking starts.

**File > Opened Files...** shows Luna's full playback list. Its context menu includes rating and favorite actions for server songs. If a song is missing or unavailable, Luna marks it unavailable instead of repeatedly trying to play it. After correcting the problem, refresh the music-server browser to let Luna retry.

To save a song to your computer, use its download commands. These download the original file, including when you use transcoding for playback. See [Downloading online media](#15-downloading-online-media) for download options, progress and the shared download queue.

### Favorites and ratings

Open a song's context menu to choose **Add to favorites**, **Remove from favorites** or **Set rating...**. Favorite commands also work for supported artist and album selections. Luna offers the favorite action that applies to the selection and speaks a short confirmation after a successful change, such as "Added to favorites."

To rate one or more songs:

1. Select the songs and choose **Set rating...**, or press <kbd>Ctrl</kbd>+<kbd>R</kbd>.
2. Choose a radio button for one through five stars. Choose **Not rated** to remove the rating.
3. Choose **Save**. The dialog closes, Luna saves the rating in the background and announces when it has succeeded.

The chosen rating applies to all selected songs, including when they previously had different ratings. The server determines whether your account may make these changes.

### Song details and lyrics

For a single song, choose **Details...** or press <kbd>Shift</kbd>+<kbd>Enter</kbd> to read its metadata and original audio quality. Artist and album details can include a biography or album notes. **Open information page** opens the source website when the server supplies a link.

Choose **Lyrics...**, or press <kbd>Ctrl</kbd>+<kbd>L</kbd>, to read a song's lyrics. If the server supplies several versions, choose one from **Lyrics version**. Timed lyrics include the position of each line. Select **Follow playback** to keep the current line selected and visible while that song plays. Follow playback is available only for timed lyrics; plain-text lyrics can still be read normally. Luna tells you when no lyrics are available.

### Actions for the currently playing song

You do not have to find the playing song again in the browser to change its rating or read its lyrics. The main window's **Tools > Music servers** submenu contains:

- **Add to favorites** and **Remove from favorites**, with only the applicable action enabled.
- **Set rating...**.
- **Song lyrics...**.
- **Song details...**.

These commands apply to the loaded server song, regardless of the browser's selection. They are unavailable when the loaded item is not a music-server song. To bookmark the playing position, use the **Bookmarks** menu described below.

### Creating a server playlist

A server playlist is a named list saved on the selected account. You can open it again in Luna or another compatible client. It is separate from Luna's current playback list and the local playlists under **File > Playlists > My Playlists**.

To create one:

1. In the music-server browser, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd>, or open the context menu and choose **Server > Create server playlist...**.
2. Enter a **Name** and, if wanted, a **Description**. Select **Public playlist** if you want to share it and your server permits this.
3. Choose **Add songs...** to open the song picker. Search for songs, then select the results you want. This is a normal multi-column selection list: Shift with the arrow keys selects a range, while Ctrl with the arrow keys and Ctrl+Space selects separate songs.
4. Choose **Add selected songs** to put those songs into the playlist draft and close the picker. To add results from another search, open Add songs again.
5. Review the draft. Remove unwanted entries, or use **Move up** and **Move down** to change their order. You can also move selected entries with <kbd>Shift</kbd>+<kbd>Up</kbd> and <kbd>Shift</kbd>+<kbd>Down</kbd>. The selection follows the moved entries. In this editor those keys reorder songs; in the song picker they select a range.
6. Choose **Save** to create the playlist on the server. Luna's confirmation names the saved playlist. Cancel discards the unsaved draft.

Adding songs to the draft does not save it until you choose Save. If saving fails, the draft stays open so you can correct the problem or try again.

### Editing a server playlist or adding songs to it

Choose **Server playlists** in the category tree. Press Enter on a playlist to browse its songs, or choose **Edit...** from its context menu to change its name, description, public visibility or song order. Editing and deletion depend on ownership and server permissions. If another client changes the playlist while you are editing, reopen it before saving again.

To add songs, albums or artists from another browser view, select them and choose **Add to server playlist...**. Choose the destination playlist by name. You can instead choose **Create server playlist...** there to make a new playlist from the selection. The confirmation names the playlist that received the songs.

A server playlist can contain the same song more than once. While browsing its songs, **Remove from playlist** removes the selected occurrences after confirmation. Removing a song from a playlist does not delete its music file. Removing the playlist deletes the saved list, not the music in the library.

### Making a server playlist from Luna's playback list

Choose **Server > Send current playlist to server...** from the browser's context menu. This opens a new server-playlist draft containing songs from the selected account that are currently in Luna's playback list.

Local files and songs from other accounts cannot be included. If any are present, Luna asks before leaving them out. Repeated songs from the selected account are included once in this transfer. Review the name, description and contents, then choose Save. You are creating a new named playlist; this command does not append to an existing server playlist.

### Keeping server songs in a local Luna playlist

Use **File > Playlists > Save Playlist As...** and choose Luna's JSON format to keep server songs alongside local files or other media. Luna saves references to the account and song, without passwords or temporary stream addresses. That saved account must still be configured when you open the playlist later. Editing a local Luna playlist does not edit a playlist stored on the server.

M3U and M3U8 cannot retain these private account references. When exporting either format, Luna asks before leaving server items out. Confirm only if you want the remaining local files and public streams exported. If all entries are server items, Luna leaves the destination unchanged.

### Server bookmarks

A server bookmark saves a song position and an optional comment on your account. While a server song is playing, choose **Bookmarks > Add a new bookmark**, or press <kbd>Ctrl</kbd>+<kbd>B</kbd>. Enter a comment and choose Save. The comment dialog closes immediately, the position is saved in the background and Luna announces "Bookmark added." when it succeeds.

Inside the browser, Ctrl+B bookmarks the playing song from the selected account. It does not bookmark the highlighted search result.

Choose **Server bookmarks** in the category tree to find these saved positions. Play a bookmark to resume its song at the saved position. Its context menu lets you edit the comment or remove the bookmark.

The local **Manage bookmarks** dialog, numbered bookmark slots and bookmark backups apply to local files. Manage bookmarks uses <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>B</kbd>; use the browser's Server bookmarks section for remote positions.

### Listening activity

**Report listening activity**, on the account editor's Playback tab, is enabled for new connections. It tells the server which song is playing and submits a completed listen after half the song's duration or four minutes of listening, whichever comes first. Seeking forward does not count toward listening time.

Luna also sends playback, pause and stop reports when the server supports them. The server decides how to use these reports, including whether to forward listening activity to another service.

### Saving and synchronizing the playback queue

The server queue stores a playback list, current song and position for your account. Use it to resume listening in another compatible client. A queue is different from a named server playlist: it records a listening session rather than a collection you save under a name.

For manual transfers, use **Server > Save queue to server** or **Server > Load queue from server** in the browser's context menu. Only songs from the selected account can be saved in that account's queue. When the playback list contains other media, Luna asks before saving because that media cannot be included.

To exchange queue changes automatically, enable **Synchronize queue and position automatically** on the account editor's Playback tab. This is off for new connections. When enabled, Luna sends local queue edits, periodically saves the playing position and checks for changes made through other clients.

Queue synchronization replaces only the selected account's song entries in Luna's playback list. Local files, podcasts, radio and other accounts' songs remain. While playing a song from the synchronized account, Luna can follow the remote current song and position. It keeps the current pause state; pause state is not synchronized between clients. Remote changes do not interrupt playback of a local file or another account's song.

Changes from several clients can overwrite one another. Leave automatic synchronization off if you want to choose when to save or load the server queue.

### Radio stations on a server

Choose **Radio** in the browser to see stations configured on the selected music server. Select a station and play it to open its stream in Luna. This is separate from the public directory described under [Radio browser](#16-radio-browser).

An administrator can use **Server > Add radio station...** to enter a station name, stream address and optional website. The station's context menu provides editing and removal when permitted.

### Podcasts on a server

Choose **Podcasts**, then open a channel to browse its episodes. Depending on account permissions and server support, you can subscribe to feeds, refresh subscriptions, request **Download episode to server**, delete downloaded episodes or remove subscriptions.

Downloading an episode to the server asks the remote server to obtain it for its library. Downloading to your computer saves a local copy instead. An episode needs a playable copy on the server before Luna can stream it; Luna explains when one is not available.

### Refreshing and scanning the library

Refresh and library scans do different jobs. **Refresh**, or <kbd>F5</kbd>, reads the catalogue the server already has. It is useful after a playlist or song has changed through another client, but does not make the server inspect its music folders.

The **Server** submenu in the browser's context menu provides these administrative commands:

- **Scan server library for changes** asks the server to update its catalogue for new, changed or removed files. This normally requires administrator permission.
- **Rescan entire server library** requests a full Navidrome scan, including folders whose modification times have not changed. It is available for permitted Navidrome accounts and can take longer than a scan for changes.
- **Server scan status** checks whether a scan is running. If it is, Luna opens a status window. Closing the window stops monitoring without cancelling the server's scan. If no scan is running, Luna shows one informational message instead.
- **Server information** shows the server type, version and advertised OpenSubsonic extensions.

After a scan finishes, refresh the browser to read the updated catalogue. A command may be unavailable if the server does not support it or your account lacks permission. Luna can also stop offering a command for the current session after the server rejects it as unsupported or forbidden.

### Managing library folders and source files

Luna can browse the libraries made available by the server, but cannot create server library folders or upload, edit or delete source music files through the standard Subsonic interface. Favorites, ratings and playlists have separate editing commands and do not modify the music files.

For Navidrome, administrators manage libraries on the server website under Settings > Libraries. A library points to a folder accessible to the server machine. Navidrome does not upload or modify the source music files. Add or change those files through the server's filesystem, a network share or a separately configured file-transfer service, then scan the library to update its catalogue.

See the [Navidrome library documentation](https://www.navidrome.org/docs/usage/features/multi-library/) and [file management and upload FAQ](https://www.navidrome.org/docs/faq/) for the server-side setup.

### Finding related music

An artist's context menu can offer **Top songs** and **Similar artists**. **Instant mix** finds music related to a selected song, album or artist. When the server supports sonic similarity, **Sonic similar tracks** offers related songs, and selecting two songs makes **Find sonic path** available. The resulting lists use the usual playback, download and playlist commands.

AudioMuse provides additional recommendations through a separate service. You need access to an AudioMuse service before configuring it; adding its settings in Luna does not create an account. On the connection editor's **AudioMuse** tab, enter:

- The service address and access token.
- The music-server name expected by the service, if it requires one.
- The default number of results, from 1 to 500. You can change the count for an individual request.

Once configured, the browser's **AudioMuse** context submenu offers:

- **Search by description...:** describe the music you want to find.
- **Generate playlist...:** request a generated collection from a description. Luna reports progress while waiting for the result.
- **Song Alchemy...:** start with selected songs or artists. Use Include and Exclude to add song seeds for music you want or want to avoid. Remove unwanted seeds, set the result count and choose Generate.

Results are matched to songs on the selected server and restricted to the chosen libraries. Unavailable songs are marked. You can play the results, add them to Luna's playback list or save them in a server playlist. Instant mix can also use AudioMuse when the music server cannot supply similar music.

### Browser shortcuts

These shortcuts apply while you are in the music-server browser. Most actions use the selected list rows. Bookmarking uses the currently playing song from the selected account. Change browser shortcuts under **Preferences > Keyboard Shortcuts > Music servers**, separately from the main player's shortcuts.

| Action | Shortcut |
| --- | --- |
| Open the browser from the main player | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>M</kbd> |
| Open a collection or play selected songs | <kbd>Enter</kbd> |
| Play selected albums, artists or playlists; show details for one song | <kbd>Shift</kbd>+<kbd>Enter</kbd> |
| Add selected items to Luna's playback list without playing | <kbd>Ctrl</kbd>+<kbd>Enter</kbd> |
| Set selected songs' rating | <kbd>Ctrl</kbd>+<kbd>R</kbd> |
| Show one song's lyrics | <kbd>Ctrl</kbd>+<kbd>L</kbd> |
| Bookmark the current song position | <kbd>Ctrl</kbd>+<kbd>B</kbd> |
| Create a server playlist | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd> |
| Direct download | <kbd>Ctrl</kbd>+<kbd>D</kbd> |
| Download options | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd> |
| Add to download queue | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> |
| Open the download queue | <kbd>Ctrl</kbd>+<kbd>Q</kbd> |
| Focus search | <kbd>Ctrl</kbd>+<kbd>F</kbd> |
| Refresh | <kbd>F5</kbd> |
| Back | <kbd>Alt</kbd>+<kbd>Left</kbd> |
| Focus Sort by | <kbd>Alt</kbd>+<kbd>B</kbd> |
| Focus Sort order | <kbd>Alt</kbd>+<kbd>O</kbd> |
| Rename or edit a supported selection | <kbd>F2</kbd> |
| Remove a supported selection | <kbd>Delete</kbd>, followed by confirmation |

Press <kbd>F1</kbd> in the browser or a server-playlist editor for help with that window.

## 15. Downloading online media

Luna's download commands work with YouTube, music-server songs and available podcast episodes, and supported direct HTTP or HTTPS file links. For the current online item, use the **File** menu. Luna chooses the appropriate options for its source.

| Command in the main player | Shortcut | Purpose |
| --- | --- | --- |
| **File > Download...** | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd> | Choose options or a destination for the current item. |
| **File > Direct download** | <kbd>Ctrl</kbd>+<kbd>D</kbd> | Start with the source's saved download settings. |
| **File > Add to download queue** | <kbd>Ctrl</kbd>+<kbd>Q</kbd> | Queue the current item with its defaults. |
| **Tools > Download queue...** | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> | Open the shared queue to start, monitor or stop work. |

YouTube results and music-server lists also offer downloads for selected items, so a file does not have to be playing before you download it. The music-server browser has its own queue shortcuts, listed in [Browser shortcuts](#browser-shortcuts).

### Options for each source

For **YouTube**, Download opens options for name, destination, video or audio output, format, quality and extra processing. Direct download and queueing use saved YouTube defaults. See [YouTube download options](#youtube-download-options) for the full list.

For **music-server files**, these commands request originals even when playback uses transcoding. Direct download uses the account's destination and artist/album folder settings. Download lets you change the destination and organization for that operation and give a single song an optional filename. Choose **Download** to start, or **Add to download queue** to retain the chosen options in queued jobs. In the browser, you can select several songs, albums or artists to download a collection. The server must permit downloading independently of streaming, and podcast episodes must already have a downloadable copy on the server. Closing the server browser does not stop these jobs.

For **other supported direct file links**, Download opens a native Save dialog for the destination and filename. Direct download and queueing use Luna's default `Downloads\Luna Player` folder. Luna copies the file as supplied; ordinary file links do not have YouTube format-conversion options. These download commands are unavailable for local files, Spotify tracks and live radio. Streaming playlist links such as M3U, M3U8 and MPD are also excluded from direct-file downloads.

An existing destination file is preserved: Luna chooses an unused numbered filename for a repeated download.

### Progress and completion

An immediate download opens a separate progress window. It reports progress, downloaded size, speed and estimated time when the source supplies enough information. YouTube jobs can continue with finishing work such as merging, conversion or embedding metadata after the file bytes have arrived. Wait for the final result before assuming the output is ready.

The progress window leaves the main player usable. **Minimize to queue** closes the individual view and keeps the same job running in the shared queue. On completion, the progress window closes and a success message gives the saved path; an error explains a failure. A download started from a result window does not require closing that window before completion is reported.

### Using the shared download queue

Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> in the main player or choose **Tools > Download queue...**. The list can contain YouTube downloads, music-server files and direct online files together, with each item's title and state. An item entered as a YouTube link is listed by that link when no media title is available.

- **Add...** accepts one YouTube video link per line. Add music-server or other direct-file jobs from the relevant browser or File menu instead.
- **Start** starts queued items and retries failed ones.
- **Stop** cancels the selected active download and returns it to the queue for a later start.
- **Stop all** cancels every active download and leaves unfinished items queued.
- <kbd>Delete</kbd> removes the selected item after confirmation. Removing an active item also cancels it.
- **Close** hides the queue without stopping active work.

The status box and total progress bar appear while the queue contains items. During a batch they show overall progress, combined speed and estimated time when available. A YouTube item can remain in a finishing state while its output is converted, merged, tagged or moved. The shared queue uses the **Simultaneous downloads** limit under YouTube Preferences, from one through sixteen active jobs.

Completed rows remain available until the queue window is closed, when they are cleared. Luna sends a desktop notification when all visible queue jobs finish successfully. Closing Luna with unfinished work asks whether to cancel it and exit or return to the player. The queue belongs to the current Luna session; unfinished jobs are not restored after a restart.

## 16. Radio browser

Choose **Tools > Radio browser...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>R</kbd> to find internet radio stations through the public [Radio Browser directory](https://www.radio-browser.info/). You do not need an account.

### Finding and playing a station

Use the **Categories** tree to choose what to browse, then move to the **Stations** list. Its columns show the station's name, country, language, codec and bitrate.

- **Global stations** searches across the directory.
- **Favorites** contains stations you saved in Luna.
- **Recently played** contains your local listening history.
- **Discover** contains **Most played**, **Trending**, **Most voted**, **Recently clicked** and **Recently updated** lists. These rankings refer to activity in the public directory, not just your own listening.
- **Countries** lets you browse by country and region. **Languages**, **Genres and tags**, and **Formats** offer other ways to narrow the station list.

Expand a category to load its choices. To search, enter a name or term in **Search** and activate the Search button or press <kbd>Enter</kbd> in the field. Select a station category, such as Global stations or a particular language, before searching to narrow its stations. Use **Clear search** to remove the current query. Additional entries load as you move towards the end of a list.

Select a station and choose **Play** or press <kbd>Enter</kbd> in the station list. The browser closes and the station plays in the main window. Previous and Next move through the station list you selected, rather than through unrelated local files. Reopen the radio browser to select a different category or station.

Press <kbd>I</kbd> in the main window to hear the broadcast title and <kbd>Shift</kbd>+<kbd>I</kbd> for the artist when the station supplies them. A live station may not provide a duration, seeking or complete track information. The station name and the current song information are separate: a broadcaster can keep playing even when it sends no artist or title.

### Filtering stations

Choose **Filters…** to restrict the results. Under **Apply to**, choose **This category** for a local restriction or **All categories** for a global one. Category restrictions are combined with global restrictions, so a station must satisfy both.

You can filter by a two-letter **Country code**, **Region**, **Language**, comma-separated **Tags**, **Codec**, and minimum or maximum bitrate. A bitrate of 0 means there is no limit at that end of the range. **Only working stations** hides stations whose last directory check failed; it is enabled by default. **HTTPS only** excludes ordinary HTTP streams. Choose **Apply** to use the displayed values, **Reset** to restore the displayed filter defaults, or **Cancel** to leave them unchanged.

If a category appears empty, clear its search and check both its category filters and the filters for all categories. A station marked working can still be temporarily offline or unavailable from your location.

### Favorites, details and station actions

With a station selected, press <kbd>Ctrl</kbd>+<kbd>Space</kbd> to add it to or remove it from Favorites. You can also use the station list's context menu. These favorites are kept in Luna's radio library, separately from **File > Favorite videos...**.

Choose **Station details** or press <kbd>Shift</kbd>+<kbd>Enter</kbd> to read the directory's information about the station. The context menu also offers **Open website**, **Copy stream URL**, **Copy station name**, and **Vote for station**. The website and stream URL are different: the website is the station's home page, while the stream URL points to its audio.

Use **Refresh** to reload the current view. **Clear history…** asks before removing your local Recently played history; it does not remove favorites or change the public directory.

## 17. Recording audio

Luna can record microphones and other inputs, everything played through an output device, or the audio of a particular program. Multiple sources can be mixed into one recording with separate volume levels.

### Fast recording with the default microphone

If no sources have been created in the current session, press <kbd>F9</kbd> to record the Windows default input device using the saved Recording Preferences. Press <kbd>F7</kbd> to pause or resume, and <kbd>F8</kbd> to stop.

A rising tone confirms that recording is starting. A falling tone confirms that it has stopped. Successful start and stop do not add a spoken message over those tones.

### Opening the recording interface

Choose **Recording > Open the recording interface...** or press <kbd>Alt</kbd>+<kbd>R</kbd>. The window has a source list, source controls, output settings, and Start/Pause controls. It leaves the main player usable. Source and output dialogs belong to the recording window and do not block playback controls in the main window.

Closing this window does not stop an active recording. Recording belongs to the application and continues until you use Stop, <kbd>F8</kbd>, or exit Luna.

### Adding sources

Activate **Add** and choose:

- **Input device...** for a microphone, line input, or other Windows capture device.
- **System output...** for everything playing through a selected speaker or headphone device.
- **Program...** for one program's audio. This requires Windows 10 version 2004 or later.

Give the source a descriptive name, choose the device or program, and set its volume. A device source can follow the current Windows default device rather than a named device.

For a program source, **Capture everything except this application** reverses the selection and captures other application audio instead. Only programs with a Windows audio session appear in the list; start audio in the target program if it is missing.

Use **Edit...** to change the selected source and **Remove...** to delete it from the session. The source volume slider changes its level in the final mix.

Sources are session-only. They remain when the recording window is closed and reopened during the same Luna session, but are not restored after Luna exits because devices and process identifiers may no longer represent the same sources.

### Recording format and destination

Choose the format, sample rate, channel count, quality where applicable, and destination folder:

- **WAV** is uncompressed and usually produces the largest file.
- **FLAC** is lossless and normally smaller than WAV.
- **MP3** and **AAC** are lossy and use the selected bitrate to trade size for quality.
- **Mono** is often sufficient for one microphone or speech.
- **Stereo** preserves separate left and right channels for music and system output.

The available sample rates, channel counts, and bitrates are obtained from the encoders installed with Windows. Choices therefore change with the selected format and can vary by system. Luna gives each recording a timestamped name to avoid replacing an earlier file.

Changes made inside the recording interface apply to its session. To change the defaults used after the next launch or by direct shortcuts, use **Preferences > Recording**.

### Starting, pausing, and stopping

The recording window requires at least one configured source before **Start** becomes available. Once recording starts, source and output controls are locked so the recording format and mix remain consistent.

- The **Start** button becomes **Stop** while recording.
- The **Pause** button becomes **Resume** while paused.
- <kbd>F9</kbd>, <kbd>F7</kbd>, and <kbd>F8</kbd> work from the main Luna window.
- The corresponding global shortcuts work even when another application has focus.

If only some sources can be opened, Luna starts with the available sources and lists those that failed. If none can be opened, recording does not start.

Choose **Recording > Open recordings folder** to open the active destination. The default folder is `Documents\Luna Player\Recordings`.

## 18. IPTV

IPTV sources and channels are under **Tools > IPTV**. Luna supports extended M3U or M3U8 playlists from a file or web address, Xtream Codes accounts, and Stalker portals.

### Managing sources

Choose **Tools > IPTV > Manage IPTV sources...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>I</kbd>. The source list provides **Open**, **Add...**, **Edit...**, and **Remove...** commands.

Each source has a name and type. Depending on that type, Luna asks for a playlist path or address, an Xtream server with username and password, or a Stalker portal with MAC address and optional serial. You can also supply an optional XMLTV programme-guide address. Xtream live channels can be opened as HLS instead of transport streams when **Open live channels as HLS** is enabled.

Saved IPTV credentials are protected for the current Windows user. Do not share the IPTV source data file because it still belongs to your account and computer.

### Browsing channels

Open a source from the manager, or choose **Tools > IPTV > Browse channels...** to reopen the current source. The channel browser can filter by live TV, movies, or series, by category, and by text typed into the Search box.

Press <kbd>Enter</kbd> on a selected row or activate **Play** to open it. When programme-guide information is available, press <kbd>Ctrl</kbd>+<kbd>I</kbd> to hear what is showing now and what follows. Press <kbd>F1</kbd> in the channel browser for a spoken summary of its controls.

## 19. Media converter

Choose **Tools > Media converter...** to convert one or more media files to an audio format. Add individual files or add a folder; adding a folder includes supported media in its subfolders. Duplicate paths are ignored.

The converter writes AAC, AC-3, AIFF, FLAC, M4A, MKA, MP3, OGA, OGG, Opus, WAV, and WMA. It chooses a sensible initial format from the first source file, but you can change it. Depending on the output format, you can also choose a sample rate, mono or stereo channels, bitrate, and Opus bitrate mode and profile.

Set **Maximum simultaneous conversions** to control how many files run at once. By default, converted files are written beside their sources. Clear **Preserve original file locations** to send all results to one destination folder.

**Delete original files after conversion** removes each source only after that file converts successfully. Use this option carefully; deleted originals are not sent to the Recycle Bin.

The progress window reports the current file, its percentage, the number of remaining files, total percentage, and estimated time. You can cancel the remaining work. When conversion ends, Luna reports complete success or lists the files that failed and why.

## 20. Sleep timer

Choose **Tools > Sleep timer > Set timer...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>S</kbd>. A timer can fire after a chosen duration or at the end of the current track. Choose whether it pauses playback or stops and unloads the current item.

For a duration, choose 15, 30, 45, 60, 90, or 120 minutes, or enter custom hours and minutes. An end-of-track timer requires an open item and prevents normal automatic advance when that item finishes.

Enable **Fade the volume out before firing** to lower the volume during the final 1 through 120 seconds. Luna restores the original volume after the timer fires or is cancelled; the faded value is not saved as your preferred volume.

- Press <kbd>Ctrl</kbd>+<kbd>T</kbd> or choose **Time remaining** to hear the remaining time or that the timer is waiting for the end of the track.
- Choose **Cancel timer** to turn it off. Opening the timer window while one is active also provides **Turn off timer**.
- Luna announces a one-minute warning for duration timers.

The last timer choices are remembered, but an active countdown does not survive closing Luna.

## 21. Preferences reference

Open Preferences with **File > Preferences...** or <kbd>Ctrl</kbd>+<kbd>P</kbd>. Select a page in the category tree. Choose OK to save and apply the settings you changed, or Cancel to discard unaccepted changes. Unchanged settings are not reapplied, so saving an unrelated preference does not reset temporary playback adjustments such as headphone spatialization.

Press <kbd>F1</kbd> while a setting has focus to hear a detailed explanation of that setting.

### General

| Setting | Purpose |
| --- | --- |
| Language | Uses the Windows display language or an included translation. Restart Luna after changing it. |
| Theme | Follows Windows or forces a light or dark appearance. High contrast always follows Windows. Restart Luna after changing it. |
| Start maximized | Opens the main window maximized on the next launch. Off by default, keeping the normal window size. |
| Remember last file position | Restores the last active local file and its position on the next launch. |
| Speak file name when navigating | Announces the item reached with Previous or Next. |
| Check for app updates on startup | Checks quietly after startup and prompts only for a ready, newer release. |
| Save settings on close | Saves session changes such as volume and speed when Luna exits. Explicitly accepted Preferences changes are still saved immediately. |
| Open files in the existing window when Luna is already running | Sends files opened from Windows to the current Luna window. Clear it to open each request in a separate window. |
| Disable system media controls | Removes Luna from the Windows media overlay and ignores hardware media keys. |
| Show the playing track in the window title | Shows the current item and whether it is paused in Luna's title bar. This is off by default to avoid repeated screen-reader announcements. |
| Interrupt the current line when a new subtitle appears | Lets a new spoken subtitle cut off the preceding one to keep pace with the picture. |
| Verbosity | Chooses full Beginner announcements or shorter Advanced announcements. |
| What would you like to open with files? | Controls whether one opened file brings in no neighbors, its folder, or its folder and subfolders. |
| Register/Unregister file extensions | Adds or removes Luna's supported media registrations for the current Windows user. |

### Backup and restore

| Command | Purpose |
| --- | --- |
| Export settings | Saves the currently applied preferences to a JSON file without moving the working copy. |
| Import settings | Loads a valid settings JSON file and replaces the values shown in Preferences. |
| Export bookmarks | Saves all bookmarks to a JSON file. |
| Import bookmarks | Replaces the current bookmark collection with a valid exported collection. |
| Reset settings | Restores every preference and shortcut to its default after confirmation. |
| Open user settings folder | Opens Luna's configuration directory in File Explorer. |

Import replaces the corresponding collection; it does not merge entries. Keep exported files somewhere separate from Luna's configuration folder if they are intended as backups.

### Audio

| Setting | Purpose |
| --- | --- |
| Custom seek value | Number of seconds used by the Custom seek amount; positive decimals such as 2.5 are accepted. |
| Speed step | Amount added or subtracted by each speed command. |
| Pitch step | Pitch change per press, from 0.001 through 12 semitones. |
| Volume step | Volume change per press, from 1 through 20 percentage points. |
| Pan step | Left/right movement per press, from 1 through 100 percentage points. |
| What happens after a file ends? | Advances, loops, or stops at the end. |
| Wrap to top for multiple files | Connects the end and beginning of a multi-item list. |
| Save current position for each file | Maintains a separate resume position for each local item. |
| Enable dynamic normalize and limiter | Evens loudness and restrains peaks. |
| Play audio as Mono | Mixes left and right channels into mono. |

#### Headphone spatialization defaults

This page selects the SOFA dataset and the default enabled state, gain, rotation, elevation and direction steps. Luna does not include a dataset. See [Headphone spatialization](#headphone-spatialization) in the audio-processing section for direct downloads, setup instructions and playback shortcuts.

#### Fades and gapless playback

Under **Audio > Playback**, the volume-fading options control the start and end of files, pausing and resuming, and seeking. **Volume fading interval (milliseconds)** sets the ordinary fade duration; 0 disables these fades. **Enable volume fading during seeking** uses the separate **Seek fading interval (milliseconds)**, from 1 to 10000. Fading lowers the sound around an operation; it does not overlap two tracks.

**Gapless playback mode** controls whether Luna keeps the audio output open between tracks. **Disabled** uses ordinary file changes. **Compatible files** preserves the original audio format and joins tracks when their formats are compatible. **Always when possible** can adapt later tracks to the first track's output format so playback can continue through that output. Choose Compatible files if you want to preserve each track's format. Online playback also requires the next stream to be ready in time. See [Spotify](#13-spotify) for playback from a Spotify collection.

#### Cross-fading

Under **Audio > Playback**, select **Enable cross-fading for audio files** to overlap the end of one audio file with the beginning of the next. Cross-fading is off by default and excludes files containing video. Album cover art does not count as video.

Set **Cross-fading duration (milliseconds)** from 1 to 30000; the default is 3000, or three seconds. Choose **Equal-power** for an even perceived loudness during the overlap, or **Linear** for a constant rate of gain change. Equal-power can produce higher peaks.

Select **Enable cross-fading during manual navigation** to use overlaps when moving between audio files yourself, including Previous, Next and file-number commands. This option is off by default. Navigation while paused uses an ordinary file change. The same cross-fading settings also apply to Spotify tracks.

Cross-fading works independently of the gapless setting and takes precedence over ordinary file-start and file-end fades during an overlap. Active selections, A-B loops, repeat-file mode and end-of-track sleep timers prevent automatic transitions. Online audio uses an overlap only when the next stream is ready; short files or late streams may shorten it. Longer durations require more decoded audio buffering.

### Silence removal

See [Silence removal and audio processing](#8-silence-removal-and-audio-processing) for the basic and advanced controls.

### YouTube

| Setting | Purpose |
| --- | --- |
| Video quality | Chooses the preferred picture height for video playback. |
| Audio quality | Chooses the preferred bitrate for audio-only playback. |
| Number of search results | Sets the initial target from 5 through 100; more results load at the end. |
| Show search suggestions while typing | Fetches and displays possible completions for a YouTube query. |
| Video+playlist link behavior | Asks each time, always plays the video, or always opens the playlist. |
| Download folder | Sets the base destination, or uses `Downloads\Luna Player` when left empty. |
| Sort downloads by type | Places files in `videos` and `audios` subfolders. |
| Default download type | Chooses video or audio for direct downloads and the initial Download dialog selection. |
| Download video quality and format | Sets the height cap and MP4 or MKV container used by default. |
| Download audio quality and format | Sets the bitrate and MP3, M4A, Opus, FLAC, or WAV format used by default. |
| Concurrent fragments | Sets how many pieces of one file yt-dlp fetches at once, from 1 through 16. |
| Simultaneous downloads | Sets how many queue items may run at once, from 1 through 16. |
| Embed metadata, thumbnail, chapters, or subtitles | Adds the selected information to downloaded files where the output format supports it. |
| Remove SponsorBlock segments | Cuts supported sponsor, intro, and similar sections from downloads. |
| Cookie file / Import from Firefox | Supplies account cookies for videos that require them. Only one cookie source can be active. |
| yt-dlp update channel | Chooses Stable, Nightly, or Master. |
| Check for yt-dlp updates on startup | Performs a quiet component check after launch. |
| Download YouTube components | Installs or refreshes yt-dlp and supporting tools. |

### Spotify

Set your developer app's **Client ID** and the preferred **Audio quality** here. The page includes the exact redirect URI to register and instructions for getting the Client ID. See [Spotify](#13-spotify) for account setup, signing in, browsing and playback.

### Discord Rich Presence

Rich Presence lets people viewing your Discord profile see what you are playing in Luna.
Sharing is off by default. You choose whether to include media details, artwork and links.

#### Enable activity sharing

1. Open **Discord Desktop** and sign in. Go to **User Settings > Activity Privacy** and turn on
   **Share your detected activities with others**.
2. In Luna, open **Preferences > Discord**, select **Enable Discord Rich Presence**, and choose **OK**.

Discord must be running on the same computer. Its browser version cannot receive Luna's activity.
Turning the option off clears the activity without interrupting playback.

#### What appears on your profile

The activity line identifies the source, such as **Listening to music on Luna**, **Listening to YouTube on Luna**, **Listening to radio on Luna**, or **Listening to Spotify
on Luna**. Listening cards use **Luna Player** as their heading. Below it, Luna shows the media title and a separate
artist credit when that information is available. Local music uses the title stored in the file; if it has
no title, Luna uses the filename without its extension. Radio shows the station name and the current
broadcast artist, when the station supplies one.

With audiobook mode enabled, files containing chapters appear as **Listening to an audiobook on Luna**.
Files without chapters use the normal music activity. The title line for an audiobook shows
the book title and the line below shows the current chapter instead of an artist credit. The book title comes from its album or title metadata,
with the filename used when neither is available.

Pausing changes the heading to **[Paused]: Luna Player**. The title and artist or chapter remain in
their own lines, without another paused label after them. The source phrase stays the same while
paused.

YouTube uses the video's thumbnail. Spotify uses album artwork, and radio uses the station's image,
when available. Local files and books use Luna's application icon. Artwork labels contain the artist
or channel name without an added "By" prefix or application version. Listening cards show a separate **By artist** credit
below the title when an artist is available. YouTube video uses **Watching YouTube on Luna**, while audio-only playback uses
**Listening to YouTube on Luna**. Video playback keeps its thumbnail and **Watch on YouTube** button.
YouTube's Watching card uses **Luna Player** as its heading, changing to **[Paused]: Luna Player** when
paused. The video title appears below the heading. The artwork label contains the plain artist or
channel name when known. Discord may shorten long titles.

For media with a known duration, Luna supplies both progress timestamps using the same elapsed
and total media time as the player, without dividing either by playback speed. Discord's Listening card
shows elapsed and total time, including when audiobook mode is enabled. Paused media remains visible with a **Paused**
label and no running timer. Resuming, seeking, changing speed or moving to another chapter updates
the timer. Live streams show elapsed time when there is no known duration.

#### Sharing controls

| Setting | Purpose |
| --- | --- |
| Enable Discord Rich Presence | Turns sharing on or off. |
| Share media titles, artists and artwork | Includes titles, artist credits, book chapters and online artwork. Clear this to show generic playback status and hide source links. |
| Show link buttons | Adds **Download Luna Player** and, when media details are shared, a source button for YouTube, Spotify or a public radio stream. |
| Show activity when idle | Shows **Idle** when playback is stopped or no media is loaded. Clear this to remove the activity while idle. Pausing does not count as idle. |

For radio, **Watch online** opens the stream itself in the viewer's browser. Some browsers may download
or reject a stream they cannot play. Luna omits private addresses, links containing credentials and
links with unrecognized query parameters. Local file paths are never used as buttons. Clearing
**Show link buttons** removes both buttons without hiding the artwork.

#### If activity is missing

Check that both Discord's activity sharing and Luna's Rich Presence option are enabled. Luna samples
playback once per second and sends state changes without a fixed four-second delay. Rapid changes
are combined when Discord's send limit is reached. Luna reconnects when Discord starts or restarts; allow
about 15 seconds. If Discord is unavailable, playback continues normally.

Discord may hide your own activity buttons when you view your profile. Another person viewing the
profile can see the buttons.

### Recording

This page sets the defaults used by direct recording shortcuts and seeds the recording interface at the beginning of each Luna session. It contains audio format, sample rate, channels, audio quality for compressed formats, and the recordings folder.

### Keyboard Shortcuts

This page lists every action that can run while Luna's main window has focus. Each action can have a primary shortcut; a few actions also have a predefined secondary slot.

1. Select an action.
2. Activate **Edit Primary Shortcut** or, where available, **Edit Secondary Shortcut**.
3. Press the desired combination. Press <kbd>Escape</kbd> to cancel capture.
4. Press OK in Preferences to apply the changes.

Immediately after the shortcut list, **Disable secondary shortcuts** turns off every assignment in the Secondary Shortcut column. The keys then keep their normal interface behavior; for example, Tab and Shift+Tab can move focus instead of changing tracks. Primary and global shortcuts are unaffected. Your secondary assignments are kept and become active again when you clear the checkbox and choose OK. Page Up and Page Down are the primary Previous and Next shortcuts.

The **Music servers** child page contains the shortcuts used inside that browser. It has separate assignments for opening collections, playing or adding songs, rating, lyrics, bookmarks, server playlists, downloads and browsing navigation. Changing these does not change the main player's shortcuts. See [Browser shortcuts](#browser-shortcuts) for the defaults.

Luna rejects a combination already assigned to another action in the same shortcut set. Local shortcuts can use printable keys, navigation keys, function keys F1 through F24, and Ctrl, Shift, or Alt modifiers. Use **Reset to Defaults** to restore the complete local shortcut page after confirmation.

### Global Shortcuts

Global shortcuts work while another application has focus, whether Luna is minimized or simply behind another window after Alt+Tab. They do not run while Luna or one of its dialogs has focus; use local shortcuts there. Global commands are limited to playback, navigation, volume, and recording actions that are useful outside Luna.

Select an action, activate **Edit Shortcut**, and press the desired combination. Global shortcuts can also use the Windows key. Luna passes these combinations on to Windows, so an operating-system shortcut with the same keys can still perform its own action. Choose a different combination if this causes an unwanted side effect.

Global shortcuts are separate from local shortcuts. Changing one does not change the other.

## 22. Application updates

### Checking manually

Choose **Help > Updates > Check for app updates**. A cancellable progress dialog appears while Luna checks the release information.

If a newer release is ready, Luna shows the installed and available versions and a scrollable list of changes. Choose **Update** to download it or **Later** to leave the current version unchanged.

Luna verifies that the actual installer or portable archive exists before offering the update. If release information was published before its package finished uploading, a manual check asks you to try again later rather than presenting a download that will fail.

### Startup checks

Enable **Check for app updates on startup** for a silent background check. It displays no progress window or no-update message; a prompt appears only when a newer compatible package is available.

### Installing a downloaded update

The download window shows total size, downloaded size, percentage, and a Cancel button. It leaves the main player usable, so you can pause, stop or change playback while waiting. After a successful download, Luna starts its small updater helper and closes:

- An installed copy starts the update installer. Its installation progress remains visible, and Luna opens when installation completes.
- A portable copy replaces the extracted application files and opens the updated player.

Do not start another Luna process while the update is being applied.

## 23. Help and release information

- **Help > User guide** or <kbd>F1</kbd> opens the installed guide that matches Luna's interface language. If no matching regional or base-language guide is installed, Luna opens the English guide.
- **Help > About** shows a description, version, copyright information, and a link to the project website.
- **Help > Release notes** opens the GitHub release page for the installed version in your default browser.

The user guide is installed as local HTML and does not require an internet connection. Release notes and the project website do require one.

## 24. Settings and user data

Luna stores user data under `%APPDATA%\Luna Player`:

| File | Contents |
| --- | --- |
| `settings.json` | Preferences and shortcut overrides |
| `bookmarks.json` | Named bookmarks for local files |
| `positions.json` | Per-file playback positions |
| `equalizer.json` | User-created presets and changes to supplied equalizer presets |
| `favorites.json` | Favorite YouTube and stream links |
| `iptv-sources.json` | Saved IPTV sources, with account fields protected for the current Windows user |
| `recents.json` | Recently opened files, folders, and playlists |
| `Playlists/` | Named JSON playlists, including descriptions and folder references |
| `subsonic-servers.json` | Saved music-server connections and last selected account, with account secrets protected for the current Windows user |
| `radio.json` | Favorite radio stations and local radio listening history |
| `spotify-preview-searches.txt` | Recent Spotify search queries |
| `spotify-token.dat` | Spotify browsing authorization, protected for the current Windows user |
| `spotify-playback-credentials.dat` | Spotify playback authorization, protected for the current Windows user |

Use **Preferences > Backup and restore > Open user settings folder** to open the exact folder rather than typing the path.

Do not edit these files while Luna is running. Use the export and import controls for settings and bookmarks whenever possible.

If `settings.json` cannot be read or contains invalid values, Luna starts with defaults, shows the reason, and protects the existing file from being overwritten. Import a valid settings backup or use **Reset settings** to replace it deliberately. Invalid bookmarks, favorites, equalizer presets, IPTV sources, and recents are also reported instead of being silently replaced.

## 25. Complete default shortcut reference

These are the factory defaults. Your current bindings may differ after customization. The authoritative list for your installation is under **Preferences > Keyboard Shortcuts** and **Global Shortcuts**.

### Files, information, and Preferences

| Action | Primary shortcut | Secondary shortcut |
| --- | --- | --- |
| Open files | <kbd>Ctrl</kbd>+<kbd>O</kbd> | — |
| Open a DVD disc | None | — |
| Add files to the current list | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>F</kbd> | — |
| Add a folder to the current list | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>D</kbd> | — |
| Open a network stream link | <kbd>Ctrl</kbd>+<kbd>L</kbd> | — |
| Open all files in a folder | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd> | — |
| Show current file in File Explorer | <kbd>Ctrl</kbd>+<kbd>F</kbd> | — |
| Windows file properties | <kbd>Alt</kbd>+<kbd>Enter</kbd> | — |
| Opened Files dialog | <kbd>F2</kbd> | — |
| Close current file | <kbd>Ctrl</kbd>+<kbd>W</kbd> | — |
| Close all files | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>W</kbd> | — |
| Preferences | <kbd>Ctrl</kbd>+<kbd>P</kbd> | — |
| Speak current file information | <kbd>F</kbd> | — |
| Speak current media title | <kbd>I</kbd> | — |
| Exit Luna | None | — |

### Playlist navigation and modes

| Action | Primary shortcut | Secondary shortcut |
| --- | --- | --- |
| Previous file | <kbd>Page Up</kbd> | <kbd>Shift</kbd>+<kbd>Tab</kbd> |
| Next file | <kbd>Page Down</kbd> | <kbd>Tab</kbd> |
| First file | <kbd>Ctrl</kbd>+<kbd>Home</kbd> | — |
| Go to file number | <kbd>Ctrl</kbd>+<kbd>G</kbd> | — |
| Last file | <kbd>Ctrl</kbd>+<kbd>End</kbd> | — |
| Toggle shuffle | <kbd>Ctrl</kbd>+<kbd>Z</kbd> | — |
| Toggle repeat file | <kbd>Ctrl</kbd>+<kbd>R</kbd> | — |

### Playback and seeking

| Action | Primary shortcut | Secondary shortcut |
| --- | --- | --- |
| Play or pause | <kbd>Space</kbd> | <kbd>Enter</kbd> |
| Rewind one step | <kbd>Left</kbd> | — |
| Forward one step | <kbd>Right</kbd> | — |
| Rewind two steps | <kbd>Shift</kbd>+<kbd>Left</kbd> | — |
| Forward two steps | <kbd>Shift</kbd>+<kbd>Right</kbd> | — |
| Beginning | <kbd>Home</kbd> | — |
| End | <kbd>End</kbd> | — |
| Go to time | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>G</kbd> | — |
| Choose sound card | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>A</kbd> | — |
| Enter or leave full screen | <kbd>F11</kbd> | — |
| Open audio track list | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> | — |
| Previous chapter | <kbd>Ctrl</kbd>+<kbd>Page Up</kbd> | — |
| Next chapter | <kbd>Ctrl</kbd>+<kbd>Page Down</kbd> | — |
| Speak current chapter | <kbd>C</kbd> | — |
| Audiobook mode | None | — |
| Stop at current chapter end | None | — |
| Previous audio track | <kbd>Shift</kbd>+<kbd>Page Up</kbd> | — |
| Next audio track | <kbd>Shift</kbd>+<kbd>Page Down</kbd> | — |
| Turn subtitle reading on or off | <kbd>Ctrl</kbd>+<kbd>U</kbd> | — |
| Load subtitle from file | <kbd>Ctrl</kbd>+<kbd>J</kbd> | — |
| Load subtitle from a web address | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>J</kbd> | — |
| Remember subtitle for this session | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>U</kbd> | — |

### Seek amount selection

| Action | Shortcut |
| --- | --- |
| Select 1-second step | <kbd>Shift</kbd>+<kbd>1</kbd> |
| Select 5-second step | <kbd>Shift</kbd>+<kbd>2</kbd> |
| Select 10-second step | <kbd>Shift</kbd>+<kbd>3</kbd> |
| Select 20-second step | <kbd>Shift</kbd>+<kbd>4</kbd> |
| Select 30-second step | <kbd>Shift</kbd>+<kbd>5</kbd> |
| Select 1-minute step | <kbd>Shift</kbd>+<kbd>6</kbd> |
| Select 2-minute step | <kbd>Shift</kbd>+<kbd>7</kbd> |
| Select 3-minute step | <kbd>Shift</kbd>+<kbd>8</kbd> |
| Select 5-minute step | <kbd>Shift</kbd>+<kbd>9</kbd> |
| Select 10-minute step | <kbd>Shift</kbd>+<kbd>0</kbd> |
| Select custom step | <kbd>Shift</kbd>+<kbd>-</kbd> |

### Percentage jumps

| Position | Shortcut | Position | Shortcut |
| --- | --- | --- | --- |
| 10% | <kbd>Ctrl</kbd>+<kbd>1</kbd> | 15% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>1</kbd> |
| 20% | <kbd>Ctrl</kbd>+<kbd>2</kbd> | 25% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>2</kbd> |
| 30% | <kbd>Ctrl</kbd>+<kbd>3</kbd> | 35% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>3</kbd> |
| 40% | <kbd>Ctrl</kbd>+<kbd>4</kbd> | 45% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>4</kbd> |
| 50% | <kbd>Ctrl</kbd>+<kbd>5</kbd> | 55% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>5</kbd> |
| 60% | <kbd>Ctrl</kbd>+<kbd>6</kbd> | 65% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>6</kbd> |
| 70% | <kbd>Ctrl</kbd>+<kbd>7</kbd> | 75% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>7</kbd> |
| 80% | <kbd>Ctrl</kbd>+<kbd>8</kbd> | 85% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>8</kbd> |
| 90% | <kbd>Ctrl</kbd>+<kbd>9</kbd> | 95% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>9</kbd> |
| 100% | <kbd>Ctrl</kbd>+<kbd>0</kbd> | 100% alternative | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>0</kbd> |

### Volume and spoken status

| Action | Shortcut |
| --- | --- |
| Increase volume | <kbd>Up</kbd> |
| Decrease volume | <kbd>Down</kbd> |
| Set volume to maximum | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Up</kbd> |
| Set volume to minimum | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Down</kbd> |
| Speak volume | <kbd>V</kbd> |
| Speak elapsed time | <kbd>E</kbd> |
| Speak remaining time | <kbd>R</kbd> |
| Speak total duration | <kbd>T</kbd> |
| Speak position percentage | <kbd>P</kbd> |
| Speak playback speed | <kbd>S</kbd> |
| Switch Beginner/Advanced verbosity | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> |

### Speed, pitch, pan, loops, and filters

| Action | Shortcut |
| --- | --- |
| Increase playback speed | <kbd>Ctrl</kbd>+<kbd>Up</kbd> |
| Decrease playback speed | <kbd>Ctrl</kbd>+<kbd>Down</kbd> |
| Reset playback speed | <kbd>Alt</kbd>+<kbd>Y</kbd> |
| Raise pitch | <kbd>Shift</kbd>+<kbd>Up</kbd> |
| Lower pitch | <kbd>Shift</kbd>+<kbd>Down</kbd> |
| Reset pitch | <kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>P</kbd> |
| Speak pitch | <kbd>Shift</kbd>+<kbd>P</kbd> |
| Pan left | <kbd>Ctrl</kbd>+<kbd>Left</kbd> |
| Pan right | <kbd>Ctrl</kbd>+<kbd>Right</kbd> |
| Speak pan | <kbd>B</kbd> |
| Toggle headphone spatialization | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> |
| Rotate headphone spatialization left | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Left</kbd> |
| Rotate headphone spatialization right | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Right</kbd> |
| Raise headphone spatialization elevation | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Up</kbd> |
| Lower headphone spatialization elevation | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Down</kbd> |
| Toggle silence removal | <kbd>Ctrl</kbd>+<kbd>M</kbd> |
| Set A-B loop start | <kbd>[</kbd> |
| Set A-B loop end | <kbd>]</kbd> |
| Clear A-B loop | <kbd>Backspace</kbd> |

### Editing and marked files

| Action | Shortcut |
| --- | --- |
| Rename current file | <kbd>Shift</kbd>+<kbd>F2</kbd> |
| Delete current file | <kbd>Shift</kbd>+<kbd>Delete</kbd> |
| Copy current file to clipboard | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> |
| Paste/open file or folder from clipboard | <kbd>Ctrl</kbd>+<kbd>V</kbd> |
| Mark or unmark current file | <kbd>Ctrl</kbd>+<kbd>K</kbd> |
| Mark or unmark all files | <kbd>Ctrl</kbd>+<kbd>A</kbd> |
| Clear all marks | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>K</kbd> |
| Speak marked-file count | <kbd>K</kbd> |
| Copy marked files to a folder | None |
| Move marked files to a folder | None |
| Copy marked files to clipboard | None |
| Delete marked files | None |

### Bookmarks

| Action | Shortcut |
| --- | --- |
| Add bookmark for the current local file or server song | <kbd>Ctrl</kbd>+<kbd>B</kbd> |
| Manage local bookmarks | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>B</kbd> |
| Jump to bookmark slots 1–9 | <kbd>Alt</kbd>+<kbd>1</kbd> through <kbd>Alt</kbd>+<kbd>9</kbd> |
| Jump to bookmark slot 10 | <kbd>Alt</kbd>+<kbd>0</kbd> |

### Online services, tools, recording, updates, and help

| Action | Shortcut |
| --- | --- |
| Open YouTube link | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd> |
| Search YouTube | <kbd>Ctrl</kbd>+<kbd>Y</kbd> |
| Favorite videos | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd> |
| Open the download queue | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> |
| Add the current downloadable online item to the queue | <kbd>Ctrl</kbd>+<kbd>Q</kbd> |
| Direct-download the current online item | <kbd>Ctrl</kbd>+<kbd>D</kbd> |
| Choose download options for the current online item | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd> |
| Show current video description | <kbd>Alt</kbd>+<kbd>D</kbd> |
| Copy current video link | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> |
| Open current video in browser | None |
| Open current video's channel in browser | None |
| Browse current video's channel in Luna | None |
| Open radio browser | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>R</kbd> |
| Open music-server browser | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>M</kbd> |
| Open Spotify browser | <kbd>Alt</kbd>+<kbd>S</kbd> |
| Spotify preferences | None |
| Open media converter | None |
| Manage IPTV sources | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>I</kbd> |
| Browse IPTV channels | None |
| Set sleep timer | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>S</kbd> |
| Cancel sleep timer | None |
| Speak sleep timer remaining | <kbd>Ctrl</kbd>+<kbd>T</kbd> |
| Open recording interface | <kbd>Alt</kbd>+<kbd>R</kbd> |
| Start recording | <kbd>F9</kbd> |
| Pause or resume recording | <kbd>F7</kbd> |
| Stop recording | <kbd>F8</kbd> |
| Open recordings folder | None |
| Open user guide | <kbd>F1</kbd> |
| About Luna | None |
| Open release notes | None |
| Check for app updates | None |
| Update YouTube components | None |

### Default global shortcuts

These commands work while another application has focus, including when Luna is minimized. They are ignored while Luna or one of its dialogs has focus.

| Action | Global shortcut |
| --- | --- |
| Play or pause | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Space</kbd> |
| Rewind one step | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Left</kbd> |
| Forward one step | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Right</kbd> |
| Increase volume | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Up</kbd> |
| Decrease volume | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Down</kbd> |
| Previous file | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Page Up</kbd> |
| Next file | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Page Down</kbd> |
| Start recording | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F9</kbd> |
| Pause or resume recording | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F7</kbd> |
| Stop recording | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F8</kbd> |

## 26. Supported formats

Luna filters file and folder selection using the following extensions. Playback is handled by mpv, so other formats supported by that engine may also play when opened directly.

**Audio:** AAC, AC-3, AIFF, ALAC, APE, AU, DTS, E-AC-3, FLAC, M4A, M4B, MKA, MP1, MP2, MP3, MPC, OGA, OGG, OGM, Opus, TAK, TrueHD (`.thd`), TTA, WAV, WMA, and WavPack.

**Video:** 3G2, 3GP, AVI, FLV, IVF, M2TS, M4V, MJ2, MKV, MOV, MP4, MPEG, MPG, MXF, OGV, RMVB, TS, WebM, WMV, and Y4M.

**Playlists:** M3U and M3U8, including HLS streams, and local Luna JSON playlists with descriptions and folder references.

**Discs:** DVD-Video and DVD-Audio through **File > Open DVD...**, DVD AutoPlay, or Explorer's DVD play action. Disc menus, Blu-ray discs and audio CDs are not supported by this disc-opening feature.

## 27. Troubleshooting

### A file or folder opens fewer items than expected

Check **Preferences > General > What would you like to open with files?** The default opens only the selected file. Folder commands include only extensions recognized as media. Inaccessible folders and system folders are skipped during recursive scans.

### A live stream has no duration or percentage

Live streams often do not publish a fixed length. Go to time, percentage jumps, remaining time, and seeking to the end require a known duration and may be unavailable.

### A YouTube video does not open

Try these steps in order:

1. Confirm that the address opens in a browser and names a video or playlist rather than a channel.
2. Choose **Help > Updates > Update YouTube components**.
3. If the components have not been installed, choose **Preferences > YouTube > Download YouTube components**.
4. Try the Stable update channel first; use Nightly or Master only if Stable cannot handle a recent service change.
5. For an age-restricted, members-only, or verification-protected video, configure a cookie file or Firefox cookies in YouTube Preferences.

### A YouTube download finishes downloading but continues working

After yt-dlp receives all media data, it may still need to merge video and audio, convert the result, embed information, process chapters, or move the finished file. The progress window title and text name this work. Do not cancel merely because the percentage has reached 100%.

### A download continues after its progress window closes

Choosing **Minimize to queue** deliberately transfers the visible progress to **Tools > Download queue...**. Closing the queue window also leaves active work running. Stop the item from the queue if you no longer want it, or close Luna and confirm that unfinished downloads should be cancelled.

### IPTV has no programme information

Programme information appears only when the provider supplies it or the source has a matching XMLTV EPG address. Channel identifiers in the guide must match the source's identifiers. Playback can still work when no programme guide is available.

### Spotify will not sign in or load a collection

Check the Client ID under **Preferences > Spotify** and the redirect URI in your developer app. The URI must be exactly `http://127.0.0.1:43891/callback/`, including the final slash. Follow both browser authorizations if Luna asks: browsing and audio playback sign in separately. If an authorization has expired, sign in again.

If Spotify refuses a playlist or collection, check that your account can access it and is allowed to use your developer app. Development-mode apps cannot access every kind of Spotify content. If Luna reports a request limit, wait for the indicated time before retrying; repeatedly refreshing does not remove the limit. A collection that cannot load because Spotify restricts it cannot be made available by changing Luna's audio quality.

### A radio station does not play or has no track information

Try **Refresh** in the radio browser and select another station. The public directory's last successful check does not guarantee that a stream is available now or in your region. If no stations appear, clear the search and inspect both category and global filters. **Only working stations** and **HTTPS only** can exclude stations you expected to find.

Not every station sends song titles or artists. Missing or unchanged broadcast information does not necessarily mean playback has stopped. Use **Station details** to inspect the directory entry, or **Open website** from the station's context menu to check the broadcaster's own site.

### Headphone spatialization cannot open a dataset

Download a `.sofa` dataset from the links in [Headphone spatialization](#headphone-spatialization), then select the file itself in Preferences. A saved HTML page, compressed archive, or ordinary audio file is not an HRTF dataset. Keep the selected file in place; settings backups do not contain it. If a valid dataset cannot be opened from a path containing non-ASCII characters, try a folder and filename using plain English letters and select it again.

To compare directions, listen through headphones, turn off **Play audio as Mono**, and start at an elevation near 0 degrees. Lower the gain if the sound distorts. If front/back or height cues are unconvincing, try another dataset rather than assuming that a larger file will sound better for you.

### Recording cannot find a program

Program capture requires Windows 10 version 2004 or later. The program must also have an active Windows audio session. Start playback in that program, reopen the source dialog, and choose it from the refreshed list.

### Recording starts without one of its sources

Luna can continue when at least one source opens. Read the warning listing failed sources, then check whether their devices are connected, enabled, and available. Stop the recording before editing the source list.

### A global shortcut does not work

Global shortcuts are active only while another application has focus. Switch to another application with Alt+Tab or minimize Luna, then use the shortcut. While Luna or one of its dialogs has focus, use the local shortcuts instead. Check the binding under **Preferences > Global Shortcuts**. If the same combination also performs a Windows action, assign different keys; Luna does not suppress the operating system's handling of these combinations.

### Settings will not save after a startup error

Luna protects an invalid or unreadable `settings.json` rather than overwriting it. Open Preferences and either import a valid settings backup or choose **Backup and restore > Reset settings**. If you need the original for diagnosis, copy it from the user settings folder first.

### A music server connects, but a category or command is missing

Open the saved account's **Browsing** tab and check **Visible browser sections** and any library restriction. Different servers implement different Subsonic features, and account permissions can limit playlists, downloads, podcasts or administration. Refresh the browser after correcting the settings or server permissions. **Server information** shows the version and extensions reported by the server.

### A saved playlist contains an unavailable server song

The Luna JSON playlist needs its original saved server connection. Check that the account still exists in Manage servers, test its address and credentials, and confirm the song still exists on that account. Removing a connection and creating another one does not repair references to the removed account automatically. Refresh the browser after fixing access to retry unavailable songs. Luna requests a fresh stream when it plays a server reference; temporary streaming links are not stored in the playlist.

### A server change replaces songs in the current queue

Check **Synchronize queue and position automatically** in the account's Playback tab. When enabled, changes made by other clients are adopted without another prompt for that account's songs. Turn it off to use **Save queue to server** and **Load queue from server** explicitly. Named server playlists and Luna's local saved playlists are separate from this synchronized queue.

### Bookmarks or favorites report invalid data

An invalid storage file is preserved rather than silently replaced. Open the user settings folder, make a copy of the affected JSON file, and restore a known-good backup. Bookmarks can be restored through Preferences. Favorites currently require restoring the `favorites.json` file itself while Luna is closed.

### The user guide does not open

The installed guide should be at `docs\<language-code>\user-guide.html` beside the executable. Re-extract the complete portable archive or repair/reinstall the application if the `docs` folder is missing. Luna falls back from a regional language to its base language and then to English.

## 28. Publisher and licensing

Luna Player is published by **Diamond Star**.

Copyright © 2026 Diamond Star.

Luna Player is free software licensed under the GNU General Public License, version 3 or (at your option) any later version (GPL-3.0-or-later). The translated mpv binding in `src/Mpv.cs` is licensed under the GNU Lesser General Public License, version 2.1 or later.

Luna Player also uses separately licensed third-party components, including mpv, FFmpeg, wxWidgets, Prism, NAudio, and PyYt. Each component remains under its own license. See `NOTICE.txt`, installed alongside Luna Player, for copyright statements, component versions, license names, and source locations. The relevant license texts are installed in the `licenses` folder.

The license summary in this guide is provided for convenience. `LICENSE.txt`, `NOTICE.txt`, and the files in the `licenses` folder contain the authoritative terms and attributions. In the source repository, the corresponding top-level files are named `LICENSE` and `NOTICE`.

## 29. Contact and support

Luna Player is developed and published by Diamond Star. You can use the following channels:

- **Email:** [ramymaherali55@gmail.com](mailto:ramymaherali55@gmail.com)
- **Telegram:** [Contact Diamond Star on Telegram](https://t.me/diamondStar35)
- **Bug reports:** [Luna Player issues on GitHub](https://github.com/diamondStar35/luna_player/issues)
- **Source code and releases:** [Luna Player on GitHub](https://github.com/diamondStar35/luna_player)

For a bug report, include the Luna version shown under **Help > About**, your Windows version, what you expected, what happened, and the shortest steps that reproduce the problem. Include the exact text of any error message. Remove private file names, web addresses, account details, and other personal information from logs or screenshots before sharing them publicly.
