# Third-party components

The libraries Luna Player ships or compiles into its executable. The root
[LICENSE](../LICENSE) covers Luna Player under GPL-3.0-or-later. The components below
retain their own licences; [NOTICE](../NOTICE) records their copyrights and terms.
The `licenses/` folder holds their licence texts, including
[Apache-2.0](licenses/Apache-2.0.txt) for WxSharp and PrismSharp.

## Spotify protocol implementation

Luna's managed Spotify playback code follows the librespot protocol implementation.
Audio is decoded by the bundled mpv library and played through Luna's audio chain.

- Source: https://github.com/librespot-org/librespot
- Revision: `1599145bf2c98660d35b17817b3386767a7e4b42`
- Licence: MIT

The Shannon packet cipher is adapted from Paul Lietar's `shannon` 0.2.0 Rust
crate. Its MIT license is included separately in `licenses/shannon-LICENSE.txt`.

## mpv

Media decoding and playback.

- Source: https://github.com/mpv-player/mpv
- Build revision: https://github.com/diamondStar35/mpv-winbuild/tree/b27346e3f504c713adfe5993c2bafb728cf7d7e2
- Binary: https://github.com/diamondStar35/mpv-winbuild/actions/runs/40492335903/artifacts/12733716109
- Version: v0.41.0-1321-g8dea34900, shared GPL x86-64-v2 build
- Licence: GPL-3.0-or-later for this build; see the root [LICENSE](../LICENSE).

Includes DVD-Audio/DVD-Video readers and Luna's native fade, transport and
cross-fading patches. Upstream mpv is GPL-2.0-or-later; this build links against
GPL-3.0-or-later FFmpeg.

## FFmpeg

Media decoding libraries used by mpv, plus the command-line tool used for conversion and export.

- Source: https://github.com/FFmpeg/FFmpeg
- Build revision: https://github.com/diamondStar35/mpv-winbuild/tree/b27346e3f504c713adfe5993c2bafb728cf7d7e2
- Binary: https://github.com/diamondStar35/mpv-winbuild/actions/runs/40492335903/artifacts/12733726076
- Version: N-128110-gfa4daf630, shared GPL x86-64-v2 build
- Licence: GPL-3.0-or-later (`--enable-gpl --enable-version3`); see the root [LICENSE](../LICENSE).

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

## PyYt

YouTube search, playlist and channel metadata. A dependency-free, Native-AOT-
compatible .NET port of youtube-search-python.

- Source: https://github.com/alexmercerind/youtube-search-python
- Version: 0.8.0
- Licence: MIT
