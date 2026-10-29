namespace LunaPlayer.Discord.Publishing;

internal sealed partial class Worker
{
    private async Task ConnectAsync()
    {
        if (_client is { IsConnected: false }) await DisconnectAsync(clear: false).ConfigureAwait(false);
        if (_client is not null || Elapsed < _retryAt) return;
        _client = _factory();
        if (!await _client.ConnectAsync(_applicationId, _stop.Token).ConfigureAwait(false))
        {
            await DisconnectAsync(clear: false).ConfigureAwait(false);
            _retryAt = Elapsed + _timing.Retry;
        }
        else
        {
            _attempted = null;
            _nextArtworkRefresh = TimeSpan.Zero;
        }
    }

    private async Task DisconnectAsync(bool clear)
    {
        var client = _client;
        if (client is null) return;
        _client = null;
        try
        {
            if (clear && client.IsConnected)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                try { await client.SetActivityAsync(null, timeout.Token).ConfigureAwait(false); }
                catch (Exception exception) when (exception is IOException or OperationCanceledException or TimeoutException)
                {
                    // Closing the pipe also withdraws this process's activity if clearing was not acknowledged.
                }
            }
        }
        finally { await client.DisposeAsync().ConfigureAwait(false); }
    }
}
