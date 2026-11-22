namespace LunaPlayer.UI.Services;

/// <summary>Closes asynchronous dialog work once and suppresses callbacks queued before teardown.</summary>
internal sealed class Lifetime : IDisposable
{
    private readonly CancellationTokenSource _source = new();
    private readonly CancellationToken _token;
    private int _disposed;
    internal Lifetime() => _token = _source.Token;
    internal CancellationToken Token => _token;

    internal void Post(Action<Action> dispatch, Action action)
    {
        if (_token.IsCancellationRequested) return;
        try { dispatch(() => { if (!_token.IsCancellationRequested) action(); }); }
        catch (InvalidOperationException) when (_token.IsCancellationRequested)
        { /* Shutdown may end dispatch after the caller checked the token. */ }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try { _source.Cancel(); }
        finally { _source.Dispose(); }
    }
}
