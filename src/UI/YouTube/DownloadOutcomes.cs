using LunaPlayer.Application;
using LunaPlayer.UI.Services;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.YouTube;

internal sealed class DownloadOutcomes(
    IApplicationDispatcher dispatcher,
    Func<Window?> owner,
    Action<Downloads.Outcome, Window>? show = null) : IDisposable
{
    private readonly Queue<Downloads.Outcome> _pending = [];
    private readonly Action<Downloads.Outcome, Window> _show = show ?? Show;
    private IDisposable? _retry;
    private bool _showing;
    private bool _disposed;

    internal void Add(Downloads.Outcome? outcome)
    {
        if (_disposed || outcome is not { } result) return;
        _pending.Enqueue(result);
        // Close and destroy progress before opening a message box in a nested modal loop.
        dispatcher.Post(ShowNext);
    }

    private void ShowNext()
    {
        if (_disposed || _showing) return;
        if (_pending.Count == 0) { Clear(); return; }
        if (owner() is not { } parent)
        {
            // Another native prompt can temporarily disable every wx window. Only the
            // notification waits for it; completed progress windows have already closed.
            _retry ??= dispatcher.Repeat(TimeSpan.FromMilliseconds(150), ShowNext);
            return;
        }
        _retry?.Dispose();
        _retry = null;
        _showing = true;
        try { _show(_pending.Dequeue(), parent); }
        finally
        {
            _showing = false;
            if (!_disposed && _pending.Count > 0) dispatcher.Post(ShowNext);
        }
    }

    internal static Window? SelectOwner(Window? active, Window? results, Window main, IEnumerable<Window> progress)
    {
        if (active is not null && active.Enabled && !progress.Any(window => ReferenceEquals(window, active)))
            return active;
        if (results?.Enabled == true) return results;
        return main.Enabled ? main : null;
    }

    private static void Show(Downloads.Outcome result, Window parent)
    {
        if (result.Success)
            Messages.Success(
                // Translators: Success message box shown after a YouTube download finishes and its progress window closes.
                Tr("The download finished. The file was saved to your downloads folder."), parent);
        else
            Messages.Show(
                result.Error.Length > 0
                    // Translators: YouTube download error message box. {details} is the untranslated technical reason from yt-dlp.
                    ? TrFormat("The download could not be completed.\n\nDetails: {details}", result.Error)
                    // Translators: YouTube download error message box when no technical reason is available.
                    : Tr("The download could not be completed. Check your internet connection and try again."),
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, parent);
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
