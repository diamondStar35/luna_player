using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.Media;
using System.Diagnostics;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed class FileActions
{
    private const long FileInfoResetMilliseconds = 300;
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IClipboardService _clipboard;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly MediaGuard _guard;
    private int _fileInfoPressCount;
    private long _fileInfoLastPress;
    private readonly PlaylistInfoService _playlistInfo = new();

    internal FileActions(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        ISpeechOutput speech,
        IClipboardService clipboard,
        IApplicationDispatcher dispatcher)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _speech = speech;
        _clipboard = clipboard;
        _dispatcher = dispatcher;
        _guard = new MediaGuard(player, speech);
        router.Register(ActionId.OpenFile, OpenFileFromDialog);
        router.Register(ActionId.OpenLink, OpenLink);
        router.Register(ActionId.OpenFolder, OpenFolderFromDialog);
        router.Register(ActionId.OpenContainingFolder, OpenContainingFolder);
        router.Register(ActionId.OpenFileProperties, OpenFileProperties);
        router.Register(ActionId.OpenedFiles, OpenedFiles);
        router.Register(ActionId.CloseFile, CloseFile);
        router.Register(ActionId.CloseAllFiles, CloseAllFiles);
        router.Register(ActionId.Exit, _view.Close);
        router.Register(ActionId.AnnounceFileInfo, AnnounceFileInfo);
    }

    internal void OpenPaths(IEnumerable<string> rawPaths)
    {
        var paths = NormalizePaths(rawPaths);
        if (paths.Count == 0)
            return;

        var first = paths[0];
        if (Directory.Exists(first))
        {
            if (_player.OpenFolder(first))
                _settings.General.LastDirectory = first;
            return;
        }

        var files = paths.Where(File.Exists).ToList();
        if (files.Count == 0)
            return;
        var loaded = _settings.General.OpenFilesMode == OpenFilesMode.FileOnly && files.Count > 1
            ? _player.OpenFiles(files, files[0])
            : true;
        if (loaded)
            _settings.General.LastDirectory = Path.GetDirectoryName(files[0]) ?? string.Empty;
        if (_settings.General.OpenFilesMode != OpenFilesMode.FileOnly || files.Count <= 1)
            OpenFileWithConfiguredMode(files[0]);
    }

    internal bool OpenLocalPath(string path)
    {
        if (Directory.Exists(path))
        {
            var opened = _player.OpenFolder(path);
            if (opened)
                _settings.General.LastDirectory = path;
            return opened;
        }
        if (!File.Exists(path))
            return false;
        _settings.General.LastDirectory = Path.GetDirectoryName(path) ?? string.Empty;
        OpenFileWithConfiguredMode(path);
        return true;
    }

    internal bool RestoreSession(string path, double position)
    {
        if (!File.Exists(path)) return false;
        _settings.General.LastDirectory = Path.GetDirectoryName(path) ?? string.Empty;
        OpenFileWithConfiguredMode(path, Math.Max(0, position));
        return true;
    }

    private void OpenFileFromDialog()
    {
        var selection = _view.ChooseFile(_settings.General.LastDirectory);
        if (selection is not FileSelection file)
            return;
        _settings.General.LastDirectory = string.IsNullOrEmpty(file.Directory)
            ? Path.GetDirectoryName(file.Path) ?? string.Empty
            : file.Directory;
        OpenFileWithConfiguredMode(file.Path);
    }

    private void OpenLink()
    {
        var link = _view.PromptText(
            // Translators: Asks the user for the web address of the stream they want to play.
            Tr("Enter link to play."),
            // Translators: Title of the window that asks for the web address of a stream to play.
            Tr("Open Link"));
        if (link is null)
            return;
        // An empty entry is rejected the same way as a bad one, as the Python player does.
        var url = link.Trim();
        if (!LinkValidator.IsHttpUrl(url))
        {
            _view.ShowError(
                // Translators: Shown when the address typed into Open Link is not a web address.
                // "http" and "https" are the names of the two web protocols and are not translated.
                Tr("The link must start with http or https."),
                // Translators: Title of the message shown when the address typed into Open Link is not a web address.
                Tr("Invalid link"));
            return;
        }
        if (!_player.OpenStream(url))
            _view.ShowError(
                // Translators: Shown when the stream at the address typed into Open Link could not be played.
                Tr("Could not open the link."), Tr("Error"));
    }

    private void OpenFolderFromDialog()
    {
        var folder = _view.ChooseFolder(_settings.General.LastDirectory);
        if (string.IsNullOrEmpty(folder))
            return;
        _settings.General.LastDirectory = folder;
        if (!_player.OpenFolder(folder))
            _speech.Speak(
                // Translators: Spoken when the chosen folder holds nothing this player can play.
                Tr("No audio files found in that folder."),
                // Translators: The short wording spoken when the chosen folder holds nothing this player can play.
                Tr("No audio files."));
    }

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
        TryStart(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "properties" },
            // Translators: Spoken when the Windows properties window for the current file could not be shown.
            Tr("Could not open file properties."));
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

    private void CloseFile()
    {
        if (_player.CurrentPath is null)
            _guard.ReportNoFile();
        else if (!_player.CloseCurrent())
            _speech.Speak(
                // Translators: Spoken when the current file could not be taken out of the playlist.
                Tr("Could not close the file."),
                // Translators: The short wording spoken when a file could not be closed.
                Tr("Close failed."));
        else
            // Translators: Spoken once the current file has been taken out of the playlist.
            _speech.Speak(Tr("File closed."), Tr("File closed."));
    }

    private void CloseAllFiles()
    {
        if (_player.Count == 0)
            _guard.ReportNoFile();
        else if (!_player.CloseAll())
            _speech.Speak(
                // Translators: Spoken when the loaded files could not be taken out of the playlist.
                Tr("Could not close files."),
                // Translators: The short wording spoken when the files could not be closed.
                Tr("Close failed."));
        else
            // Translators: Spoken once every file has been taken out of the playlist.
            _speech.Speak(Tr("All files closed."), Tr("All files closed."));
    }

    private void TryStart(ProcessStartInfo startInfo, string failure)
    {
        try { Process.Start(startInfo); }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        // Translators: The short wording spoken when Windows could not be asked to open a folder or a properties window.
        { _speech.Speak(failure, Tr("Open failed.")); }
    }

    /// <summary>Opens a file, bringing its neighbours with it as the settings ask.</summary>
    /// <remarks>
    /// Walking a folder and everything under it can take long enough to look like the player has hung, so
    /// that one mode does its walking on a worker thread behind a progress window the user can abort. It
    /// therefore returns before the file is open, which is why nothing here reports whether it worked; the
    /// two cheap modes are done where they stand.
    /// </remarks>
    private void OpenFileWithConfiguredMode(string path, double? startPosition = null)
    {
        switch (_settings.General.OpenFilesMode)
        {
            case OpenFilesMode.MainFolder:
                _player.OpenFileWithFolder(path, startPosition: startPosition);
                break;
            case OpenFilesMode.MainAndSubfolders:
                OpenWithSubfolders(path, startPosition);
                break;
            default:
                _player.OpenFile(path, startPosition);
                break;
        }
    }

    /// <summary>Loads a file together with everything under its folder, scanning off the UI thread.</summary>
    private void OpenWithSubfolders(string path, double? startPosition)
    {
        var folder = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(folder))
            return;
        var prompt = new ProgressPrompt(
            // Translators: Title of the progress window shown while a folder and the folders inside it are being searched for media.
            Tr("Opening files"),
            // Translators: First message in the progress window, before any file has been found.
            Tr("Loading files from folder and subfolders..."),
            // Translators: Progress message while a folder is being searched. {found} is how many media files
            // have been found so far.
            update => TrFormat("Opening files... {found} media files found", update.Found))
        {
            // The tree is walked once, so there is never a proportion to show - only the count, which the
            // message carries. The window goes without a bar rather than showing one that cannot move.
            Proportional = false,
        };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (report, token) => MediaLibrary.CollectFiles(folder, recursive: true, report, token),
            files =>
            {
                if (files.Count > 0)
                    _player.OpenFiles(files, path, startPosition);
                else
                    _speech.Speak(
                        // Translators: Spoken when the folder and the folders inside it hold nothing this player can play.
                        Tr("No audio files found in that folder."),
                        // Translators: The short wording spoken when a folder holds nothing this player can play.
                        Tr("No audio files."));
            });
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
                var name = _player.DisplayName(path);
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

    private static List<string> NormalizePaths(IEnumerable<string> rawPaths)
    {
        var paths = new List<string>();
        foreach (var rawPath in rawPaths)
        {
            // Command lines and drops arrive quoted, and a path that will not resolve is left out rather
            // than passed on to be reported as a file that does not exist.
            var value = rawPath.Trim().Trim('"');
            if (value.Length > 0 && Paths.TryAbsolute(value, out var absolute))
                paths.Add(absolute);
        }
        return paths;
    }
}
