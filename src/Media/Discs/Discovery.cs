namespace LunaPlayer.Media.Discs;

internal sealed record DiscoveryResult(DiscInfo? Disc, DiscException? Error);

/// <summary>Serializes optical reads and discards results for an old selection or closed picker.</summary>
internal sealed class Discovery(Action<Action> post,
    Func<string, DiscFormat, CancellationToken, DiscInfo>? read = null) : IDisposable
{
    private readonly Func<string, DiscFormat, CancellationToken, DiscInfo> _read = read ?? Catalog.Read;
    private CancellationTokenSource? _cancellation;
    private Task _pending = Task.CompletedTask;
    private long _generation;
    private bool _disposed;
    internal event Action<DiscoveryResult>? Completed;

    internal void Request(string path, DiscFormat format)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Cancel();
        _cancellation = new();
        var token = _cancellation.Token;
        var generation = ++_generation;
        var previous = _pending;
        _pending = Task.Run(async () =>
        {
            await previous.ConfigureAwait(false);
            if (token.IsCancellationRequested) return;
            DiscoveryResult result;
            try { result = new(_read(path, format, token), null); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                result = new(null, exception as DiscException ?? new DiscException(Failure.Unreadable, exception.Message));
            }
            if (token.IsCancellationRequested) return;
            post(() => { if (!_disposed && generation == _generation) Completed?.Invoke(result); });
        });
    }
    internal void Cancel()
    {
        ++_generation;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cancel();
    }
}
