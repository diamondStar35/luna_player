using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.Media;
using System.Collections.Concurrent;
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
    private int _fileInfoPressCount;
    private long _fileInfoLastPress;
    private readonly PlaylistInfoService _playlistInfo = new();

    internal FileActions(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        ISpeechOutput speech,
        IClipboardService clipboard)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _speech = speech;
        _clipboard = clipboard;
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
            : OpenFileWithConfiguredMode(files[0]);
        if (loaded)
            _settings.General.LastDirectory = Path.GetDirectoryName(files[0]) ?? string.Empty;
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
        var openedFile = OpenFileWithConfiguredMode(path);
        if (openedFile)
            _settings.General.LastDirectory = Path.GetDirectoryName(path) ?? string.Empty;
        return openedFile;
    }

    internal bool RestoreSession(string path, double position)
    {
        if (!File.Exists(path)) return false;
        var opened = OpenFileWithConfiguredMode(path, Math.Max(0, position));
        if (opened) _settings.General.LastDirectory = Path.GetDirectoryName(path) ?? string.Empty;
        return opened;
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
            Tr("Enter link to play."), Tr("Open Link"));
        if (link is null)
            return;
        // An empty entry is rejected the same way as a bad one, as the Python player does.
        var url = link.Trim();
        if (!MediaLibrary.IsHttpUrl(url))
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
        var path = _player.CurrentPath;
        if (path is null || !File.Exists(path))
        {
            _speech.Speak(
                // Translators: Spoken when the user asks to show the folder of what is playing but it is a stream rather than a file on this computer.
                Tr("Open containing folder is available only for local files."), Tr("Not available for streams."));
            return;
        }
        TryStart(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true },
            // Translators: Spoken when the folder holding the current file could not be shown.
            Tr("Could not open containing folder."));
    }

    private void OpenFileProperties()
    {
        var path = _player.CurrentPath;
        if (path is null || !File.Exists(path))
        {
            _speech.Speak(
                // Translators: Spoken when the user asks for the Windows properties of what is playing but it is a stream rather than a file on this computer.
                Tr("File properties are available only for local files."), Tr("Not available for streams."));
            return;
        }
        TryStart(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "properties" },
            // Translators: Spoken when the Windows properties window for the current file could not be shown.
            Tr("Could not open file properties."));
    }

    private void OpenedFiles()
    {
        if (_player.Count == 0)
        {
            _speech.Speak(Tr("No file loaded."), Tr("No file."));
            return;
        }
        var selected = _player.CurrentIndex;
        while (true)
        {
            var names = _player.Files.Select(_player.DisplayName).ToArray();
            var request = _view.ChooseOpenedFile(names, selected);
            if (request is null) return;
            selected = request.Value.SelectedIndex;
            if (request.Value.Action == OpenedFilesAction.Jump)
            {
                _player.GoToIndex(selected);
                return;
            }
            ShowPlaylistInformation();
        }
    }

    private void ShowPlaylistInformation()
    {
        var progressQueue = new ConcurrentQueue<PlaylistProbeProgress>();
        using var cancellation = new CancellationTokenSource();
        var task = Task.Run(() => _playlistInfo.Build(
            _player.Files, _player.CurrentPath, _player.CurrentIndex, _player.Duration,
            _player.Elapsed, _player.Remaining, progressQueue.Enqueue, cancellation.Token));
        using var progress = _view.BeginProgress(
            // Translators: Title of the progress window shown while details of every file in the playlist are being read.
            Tr("Loading playlist info"),
            // Translators: First message in the progress window, shown before the first file has been read.
            Tr("Preparing..."), _player.Count);
        while (!task.IsCompleted)
        {
            var updated = false;
            while (progressQueue.TryDequeue(out var item))
            {
                updated = true;
                // Translators: Progress message naming the file being read right now. {name} is the file name.
                if (!progress.Update(item.Value, TrFormat("Reading: {name}", item.Name))) cancellation.Cancel();
            }
            if (!updated && !progress.Pulse(Tr("Preparing..."))) cancellation.Cancel();
            Thread.Sleep(25);
        }
        try
        {
            var text = task.GetAwaiter().GetResult();
            // Translators: Title of the window listing details of every file in the playlist.
            _view.ShowTextInfo(Tr("Playlist Info"), text);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CloseFile()
    {
        if (!_player.CloseCurrent())
            _speech.Speak(Tr("No file loaded."), Tr("No file."));
        else
            // Translators: Spoken once the current file has been taken out of the playlist.
            _speech.Speak(Tr("File closed."), Tr("File closed."));
    }

    private void CloseAllFiles()
    {
        if (!_player.CloseAll())
            _speech.Speak(Tr("No file loaded."), Tr("No file."));
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

    private bool OpenFileWithConfiguredMode(string path, double? startPosition = null) => _settings.General.OpenFilesMode switch
    {
        OpenFilesMode.MainFolder => _player.OpenFileWithFolder(path, startPosition: startPosition),
        OpenFilesMode.MainAndSubfolders => _player.OpenFileWithFolder(path, recursive: true, startPosition: startPosition),
        _ => _player.OpenFile(path, startPosition),
    };

    private void AnnounceFileInfo()
    {
        var path = _player.CurrentPath;
        if (string.IsNullOrEmpty(path))
        {
            _speech.Speak(Tr("No file loaded."), Tr("No file."));
            return;
        }

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
            var value = rawPath.Trim().Trim('"');
            if (value.Length == 0)
                continue;
            try
            {
                paths.Add(Path.GetFullPath(value));
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }
        }
        return paths;
    }
}
