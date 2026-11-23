using LunaPlayer.Application;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Subsonic.Servers;

namespace LunaPlayer.Subsonic.Listening;

/// <summary>Samples on the app clock; each account has one serialized network worker and its own echo checkpoint.</summary>
internal sealed partial class Synchronization : IDisposable
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
        internal string? LastLocal, LastRemote, LastStart, LastReport;
        internal long Revision, WrittenRevision = -1;
        internal readonly Queue<(string Id, DateTimeOffset Time)> Submissions = [];
        internal DateTimeOffset LastSaved, NextQueueRead, NextPlaybackReport, RetryAfter;
        internal int Failures;
    }
    private sealed record Sample(Guid Server, string[] Ids, int? Index, long Position, Reference? Current,
        string? Occurrence, bool Playing, bool Loaded, double Speed, string LocalKey, long Revision, DateTimeOffset CapturedAt)
    {
        internal string ReportKey => LocalKey + ":" + Occurrence + ":" + Loaded + ":" + Playing + ":" + Speed;
    }

    internal Synchronization(Connections connections, Player player, IApplicationDispatcher dispatcher, TimeProvider? time = null)
    {
        _connections = connections; _player = player; _dispatcher = dispatcher; _time = time ?? TimeProvider.System;
        _lastTick = _time.GetTimestamp(); _player.SamplingTick += Tick;
        _player.CurrentChanged += QueueChanged; _player.StateChanged += QueueChanged;
    }
    internal void ProfilesChanged()
    {
        QueueChanged();
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
            var queue = profile.SynchronizeQueue ? QueueFor(profile.Id) : QueueSnapshot.Empty;
            var index = queue.Indexes.GetValueOrDefault(occurrence ?? "", -1);
            var active = current?.ServerId == profile.Id && current.Kind == Playback.MediaKind.Song;
            var ids = queue.Ids;
            var key = queue.Key + ":" + (active ? _player.PlaybackRevision + ":" + _player.IsPaused : "");
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
                _player.Speed, key, state.Revision, now);
            if (active) state.LastPlayback = sample;
            var stopped = state.Stopped.ToArray(); state.Stopped.Clear();
            var start = profile.ReportListening && sample.Loaded && sample.Occurrence is not null && state.LastStart != sample.Occurrence;
            if (start) state.LastStart = sample.Occurrence;
            var submissions = state.Submissions.ToArray(); state.Submissions.Clear();
            var writeDue = state.Leader && (state.WrittenRevision != state.Revision
                || active && now - state.LastSaved >= TimeSpan.FromSeconds(profile.Timing.QueueSaveSeconds));
            var queueDue = profile.SynchronizeQueue && (writeDue || !state.Initialized || now >= state.NextQueueRead);
            var reportDue = profile.ReportListening && (profile.Timing.PlaybackReportSeconds == 1
                || start || sample.ReportKey != state.LastReport || now >= state.NextPlaybackReport);
            state.Busy = true;
            Process(profile, sample, state, start, submissions, stopped, writeDue, queueDue, reportDue, state.Initialized);
        }
    }
    private async void Process(Profile profile, Sample sample, Account state, bool start, (string Id, DateTimeOffset Time)[] submissions, Sample[] stopped, bool writeDue, bool queueDue, bool reportDue, bool initialized)
    {
        var token = state.Lifetime.Token;
        var localChange = state.WrittenRevision != sample.Revision;
        var expectedRemote = state.LastRemote;
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
                    if (reportDue && capabilities.Extension("playbackReport") == Support.Supported)
                        await client.Listening.ReportPlaybackAsync(current.Id, sample.Position,
                            !sample.Loaded ? PlaybackState.Stopped : sample.Playing ? PlaybackState.Playing : PlaybackState.Paused,
                            sample.Speed, ignoreScrobble: true, token: token).ConfigureAwait(false);
                }
                if (!queueDue) return (Queue: (PlayQueue?)null, Written: false);
                var byIndex = capabilities.Extension("indexBasedQueue") == Support.Supported;
                // A periodic position save must first notice another client's changes. A local
                // edit already owns the desired queue and cursor, so only its readback is needed.
                var remote = writeDue && localChange && sample.Index.HasValue ? null
                    : await client.Listening.GetQueueAsync(byIndex, token).ConfigureAwait(false);
                var write = writeDue && (localChange || remote is null || expectedRemote is null || Fingerprint(remote) == expectedRemote)
                    || !initialized && remote?.Entries.Length == 0 && sample.Ids.Length > 0;
                if (write)
                {
                    var index = sample.Index ?? (remote?.CurrentIndex is >= 0 and var previous && previous < sample.Ids.Length ? previous : 0);
                    await client.Listening.SaveQueueAsync(sample.Ids, sample.Ids.Length > 0 ? sample.Ids[index] : null,
                        sample.Current is not null ? sample.Position : (long)(remote?.Position ?? 0),
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
                if (reportDue)
                {
                    state.LastReport = sample.ReportKey;
                    // Schedule from the sample, not response completion: network latency must
                    // not lengthen every configured period. Account workers remain serialized.
                    state.NextPlaybackReport = sample.CapturedAt + TimeSpan.FromSeconds(profile.Timing.PlaybackReportSeconds);
                }
                Status?.Invoke(profile.Id, null);
                if (result.Queue is not { } remote || state.Revision != sample.Revision) return;
                state.NextQueueRead = sample.CapturedAt + TimeSpan.FromSeconds(profile.Timing.QueuePollSeconds);
                var fingerprint = Fingerprint(remote);
                if (result.Written) { state.LastSaved = sample.CapturedAt; state.LastRemote = fingerprint; state.WrittenRevision = sample.Revision; }
                else if (fingerprint != state.LastRemote)
                {
                    state.LastRemote = fingerprint;
                    if (remote.Entries.Length > 0 || state.Initialized)
                    {
                        _connections.Metadata.Observe(profile.Id, remote.Entries);
                        _player.ApplyServerQueue(profile.Id, remote);
                        var current = Reference.Parse(_player.CurrentPath);
                        state.LastLocal = QueueFor(profile.Id).Key + ":" + (current?.ServerId == profile.Id ? _player.PlaybackRevision + ":" + _player.IsPaused : "");
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
                state.Busy = false; state.Failures++; state.RetryAfter = _time.GetUtcNow()
                    + TimeSpan.FromSeconds(Math.Min(profile.Timing.SynchronizationRetrySeconds, 1 << Math.Min(30, state.Failures)));
                Status?.Invoke(profile.Id, error);
            });
        }
    }
    internal static string Fingerprint(PlayQueue queue) => string.Join("/", queue.Entries.Select(x => Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(x.Id))))
        + ":" + queue.CurrentIndex + ":" + queue.Current + ":" + Math.Round(queue.Position);
    public void Dispose()
    {
        _disposed = true; _player.SamplingTick -= Tick; _lifetime.Cancel();
        _player.CurrentChanged -= QueueChanged; _player.StateChanged -= QueueChanged; _queues.Clear();
        foreach (var account in _accounts.Values) account.Dispose();
        _accounts.Clear(); _lifetime.Dispose();
    }
}
