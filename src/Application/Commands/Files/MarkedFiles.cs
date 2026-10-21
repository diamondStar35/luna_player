using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Commands.Files;

internal sealed class MarkedFiles
{
    private readonly IMainView _view;
    private readonly Player _player;
    private readonly Settings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IClipboardService _clipboard;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly MarkedFileService _files = new();

    internal MarkedFiles(ActionRouter router, IMainView view, Player player, Settings settings,
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
        var target = _view.Dialogs.ChooseFolder(_settings.General.LastDirectory);
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
        BackgroundProgress.Start(_view, _dispatcher, prompt,
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
            copied
                // Translators: Short announcement after copying marked files to the clipboard succeeds.
                ? Tr("Copied.")
                // Translators: Short announcement after copying marked files to the clipboard fails.
                : Tr("Copy failed."));
    }

    private void Delete()
    {
        var marked = GetMarked();
        if (marked.Count == 0) return;
        if (!_view.Dialogs.Confirm(
            // Translators: Asks the user to confirm deleting the marked files. {count} is how many are marked.
            TrPluralFormat("Delete {count} marked file?", "Delete {count} marked files?", marked.Count, marked.Count),
            // Translators: Title of the dialog that confirms deletion of marked files.
            Tr("Confirm Delete"))) return;
        var current = _player.CurrentPath;
        _player.Stop();
        var result = _files.Delete(marked);
        if (result.Succeeded.Count > 0) _player.RemovePaths(result.Succeeded);
        if (current is not null && !result.Succeeded.Any(path => Paths.AreSame(path, current)) && _player.CurrentPath is not null)
            _player.Reload();
        if (result.Succeeded.Count > 0 && result.Failed.Count == 0)
        {
            _view.Dialogs.ShowInfo(
                // Translators: Shown once the marked files have been deleted. {count} is how many were deleted.
                TrPluralFormat("Deleted {count} marked file.", "Deleted {count} marked files.", result.Succeeded.Count, result.Succeeded.Count),
                // Translators: Title of the message shown once deleting the marked files has finished.
                Tr("Delete Complete"));
            _speech.Speak(
                // Translators: Spoken once the marked files have been deleted.
                Tr("Marked files deleted."),
                // Translators: Short announcement once marked files have been deleted.
                Tr("Deleted."));
        }
        else if (result.Succeeded.Count > 0)
        {
            _view.Dialogs.ShowWarning(
                // Translators: Shown when only some of the marked files could be deleted.
                // {deleted} is how many were deleted and {failed} how many could not be.
                TrFormat("Deleted {deleted} files. Failed to delete {failed} files.", result.Succeeded.Count, result.Failed.Count),
                // Translators: Title of the result shown after deleting marked files.
                Tr("Delete Complete"));
            _speech.Speak(
                // Translators: Spoken when only some of the marked files could be deleted.
                Tr("Some files were deleted."),
                // Translators: The short wording spoken when only some of the marked files could be deleted.
                Tr("Partial delete."));
        }
        else
            _speech.Speak(
                // Translators: Spoken when none of the marked files could be deleted.
                Tr("Could not delete marked files."),
                // Translators: Short announcement when no marked file could be deleted.
                Tr("Delete failed."));
    }

    private IReadOnlyList<string> GetMarked()
    {
        var marked = _player.MarkedFiles;
        if (marked.Count == 0)
            _speech.Speak(
                // Translators: Spoken when an action needs marked files but none are marked.
                Tr("No marked files."),
                // Translators: Short announcement when no files are marked.
                Tr("No marks."));
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
            _view.Dialogs.ShowInfo(
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
                    // Translators: The short wording spoken once the marked files have been copied into the chosen folder.
                    : Tr("Copied."));
        }
        else if (result.Succeeded.Count > 0)
        {
            _view.Dialogs.ShowWarning(
                // Translators: Shown when only some of the marked files could be copied or moved.
                // {processed} is how many were dealt with and {failed} how many could not be.
                TrFormat("Processed {processed} files. Failed for {failed} files.", result.Succeeded.Count, result.Failed.Count),
                // Translators: Title of a partial-success result for copying or moving marked files.
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
