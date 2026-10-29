using System.Diagnostics;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Discord;

internal sealed record PresenceTiming(TimeSpan Tick, TimeSpan MinimumSend, TimeSpan Heartbeat, TimeSpan Retry)
{
    internal static PresenceTiming Default { get; } = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(10));
}

/// <summary>Owns IPC on a worker, coalesces updates, and retries when Discord starts or restarts.
/// Clearing and disabling bypass the normal activity rate limit. The application identity is fixed
/// for the process; user settings cannot substitute another Discord application.</summary>
internal sealed class PresenceService : IAsyncDisposable
{
    private sealed record Snapshot(bool Enabled, string? Activity);
    private Snapshot _latest = new(false, null);
    private readonly string _applicationId;
    private readonly Func<IRpcClient> _factory;
    private readonly PresenceTiming _timing;
    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _wake = new(0, 1);
    private readonly Task _worker;
    private readonly object _gate = new();
    private Task? _shutdown;

    internal PresenceService(Func<IRpcClient>? factory = null, PresenceTiming? timing = null, string? applicationId = null)
    {
        // An internal constructor seam lets IPC tests use a synthetic identity. Production always
        // uses the repository-owned constant, never a value loaded from user settings.
        _applicationId = applicationId ?? PublisherApplication.ApplicationId;
        _factory = factory ?? (() => new RpcClient());
        _timing = timing ?? PresenceTiming.Default;
        _worker = Task.Run(RunAsync);
    }

    internal void Update(DiscordSettings settings, string? activity)
    {
        var snapshot = new Snapshot(settings.Enabled && PublisherApplication.IsValidApplicationId(_applicationId), activity);
        lock (_gate)
        {
            if (_shutdown is not null || snapshot == _latest) return;
            Volatile.Write(ref _latest, snapshot);
            if (_wake.CurrentCount == 0) _wake.Release();
        }
    }

    private async Task RunAsync()
    {
        IRpcClient? client = null;
        string? sent = null;
        var clock = Stopwatch.StartNew();
        var lastSend = TimeSpan.MinValue;
        var retryAt = TimeSpan.Zero;
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var snapshot = Volatile.Read(ref _latest);
                if (client is not null && !snapshot.Enabled)
                {
                    await ClearAsync(client).ConfigureAwait(false);
                    await client.DisposeAsync().ConfigureAwait(false);
                    client = null;
                    retryAt = TimeSpan.Zero;
                }
                if (snapshot.Enabled)
                {
                    try
                    {
                        if (client is not null && !client.IsConnected)
                        {
                            await client.DisposeAsync().ConfigureAwait(false);
                            client = null;
                        }
                        if (client is null && clock.Elapsed >= retryAt)
                        {
                            client = _factory();
                            if (!await client.ConnectAsync(_applicationId, _stop.Token).ConfigureAwait(false))
                            {
                                await client.DisposeAsync().ConfigureAwait(false);
                                client = null;
                                retryAt = clock.Elapsed + _timing.Retry;
                            }
                            else { sent = null; lastSend = TimeSpan.MinValue; }
                        }
                        // A preference or track can change during a handshake. Never publish that stale snapshot.
                        if (snapshot != Volatile.Read(ref _latest)) continue;
                        var since = lastSend == TimeSpan.MinValue ? TimeSpan.MaxValue : clock.Elapsed - lastSend;
                        if (client is not null && client.IsConnected
                            && (lastSend == TimeSpan.MinValue || snapshot.Activity != sent || since >= _timing.Heartbeat)
                            && (snapshot.Activity is null || since >= _timing.MinimumSend))
                        {
                            var accepted = await client.SetActivityAsync(snapshot.Activity, _stop.Token).ConfigureAwait(false);
                            if (!accepted) Trace.TraceWarning("Discord rejected Luna Player's activity. Check the application ID and Rich Presence assets.");
                            // Do not retry a rejected payload every tick. A new update or heartbeat can retry it.
                            sent = snapshot.Activity;
                            lastSend = clock.Elapsed;
                        }
                    }
                    catch (Exception e) when (e is IOException or TimeoutException or OperationCanceledException
                        or UnauthorizedAccessException)
                    {
                        if (client is not null) await client.DisposeAsync().ConfigureAwait(false);
                        client = null;
                        retryAt = clock.Elapsed + _timing.Retry;
                    }
                }
                if (!snapshot.Enabled) retryAt = TimeSpan.Zero;
                await _wake.WaitAsync(snapshot.Enabled ? _timing.Tick : Timeout.InfiniteTimeSpan, _stop.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        finally
        {
            if (client is not null)
            {
                await ClearAsync(client).ConfigureAwait(false);
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task ClearAsync(IRpcClient client)
    {
        if (!client.IsConnected) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        try { await client.SetActivityAsync(null, timeout.Token).ConfigureAwait(false); }
        catch (Exception e) when (e is IOException or OperationCanceledException or TimeoutException) { }
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
        await _worker.ConfigureAwait(false);
        _stop.Dispose();
        _wake.Dispose();
    }

    public ValueTask DisposeAsync() => new(ShutdownAsync());
}
