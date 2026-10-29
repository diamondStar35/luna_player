# Luna Player

A fast audio and video player for Windows, built to be used entirely by ear and keyboard.

Playback state, file information and setting changes are announced through speech, and every action
is a keyboard shortcut that can be reassigned. It plays local files, network streams, YouTube and
IPTV, and can record, convert and process audio along the way.

## Status

Luna Player is under active pre-1.0 development. Features, keyboard shortcuts, settings and
configuration file formats may change between releases without notice, and saved settings are not
guaranteed to carry forward.
Bug reports are welcome.

## Features

**Playback**

- Play, pause and seek, with a seek step selectable from eleven presets or a custom value
- Playback speed from 0.5x to 6x, with a configurable step
- Pitch shifting, from -12 to +12 semitones
- Left/right pan control
- Volume up to 1000%, with a limiter to prevent clipping
- A–B loop for repeating a section
- Shuffle and repeat-file
- Configurable end-of-file behaviour: advance to the next file, loop, or stop and hold position
- Sleep timer that stops playback after a set time

**Audio processing**

- A 15-band equalizer with named presets and a custom preset of your own
- Dynamic normalization with a limiter, for recordings with inconsistent levels
- Mono downmix
- Silence removal, with threshold and duration settings and a full set of advanced parameters

**Video**

- Audio track and subtitle track selection
- Works the same as audio playback, driven entirely by keyboard and speech

**Online media**

- Open any network stream by link
- Search YouTube, play results, and download them as video or audio, one at a time or through a queue
- Save YouTube videos, channels and playlists as favorites
- IPTV from M3U/M3U8 playlists, Xtream Codes portals and Stalker portals, with a channel browser and
  an electronic programme guide where the source provides one

**Recording**

- Record from a microphone or line in, from an output device as it plays, or from a single program's
  sound without touching other audio
- Write to WAV, MP3, AAC or FLAC

**Converting**

- Convert files between formats with the built-in media converter

**Navigation**

- Named bookmarks, with ten directly addressable slots
- Per-file position memory, and optional restore of the last session on startup
- Go to a specific time, or jump to a percentage of the file
- Recent files, folders and playlists
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
- Built-in updater for the application and for its YouTube components


## Requirements

- Windows 10 version 1809 (build 17763) or later, 64-bit
- A screen reader is optional. Luna speaks through NVDA, JAWS and other supported readers when one is
  running, and falls back to SAPI otherwise

## Installation

Download the latest release from the
[Releases](https://github.com/diamondStar35/luna_player/releases) page and follow the instructions
for the package you choose.


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

Audio: AAC, AC-3, AIFF, ALAC, APE, AU, DTS, E-AC-3, FLAC, M4A, MKA, MP1, MP2,
MP3, MPC, OGA, OGG, OGM, Opus, TAK, TrueHD, TTA, WAV, WMA, WavPack

Video: 3G2, 3GP, AVI, FLV, IVF, M2TS, M4V, MJ2, MKV, MOV, MP4, MPEG, MPG, MXF,
OGV, RMVB, TS, WebM, WMV, Y4M

Playlists: M3U, M3U8 (including HLS)

Playback is handled by [mpv](https://mpv.io/), so most formats it supports will play.

## Languages

The interface and spoken messages are available in English, Spanish and Vietnamese. Translations to
other languages are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

## Building from source

Requires the .NET 10 SDK and Visual Studio Build Tools with the MSVC x64 toolchain.

```
git clone --recurse-submodules https://github.com/diamondStar35/luna_player
cd luna_player
dotnet build src/LunaPlayer.csproj
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for full build instructions and known issues.

## Running tests

On Windows, install the [build prerequisites](CONTRIBUTING.md#requirements), initialize the
submodules, and install the documentation packages before running the MSTest suite:

```powershell
python -m pip install --requirement docs/requirements.txt
dotnet test --project tests/LunaPlayer.Tests.csproj -c Release --report-trx --results-directory tests/TestResults/current
```

This runs the current-code tests, including playback tests using the bundled libmpv with audio
and video output disabled. Test reports are written under `tests/TestResults/`.

To also verify playback in a Native AOT build, publish and run the native test runner:

```powershell
dotnet publish tests/LunaPlayer.Tests.csproj -c Release -m:1 -p:PublishAot=true -o tests/TestResults/native
./tests/TestResults/native/LunaPlayer.Tests.exe --filter "TestCategory=Integration" --report-trx --results-directory tests/TestResults/native-results
```

All tests must pass before submitting a pull request. Automated tests complement manual playback
and screen-reader testing; see [Testing changes](CONTRIBUTING.md#testing-changes).

## Credits

Luna Player is an independent C# application inspired by
[Simple Audio Player](https://github.com/kamalyaser31/simple-player) by kamalyaser31. The original
project inspired Luna's initial feature set and interaction model, but Luna does not incorporate
source code or assets from Simple Audio Player. Its implementation, architecture and user
interface have since developed independently.

## Contributing

Pull requests are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting one.

## License

Luna Player is free software licensed under the [GNU General Public License](LICENSE),
version 3 or (at your option) any later version (`GPL-3.0-or-later`). You may redistribute
and modify it under those terms.

Luna Player is distributed in the hope that it will be useful, but **without any warranty**,
including the implied warranties of merchantability or fitness for a particular purpose.
See [LICENSE](LICENSE) for the full terms.

Separately licensed components retain their own licences. The translated
[mpv binding](src/Mpv.cs) remains licensed under LGPL-2.1-or-later. [NOTICE](NOTICE) records
the third-party copyrights, licences, versions and source locations. Their licence texts,
including [Apache-2.0](third-party/licenses/Apache-2.0.txt) for WxSharp and PrismSharp, are
installed with the program.
