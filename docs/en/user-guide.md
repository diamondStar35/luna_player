# Luna Player User Guide

Luna Player is a keyboard-friendly audio and video player for Windows. It combines local media playback, network streams, YouTube playback and downloading, audio recording, file management, bookmarks, speech announcements, and configurable local and system-wide shortcuts.

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

You can start Luna Player directly, open a supported file with it from Windows, or pass files and folders to it on the command line. Luna runs as a single instance: opening more media while it is already running sends that media to the existing window.

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

YouTube features require an internet connection. Availability can change when YouTube changes its service. Luna includes a built-in resolver and can optionally use yt-dlp.

### Opening a YouTube link

Choose **File > Open YouTube Link...** or press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd>. Enter a video or playlist address. Channel addresses are not accepted by this command.

Some YouTube addresses identify both a video and a playlist. Luna can ask whether to play the video or open the playlist, or follow the fixed behavior selected in YouTube Preferences.

### Searching YouTube

Choose **File > Search YouTube...** or press <kbd>Ctrl</kbd>+<kbd>Y</kbd>. Enter search text, then move through the results list.

- Press <kbd>Enter</kbd> on a selected result or activate **Play** to play it.
- Activate **Download** to save it.
- Press <kbd>Ctrl</kbd>+<kbd>C</kbd> to copy the selected result's link.
- Press <kbd>Ctrl</kbd>+<kbd>B</kbd> to open it in the default browser.
- Press <kbd>Ctrl</kbd>+<kbd>N</kbd> to navigate to the uploader's channel results.
- Open the context menu for the same copy, browser, channel, and download actions.

When selection reaches the end of the current result page, Luna requests more results automatically until YouTube has no more to return.

Playlist and channel result windows work in the same way. Next moves forward through the active YouTube result session, fetching the next video when necessary. Pressing <kbd>Escape</kbd> in the main window after playback begins can return to the related result list when that session is still available.

### Audio-only playback and quality

Open **Preferences > YouTube**.

- **Play videos as audio only** requests only sound, reducing bandwidth and often starting faster.
- **Video quality** chooses Low, Medium, or Best when video is enabled.

The same choices are used when downloading a video. Audio-only mode saves sound without picture; otherwise Luna downloads at the selected video quality.

### Video options

While a YouTube video is active, the **Video options** menu provides:

- **Download...** or <kbd>Ctrl</kbd>+<kbd>D</kbd>: choose a folder and save the current video with progress and cancellation.
- **Video description...** or <kbd>Alt</kbd>+<kbd>D</kbd>: fetch the title and uploader description and show them in a read-only text window.
- **Copy video link**: copy the stable YouTube watch address rather than the temporary media stream address.

A repeated download does not overwrite an existing file with the same name; Luna chooses an unused numbered name.

### Favorite videos and streams

Favorites save links, while bookmarks save positions inside local files. Open favorites with **File > Favorite videos...** or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd>.

The Favorites dialog can open, add, edit, or remove entries. Each entry has a name, link, and type:

- **Video** requires a YouTube video link.
- **Playlist** requires a YouTube playlist link.
- **Combined link** requires an address containing both video and playlist identifiers and opens its video.
- **Generic stream** accepts any `http` or `https` stream address.

Luna validates the address format when an entry is saved. It checks whether the remote content is still available only when you open it.

### Built-in resolver and yt-dlp

Luna normally uses its built-in YouTube resolver. If that cannot open a video, enable **Use yt-dlp to resolve streams** on the YouTube Preferences page. Luna offers to download the required component if it is missing.

yt-dlp is always needed for the yt-dlp resolver path, but the built-in resolver and other YouTube features can work without it. The component page offers three update channels:

- **Stable** changes least often and is recommended for normal use.
- **Nightly** follows nightly yt-dlp builds.
- **Master** follows the newest source and can be less reliable.

Use **Download YouTube components** in Preferences to install missing yt-dlp and Deno executables. Use **Help > Updates > Update YouTube components** to update an installed yt-dlp. Enable **Check for yt-dlp updates on startup** for a silent background check that prompts only when an update is available.

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

## 14. Preferences reference

Open Preferences with **File > Preferences...** or <kbd>Ctrl</kbd>+<kbd>P</kbd>. Select a page in the category tree. Press OK to validate and apply all pages, or Cancel to discard unaccepted changes.

Press <kbd>F1</kbd> while a setting has focus to hear a detailed explanation of that setting.

### General

| Setting | Purpose |
| --- | --- |
| Language | Uses the Windows display language or an included translation. Restart Luna after changing it. |
| Remember last file position | Restores the last active local file and its position on the next launch. |
| Speak file name when navigating | Announces the item reached with Previous or Next. |
| Check for app updates on startup | Checks quietly after startup and prompts only for a ready, newer release. |
| Save settings on close | Saves session changes such as volume and speed when Luna exits. Explicitly accepted Preferences changes are still saved immediately. |
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

### Silence removal

See [Silence removal and audio processing](#8-silence-removal-and-audio-processing) for the basic and advanced controls.

### YouTube

| Setting | Purpose |
| --- | --- |
| Play videos as audio only | Requests sound without picture. |
| Video quality | Chooses Low, Medium, or Best for video playback and downloads. |
| Number of search results | Sets the initial target from 5 through 100; more results load at the end. |
| Video+playlist link behavior | Asks each time, always plays the video, or always opens the playlist. |
| Use yt-dlp to resolve streams | Uses the downloaded yt-dlp component instead of Luna's built-in resolver. |
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

## 15. Application updates

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

## 16. Help and release information

- **Help > User guide** or <kbd>F1</kbd> opens the installed guide that matches Luna's interface language. If no matching regional or base-language guide is installed, Luna opens the English guide.
- **Help > About** shows a description, version, copyright information, and a link to the project website.
- **Help > Release notes** opens the GitHub release page for the installed version in your default browser.

The user guide is installed as local HTML and does not require an internet connection. Release notes and the project website do require one.

## 17. Settings and user data

Luna stores user data under `%APPDATA%\Luna Player`:

| File | Contents |
| --- | --- |
| `settings.json` | Preferences and shortcut overrides |
| `bookmarks.json` | Named bookmarks for local files |
| `positions.json` | Per-file playback positions |
| `favorites.json` | Favorite YouTube and stream links |

Use **Preferences > Backup and restore > Open user settings folder** to open the exact folder rather than typing the path.

Do not edit these files while Luna is running. Use the export and import controls for settings and bookmarks whenever possible.

If `settings.json` cannot be read or contains invalid values, Luna starts with defaults, shows the reason, and protects the existing file from being overwritten. Import a valid settings backup or use **Reset settings** to replace it deliberately. Invalid bookmark and favorites files are also reported instead of being silently replaced.

## 18. Complete default shortcut reference

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

### YouTube, recording, updates, and help

| Action | Shortcut |
| --- | --- |
| Open YouTube link | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd> |
| Search YouTube | <kbd>Ctrl</kbd>+<kbd>Y</kbd> |
| Favorite videos | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd> |
| Download current YouTube video | <kbd>Ctrl</kbd>+<kbd>D</kbd> |
| Show current video description | <kbd>Alt</kbd>+<kbd>D</kbd> |
| Copy current video link | None |
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

## 19. Supported formats

Luna filters file and folder selection using the following extensions. Playback is handled by mpv, so other formats supported by that engine may also play when opened directly.

**Audio:** AAC, AC-3, AIFF, ALAC, APE, AU, DTS, E-AC-3, FLAC, M4A, MKA, MP1, MP2, MP3, MPC, OGA, OGG, OGM, Opus, TAK, TrueHD (`.thd`), TTA, WAV, WMA, and WavPack.

**Video:** 3G2, 3GP, AVI, FLV, IVF, M2TS, M4V, MJ2, MKV, MOV, MP4, MPEG, MPG, MXF, OGV, RMVB, TS, WebM, WMV, and Y4M.

**Playlists:** M3U and M3U8, including HLS streams.

## 20. Troubleshooting

### A file or folder opens fewer items than expected

Check **Preferences > General > What would you like to open with files?** The default opens only the selected file. Folder commands include only extensions recognized as media. Inaccessible folders and system folders are skipped during recursive scans.

### A live stream has no duration or percentage

Live streams often do not publish a fixed length. Go to time, percentage jumps, remaining time, and seeking to the end require a known duration and may be unavailable.

### A YouTube video does not open

Try these steps in order:

1. Confirm that the address opens in a browser and names a video or playlist rather than a channel.
2. Choose **Help > Updates > Update YouTube components**.
3. Enable **Use yt-dlp to resolve streams** under YouTube Preferences.
4. Try the Stable channel first; use Nightly or Master only if Stable cannot handle a recent service change.

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

## 21. Publisher and licensing

Luna Player is published by **Diamond Star**.

Copyright © 2026 Diamond Star.

Luna Player's original source code is licensed under the Apache License, Version 2.0. The translated mpv binding in `src/Mpv.cs` is licensed under the GNU Lesser General Public License, version 2.1 or later.

Luna Player also uses separately licensed third-party components, including mpv, FFmpeg, wxWidgets, Prism, NAudio, and YoutubeExplode. Each component remains under its own license. See `NOTICE.txt`, installed alongside Luna Player, for copyright statements, component versions, license names, and source locations. The relevant license texts are installed in the `licenses` folder.

The license summary in this guide is provided for convenience. `LICENSE.txt`, `NOTICE.txt`, and the files in the `licenses` folder contain the authoritative terms and attributions. In the source repository, the corresponding top-level files are named `LICENSE` and `NOTICE`.

## 22. Contact and support

Luna Player is developed and published by Diamond Star. You can use the following channels:

- **Email:** [ramymaherali55@gmail.com](mailto:ramymaherali55@gmail.com)
- **Telegram:** [Contact Diamond Star on Telegram](https://t.me/diamondStar35)
- **Bug reports:** [Luna Player issues on GitHub](https://github.com/diamondStar35/luna_player/issues)
- **Source code and releases:** [Luna Player on GitHub](https://github.com/diamondStar35/luna_player)

For a bug report, include the Luna version shown under **Help > About**, your Windows version, what you expected, what happened, and the shortest steps that reproduce the problem. Include the exact text of any error message. Remove private file names, web addresses, account details, and other personal information from logs or screenshots before sharing them publicly.
