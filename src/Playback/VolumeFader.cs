namespace LunaPlayer.Playback;

/// <summary>Runs one cancellable volume ramp. Starting another ramp supersedes the old one, which keeps
/// rapid seeks and repeated transport commands from leaving playback at an intermediate volume.</summary>
internal sealed class VolumeFader : IDisposable
{
    private const int FrameMilliseconds = 16;
    private readonly Lock _sync = new();
    private readonly Action<double> _apply;
    private CancellationTokenSource? _cancellation;
    private double _level = 1;
    private bool _disposed;

    internal VolumeFader(Action<double> apply)
    {
        _apply = apply;
    }

    internal double Level
    {
        get { lock (_sync) return _level; }
    }

    internal void Set(double level)
    {
        CancellationTokenSource? cancellation;
        lock (_sync)
        {
            if (_disposed)
                return;
            cancellation = _cancellation;
            _cancellation = null;
            _level = Math.Clamp(level, 0, 1);
            level = _level;
        }
        cancellation?.Cancel();
        cancellation?.Dispose();
        _apply(level);
    }

    internal void FadeTo(double target, int milliseconds, Action? completed = null)
    {
        target = Math.Clamp(target, 0, 1);
        if (milliseconds <= 0)
        {
            Set(target);
            completed?.Invoke();
            return;
        }

        CancellationTokenSource cancellation;
        double start;
        lock (_sync)
        {
            if (_disposed)
                return;
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            start = _level;
        }
        // Keep a constant rate when a fade is reversed part-way through. The configured interval is the
        // time needed to travel the full volume range, not a new full delay for every partial ramp.
        var duration = Math.Max(1, (int)Math.Round(milliseconds * Math.Abs(target - start)));
        _ = Run(start, target, duration, cancellation, completed);
    }

    private async Task Run(double start, double target, int milliseconds,
        CancellationTokenSource cancellation, Action? completed)
    {
        var steps = Math.Max(1, (int)Math.Ceiling(milliseconds / (double)FrameMilliseconds));
        try
        {
            for (var step = 1; step <= steps; step++)
            {
                await Task.Delay(milliseconds / steps, cancellation.Token).ConfigureAwait(false);
                var level = start + (target - start) * step / steps;
                lock (_sync)
                {
                    if (_disposed || !ReferenceEquals(_cancellation, cancellation))
                        return;
                    _level = level;
                }
                _apply(level);
            }
            lock (_sync)
            {
                if (_disposed || !ReferenceEquals(_cancellation, cancellation))
                    return;
                _cancellation = null;
            }
            completed?.Invoke();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    public void Dispose()
    {
        CancellationTokenSource? cancellation;
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            cancellation = _cancellation;
            _cancellation = null;
        }
        cancellation?.Cancel();
        cancellation?.Dispose();
    }
}
