namespace LunaPlayer.Media.Discs;

internal sealed record DiscoveryResult(DiscInfo? Disc, DiscException? Error);

/// <summary>Serializes optical reads and discards results for an old selection or closed picker.</summary>
internal sealed class Discovery(Action<Action> post,
    Func<string, DiscFormat, CancellationToken, DiscInfo>? read = null) : IDisposable
{
    private readonly Func<string, DiscFormat, CancellationToken, DiscInfo> _read = read ?? Catalog.Read;
    private Inspection? _current;
    private Task _pending = Task.CompletedTask;
    private long _generation;
    private volatile bool _disposed;
    internal event Action<DiscoveryResult>? Completed;

    internal void Request(string path, DiscFormat format)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Cancel();
        var generation = Interlocked.Increment(ref _generation);
        var inspection = new Inspection(new Request(format, path), _pending,
            (request, token) => _read(request.Location, request.Format!.Value, token));
        _current = inspection;
        _pending = inspection.Result;
        _ = Notify(inspection, generation);
    }

    /// <summary>Transfers the accepted read, including a result not yet delivered to the picker.</summary>
    internal Inspection Take(string path, DiscFormat format)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_current is null || _current.Request.Format != format
            || !LunaPlayer.Configuration.Paths.AreSame(_current.Request.Location, path))
            Request(path, format);
        var accepted = _current!;
        _current = null;
        Interlocked.Increment(ref _generation);
        return accepted;
    }

    private async Task Notify(Inspection inspection, long generation)
    {
        try
        {
            var result = await inspection.Result.ConfigureAwait(false);
            if (_disposed || generation != Volatile.Read(ref _generation)) return;
            post(() => { if (!_disposed && generation == _generation) Completed?.Invoke(result); });
        }
        catch (OperationCanceledException)
        {
            // Selection changes and cancelled pickers do not publish results.
        }
        catch (InvalidOperationException) when (_disposed)
        {
            // The application can finish shutting down just before a worker posts its final result.
        }
    }
    internal void Cancel()
    {
        Interlocked.Increment(ref _generation);
        _current?.Dispose();
        _current = null;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cancel();
    }
}
