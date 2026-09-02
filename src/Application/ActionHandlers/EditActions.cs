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
    private readonly MediaGuard _guard;

    internal EditActions(ActionRouter router, IMainView view, MediaPlayer player, ISpeechOutput speech,
        IClipboardService clipboard, FileActions fileActions)
    {
        _view = view;
        _player = player;
        _speech = speech;
        _clipboard = clipboard;
        _fileActions = fileActions;
        _guard = new MediaGuard(player, speech);
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
        // Translators: Spoken when the user asks to rename what is playing but it is a stream rather than a file on this computer.
        if (!_guard.RequireLocalFile(Tr("Rename is available only for local files."), out var path))
            return;
        var oldName = Path.GetFileName(path);
        var value = _view.PromptText(
            // Translators: Asks the user for the name the current file should be given.
            Tr("Enter new name for the file."),
            // Translators: Title of the window that asks for the name the current file should be given.
            Tr("Rename File"), oldName);
        if (value is null)
            return;
        value = Path.GetFileName(value.Trim());
        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(value)))
        {
            // Translators: Spoken and shown when the user confirms a new file name without typing anything.
            _view.ShowWarning(Tr("File name cannot be empty."), Tr("Rename File"));
            return;
        }
        if (string.IsNullOrEmpty(Path.GetExtension(value)))
            value += extension;
        var newPath = Path.Combine(Path.GetDirectoryName(path)!, value);
        if (File.Exists(newPath))
        {
            // Translators: Spoken and shown when the new name given to a file is already taken by another file in the same folder.
            _view.ShowWarning(Tr("A file with that name already exists."), Tr("Rename File"));
            return;
        }
        if (_player.RenameCurrent(newPath))
            _speech.Speak(
                // Translators: Spoken once the current file has been given its new name.
                Tr("File renamed."),
                // Translators: The short wording spoken once the current file has been given its new name.
                Tr("Renamed."));
        else
            _speech.Speak(
                // Translators: Spoken when the current file could not be given a new name.
                Tr("Could not rename the file."),
                // Translators: The short wording spoken when the current file could not be given a new name.
                Tr("Rename failed."));
    }

    private void Delete()
    {
        // Translators: Spoken when the user asks to delete what is playing but it is a stream rather than a file on this computer.
        if (!_guard.RequireLocalFile(Tr("Delete is available only for local files."), out var path))
            return;
        var name = Path.GetFileName(path);
        if (!_view.Confirm(
            // Translators: Asks the user to confirm deleting a file. {name} is the file name.
            TrFormat("Delete '{name}'?", name),
            // Translators: Title of the window that asks the user to confirm deleting a file.
            Tr("Confirm Delete")))
            return;
        if (_player.DeleteCurrent())
            _speech.Speak(
                // Translators: Spoken once the current file has been deleted from the disk.
                Tr("File deleted."),
                // Translators: The short wording spoken once the current file has been deleted from the disk.
                Tr("Deleted."));
        else
            _speech.Speak(
                // Translators: Spoken when the current file could not be deleted from the disk.
                Tr("Could not delete the file."),
                // Translators: The short wording spoken when the current file could not be deleted from the disk.
                Tr("Delete failed."));
    }

    private void Copy()
    {
        if (!_guard.RequireFile(out var path))
            return;
        var copied = _clipboard.SetFiles([path]);
        _speech.Speak(
            copied
                // Translators: Spoken once the current file has been copied to the clipboard, ready to be pasted elsewhere.
                ? Tr("Current file copied to clipboard.")
                // Translators: Spoken when the current file could not be copied to the clipboard.
                : Tr("Unable to copy current file to clipboard."),
            copied
                // Translators: The short wording spoken once the current file has been copied to the clipboard.
                ? Tr("Copied.")
                // Translators: The short wording spoken when the current file could not be copied to the clipboard.
                : Tr("Copy failed."));
    }

    private void Paste()
    {
        var paths = _clipboard.GetPaths();
        if (paths.Count == 0)
        {
            _speech.Speak(
                // Translators: Spoken when the user pastes but the clipboard holds no files or folders to open.
                Tr("Clipboard does not contain files or folders."),
                // Translators: The short wording spoken when the clipboard holds no files or folders to open.
                Tr("Clipboard empty."));
            return;
        }
        // Which of these went wrong matters: a path that is not there at all, one the player cannot make
        // sense of, and one that holds nothing playable are three different things for the user to fix.
        var resolved = paths.FirstOrDefault(path => File.Exists(path) || Directory.Exists(path));
        if (resolved is null)
        {
            _speech.Speak(
                // Translators: Spoken when what was pasted names a file or folder that is not there. {path} is what was pasted.
                TrFormat("Clipboard path could not be resolved: {path}", paths[0]),
                // Translators: The short wording spoken when what was pasted names nothing that exists.
                Tr("Invalid path in clipboard."));
            return;
        }
        if (!_fileActions.OpenLocalPath(resolved))
            _speech.Speak(
                // Translators: Spoken when what was pasted exists but holds nothing this player can play.
                Tr("Could not open the file from clipboard."),
                // Translators: The short wording spoken when what was pasted could not be opened.
                Tr("Cannot open clipboard data."));
    }

    private void ToggleCurrentMark()
    {
        var marked = _player.ToggleCurrentMarked();
        if (!marked.HasValue)
        {
            _guard.ReportNoFile();
            return;
        }
        _speech.Speak(
            marked.Value
                // Translators: Spoken once the current file has been added to the marked files.
                ? Tr("File added to marked files.")
                // Translators: Spoken once the current file has been taken out of the marked files.
                : Tr("File removed from marked files."),
            marked.Value
                // Translators: The short wording spoken once the current file has been added to the marked files.
                ? Tr("Marked.")
                // Translators: The short wording spoken once the current file has been taken out of the marked files.
                : Tr("Unmarked."));
    }

    private void ToggleAllMarks()
    {
        if (!_guard.RequireAnyFile())
            return;
        var marked = _player.ToggleAllMarked();
        _speech.Speak(
            marked
                // Translators: Spoken once every loaded file has been marked.
                ? Tr("All files marked.")
                // Translators: Spoken once every loaded file has been unmarked.
                : Tr("All files unmarked."),
            marked
                // Translators: The short wording spoken once every loaded file has been marked.
                ? Tr("All marked.")
                // Translators: The short wording spoken once every loaded file has been unmarked.
                : Tr("All unmarked."));
    }

    private void ClearMarks()
    {
        var cleared = _player.ClearMarked();
        _speech.Speak(
            cleared
                // Translators: Spoken once the list of marked files has been emptied.
                ? Tr("Marked files list cleared.")
                // Translators: Spoken when the user clears the marked files but none were marked.
                : Tr("No marked files."),
            cleared
                // Translators: The short wording spoken once the list of marked files has been emptied.
                ? Tr("Marks cleared.")
                // Translators: The short wording spoken when the user clears the marked files but none were marked.
                : Tr("No marks."));
    }

    private void AnnounceMarkedCount()
    {
        var count = _player.MarkedCount;
        // Translators: Spoken when the user asks how many files are marked. {count} is that number.
        _speech.SpeakText(TrPluralFormat("{count} file marked", "{count} files marked", count, count));
    }

}
