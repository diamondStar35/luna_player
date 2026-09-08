# Third-party components

The libraries Luna Player ships or compiles into its executable. `NOTICE` in the
repository root records the terms each one comes under; `licenses/` holds the
licence texts that are not already represented by the root Apache licence.

## mpv

Media decoding and playback.

- Source: https://github.com/mpv-player/mpv
- Build project: https://github.com/diamondStar35/mpv-winbuild
- Binary: https://github.com/diamondStar35/mpv-winbuild/releases/download/2026-09-07-989d32716e/mpv-dev-lgpl-shared-x86_64-v3-20260907-git-989d32716e.7z
- Version: v0.41.0-1037-g989d32716
- Licence: LGPL-2.1-or-later

## FFmpeg

Media decoding libraries used by mpv, plus the command-line tool used for conversion and export.

- Source: https://github.com/FFmpeg/FFmpeg
- Build project: https://github.com/diamondStar35/mpv-winbuild
- Binary: https://github.com/diamondStar35/mpv-winbuild/releases/download/2026-09-07-989d32716e/ffmpeg-lgpl-shared-x86_64-v3-git-ecc7eb519.7z
- Version: N-126455-gecc7eb519
- Licence: LGPL-3.0-or-later

## wxWidgets

Windows, controls, and the accessibility information screen readers read.

- Source: https://github.com/wxWidgets/wxWidgets
- Binaries: built and shipped by the WxSharp submodule
- Version: 3.3.3
- Licence: wxWindows Library Licence 3.1

## WxSharp

The managed binding for wxWidgets and the native layer beneath it.

- Source: https://github.com/diamondStar35/wx_sharp
- Binaries: built from the submodule
- Licence: Apache-2.0

## Prism

Speech output, and the screen readers behind it.

- Source: https://github.com/ethindp/prism
- Version: 0.18.2
- Licence: MPL-2.0

## NAudio

Audio capture, recording and device support. Built from the source submodule
with Luna's Native AOT fixes.

- Source: https://github.com/diamondStar35/NAudio
- Version: 3.1.0
- Licence: MIT

## YoutubeExplode

YouTube search and stream metadata.

- Source: https://github.com/Tyrrrz/YoutubeExplode
- Version: 6.6.2
- Licence: MIT
