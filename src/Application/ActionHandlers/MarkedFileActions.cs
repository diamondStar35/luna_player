using System.Collections.Concurrent;
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
    private readonly MarkedFileService _files = new();

    internal MarkedFileActions(ActionRouter router, IMainView view, MediaPlayer player, PlayerSettings settings,
        ISpeechOutput speech, IClipboardService clipboard)
    {
        _view = view; _player = player; _settings = settings; _speech = speech; _clipboard = clipboard;
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
        var queue = new ConcurrentQueue<FileOperationProgress>();
        using var cancellation = new CancellationTokenSource();
        var task = Task.Run(() => _files.Transfer(marked, target, move, queue.Enqueue, cancellation.Token));
        using var progress = _view.BeginProgress(move ? "Moving marked files" : "Copying marked files", "Starting...", marked.Count);
        while (!task.IsCompleted)
        {
            var updated = false;
            while (queue.TryDequeue(out var item))
            {
                updated = true;
                if (!progress.Update(item.Value, $"Processing: {item.Name}")) cancellation.Cancel();
            }
            if (!updated && !progress.Pulse("Working...")) cancellation.Cancel();
            Thread.Sleep(25);
        }
        var result = task.GetAwaiter().GetResult();
        if (move && result.Succeeded.Count > 0) _player.RemovePaths(result.Succeeded);
        ReportTransfer(result, move);
    }

    private void CopyToClipboard()
    {
        var marked = GetMarked();
        if (marked.Count == 0) return;
        var copied = _clipboard.SetFiles(marked);
        _speech.Speak(copied ? "Marked files copied to clipboard." : "Unable to copy marked files to clipboard.", copied ? "Copied." : "Copy failed.");
    }

    private void Delete()
    {
        var marked = GetMarked();
        if (marked.Count == 0) return;
        if (!_view.Confirm($"Delete {marked.Count} marked files?", "Confirm Delete")) return;
        var current = _player.CurrentPath;
        _player.Stop();
        var result = _files.Delete(marked);
        if (result.Succeeded.Count > 0) _player.RemovePaths(result.Succeeded);
        if (current is not null && !result.Succeeded.Any(path => SamePath(path, current)) && _player.CurrentPath is not null)
            _player.Reload();
        if (result.Succeeded.Count > 0 && result.Failed.Count == 0)
        {
            _view.ShowInfo($"Deleted {result.Succeeded.Count} marked files.", "Delete Complete");
            _speech.Speak("Marked files deleted.", "Deleted.");
        }
        else if (result.Succeeded.Count > 0)
        {
            _view.ShowWarning($"Deleted {result.Succeeded.Count} files. Failed to delete {result.Failed.Count} files.", "Delete Complete");
            _speech.Speak("Some files were deleted.", "Partial delete.");
        }
        else _speech.Speak("Could not delete marked files.", "Delete failed.");
    }

    private IReadOnlyList<string> GetMarked()
    {
        var marked = _player.MarkedFiles;
        if (marked.Count == 0) _speech.Speak("No marked files.", "No marks.");
        return marked;
    }

    private void ReportTransfer(FileOperationResult result, bool move)
    {
        if (result.Cancelled)
        {
            _speech.Speak("Operation canceled.", "Canceled.");
            return;
        }
        if (result.Succeeded.Count > 0 && result.Failed.Count == 0)
        {
            _view.ShowInfo($"{result.Succeeded.Count} files processed successfully.", "Operation Complete");
            _speech.Speak(move ? "Marked files moved." : "Marked files copied.", move ? "Moved." : "Copied.");
        }
        else if (result.Succeeded.Count > 0)
        {
            _view.ShowWarning($"Processed {result.Succeeded.Count} files. Failed for {result.Failed.Count} files.", "Operation Complete");
            _speech.Speak("Operation completed with errors.", "Partial success.");
        }
        else _speech.Speak("Operation failed.", "Failed.");
    }

    private static bool SamePath(string left, string right)
    {
        try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException) { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }
    }
}
