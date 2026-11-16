namespace LunaPlayer.Media.Discs;

/// <summary>Owns one disc read and its cancellation independently of the window that requested it.</summary>
internal sealed class Inspection : IDisposable
{
    private readonly Lock _sync = new();
    private readonly CancellationTokenSource _cancellation = new();
    private bool _disposed;
    internal Request Request { get; }
    internal Task<DiscoveryResult> Result { get; }

    internal Inspection(Request request, Task? previous = null,
        Func<Request, CancellationToken, DiscInfo>? read = null)
    {
        Request = request;
        var token = _cancellation.Token;
        var reader = read ?? Catalog.Read;
        Result = Task.Run(async () =>
        {
            if (previous is not null)
            {
                try { await previous.ConfigureAwait(false); }
                catch (OperationCanceledException)
                {
                    // A replaced read must finish before the next one accesses the optical drive.
                }
            }
            token.ThrowIfCancellationRequested();
            try
            {
                var disc = reader(request, token);
                token.ThrowIfCancellationRequested();
                return new DiscoveryResult(disc, null);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                return new DiscoveryResult(null,
                    exception as DiscException ?? new DiscException(Failure.Unreadable, exception.Message));
            }
        }, token);
    }

    internal void Cancel()
    {
        lock (_sync)
            if (!_disposed) _cancellation.Cancel();
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _cancellation.Cancel();
        }
        // Native I/O can outlive cancellation; retain the token source until that read has exited.
        _ = Result.ContinueWith(static (_, state) => ((CancellationTokenSource)state!).Dispose(),
            _cancellation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
