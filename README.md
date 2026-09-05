# Luna Player

A fast audio and video player for Windows with speech announcements and configurable keyboard shortcuts.

Playback state, file information and setting changes are announced through speech. Actions are
driven by keyboard shortcuts, and each one can be reassigned.

## Status

Luna Player is under active pre-1.0 development. Features, keyboard shortcuts, settings and
configuration file formats may change between releases without notice, and saved settings are not
guaranteed to carry forward.
Bug reports are welcome.

## Features

**Playback**

- Play, pause and seek, with a seek step selectable from eleven presets or a custom value
- Playback speed from 0.5x to 6x, with a configurable step
- Volume up to 1000%, with a limiter to prevent clipping
- A–B loop for repeating a section
- Shuffle and repeat-file
- Configurable end-of-file behaviour: advance to the next file, loop, or stop and hold position

**Audio processing**

- Dynamic normalization with a limiter, for recordings with inconsistent levels
- Mono downmix
- Silence removal, with threshold and duration settings and a full set of advanced parameters

**Navigation**

- Named bookmarks, with ten directly addressable slots
- Per-file position memory, and optional restore of the last session on startup
- Go to a specific time, or jump to a percentage of the file
- Opened files list, and a playlist summary reporting file count, total size, duration, elapsed and
  remaining time

**File management**

- Open a single file, a folder, or a folder and its subfolders
- Mark multiple files, then copy, move, delete, or copy them to the clipboard as a batch
- Rename and delete the current file
- Register Luna Player as a handler for supported file types

**Speech and input**

- Spoken announcements for playback, navigation and setting changes
- Two verbosity levels: full messages, or short ones
- Optional announcement of the file name when moving between tracks
- Primary and secondary shortcuts for actions, all reassignable
- Context-sensitive help in the settings dialog: press <kbd>F1</kbd> on a control to hear what it does

**Windows integration**

- System Media Transport Controls: title and playback position appear in the Windows media overlay
- Hardware media key support
- Output device selection


## Requirements

- Windows 10 version 1809 (build 17763) or later, 64-bit

## Installation

Download the latest release from the
[Releases](https://github.com/diamondStar35/luna_player/releases) page and follow the instructions
for the package you choose.

Important: As of now there are no releases published. The only way is to build it from source. This readme will be updated in the future as the project grows.

## Default shortcuts

| Shortcut | Action |
| --- | --- |
| <kbd>Space</kbd> | Play / pause |
| <kbd>Left</kbd> / <kbd>Right</kbd> | Seek backward / forward |
| <kbd>Up</kbd> / <kbd>Down</kbd> | Volume up / down |
| <kbd>Ctrl</kbd>+<kbd>Up</kbd> / <kbd>Ctrl</kbd>+<kbd>Down</kbd> | Speed up / down |
| <kbd>Tab</kbd> / <kbd>Shift</kbd>+<kbd>Tab</kbd> | Next / previous track |
| <kbd>F</kbd> | Announce current file |
| <kbd>E</kbd> / <kbd>R</kbd> / <kbd>T</kbd> | Announce elapsed / remaining / total time |
| <kbd>[</kbd> / <kbd>]</kbd> | Set A–B loop start / end |
| <kbd>Shift</kbd>+<kbd>M</kbd> | Add bookmark |
| <kbd>Ctrl</kbd>+<kbd>M</kbd> | Toggle silence removal |
| <kbd>Ctrl</kbd>+<kbd>O</kbd> | Open file |
| <kbd>Ctrl</kbd>+<kbd>P</kbd> | Settings |

The complete list is in **Settings → Keyboard Shortcuts**, where any shortcut can be reassigned.

## Supported formats

Audio: AAC, AIFF, ALAC, FLAC, M4A, MP3, OGG, Opus, WAV, WMA

Video: 3GP, AVI, FLV, M2TS, M4V, MKV, MOV, MP4, MPEG, MPG, TS, WebM, WMV

Playback is handled by [mpv](https://mpv.io/), so most formats it supports will play.

## Building from source

Requires the .NET 10 SDK and Visual Studio Build Tools with the MSVC x64 toolchain.

```
git clone --recurse-submodules https://github.com/diamondStar35/luna_player
cd luna_player
dotnet build src/LunaPlayer.csproj
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for full build instructions and known issues.

## Credits

Luna Player is a fork and complete rewrite of
[Simple Audio Player](https://github.com/kamalyaser31/simple-player) by kamalyaser31. The original
project defined the feature set and interaction model that Luna Player is based on. The two have
since diverged: some features of the original are not yet implemented here, and some functionality
in Luna Player is new.

## Contributing

Pull requests are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting one.

## License

Luna Player is licensed under the [Apache License 2.0](LICENSE).

It is distributed with mpv, wxWidgets and Prism, which remain under their own
licences. [NOTICE](NOTICE) records each of them, and the full licence texts are in
[third-party/licenses](third-party/licenses). Both are installed alongside the program.
