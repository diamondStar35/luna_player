using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.Media;
using System.Diagnostics;

namespace LunaPlayer.Application.Commands.Files;

internal sealed partial class Files
{
    private void OpenContainingFolder()
    {
        // Translators: Spoken when the user asks to show the folder of what is playing but it is a stream rather than a file on this computer.
        if (!_guard.RequireLocalFile(Tr("Open containing folder is available only for local files."), out var path))
            return;
        TryStart(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true },
            // Translators: Spoken when the folder holding the current file could not be shown.
            Tr("Could not open containing folder."));
    }

    private void OpenFileProperties()
    {
        // Translators: Spoken when the user asks for the Windows properties of what is playing but it is a stream rather than a file on this computer.
        if (!_guard.RequireLocalFile(Tr("File properties are available only for local files."), out var path))
            return;
        if (!WindowsShell.ShowFileProperties(path))
            _speech.Speak(
                // Translators: Spoken when the Windows properties window for the current file could not be shown.
                Tr("Could not open file properties."),
                // Translators: The short wording spoken when Windows could not show a file's properties.
                Tr("Open failed."));
    }

    private void OpenedFiles()
    {
        if (!_guard.RequireAnyFile())
            return;
        ShowOpenedFiles(_player.CurrentIndex);
    }

    /// <summary>Shows the list of loaded files. Written as a step rather than a loop because asking for the
    /// playlist summary no longer blocks: the scan runs while the window stays live, and this list comes back
    /// only once it has finished.</summary>
    private void ShowOpenedFiles(int selected)
    {
        if (_player.Count == 0)
            return;
        // The names are not worked out here: the list asks for the ones it is about to draw.
        var files = _player.Files;
        var request = _view.ChooseOpenedFile(files.Count,
            index => index >= 0 && index < files.Count ? _player.DisplayName(files[index]) : string.Empty,
            selected);
        if (request is null)
            return;
        if (request.Value.Action == OpenedFilesAction.Jump)
        {
            _player.GoToIndex(request.Value.SelectedIndex);
            return;
        }
        ShowPlaylistInformation(() => ShowOpenedFiles(request.Value.SelectedIndex));
    }

    private void ShowPlaylistInformation(Action completed)
    {
        var prompt = new ProgressPrompt(
            // Translators: Title of the progress window shown while details of every file in the playlist are being read.
            Tr("Loading playlist info"),
            // Translators: First message in the progress window, shown before the first file has been read.
            Tr("Preparing..."),
            // Translators: Progress message naming the file being read right now. {name} is the file name.
            update => TrFormat("Reading: {name}", update.Name));
        // Everything the scan needs is read here, on the UI thread. mpv's properties belong to it, and the
        // scan itself only gets plain numbers to work from.
        var files = _player.Files;
        var currentPath = _player.CurrentPath;
        var currentIndex = _player.CurrentIndex;
        var duration = _player.Duration;
        var elapsed = _player.Elapsed;
        var remaining = _player.Remaining;
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (report, token) => _playlistInfo.Build(
                files, currentPath, currentIndex, duration, elapsed, remaining, report, token),
            totals =>
            {
                // Translators: Title of the window listing details of every file in the playlist.
                _view.ShowTextInfo(Tr("Playlist Info"), Describe(totals));
                completed();
            });
    }

    /// <summary>Turns the scan's numbers into the text shown to the user. On the UI thread, because the
    /// translation lookup is a wxWidgets object and the scan runs on a worker thread.</summary>
    private static string Describe(PlaylistTotals totals) => string.Join(Environment.NewLine,
        // Translators: The playlist summary. {count} is how many files are loaded.
        TrFormat("Number of files: {count}", totals.FileCount),
        // Translators: The playlist summary. {value} is a size such as "12.5 MB".
        TrFormat("Total size: {value}", FormatSize(totals.TotalBytes)),
        // Translators: The playlist summary. {value} is a duration as hours:minutes:seconds.
        TrFormat("Total duration: {value}", PlaybackTimeFormatter.Format(totals.TotalDuration)),
        // Translators: The playlist summary: how much of the whole playlist has already played.
        TrFormat("Elapsed: {value}", PlaybackTimeFormatter.Format(totals.Elapsed)),
        // Translators: The playlist summary: how much of the whole playlist is left to play.
        TrFormat("Remaining: {value}", PlaybackTimeFormatter.Format(totals.Remaining)));

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.##} {units[unit]}";
    }

    private void AnnounceFileInfo()
    {
        if (!_guard.RequireFile(out var path))
            return;

        var now = Environment.TickCount64;
        if (now - _fileInfoLastPress > FileInfoResetMilliseconds)
            _fileInfoPressCount = 0;
        _fileInfoLastPress = now;
        _fileInfoPressCount++;

        switch (_fileInfoPressCount)
        {
            case 1:
                var name = _player.CurrentName ?? MediaLibrary.DisplayName(path);
                _speech.Speak(name, name);
                break;
            case 2:
                _speech.Speak(path, path);
                break;
            default:
                var copied = _clipboard.SetText(path);
                _speech.Speak(
                    copied
                        // Translators: Spoken once the full location of the current file has been copied to the clipboard.
                        ? Tr("File path copied to clipboard.")
                        // Translators: Spoken when the full location of the current file could not be copied to the clipboard.
                        : Tr("Unable to copy to clipboard."),
                    copied ? Tr("Copied.") : Tr("Copy failed."));
                _fileInfoPressCount = 0;
                break;
        }
    }

    private void AnnounceTitle()
    {
        if (!_guard.RequireFile(out _))
            return;
        if (_player.CurrentTitle is { Length: > 0 } title)
            _speech.Speak(title, title);
        else
            _speech.Speak(
                // Translators: Spoken when the current media file contains no title metadata.
                Tr("The current file has no title."),
                // Translators: Short announcement when the current media file contains no title metadata.
                Tr("No title."));
    }
}
