using LunaPlayer.Application;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Subsonic.Servers;

namespace LunaPlayer.Subsonic.Listening;

/// <summary>Samples on the app clock; each account has one serialized network worker and its own echo checkpoint.</summary>
internal sealed class Synchronization : IDisposable
{
    private readonly Connections _connections;
    private readonly Player _player;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly TimeProvider _time;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<Guid, Account> _accounts = [];
    private string? _occurrence;
    private PlayTracker _tracker = new();
    private long _lastTick;
    private bool _disposed;
    internal event Action<Guid, Exception?>? Status;
    private sealed class Account(Profile profile, CancellationToken lifetime) : IDisposable
    {
        internal readonly Profile Profile = profile;
        internal readonly CancellationTokenSource Lifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        internal Sample? LastPlayback;
        internal readonly Queue<Sample> Stopped = [];
        public void Dispose() { Lifetime.Cancel(); Lifetime.Dispose(); }
        internal bool Busy, Initialized, Leader;
        internal string? LastLocal, LastRemote, LastStart;
        internal long Revision, WrittenRevision = -1;
        internal readonly Queue<(string Id, DateTimeOffset Time)> Submissions = [];
        internal DateTimeOffset LastSaved, RetryAfter;
        internal int Failures;
    }
    private sealed record Sample(Guid Server, string[] Ids, int? Index, long Position, Reference? Current,
        string? Occurrence, bool Playing, bool Loaded, double Speed, string LocalKey, long Revision);

    internal Synchronization(Connections connections, Player player, IApplicationDispatcher dispatcher, TimeProvider? time = null)
    {
        _connections = connections; _player = player; _dispatcher = dispatcher; _time = time ?? TimeProvider.System;
        _lastTick = _time.GetTimestamp(); _player.SamplingTick += Tick;
    }
    internal void ProfilesChanged()
    {
        foreach (var id in _accounts.Keys.Where(id => !_connections.Profiles.Any(x => x.Id == id && (x.ReportListening || x.SynchronizeQueue)
            && x == _accounts[id].Profile)).ToArray())
        { _accounts[id].Dispose(); _accounts.Remove(id); }
        _player.RequiresMediaTick = _connections.Profiles.Any(x => x.ReportListening || x.SynchronizeQueue);
        _player.RefreshState();
    }
    private void Tick()
    {
        if (_disposed) return;
        var now = _time.GetUtcNow(); var timestamp = _time.GetTimestamp();
        var delta = _time.GetElapsedTime(_lastTick, timestamp); _lastTick = timestamp;
        var current = Reference.Parse(_player.CurrentPath);
        var occurrence = current is null ? null : _player.CurrentItemId?.Split('/')[0];
        if (occurrence != _occurrence) { _occurrence = occurrence; _tracker = new(); delta = TimeSpan.Zero; }
        var submit = current is { Kind: Playback.MediaKind.Song } && _player.IsPlaying
            && _tracker.AddListeningTime(delta > TimeSpan.FromSeconds(3) ? TimeSpan.Zero : delta,
                _player.Duration is { } duration ? TimeSpan.FromSeconds(duration) : null);
        foreach (var profile in _connections.Profiles.Where(x => x.ReportListening || x.SynchronizeQueue))
        {
            if (!_accounts.TryGetValue(profile.Id, out var state)) _accounts[profile.Id] = state = new(profile, _lifetime.Token);
            var items = _player.Items.DistinctBy(x => x.SourceId).Where(x => x.ServerMedia is { Kind: Playback.MediaKind.Song } source && source.ServerId == profile.Id).ToArray();
            var index = Array.FindIndex(items, x => x.SourceId.ToString("N") == occurrence);
            var active = current?.ServerId == profile.Id && current.Kind == Playback.MediaKind.Song;
            var ids = items.Select(x => x.ServerMedia!.Id).ToArray();
            var key = string.Join("/", items.Select(x => x.Id)) + ":" + (active ? _player.PlaybackRevision + ":" + _player.IsPaused : "");
            if (state.LastLocal != key)
            {
                if (state.LastLocal is null && active && _player.IsLoaded) state.Leader = true;
                if (state.LastLocal is not null) { state.Leader = true; state.Revision++; }
                state.LastLocal = key;
            }
            if (state.LastPlayback is { Loaded: true } previous && (!active || !_player.IsLoaded || occurrence != previous.Occurrence))
            { state.Stopped.Enqueue(previous with { Loaded = false, Playing = false }); state.LastPlayback = null; }
            if (!profile.SynchronizeQueue && !active && state.Submissions.Count == 0 && state.Stopped.Count == 0) continue;
            if (submit && active && profile.ReportListening) state.Submissions.Enqueue((current!.Id, now));
            if (state.Busy || now < state.RetryAfter) continue;
            var sample = new Sample(profile.Id, ids, index < 0 ? null : index, active ? (long)((_player.SourceElapsed ?? 0) * 1000) : 0,
                active ? current : null, active ? occurrence : null, active && _player.IsPlaying, active && _player.IsLoaded,
                _player.Speed, key, state.Revision);
            if (active) state.LastPlayback = sample;
            var stopped = state.Stopped.ToArray(); state.Stopped.Clear();
            var start = profile.ReportListening && sample.Loaded && sample.Occurrence is not null && state.LastStart != sample.Occurrence;
            if (start) state.LastStart = sample.Occurrence;
            var submissions = state.Submissions.ToArray(); state.Submissions.Clear();
            var writeDue = state.Leader && (state.WrittenRevision != state.Revision
                || active && now - state.LastSaved >= TimeSpan.FromSeconds(5));
            state.Busy = true;
            Process(profile, sample, state, start, submissions, stopped, writeDue, state.Initialized);
        }
    }
    private async void Process(Profile profile, Sample sample, Account state, bool start, (string Id, DateTimeOffset Time)[] submissions, Sample[] stopped, bool writeDue, bool initialized)
    {
        var token = state.Lifetime.Token;
        try
        {
            var result = await Task.Run(async () =>
            {
                var client = _connections.Client(profile.Id);
                var capabilities = await client.DiscoverAsync(token: token).ConfigureAwait(false);
                if (profile.ReportListening && capabilities.Extension("playbackReport") == Support.Supported)
                    foreach (var previous in stopped)
                        await client.Listening.ReportPlaybackAsync(previous.Current!.Id, previous.Position, PlaybackState.Stopped,
                            previous.Speed, ignoreScrobble: true, token: token).ConfigureAwait(false);
                foreach (var submission in submissions)
                    await client.Listening.ScrobbleAsync(submission.Id, true, submission.Time, token).ConfigureAwait(false);
                if (profile.ReportListening && sample.Current is { } current)
                {
                    if (start) await client.Listening.ScrobbleAsync(current.Id, false, token: token).ConfigureAwait(false);
                    if (capabilities.Extension("playbackReport") == Support.Supported)
                        await client.Listening.ReportPlaybackAsync(current.Id, sample.Position,
                            !sample.Loaded ? PlaybackState.Stopped : sample.Playing ? PlaybackState.Playing : PlaybackState.Paused,
                            sample.Speed, ignoreScrobble: true, token: token).ConfigureAwait(false);
                }
                if (!profile.SynchronizeQueue) return (Queue: (PlayQueue?)null, Written: false);
                var byIndex = capabilities.Extension("indexBasedQueue") == Support.Supported;
                var remote = await client.Listening.GetQueueAsync(byIndex, token).ConfigureAwait(false);
                var write = writeDue || !initialized && remote.Entries.Length == 0 && sample.Ids.Length > 0;
                if (write)
                {
                    var index = sample.Index ?? (remote.CurrentIndex is >= 0 and var previous && previous < sample.Ids.Length ? previous : 0);
                    await client.Listening.SaveQueueAsync(sample.Ids, sample.Ids.Length > 0 ? sample.Ids[index] : null,
                        sample.Current is not null ? sample.Position : (long)remote.Position,
                        byIndex && sample.Ids.Length > 0 ? index : null, byIndex, token).ConfigureAwait(false);
                    remote = await client.Listening.GetQueueAsync(byIndex, token).ConfigureAwait(false);
                }
                return (Queue: (PlayQueue?)remote, Written: write);
            }, token).ConfigureAwait(false);
            _dispatcher.Post(() =>
            {
                if (_disposed) return;
                state.Busy = false; state.Failures = 0;
                if (!_accounts.TryGetValue(profile.Id, out var account) || !ReferenceEquals(account, state)) return;
                Status?.Invoke(profile.Id, null);
                if (result.Queue is not { } remote || state.Revision != sample.Revision) return;
                var fingerprint = Fingerprint(remote);
                if (result.Written) { state.LastSaved = _time.GetUtcNow(); state.LastRemote = fingerprint; state.WrittenRevision = sample.Revision; }
                else if (fingerprint != state.LastRemote)
                {
                    state.LastRemote = fingerprint;
                    if (remote.Entries.Length > 0 || state.Initialized)
                    {
                        _player.ApplyServerQueue(profile.Id, remote);
                        var current = Reference.Parse(_player.CurrentPath);
                        var items = _player.Items.DistinctBy(x => x.SourceId).Where(x => x.ServerMedia is { Kind: Playback.MediaKind.Song } source && source.ServerId == profile.Id);
                        state.LastLocal = string.Join("/", items.Select(x => x.Id)) + ":" + (current?.ServerId == profile.Id ? _player.PlaybackRevision + ":" + _player.IsPaused : "");
                        state.Leader = false;
                    }
                }
                state.Initialized = true;
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error)
        {
            _dispatcher.Post(() =>
            {
                if (_disposed) return;
                if (!_accounts.TryGetValue(profile.Id, out var account) || !ReferenceEquals(account, state)) return;
                if (start) state.LastStart = null;
                state.Busy = false; state.Failures++; state.RetryAfter = _time.GetUtcNow() + TimeSpan.FromSeconds(Math.Min(30, 1 << Math.Min(5, state.Failures)));
                Status?.Invoke(profile.Id, error);
            });
        }
    }
    internal static string Fingerprint(PlayQueue queue) => string.Join("/", queue.Entries.Select(x => Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(x.Id))))
        + ":" + queue.CurrentIndex + ":" + queue.Current + ":" + Math.Round(queue.Position);
    public void Dispose()
    {
        _disposed = true; _player.SamplingTick -= Tick; _lifetime.Cancel();
        foreach (var account in _accounts.Values) account.Dispose();
        _accounts.Clear(); _lifetime.Dispose();
    }
}
