using System.Collections.Concurrent;
using System.Diagnostics;
using LunaPlayer.Application;
using WxSharp;

namespace LunaPlayer.UI.Radio;

/// <summary>Workers enqueue prepared data; bounded UI turns apply it without holding up the reader.</summary>
internal sealed class Updates : IDisposable
{
    private readonly IApplicationDispatcher _dispatcher;
    private readonly WxSharp.Timer _timer;
    private readonly Action _render;
    private readonly Action<Exception> _report;
    private readonly ConcurrentQueue<Pending> _foreground = new();
    private readonly ConcurrentQueue<Pending> _background = new();
    private int _scheduled;
    private int _disposed;

    // Returning false retains the head until its remaining work has been applied. Completion records
    // therefore cannot overtake the streamed rows they describe.
    private sealed record Pending(Func<bool> Apply, CancellationToken Token);

    internal Updates(IApplicationDispatcher dispatcher, Action render, Action<Exception> report)
    {
        _dispatcher = dispatcher;
        _render = render;
        _report = report;
        _timer = new WxSharp.Timer(App.Current!);
        _timer.Tick += (_, _) => Drain();
    }

    internal void Post(Action apply, CancellationToken token, bool background = false)
        => Post(() => { apply(); return true; }, token, background);

    internal void Post(Func<bool> apply, CancellationToken token, bool background = false)
    {
        if (Volatile.Read(ref _disposed) != 0 || token.IsCancellationRequested) return;
        (background ? _background : _foreground).Enqueue(new Pending(apply, token));
        if (Interlocked.CompareExchange(ref _scheduled, 1, 0) == 0)
            _dispatcher.Post(() => Start(1));
    }

    private void Start(int interval)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (_timer.StartOnce(interval)) return;
        Interlocked.Exchange(ref _scheduled, 0);
        _foreground.Clear();
        _background.Clear();
        // Translators: Error shown if Windows cannot schedule the radio browser's background-result updates.
        _report(new InvalidOperationException(Tr("Could not schedule radio browser updates.")));
    }

    private void Drain()
    {
        var started = Stopwatch.GetTimestamp();
        var applied = 0;
        while (Volatile.Read(ref _disposed) == 0 && applied < 4
            && Stopwatch.GetElapsedTime(started).TotalMilliseconds < 4)
        {
            // A station result must not wait behind an expanded directory's remaining native nodes.
            var pending = !_foreground.IsEmpty ? _foreground : _background;
            if (!pending.TryPeek(out var update)) break;
            if (update.Token.IsCancellationRequested) { pending.TryDequeue(out _); continue; }
            try
            {
                if (!update.Apply()) break;
                pending.TryDequeue(out _);
            }
            catch (Exception error)
            {
                pending.TryDequeue(out _);
                _report(error);
            }
            applied++;
        }
        if (Volatile.Read(ref _disposed) != 0) return;
        // Several arrived batches change the same source. Publish its count and repaint only once.
        try { _render(); }
        catch (Exception error) { _report(error); }
        if (!_foreground.IsEmpty || !_background.IsEmpty) { Start(16); return; }
        Interlocked.Exchange(ref _scheduled, 0);
        // A producer may have enqueued between the empty check and releasing the scheduling flag.
        if ((!_foreground.IsEmpty || !_background.IsEmpty)
            && Interlocked.CompareExchange(ref _scheduled, 1, 0) == 0) Start(1);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _timer.Dispose();
        _foreground.Clear();
        _background.Clear();
    }
}
