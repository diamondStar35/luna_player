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

    private void OpenFolderFromDialog()
    {
        var folder = _view.ChooseFolder(_settings.General.LastDirectory);
        if (string.IsNullOrEmpty(folder))
            return;
        _settings.General.LastDirectory = folder;
        if (!_player.OpenFolder(folder))
            _speech.Speak("No audio files found in that folder.", "No audio files.");
    }

    private void OpenContainingFolder()
    {
        var path = _player.CurrentPath;
        if (path is null || !File.Exists(path))
        {
            _speech.Speak("Open containing folder is available only for local files.", "Not available for streams.");
            return;
        }
        TryStart(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true }, "Could not open containing folder.");
    }

    private void OpenFileProperties()
    {
        var path = _player.CurrentPath;
        if (path is null || !File.Exists(path))
        {
            _speech.Speak("File properties are available only for local files.", "Not available for streams.");
            return;
        }
        TryStart(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "properties" }, "Could not open file properties.");
    }

    private void OpenedFiles()
    {
        if (_player.Count == 0)
        {
            _speech.Speak("No file loaded.", "No file.");
            return;
        }
        var selected = _player.CurrentIndex;
        while (true)
        {
            var names = _player.Files.Select(path => Path.GetFileName(path) is { Length: > 0 } name ? name : path).ToArray();
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
        using var progress = _view.BeginProgress("Loading playlist info", "Preparing...", _player.Count);
        while (!task.IsCompleted)
        {
            var updated = false;
            while (progressQueue.TryDequeue(out var item))
            {
                updated = true;
                if (!progress.Update(item.Value, $"Reading: {item.Name}")) cancellation.Cancel();
            }
            if (!updated && !progress.Pulse("Preparing...")) cancellation.Cancel();
            Thread.Sleep(25);
        }
        try
        {
            var text = task.GetAwaiter().GetResult();
            _view.ShowTextInfo("Playlist Info", text);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CloseFile()
    {
        if (!_player.CloseCurrent())
            _speech.Speak("No file loaded.", "No file.");
        else
            _speech.Speak("File closed.", "File closed.");
    }

    private void CloseAllFiles()
    {
        if (!_player.CloseAll())
            _speech.Speak("No file loaded.", "No file.");
        else
            _speech.Speak("All files closed.", "All files closed.");
    }

    private void TryStart(ProcessStartInfo startInfo, string failure)
    {
        try { Process.Start(startInfo); }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        { _speech.Speak(failure, "Open failed."); }
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
            _speech.Speak("No file loaded.", "No file.");
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
                var name = Path.GetFileName(path);
                _speech.Speak(string.IsNullOrEmpty(name) ? path : name, string.IsNullOrEmpty(name) ? path : name);
                break;
            case 2:
                _speech.Speak(path, path);
                break;
            default:
                var copied = _clipboard.SetText(path);
                _speech.Speak(
                    copied ? "File path copied to clipboard." : "Unable to copy to clipboard.",
                    copied ? "Copied." : "Copy failed.");
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
