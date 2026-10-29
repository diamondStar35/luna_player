using System.Diagnostics;

namespace LunaPlayer.Discord.Publishing;

/// <summary>Serializes IPC on one worker. Its source uses immutable state, without Player, settings
/// or wxWidgets access. Wakes coalesce; clearing and shutdown bypass normal send scheduling.</summary>
internal sealed partial class Worker : IAsyncDisposable
{
    private readonly Func<string?, Publication> _source;
    private readonly Func<IRpcClient> _factory;
    private readonly string _applicationId;
    private readonly Timing _timing;
    private readonly TimeProvider _time;
    private readonly long _started;
    private readonly SendBudget _budget;
    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _wake = new(0, 1);
    private readonly object _gate = new();
    private readonly Task _task;
    private Task? _shutdown;
    private IRpcClient? _client;
    private TimeSpan _retryAt;
    private Publication? _attempted;
    private bool _accepted;
    private TimeSpan _rejectedUntil;

    internal Worker(Func<string?, Publication> source, Func<IRpcClient>? factory = null, Timing? timing = null,
        string? applicationId = null, IApplicationArtwork? artwork = null, SendBudget? budget = null, TimeProvider? time = null)
    {
        _source = source;
        _factory = factory ?? (() => new RpcClient());
        _applicationId = applicationId ?? PublisherApplication.ApplicationId;
        _timing = timing ?? Timing.Default;
        _artwork = artwork ?? new ApplicationArtwork();
        _budget = budget ?? new SendBudget();
        _time = time ?? TimeProvider.System;
        _started = _time.GetTimestamp();
        _task = Task.Run(RunAsync);
    }

    private TimeSpan Elapsed => _time.GetElapsedTime(_started);

    internal void Wake()
    {
        lock (_gate)
        {
            if (_shutdown is not null) return;
            if (_wake.CurrentCount == 0) _wake.Release();
        }
    }

    private async Task RunAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                await ReadArtworkAsync().ConfigureAwait(false);
                var frame = _source(_icon);
                if (!frame.Enabled)
                {
                    _artworkCancellation?.Cancel();
                    await DisconnectAsync(clear: true).ConfigureAwait(false);
                    _retryAt = TimeSpan.Zero;
                }
                else
                {
                    try
                    {
                        await ConnectAsync().ConfigureAwait(false);
                        // A handshake can overlap a pause or a privacy change: render the latest snapshot.
                        frame = _source(_icon);
                        if (!frame.Enabled) continue;
                        if (_client is { IsConnected: true })
                        {
                            StartArtwork();
                            await PublishAsync(frame).ConfigureAwait(false);
                        }
                    }
                    catch (Exception exception) when (exception is IOException or TimeoutException
                        or OperationCanceledException or UnauthorizedAccessException)
                    {
                        await DisconnectAsync(clear: false).ConfigureAwait(false);
                        _retryAt = Elapsed + _timing.Retry;
                    }
                }
                await _wake.WaitAsync(frame.Enabled ? _timing.Tick : Timeout.InfiniteTimeSpan, _stop.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        finally
        {
            _artworkCancellation?.Cancel();
            await DisconnectAsync(clear: true).ConfigureAwait(false);
            if (_artworkTask is not null) await ReadArtworkAsync(wait: true).ConfigureAwait(false);
            _artworkCancellation?.Dispose();
        }
    }

    private async Task PublishAsync(Publication frame)
    {
        if (_client is null) return;
        if (_attempted is not null && frame.Activity == _attempted.Activity
            && (_accepted || Elapsed < _rejectedUntil)) return;
        var urgent = _attempted is null || !Equals(frame.State, _attempted.State);
        if (frame.Activity is not null && _budget.Delay(Elapsed, urgent) > TimeSpan.Zero) return;
        _budget.Record(Elapsed);
        _accepted = await _client.SetActivityAsync(frame.Activity, _stop.Token).ConfigureAwait(false);
        _attempted = frame;
        if (!_accepted)
        {
            _rejectedUntil = Elapsed + _timing.Retry;
            Trace.TraceWarning("Discord rejected Luna Player's activity; the latest state will be retried.");
        }
    }

    internal Task ShutdownAsync()
    {
        lock (_gate)
        {
            if (_shutdown is not null) return _shutdown;
            _stop.Cancel();
            return _shutdown = FinishAsync();
        }
    }

    private async Task FinishAsync()
    {
        await _task.ConfigureAwait(false);
        _stop.Dispose();
        _wake.Dispose();
    }

    public ValueTask DisposeAsync() => new(ShutdownAsync());
}
