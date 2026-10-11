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
    private readonly RecentsStore _recents;

    internal Files(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        ISpeechOutput speech,
        IClipboardService clipboard,
        IApplicationDispatcher dispatcher,
        RecentsStore recents)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _speech = speech;
        _clipboard = clipboard;
        _dispatcher = dispatcher;
        _recents = recents;
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
        router.Register(ActionId.AnnounceTitle, AnnounceTitle);
        _view.RecentRequested += OnRecentRequested;
        RefreshRecents();
    }

    internal void OpenPaths(IEnumerable<string> rawPaths)
    {
        var paths = NormalizePaths(rawPaths);
        if (paths.Count == 0)
            return;

        var first = paths[0];
        if (Directory.Exists(first))
        {
            if (OpenFolderWithConfiguredMode(first))
                _settings.General.LastDirectory = first;
            return;
        }

        if (File.Exists(first) && MediaLibrary.IsPlaylist(first))
        {
            OpenLocalPlaylist(first);
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
        else if (loaded)
            RecordRecent(RecentKind.File, files[0]);
    }

    internal bool OpenLocalPath(string path)
    {
        if (Directory.Exists(path))
        {
            var opened = OpenFolderWithConfiguredMode(path);
            if (opened)
                _settings.General.LastDirectory = path;
            return opened;
        }
        if (!File.Exists(path))
            return false;
        _settings.General.LastDirectory = Path.GetDirectoryName(path) ?? string.Empty;
        if (MediaLibrary.IsPlaylist(path))
            return OpenLocalPlaylist(path);
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
        if (MediaLibrary.IsPlaylist(file.Path))
        {
            OpenLocalPlaylist(file.Path);
            return;
        }
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
        // An empty entry is rejected the same way as a bad one.
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
        if (MediaLibrary.IsPlaylist(url))
        {
            OpenNetworkPlaylist(url);
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
        if (!OpenFolderWithConfiguredMode(folder))
            _speech.Speak(
                // Translators: Spoken when the chosen folder holds nothing this player can play.
                Tr("No audio files found in that folder."),
                // Translators: The short wording spoken when the chosen folder holds nothing this player can play.
                Tr("No audio files."));
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
        // Translators: The short wording spoken when Windows could not be asked to open a folder.
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
        RecordRecent(RecentKind.File, path);
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
        OpenFolderAndSubfolders(folder, path, startPosition);
    }

    /// <summary>Opens a folder according to the configured scope.</summary>
    /// <remarks>
    /// A folder has no single-file equivalent, so FileOnly and MainFolder both mean its immediate contents.
    /// The recursive mode starts its scan in the background and reports true once that work is accepted; an
    /// empty result is reported when the scan completes.
    /// </remarks>
    private bool OpenFolderWithConfiguredMode(string folder)
    {
        bool opened;
        if (_settings.General.OpenFilesMode != OpenFilesMode.MainAndSubfolders)
            opened = _player.OpenFolder(folder);
        else
        {
            OpenFolderAndSubfolders(folder);
            opened = true;
        }
        if (opened)
            RecordRecent(RecentKind.Folder, folder);
        return opened;
    }

    /// <summary>Loads everything under a folder, scanning off the UI thread.</summary>
    /// <param name="preferredPath">The file to select after scanning, or null to select the first file.</param>
    private void OpenFolderAndSubfolders(
        string folder, string? preferredPath = null, double? startPosition = null)
    {
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
                    _player.OpenFiles(files, preferredPath, startPosition);
                else
                    _speech.Speak(
                        // Translators: Spoken when the folder and the folders inside it hold nothing this player can play.
                        Tr("No audio files found in that folder."),
                        // Translators: The short wording spoken when a folder holds nothing this player can play.
                        Tr("No audio files."));
            });
    }

    private void RecordRecent(RecentKind kind, string path)
    {
        _recents.Add(kind, path);
        RefreshRecents();
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
