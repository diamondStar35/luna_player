# Luna Player User Guide

Luna Player is a keyboard-friendly audio and video player for Windows. It plays local media, network streams, YouTube and IPTV; reads subtitles aloud; downloads and converts media; records audio; and provides file management, bookmarks, speech announcements, and configurable local and system-wide shortcuts.

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

Luna integrates with the Windows media overlay and compatible hardware media keys. The overlay can show the current title and timeline. Play, pause, previous, next, rewind, and fast-forward buttons invoke the corresponding Luna commands.

If a keyboard, headset, or another program sends media commands to the wrong application or sends them twice, enable **Disable system media controls** on the General Preferences page. This removes Luna from the Windows media overlay and stops it from handling hardware media keys.

## 3. Quick start

To play a local file:

1. Press <kbd>Ctrl</kbd>+<kbd>O</kbd>.
2. Choose a media file or an M3U/M3U8 playlist.
3. Press <kbd>Space</kbd> to pause or resume.
4. Press <kbd>Left</kbd> or <kbd>Right</kbd> to seek.
5. Press <kbd>Up</kbd> or <kbd>Down</kbd> to change the volume.

To play every supported file in a folder, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd> and choose the folder. Use <kbd>Tab</kbd> and <kbd>Shift</kbd>+<kbd>Tab</kbd> to move between loaded items.

To play a YouTube video, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd> and enter its address. To search instead, press <kbd>Ctrl</kbd>+<kbd>Y</kbd>.

## 4. Opening media

### Opening a file

Choose **File > Open File...** or press <kbd>Ctrl</kbd>+<kbd>O</kbd>. The dialog accepts supported audio, video, M3U, and M3U8 files.

The **What would you like to open with files?** setting controls what happens when one media file is opened from Windows or from the file dialog:

- **Open the file only** loads only the selected file.
- **Open the file and the main folder files** loads the supported files in the same folder and selects the requested file.
- **Open the file with the main and subfolder files** scans the folder and all folders below it, loads every supported media file found, and selects the requested file.

A recursive scan has a cancellable progress dialog and reports how many media files it has found. Files are placed in Windows natural sort order.

When several files are sent to Luna together and **Open the file only** is selected, Luna loads all of those explicitly selected files rather than discarding all but one.

### Opening a folder

Choose **File > Open Folder...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd>.

With either of the first two file-opening modes, Luna loads supported media directly inside the chosen folder. With the subfolder mode, it scans recursively. Inaccessible and system folders are skipped during a recursive scan.

### Opening a network stream

Choose **File > Open Link...** or press <kbd>Ctrl</kbd>+<kbd>L</kbd>. Enter an `http` or `https` address.

Use this command for direct media streams and remote M3U or M3U8 playlists. Use **Open YouTube Link...** for YouTube video and playlist addresses.

### Opening playlists

Luna supports local and remote M3U and M3U8 playlists. Relative entries in a local playlist are resolved from the playlist's folder; relative entries in a network playlist are resolved from its address.

An HLS M3U8 manifest is treated as one stream and passed to the playback engine. It is not expanded into a list of media segments. Other playlists become an opened-files list containing their playable local paths and web links.

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
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Left</kbd> or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Right</kbd> moves by four steps.
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

In audiobook mode, the opened-files list expands each book into its chapters; files without chapters remain single items. Previous/next file commands, shuffle, repeat, end-of-file behavior, and end-of-track sleep timers apply to the active chapter. Elapsed, remaining, and total time refer to that chapter. Seeking backward stops at its beginning; reaching its end, including by seeking, follows the configured end behavior. A-B loops and selection points stay within the chapter, and selecting a different chapter clears them. Automatic chapter announcements follow **Speak file name when navigating (Previous/Next)**.

Bookmarks and remembered listening positions belong to the whole book. A bookmark in another chapter selects that chapter and its saved position. Copying, renaming, deleting, marking, or closing a chapter acts on its complete source book, including all its chapter entries.

## 6. Working with several files

### Previous, next, first, and last

- <kbd>Shift</kbd>+<kbd>Tab</kbd> or <kbd>Page Up</kbd> plays the previous item.
- <kbd>Tab</kbd> or <kbd>Page Down</kbd> plays the next item.
- <kbd>Ctrl</kbd>+<kbd>Home</kbd> plays the first item.
- <kbd>Ctrl</kbd>+<kbd>End</kbd> plays the last item.

If **Wrap to top for multiple files** is enabled, Next on the last item returns to the first and Previous on the first returns to the last. Automatic advance follows the same rule.

### Going to a file number

Choose **Player > Go to file...** or press <kbd>Ctrl</kbd>+<kbd>G</kbd>. Enter a number from 1 through the number of loaded items.

### Opened Files dialog

Choose **File > Opened Files...** or press <kbd>F2</kbd>. The current item is selected in the list.

- Choose an item and activate **Jump to selected**, or press <kbd>Enter</kbd> on it, to play it.
- Activate **Playlist info** to calculate the number of files, total size, total duration, elapsed time across the playlist, and remaining time. The scan is cancellable and the result opens in a read-only text window.

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

The Audio Preferences page contains two additional filters:

- **Enable dynamic normalize and limiter** evens out changing loudness and restrains peaks to reduce clipping.
- **Play audio as Mono** combines the left and right channels into one mono output.

Neither option changes the source file.

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

Bookmarks save named playback positions for local files. They are stored per file and are not available for streams or YouTube videos.

### Adding a bookmark

1. Play or seek to the desired position.
2. Choose **Bookmarks > Add a new bookmark** or press <kbd>Shift</kbd>+<kbd>M</kbd>.
3. Accept the generated time-based name or type a descriptive name.

Bookmarks are ordered by playback position. The first ten in that order occupy the slots exposed by direct shortcuts.

### Jumping to a numbered bookmark

Press <kbd>Alt</kbd>+<kbd>1</kbd> through <kbd>Alt</kbd>+<kbd>9</kbd> for slots 1 through 9. Press <kbd>Alt</kbd>+<kbd>0</kbd> for slot 10. Luna announces when the requested slot is empty.

### Managing bookmarks

Choose **Bookmarks > Manage bookmarks** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>M</kbd>. The dialog lists bookmark names and positions for the current file.

- **Jump** moves playback to the selected bookmark.
- **Edit** changes its name.
- **Delete** removes it after confirmation.
- Activating a bookmark in the list jumps to it directly.

Use the Backup and restore Preferences page to export or import the entire bookmark collection.

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

- **Download...** or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd>: choose the name, destination, format, quality, and additional processing options.
- **Direct download** or <kbd>Ctrl</kbd>+<kbd>D</kbd>: start immediately with the defaults from YouTube Preferences.
- **Add to download queue** or <kbd>Ctrl</kbd>+<kbd>Q</kbd>: add the current video with those defaults without starting the queue.
- **Video description...** or <kbd>Alt</kbd>+<kbd>D</kbd>: fetch the title and uploader description and show them in a read-only text window.
- **Copy video link**: copy the stable YouTube watch address rather than the temporary media stream address.
- **Open video in browser** and **Open channel in browser**: open the corresponding YouTube page in the default browser.
- **Go to channel**: browse the uploader's channel inside Luna.

A repeated download does not overwrite an existing file with the same name; Luna chooses an unused numbered name.

### Choosing download options

The Download dialog starts with the defaults from YouTube Preferences and lets you change them for one item. You can choose:

- A file name and destination folder.
- Video or audio-only output.
- Video quality and MP4 or MKV for video.
- MP3, M4A, Opus, FLAC, or WAV and an audio bitrate for audio-only output.
- How many fragments yt-dlp downloads at once.
- Whether to embed metadata, a thumbnail, chapters, or subtitles.
- Whether SponsorBlock sections should be removed.

Choose **Download** to start immediately. Its modeless progress window reports downloaded size, percentage, speed, estimated time, and finishing work such as merging, converting, or embedding information. You can continue using Luna while this window is open. Choose **Minimize to queue** to hide the individual progress window and continue the download in the queue.

Choose **Add to queue** instead to retain every choice made in the dialog as settings for that queue item.

### Download queue

Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> or choose **Tools > Download queue...** to open the queue. The list shows each video's title and its current state. A link entered manually is shown as the title when Luna has not looked up its metadata.

- **Add...** accepts one YouTube video link per line.
- **Start** begins every queued item and retries failed items.
- **Stop** cancels the selected active download and returns it to the queue for a later start.
- **Stop all** cancels every active download and leaves unfinished items in the queue.
- Press <kbd>Delete</kbd> to remove the selected item after confirmation. Removing an active item also cancels it.
- **Close** hides the queue window without stopping active downloads.

The status box and total progress bar appear only while the queue contains items. During a running batch, they report the current item, total percentage, combined speed, and estimated time left. A downloaded item can remain in a finishing state while yt-dlp converts, merges, embeds, or moves its output.

The queue runs up to the **Simultaneous downloads** limit from YouTube Preferences. Finished rows remain available for review until the queue window is closed, then they are cleared. Luna sends a desktop notification when all visible queue items finish successfully.

If you close Luna while the queue still has unfinished items, Luna asks whether to exit and cancel them or return to the application.

### Favorite videos and streams

Favorites save links, while bookmarks save positions inside local files. Open favorites with **File > Favorite videos...** or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd>.

The Favorites dialog can open, add, edit, or remove entries. Each entry has a name, link, and type:

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

## 13. Recording audio

Luna can record microphones and other inputs, everything played through an output device, or the audio of a particular program. Multiple sources can be mixed into one recording with separate volume levels.

### Fast recording with the default microphone

If no sources have been created in the current session, press <kbd>F9</kbd> to record the Windows default input device using the saved Recording Preferences. Press <kbd>F7</kbd> to pause or resume, and <kbd>F8</kbd> to stop.

A rising tone confirms that recording is starting. A falling tone confirms that it has stopped. Successful start and stop do not add a spoken message over those tones.

### Opening the recording interface

Choose **Recording > Open the recording interface...** or press <kbd>Alt</kbd>+<kbd>R</kbd>. The window has a source list, source controls, output settings, and Start/Pause controls.

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

## 14. IPTV

IPTV sources and channels are under **Tools > IPTV**. Luna supports extended M3U or M3U8 playlists from a file or web address, Xtream Codes accounts, and Stalker portals.

### Managing sources

Choose **Tools > IPTV > Manage IPTV sources...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>I</kbd>. The source list provides **Open**, **Add...**, **Edit...**, and **Remove...** commands.

Each source has a name and type. Depending on that type, Luna asks for a playlist path or address, an Xtream server with username and password, or a Stalker portal with MAC address and optional serial. You can also supply an optional XMLTV programme-guide address. Xtream live channels can be opened as HLS instead of transport streams when **Open live channels as HLS** is enabled.

Saved IPTV credentials are protected for the current Windows user. Do not share the IPTV source data file because it still belongs to your account and computer.

### Browsing channels

Open a source from the manager, or choose **Tools > IPTV > Browse channels...** to reopen the current source. The channel browser can filter by live TV, movies, or series, by category, and by text typed into the Search box.

Press <kbd>Enter</kbd> on a selected row or activate **Play** to open it. When programme-guide information is available, press <kbd>Ctrl</kbd>+<kbd>I</kbd> to hear what is showing now and what follows. Press <kbd>F1</kbd> in the channel browser for a spoken summary of its controls.

## 15. Media converter

Choose **Tools > Media converter...** to convert one or more media files to an audio format. Add individual files or add a folder; adding a folder includes supported media in its subfolders. Duplicate paths are ignored.

The converter writes AAC, AC-3, AIFF, FLAC, M4A, MKA, MP3, OGA, OGG, Opus, WAV, and WMA. It chooses a sensible initial format from the first source file, but you can change it. Depending on the output format, you can also choose a sample rate, mono or stereo channels, bitrate, and Opus bitrate mode and profile.

Set **Maximum simultaneous conversions** to control how many files run at once. By default, converted files are written beside their sources. Clear **Preserve original file locations** to send all results to one destination folder.

**Delete original files after conversion** removes each source only after that file converts successfully. Use this option carefully; deleted originals are not sent to the Recycle Bin.

The progress window reports the current file, its percentage, the number of remaining files, total percentage, and estimated time. You can cancel the remaining work. When conversion ends, Luna reports complete success or lists the files that failed and why.

## 16. Sleep timer

Choose **Tools > Sleep timer > Set timer...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>S</kbd>. A timer can fire after a chosen duration or at the end of the current track. Choose whether it pauses playback or stops and unloads the current item.

For a duration, choose 15, 30, 45, 60, 90, or 120 minutes, or enter custom hours and minutes. An end-of-track timer requires an open item and prevents normal automatic advance when that item finishes.

Enable **Fade the volume out before firing** to lower the volume during the final 1 through 120 seconds. Luna restores the original volume after the timer fires or is cancelled; the faded value is not saved as your preferred volume.

- Press <kbd>Ctrl</kbd>+<kbd>T</kbd> or choose **Time remaining** to hear the remaining time or that the timer is waiting for the end of the track.
- Choose **Cancel timer** to turn it off. Opening the timer window while one is active also provides **Turn off timer**.
- Luna announces a one-minute warning for duration timers.

The last timer choices are remembered, but an active countdown does not survive closing Luna.

## 17. Preferences reference

Open Preferences with **File > Preferences...** or <kbd>Ctrl</kbd>+<kbd>P</kbd>. Select a page in the category tree. Press OK to validate and apply all pages, or Cancel to discard unaccepted changes.

Press <kbd>F1</kbd> while a setting has focus to hear a detailed explanation of that setting.

### General

| Setting | Purpose |
| --- | --- |
| Language | Uses the Windows display language or an included translation. Restart Luna after changing it. |
| Theme | Follows Windows or forces a light or dark appearance. High contrast always follows Windows. Restart Luna after changing it. |
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

#### Cross-fading

Under **Audio > Playback**, select **Enable cross-fading for audio files** to overlap the end of one audio file with the beginning of the next. Cross-fading is off by default and excludes files containing video. Album cover art does not count as video.

Set **Cross-fading duration (milliseconds)** from 1 to 30000; the default is 3000, or three seconds. Choose **Equal-power** for an even perceived loudness during the overlap, or **Linear** for a constant rate of gain change. Equal-power can produce higher peaks.

Select **Enable cross-fading during manual navigation** to use overlaps when moving between audio files yourself, including Previous, Next and file-number commands. This option is off by default. Navigation while paused uses an ordinary file change.

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

### Recording

This page sets the defaults used by direct recording shortcuts and seeds the recording interface at the beginning of each Luna session. It contains audio format, sample rate, channels, audio quality for compressed formats, and the recordings folder.

### Keyboard Shortcuts

This page lists every action that can run while Luna's main window has focus. Each action can have a primary shortcut; a few actions also have a predefined secondary slot.

1. Select an action.
2. Activate **Edit Primary Shortcut** or, where available, **Edit Secondary Shortcut**.
3. Press the desired combination. Press <kbd>Escape</kbd> to cancel capture.
4. Press OK in Preferences to apply the changes.

Luna rejects a combination already assigned to another action in the same shortcut set. Local shortcuts can use printable keys, navigation keys, function keys F1 through F24, and Ctrl, Shift, or Alt modifiers. Use **Reset to Defaults** to restore the complete local shortcut page after confirmation.

### Global Shortcuts

Global shortcuts work while another application has focus. They are intentionally limited to playback, navigation, volume, and recording commands that are useful outside Luna.

Select an action, activate **Edit Shortcut**, and press the desired combination. Global shortcuts can also use the Windows key. Windows may refuse a shortcut already reserved by the operating system or another application; Luna reports shortcuts it could not register.

Global shortcuts are separate from local shortcuts. Changing one does not change the other.

## 18. Application updates

### Checking manually

Choose **Help > Updates > Check for app updates**. A cancellable progress dialog appears while Luna checks the release information.

If a newer release is ready, Luna shows the installed and available versions and a scrollable list of changes. Choose **Update** to download it or **Later** to leave the current version unchanged.

Luna verifies that the actual installer or portable archive exists before offering the update. If release information was published before its package finished uploading, a manual check asks you to try again later rather than presenting a download that will fail.

### Startup checks

Enable **Check for app updates on startup** for a silent background check. It displays no progress window or no-update message; a prompt appears only when a newer compatible package is available.

### Installing a downloaded update

The download window shows total size, downloaded size, percentage, and a Cancel button. After a successful download, Luna starts its small updater helper and closes:

- An installed copy starts the update installer. Its installation progress remains visible, and Luna opens when installation completes.
- A portable copy replaces the extracted application files and opens the updated player.

Do not start another Luna process while the update is being applied.

## 19. Help and release information

- **Help > User guide** or <kbd>F1</kbd> opens the installed guide that matches Luna's interface language. If no matching regional or base-language guide is installed, Luna opens the English guide.
- **Help > About** shows a description, version, copyright information, and a link to the project website.
- **Help > Release notes** opens the GitHub release page for the installed version in your default browser.

The user guide is installed as local HTML and does not require an internet connection. Release notes and the project website do require one.

## 20. Settings and user data

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

Use **Preferences > Backup and restore > Open user settings folder** to open the exact folder rather than typing the path.

Do not edit these files while Luna is running. Use the export and import controls for settings and bookmarks whenever possible.

If `settings.json` cannot be read or contains invalid values, Luna starts with defaults, shows the reason, and protects the existing file from being overwritten. Import a valid settings backup or use **Reset settings** to replace it deliberately. Invalid bookmarks, favorites, equalizer presets, IPTV sources, and recents are also reported instead of being silently replaced.

## 21. Complete default shortcut reference

These are the factory defaults. Your current bindings may differ after customization. The authoritative list for your installation is under **Preferences > Keyboard Shortcuts** and **Global Shortcuts**.

### Files, information, and Preferences

| Action | Primary shortcut | Secondary shortcut |
| --- | --- | --- |
| Open files | <kbd>Ctrl</kbd>+<kbd>O</kbd> | — |
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
| Previous file | <kbd>Shift</kbd>+<kbd>Tab</kbd> | <kbd>Page Up</kbd> |
| Next file | <kbd>Tab</kbd> | <kbd>Page Down</kbd> |
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
| Rewind four steps | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Left</kbd> | — |
| Forward four steps | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Right</kbd> | — |
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
| Add bookmark | <kbd>Shift</kbd>+<kbd>M</kbd> |
| Manage bookmarks | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>M</kbd> |
| Jump to bookmark slots 1–9 | <kbd>Alt</kbd>+<kbd>1</kbd> through <kbd>Alt</kbd>+<kbd>9</kbd> |
| Jump to bookmark slot 10 | <kbd>Alt</kbd>+<kbd>0</kbd> |

### YouTube, tools, recording, updates, and help

| Action | Shortcut |
| --- | --- |
| Open YouTube link | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd> |
| Search YouTube | <kbd>Ctrl</kbd>+<kbd>Y</kbd> |
| Favorite videos | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd> |
| Open the download queue | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> |
| Add the current YouTube video to the queue | <kbd>Ctrl</kbd>+<kbd>Q</kbd> |
| Direct-download current YouTube video | <kbd>Ctrl</kbd>+<kbd>D</kbd> |
| Choose options for current YouTube video download | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd> |
| Show current video description | <kbd>Alt</kbd>+<kbd>D</kbd> |
| Copy current video link | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> |
| Open current video in browser | None |
| Open current video's channel in browser | None |
| Browse current video's channel in Luna | None |
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

These commands work system-wide when Luna is running, even if another program has focus.

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

## 22. Supported formats

Luna filters file and folder selection using the following extensions. Playback is handled by mpv, so other formats supported by that engine may also play when opened directly.

**Audio:** AAC, AC-3, AIFF, ALAC, APE, AU, DTS, E-AC-3, FLAC, M4A, M4B, MKA, MP1, MP2, MP3, MPC, OGA, OGG, OGM, Opus, TAK, TrueHD (`.thd`), TTA, WAV, WMA, and WavPack.

**Video:** 3G2, 3GP, AVI, FLV, IVF, M2TS, M4V, MJ2, MKV, MOV, MP4, MPEG, MPG, MXF, OGV, RMVB, TS, WebM, WMV, and Y4M.

**Playlists:** M3U and M3U8, including HLS streams.

## 23. Troubleshooting

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

### Recording cannot find a program

Program capture requires Windows 10 version 2004 or later. The program must also have an active Windows audio session. Start playback in that program, reopen the source dialog, and choose it from the refreshed list.

### Recording starts without one of its sources

Luna can continue when at least one source opens. Read the warning listing failed sources, then check whether their devices are connected, enabled, and available. Stop the recording before editing the source list.

### A global shortcut does not work

Open **Preferences > Global Shortcuts** and apply the binding again. Windows does not allow two applications to register the same global combination. Choose another combination if Luna reports that one or more shortcuts could not be registered.

### Settings will not save after a startup error

Luna protects an invalid or unreadable `settings.json` rather than overwriting it. Open Preferences and either import a valid settings backup or choose **Backup and restore > Reset settings**. If you need the original for diagnosis, copy it from the user settings folder first.

### Bookmarks or favorites report invalid data

An invalid storage file is preserved rather than silently replaced. Open the user settings folder, make a copy of the affected JSON file, and restore a known-good backup. Bookmarks can be restored through Preferences. Favorites currently require restoring the `favorites.json` file itself while Luna is closed.

### The user guide does not open

The installed guide should be at `docs\<language-code>\user-guide.html` beside the executable. Re-extract the complete portable archive or repair/reinstall the application if the `docs` folder is missing. Luna falls back from a regional language to its base language and then to English.

## 24. Publisher and licensing

Luna Player is published by **Diamond Star**.

Copyright © 2026 Diamond Star.

Luna Player's original source code is licensed under the Apache License, Version 2.0. The translated mpv binding in `src/Mpv.cs` is licensed under the GNU Lesser General Public License, version 2.1 or later.

Luna Player also uses separately licensed third-party components, including mpv, FFmpeg, wxWidgets, Prism, NAudio, and PyYt. Each component remains under its own license. See `NOTICE.txt`, installed alongside Luna Player, for copyright statements, component versions, license names, and source locations. The relevant license texts are installed in the `licenses` folder.

The license summary in this guide is provided for convenience. `LICENSE.txt`, `NOTICE.txt`, and the files in the `licenses` folder contain the authoritative terms and attributions. In the source repository, the corresponding top-level files are named `LICENSE` and `NOTICE`.

## 25. Contact and support

Luna Player is developed and published by Diamond Star. You can use the following channels:

- **Email:** [ramymaherali55@gmail.com](mailto:ramymaherali55@gmail.com)
- **Telegram:** [Contact Diamond Star on Telegram](https://t.me/diamondStar35)
- **Bug reports:** [Luna Player issues on GitHub](https://github.com/diamondStar35/luna_player/issues)
- **Source code and releases:** [Luna Player on GitHub](https://github.com/diamondStar35/luna_player)

For a bug report, include the Luna version shown under **Help > About**, your Windows version, what you expected, what happened, and the shortest steps that reproduce the problem. Include the exact text of any error message. Remove private file names, web addresses, account details, and other personal information from logs or screenshots before sharing them publicly.
