# Contributing

Pull requests and bug reports are welcome.

Luna Player is in alpha and changes frequently. For anything beyond a small fix, open an issue first
to discuss the approach before writing code.

## Requirements

- .NET 10 SDK
- Visual Studio 2022 Build Tools or later, with the MSVC v143 x64 build tools
- CMake, only if you modify the native windowing wrapper

## Building

```
git clone --recurse-submodules https://github.com/diamondStar35/luna_player
cd luna_player
dotnet build src/LunaPlayer.csproj
```

The build output is written to `src/bin/<Configuration>/net10.0-windows10.0.19041.0/win-x64/`.

If the repository was cloned without submodules, run:

```
git submodule update --init --recursive
```

### Known build issue

If the build fails at the native link step with `MSB3073` and a message about `vswhere.exe`, add the
Visual Studio Installer directory to `PATH`:

```
C:\Program Files (x86)\Microsoft Visual Studio\Installer
```

The error text names the linker rather than the actual cause. Only the ahead-of-time compilation step
is affected; the managed build succeeds without this.

### Rebuilding the native wrapper

Required only when changing files under `third-party/WxSharp/src/WxSharp.Native`. From an x64 Native
Tools Command Prompt:

```
cd third-party/WxSharp
powershell -ExecutionPolicy Bypass -File scripts/build-wrapper-windows.ps1 -Configuration Release
```

Copy the resulting `wx.dll` from `third-party/WxSharp/build/stage/win-x64/native/` to `third-party/`.

## Testing changes

Test your changes before submitting:

- Run the application and exercise the affected functionality, including error cases: no file loaded,
  a missing or deleted file, an empty input field, an empty playlist.
- Test with a screen reader. NVDA is free and is what most users run. Confirm that new or modified
  controls are reachable and announced correctly.
- For settings changes, confirm the value persists: change it, confirm with OK, restart the
  application, and check that it was restored.
- Verify the build is clean. Warnings are treated as errors, and the AOT and trimming analyzers are
  enabled. If a suppression is unavoidable, explain why in the pull request.

State in the pull request what you tested and what you were unable to test.

## Code standards

- Follow the conventions of the surrounding code: naming, formatting, and file organisation.
- Comments should explain why the code is written that way, not restate what it does. Platform
  behaviour, ordering requirements and non-obvious constraints are worth documenting.
- User-facing strings are part of the interface. Match the style of existing messages, and provide
  both a full and a short form where the surrounding code does.
- Handle errors or report them. An empty catch block requires a comment explaining why.
- Keep pull requests focused. Submit unrelated cleanups separately.

## Accessibility

Changes to the user interface should meet the following:

- All controls reachable by keyboard, in a logical tab order
- All controls labelled so a screen reader can announce them
- Actions that change state produce a spoken announcement
- No information conveyed by colour, position or icon alone
- New settings include context help text, so <kbd>F1</kbd> describes them

## Translations

Every string the user can read or hear goes through `Tr`, and every call carries a
`// Translators:` comment on the line above it saying what the string is and where it appears.
That comment is the only context a translator gets, so write it for someone who cannot see the
code: name the kind of control, the page it sits on, and explain any word that must be left
alone. Where a message contains a value, use a named placeholder and say what it holds:

```csharp
// Translators: Asks the user which file in the playlist to move to. {count} is how many files are loaded.
TrFormat("Enter file number (1-{count})", _player.Count)
```

Names bind values to placeholders, not positions, so a translation is free to reorder them. Two
`Tr` calls on one line share a single comment, so give each string its own line.

After changing any string, rebuild the template and the catalogues:

```powershell
./scripts/update-pot.ps1
```

This needs the GNU gettext tools on `PATH`. It rewrites `locale/LunaPlayer.pot`, merges it into
every `locale/<language>/LC_MESSAGES/LunaPlayer.po`, compiles each one to the `.mo` the player
loads, and fails if a translation uses different placeholder names from the string it translates.
Pass `-Language <code>` to start a new catalogue, or `-Compile` to only rebuild the `.mo` files.
The `.po` and `.pot` files are committed; the `.mo` files are build output and are not.

## Submodules

The windowing layer is a Git submodule with its own repository and contribution guidelines. Submit
changes to it there, then update this repository to reference the new commit. Do not commit build
output.

## Commits and pull requests

Write commit messages that describe the change and the reason for it. In the pull request
description, include what changed, how it was tested, and any known limitations.

## Bug reports

Include:

- Windows version
- Screen reader and version
- Steps to reproduce
- Expected and actual behaviour
- The file format involved, if a specific file triggers the issue

## License

Contributions are licensed under the [Apache License 2.0](LICENSE), the same terms as the project.
