using System.Threading.Channels;

namespace LunaPlayer.Playback;

/// <summary>Serializes native work without holding the playback or UI locks.</summary>
internal sealed class WorkQueue : IDisposable
{
    private readonly Lock _gate = new();
    private readonly Channel<Work> _queue = Channel.CreateUnbounded<Work>(new() { SingleReader = true });
    private readonly Dictionary<AudioFilter, Work> _pending = [];
    private readonly Dictionary<AudioFilter, long> _versions = [];
    private long _version;
    private bool _disposed;

    internal WorkQueue() => Completion = Task.Run(ProcessAsync);
    internal Task Completion { get; }
    internal event Action<AudioFilter>? Failed;

    internal bool Post(Func<bool> apply, AudioFilter? filter = null, bool replay = false)
    {
        lock (_gate)
        {
            if (_disposed) return false;
            var work = new Work(apply, filter, ++_version);
            if (filter is { } key)
            {
                // A lifecycle replay carries the same desired state. It cannot suppress a
                // failure of that state as a newer user request would.
                if (replay && _versions.TryGetValue(key, out var version))
                    work = work with { Version = version };
                var queued = _pending.ContainsKey(key);
                _pending[key] = work;
                _versions[key] = work.Version;
                // An in-flight operation finishes; only the newest pending value follows it.
                if (queued) return true;
                work = new Work(null, key, work.Version);
            }
            return _queue.Writer.TryWrite(work);
        }
    }

    private async Task ProcessAsync()
    {
        await foreach (var queued in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            Work work;
            lock (_gate)
            {
                if (_disposed) continue;
                work = queued;
                if (queued.Filter is { } key)
                {
                    if (!_pending.Remove(key, out work)) continue;
                }
            }
            bool success;
            try { success = work.Apply!(); }
            catch (Exception exception)
            {
                // Keep subsequent requests usable after a failed native operation.
                Console.Error.WriteLine(exception);
                success = false;
            }
            if (success || work.Filter is not { } filter) continue;
            lock (_gate)
            {
                if (_disposed || _versions[filter] != work.Version) continue;
            }
            try { Failed?.Invoke(filter); }
            catch (Exception exception) { Console.Error.WriteLine(exception); }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _pending.Clear();
            _queue.Writer.TryComplete();
        }
    }

    private readonly record struct Work(Func<bool>? Apply, AudioFilter? Filter, long Version);
}
