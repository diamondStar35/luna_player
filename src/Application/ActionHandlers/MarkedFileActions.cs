using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed class MarkedFileActions
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IClipboardService _clipboard;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly MarkedFileService _files = new();

    internal MarkedFileActions(ActionRouter router, IMainView view, MediaPlayer player, PlayerSettings settings,
        ISpeechOutput speech, IClipboardService clipboard, IApplicationDispatcher dispatcher)
    {
        _view = view; _player = player; _settings = settings; _speech = speech; _clipboard = clipboard;
        _dispatcher = dispatcher;
        router.Register(ActionId.MarkedCopyToFolder, () => Transfer(move: false));
        router.Register(ActionId.MarkedMoveToFolder, () => Transfer(move: true));
        router.Register(ActionId.MarkedCopyToClipboard, CopyToClipboard);
        router.Register(ActionId.MarkedDelete, Delete);
    }

    private void Transfer(bool move)
    {
        var marked = GetMarked();
        if (marked.Count == 0) return;
        var target = _view.ChooseFolder(_settings.General.LastDirectory);
        if (string.IsNullOrEmpty(target)) return;
        _settings.General.LastDirectory = target;
        var prompt = new ProgressPrompt(
            move
                // Translators: Title of the progress window shown while the marked files are being moved into another folder.
                ? Tr("Moving marked files")
                // Translators: Title of the progress window shown while the marked files are being copied into another folder.
                : Tr("Copying marked files"),
            // Translators: First message in the progress window, shown before the first file has been dealt with.
            Tr("Starting..."),
            // Translators: Progress message naming the file being copied or moved right now. {name} is the file name.
            update => TrFormat("Processing: {name}", update.Name));
        // Cancelling shows up as Cancelled on the result rather than as an exception, so the run still
        // finishes normally and still has something to report.
        BackgroundProgress.Start(_view, _dispatcher, prompt, marked.Count,
            (report, token) => _files.Transfer(marked, target, move, report, token),
            result =>
            {
                if (move && result.Succeeded.Count > 0) _player.RemovePaths(result.Succeeded);
                ReportTransfer(result, move);
            });
    }

    private void CopyToClipboard()
    {
        var marked = GetMarked();
        if (marked.Count == 0) return;
        var copied = _clipboard.SetFiles(marked);
        _speech.Speak(
            copied
                // Translators: Spoken once the marked files have been copied to the clipboard, ready to be pasted elsewhere.
                ? Tr("Marked files copied to clipboard.")
                // Translators: Spoken when the marked files could not be copied to the clipboard.
                : Tr("Unable to copy marked files to clipboard."),
            copied ? Tr("Copied.") : Tr("Copy failed."));
    }

    private void Delete()
    {
        var marked = GetMarked();
        if (marked.Count == 0) return;
        if (!_view.Confirm(
            // Translators: Asks the user to confirm deleting the marked files. {count} is how many are marked.
            TrPluralFormat("Delete {count} marked file?", "Delete {count} marked files?", marked.Count, marked.Count),
            Tr("Confirm Delete"))) return;
        var current = _player.CurrentPath;
        _player.Stop();
        var result = _files.Delete(marked);
        if (result.Succeeded.Count > 0) _player.RemovePaths(result.Succeeded);
        if (current is not null && !result.Succeeded.Any(path => Paths.AreSame(path, current)) && _player.CurrentPath is not null)
            _player.Reload();
        if (result.Succeeded.Count > 0 && result.Failed.Count == 0)
        {
            _view.ShowInfo(
                // Translators: Shown once the marked files have been deleted. {count} is how many were deleted.
                TrPluralFormat("Deleted {count} marked file.", "Deleted {count} marked files.", result.Succeeded.Count, result.Succeeded.Count),
                // Translators: Title of the message shown once deleting the marked files has finished.
                Tr("Delete Complete"));
            _speech.Speak(
                // Translators: Spoken once the marked files have been deleted.
                Tr("Marked files deleted."), Tr("Deleted."));
        }
        else if (result.Succeeded.Count > 0)
        {
            _view.ShowWarning(
                // Translators: Shown when only some of the marked files could be deleted.
                // {deleted} is how many were deleted and {failed} how many could not be.
                TrFormat("Deleted {deleted} files. Failed to delete {failed} files.", result.Succeeded.Count, result.Failed.Count),
                Tr("Delete Complete"));
            _speech.Speak(
                // Translators: Spoken when only some of the marked files could be deleted.
                Tr("Some files were deleted."),
                // Translators: The short wording spoken when only some of the marked files could be deleted.
                Tr("Partial delete."));
        }
        // Translators: Spoken when none of the marked files could be deleted.
        else _speech.Speak(Tr("Could not delete marked files."), Tr("Delete failed."));
    }

    private IReadOnlyList<string> GetMarked()
    {
        var marked = _player.MarkedFiles;
        if (marked.Count == 0) _speech.Speak(Tr("No marked files."), Tr("No marks."));
        return marked;
    }

    private void ReportTransfer(FileOperationResult result, bool move)
    {
        if (result.Cancelled)
        {
            _speech.Speak(
                // Translators: Spoken when the user stops copying or moving the marked files before it has finished.
                Tr("Operation canceled."),
                // Translators: The short wording spoken when the user stops copying or moving the marked files.
                Tr("Canceled."));
            return;
        }
        if (result.Succeeded.Count > 0 && result.Failed.Count == 0)
        {
            _view.ShowInfo(
                // Translators: Shown once every marked file has been copied or moved. {count} is how many were dealt with.
                TrPluralFormat("{count} file processed successfully.", "{count} files processed successfully.", result.Succeeded.Count, result.Succeeded.Count),
                // Translators: Title of the message shown once copying or moving the marked files has finished.
                Tr("Operation Complete"));
            _speech.Speak(
                move
                    // Translators: Spoken once the marked files have been moved into the chosen folder.
                    ? Tr("Marked files moved.")
                    // Translators: Spoken once the marked files have been copied into the chosen folder.
                    : Tr("Marked files copied."),
                move
                    // Translators: The short wording spoken once the marked files have been moved into the chosen folder.
                    ? Tr("Moved.")
                    : Tr("Copied."));
        }
        else if (result.Succeeded.Count > 0)
        {
            _view.ShowWarning(
                // Translators: Shown when only some of the marked files could be copied or moved.
                // {processed} is how many were dealt with and {failed} how many could not be.
                TrFormat("Processed {processed} files. Failed for {failed} files.", result.Succeeded.Count, result.Failed.Count),
                Tr("Operation Complete"));
            _speech.Speak(
                // Translators: Spoken when copying or moving the marked files worked for some of them but not all.
                Tr("Operation completed with errors."),
                // Translators: The short wording spoken when copying or moving the marked files worked for some of them but not all.
                Tr("Partial success."));
        }
        else _speech.Speak(
            // Translators: Spoken when none of the marked files could be copied or moved.
            Tr("Operation failed."),
            // Translators: The short wording spoken when none of the marked files could be copied or moved.
            Tr("Failed."));
    }
}
