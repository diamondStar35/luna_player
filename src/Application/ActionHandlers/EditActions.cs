using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed class EditActions
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly ISpeechOutput _speech;
    private readonly IClipboardService _clipboard;
    private readonly FileActions _fileActions;

    internal EditActions(ActionRouter router, IMainView view, MediaPlayer player, ISpeechOutput speech,
        IClipboardService clipboard, FileActions fileActions)
    {
        _view = view;
        _player = player;
        _speech = speech;
        _clipboard = clipboard;
        _fileActions = fileActions;
        router.Register(ActionId.RenameFile, Rename);
        router.Register(ActionId.DeleteFile, Delete);
        router.Register(ActionId.CopyFile, Copy);
        router.Register(ActionId.PasteFile, Paste);
        router.Register(ActionId.ToggleMarkCurrent, ToggleCurrentMark);
        router.Register(ActionId.ToggleMarkAll, ToggleAllMarks);
        router.Register(ActionId.ClearMarks, ClearMarks);
        router.Register(ActionId.AnnounceMarkedCount, AnnounceMarkedCount);
    }

    private void Rename()
    {
        if (!TryGetLocalPath("Rename", out var path))
            return;
        var oldName = Path.GetFileName(path);
        var value = _view.PromptText("Enter new name for the file.", "Rename File", oldName);
        if (value is null)
            return;
        value = Path.GetFileName(value.Trim());
        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(value)))
        {
            _view.ShowWarning("File name cannot be empty.", "Rename File");
            return;
        }
        if (string.IsNullOrEmpty(Path.GetExtension(value)))
            value += extension;
        var newPath = Path.Combine(Path.GetDirectoryName(path)!, value);
        if (File.Exists(newPath))
        {
            _view.ShowWarning("A file with that name already exists.", "Rename File");
            return;
        }
        if (_player.RenameCurrent(newPath))
            _speech.Speak("File renamed.", "Renamed.");
        else
            _speech.Speak("Could not rename the file.", "Rename failed.");
    }

    private void Delete()
    {
        if (!TryGetLocalPath("Delete", out var path))
            return;
        var name = Path.GetFileName(path);
        if (!_view.Confirm($"Delete '{name}'?", "Confirm Delete"))
            return;
        if (_player.DeleteCurrent())
            _speech.Speak("File deleted.", "Deleted.");
        else
            _speech.Speak("Could not delete the file.", "Delete failed.");
    }

    private void Copy()
    {
        if (!TryGetCurrentPath(out var path))
            return;
        var copied = _clipboard.SetFiles([path]);
        _speech.Speak(copied ? "Current file copied to clipboard." : "Unable to copy current file to clipboard.", copied ? "Copied." : "Copy failed.");
    }

    private void Paste()
    {
        var paths = _clipboard.GetPaths();
        if (paths.Count == 0)
        {
            _speech.Speak("Clipboard does not contain files or folders.", "Clipboard empty.");
            return;
        }
        foreach (var path in paths)
        {
            if (_fileActions.OpenLocalPath(path))
                return;
        }
        _speech.Speak("Clipboard does not contain openable files or folders.", "Cannot open clipboard data.");
    }

    private void ToggleCurrentMark()
    {
        var marked = _player.ToggleCurrentMarked();
        if (!marked.HasValue)
        {
            _speech.Speak("No file loaded.", "No file.");
            return;
        }
        _speech.Speak(marked.Value ? "File added to marked files." : "File removed from marked files.", marked.Value ? "Marked." : "Unmarked.");
    }

    private void ToggleAllMarks()
    {
        if (_player.Count == 0)
        {
            _speech.Speak("No file loaded.", "No file.");
            return;
        }
        var marked = _player.ToggleAllMarked();
        _speech.Speak(marked ? "All files marked." : "All files unmarked.", marked ? "All marked." : "All unmarked.");
    }

    private void ClearMarks()
    {
        var cleared = _player.ClearMarked();
        _speech.Speak(cleared ? "Marked files list cleared." : "No marked files.", cleared ? "Marks cleared." : "No marks.");
    }

    private void AnnounceMarkedCount()
    {
        var count = _player.MarkedCount;
        _speech.SpeakText(count == 1 ? "1 file marked" : $"{count} files marked");
    }

    private bool TryGetCurrentPath(out string path)
    {
        path = _player.CurrentPath ?? string.Empty;
        if (path.Length > 0)
            return true;
        _speech.Speak("No file loaded.", "No file.");
        return false;
    }

    private bool TryGetLocalPath(string action, out string path)
    {
        if (!TryGetCurrentPath(out path))
            return false;
        if (File.Exists(path))
            return true;
        _speech.Speak($"{action} is available only for local files.", "Not available for streams.");
        return false;
    }

}
