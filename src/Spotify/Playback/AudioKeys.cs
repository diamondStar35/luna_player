using System.Security.Cryptography;

namespace LunaPlayer.Spotify.Playback;

/// <summary>Coalesces keys and schedules ready foreground work before speculative work.</summary>
internal sealed class AudioKeys(
    Func<TrackId, byte[], CancellationToken, Task<byte[]>> request,
    CancellationToken lifetime, TimeSpan? minimumInterval = null, TimeSpan? retryDelay = null) : IDisposable
{
    private const int MaximumEntries = 64;
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(10);
    private readonly object _sync = new();
    private readonly Dictionary<(TrackId Track, string File), Entry> _entries = [];
    private readonly TimeSpan _minimumInterval = minimumInterval ?? TimeSpan.FromMilliseconds(250);
    private readonly TimeSpan _retryDelay = retryDelay ?? TimeSpan.FromSeconds(2);
    private TaskCompletionSource _changed = NewSignal();
    private DateTimeOffset _nextRequest;
    private bool _running;
    private bool _disposed;

    private sealed class Entry(byte[] file)
    {
        internal TaskCompletionSource Completion { get; } = NewSignal();
        internal byte[] File { get; } = file;
        internal byte[]? Bytes;
        internal DateTimeOffset SavedAt;
        internal DateTimeOffset Due;
        internal int Readers;
        internal List<(CancellationToken Token, bool Speculative)> Waiters { get; } = [];
        internal int Refusals;
        internal bool Started;
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Called under _sync when a selection arrives or leaves.
    private void Wake()
    {
        _changed.TrySetResult();
        _changed = NewSignal();
    }

    internal async Task<byte[]> GetAsync(TrackId track, byte[] file, CancellationToken cancellationToken,
        bool speculative = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = (track, Convert.ToHexString(file));
        Entry entry;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            foreach (var cached in _entries.ToArray())
                if (cached.Value.Readers == 0 && cached.Value.Bytes is not null
                    && DateTimeOffset.UtcNow - cached.Value.SavedAt >= Retention)
                    Remove(cached.Key, cached.Value);
            if (!_entries.TryGetValue(id, out entry!))
            {
                if (_entries.Count >= MaximumEntries)
                {
                    var oldest = _entries.Where(item => item.Value.Readers == 0 && item.Value.Bytes is not null)
                        .OrderBy(item => item.Value.SavedAt).FirstOrDefault();
                    if (oldest.Value is null) throw new IOException("Too many pending Spotify audio-key requests.");
                    Remove(oldest.Key, oldest.Value);
                }
                entry = new Entry(file.ToArray());
                _entries.Add(id, entry);
            }
            entry.Readers++;
            entry.Waiters.Add((cancellationToken, speculative));
            Wake();
            if (!_running) { _running = true; _ = Task.Run(RunAsync); }
        }
        using var navigation = cancellationToken.UnsafeRegister(static value =>
        {
            var keys = (AudioKeys)value!;
            lock (keys._sync) keys.Wake();
        }, this);
        try
        {
            await entry.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                cancellationToken.ThrowIfCancellationRequested();
                return entry.Bytes!.ToArray();
            }
        }
        finally
        {
            lock (_sync)
            {
                entry.Readers--;
                entry.Waiters.Remove((cancellationToken, speculative));
                if (entry.Readers == 0 && entry.Bytes is null && !entry.Started)
                {
                    if (_entries.TryGetValue(id, out var current) && ReferenceEquals(current, entry)) _entries.Remove(id);
                    entry.Completion.TrySetCanceled();
                }
                Wake();
            }
        }
    }

    private async Task RunAsync()
    {
        while (true)
        {
            (TrackId Track, string File) id = default;
            Entry? entry;
            Task changed;
            TimeSpan delay;
            lock (_sync)
            {
                if (_disposed || lifetime.IsCancellationRequested)
                {
                    foreach (var value in _entries.Values) value.Completion.TrySetCanceled();
                    _running = false;
                    return;
                }
                // Check tokens directly: a cancelled waiter's async finally may not have run yet.
                var pending = _entries.Where(item => item.Value.Bytes is null
                    && item.Value.Waiters.Any(reader => !reader.Token.IsCancellationRequested)).ToArray();
                if (pending.Length == 0) { _running = false; return; }
                var now = DateTimeOffset.UtcNow;
                var ready = pending.Where(item => item.Value.Due <= now)
                    .OrderByDescending(item => item.Value.Waiters.Any(reader =>
                        !reader.Speculative && !reader.Token.IsCancellationRequested)).FirstOrDefault();
                entry = ready.Value;
                changed = _changed.Task;
                delay = (entry is null ? pending.Min(item => item.Value.Due) : now) - now;
                if (_nextRequest - now > delay) delay = _nextRequest - now;
                if (delay <= TimeSpan.Zero && entry is not null)
                {
                    id = ready.Key;
                    entry.Started = true;
                    _nextRequest = now + _minimumInterval;
                }
                else entry = null;
            }
            if (entry is null)
            {
                // No transport gate is held. New selections wake and reorder the queue.
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
                try { await Task.WhenAny(changed, Task.Delay(delay, wait.Token)).ConfigureAwait(false); }
                finally { await wait.CancelAsync().ConfigureAwait(false); }
                continue;
            }
            try
            {
                // Once selected for sending, finish on the connection lifetime. Navigation
                // cancellation must not interrupt encrypted packet framing.
                var bytes = await request(id.Track, entry.File, lifetime).ConfigureAwait(false);
                lock (_sync)
                {
                    if (_disposed) CryptographicOperations.ZeroMemory(bytes);
                    else
                    {
                        entry.Bytes = bytes;
                        entry.SavedAt = DateTimeOffset.UtcNow;
                        entry.Completion.TrySetResult();
                    }
                }
            }
            catch (Exception error)
            {
                lock (_sync)
                {
                    if (!_disposed && !lifetime.IsCancellationRequested && entry.Readers > 0
                        && error is AudioKeyUnavailableException { Code: 2 } && entry.Refusals++ == 0)
                    {
                        entry.Due = DateTimeOffset.UtcNow + _retryDelay;
                        Diagnostics.Write("key.retry", detail: "delayMs=" + _retryDelay.TotalMilliseconds, error: error);
                    }
                    else
                    {
                        if (_entries.TryGetValue(id, out var current) && ReferenceEquals(current, entry)) _entries.Remove(id);
                        if (error is OperationCanceledException) entry.Completion.TrySetCanceled();
                        else
                        {
                            entry.Completion.TrySetException(error);
                            _ = entry.Completion.Task.Exception;
                        }
                    }
                }
            }
            finally
            {
                lock (_sync)
                {
                    entry.Started = false;
                    if (entry.Readers == 0 && entry.Bytes is null)
                    {
                        if (_entries.TryGetValue(id, out var current) && ReferenceEquals(current, entry)) _entries.Remove(id);
                        entry.Completion.TrySetCanceled();
                    }
                }
            }
        }
    }

    private void Remove((TrackId Track, string File) id, Entry entry)
    {
        _entries.Remove(id);
        if (entry.Bytes is { } bytes) CryptographicOperations.ZeroMemory(bytes);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var entry in _entries.Values)
            {
                if (entry.Bytes is { } bytes) CryptographicOperations.ZeroMemory(bytes);
                entry.Completion.TrySetCanceled();
            }
            _entries.Clear();
            Wake();
        }
    }
}
