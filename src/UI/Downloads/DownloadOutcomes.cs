using LunaPlayer.Application;
using LunaPlayer.UI.Services;
using WxSharp;
using SharedDownloads = LunaPlayer.Downloads;

namespace LunaPlayer.UI.Downloads;

internal sealed class DownloadOutcomes(
    IApplicationDispatcher dispatcher,
    Func<Window?> owner,
    Action<SharedDownloads.Outcome, Window>? show = null) : IDisposable
{
    private readonly Queue<(SharedDownloads.Outcome Result, Window? Owner)> _pending = [];
    private readonly Action<SharedDownloads.Outcome, Window> _show = show ?? Show;
    private IDisposable? _retry;
    private bool _showing;
    private bool _disposed;

    internal void Add(SharedDownloads.Outcome? outcome, Window? completionOwner = null)
    {
        if (_disposed || outcome is not { } result) return;
        _pending.Enqueue((result, completionOwner));
        // Close and destroy progress before opening a message box in a nested modal loop.
        dispatcher.Post(ShowNext);
    }

    private void ShowNext()
    {
        if (_disposed || _showing) return;
        if (_pending.Count == 0) { Clear(); return; }
        Window? preferred = _pending.Peek().Owner;
        try { if (preferred is not null && !preferred.Enabled) { Wait(); return; } }
        catch (ObjectDisposedException) { preferred = null; }
        if ((preferred ?? owner()) is not { } parent)
        {
            // Another native prompt can temporarily disable every wx window. Only the
            // notification waits for it; completed progress windows have already closed.
            Wait();
            return;
        }
        _retry?.Dispose();
        _retry = null;
        _showing = true;
        try { _show(_pending.Dequeue().Result, parent); }
        finally
        {
            _showing = false;
            if (!_disposed && _pending.Count > 0) dispatcher.Post(ShowNext);
        }
    }

    private void Wait() => _retry ??= dispatcher.Repeat(TimeSpan.FromMilliseconds(150), ShowNext);

    internal static Window? SelectOwner(Window? active, Window? results, Window main, IEnumerable<Window> progress)
    {
        if (active is not null && active.Enabled && !progress.Any(window => ReferenceEquals(window, active)))
            return active;
        if (results?.Enabled == true) return results;
        return main.Enabled ? main : null;
    }

    private static void Show(SharedDownloads.Outcome result, Window parent)
    {
        if (result.Success)
            Messages.Success(
                // Translators: Download success message. {path} is the full destination path.
                TrFormat("The download finished. The file was saved to:\n{path}", result.FilePath), parent, ownerOnly: true);
        else
            Messages.Show(
                result.Error.Length > 0
                    // Translators: Download error message box. {details} is the untranslated technical reason from the download provider.
                    ? TrFormat("The download could not be completed.\n\nDetails: {details}", result.Error)
                    // Translators: Download error message box when no technical reason is available.
                    : Tr("The download could not be completed. Check your internet connection and try again."),
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, parent, ownerOnly: true);
    }

    internal void Clear()
    {
        _pending.Clear();
        _retry?.Dispose();
        _retry = null;
    }

    public void Dispose()
    {
        _disposed = true;
        Clear();
    }
}
