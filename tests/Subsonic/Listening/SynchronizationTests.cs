using System.Globalization;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Listening;
using LunaPlayer.Subsonic.Servers;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Subsonic.Listening;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class SynchronizationTests
{
    [TestMethod]
    public async Task DisabledReportingAndQueueSyncMakeNoBackgroundRequests()
    {
        using var rig = new Rig(); await rig.Create(false, false);
        rig.Time.Advance(); rig.Player.SampleMedia(); rig.Dispatcher.Drain();
        Assert.IsFalse(rig.Player.RequiresMediaTick); Assert.AreEqual(0, rig.Handler.Requests.Count);
    }
    [TestMethod]
    public async Task RemoteQueuePreservesLocalPlaybackAndIsNotWrittenBack()
    {
        using var rig = new Rig(); await rig.Create(false, true);
        rig.RemoteIds = ["one", "one"]; rig.RemoteIndex = 1; rig.RemotePosition = 2000;
        rig.Player.OpenFile("local"); await rig.Tick();
        Assert.AreEqual("local", rig.Player.CurrentPath); Assert.AreEqual(3, rig.Player.Count);
        Assert.AreEqual(1, rig.Engine.Loads.Count);
        await rig.Tick(); Assert.AreEqual(0, rig.RequestCount("savePlayQueueByIndex"));
    }
    [TestMethod]
    public async Task LocalQueueEditsAreSentImmediatelyAndOwnEchoDoesNotSeek()
    {
        using var rig = new Rig(); await rig.Create(false, true);
        await rig.Play("one"); await rig.Tick();
        Assert.AreEqual(1, rig.RequestCount("savePlayQueueByIndex"));
        rig.Player.InsertMedia([(new Reference(rig.Profile.Id, "two").Address, LunaPlayer.Media.MediaTags.None)], false);
        await rig.Tick(); Assert.AreEqual(2, rig.RequestCount("savePlayQueueByIndex"));
        await rig.Tick(); Assert.AreEqual(0, rig.Engine.RequestedSeeks.Count);
        CollectionAssert.AreEqual(new[] { "one", "two" }, rig.RemoteIds);
    }
    [TestMethod]
    public async Task RemotePositionIsAdoptedOnlyDuringPlaybackFromThatServer()
    {
        using var rig = new Rig(); await rig.Create(false, true);
        await rig.Play("one"); await rig.Tick();
        rig.RemotePosition = 3000; await rig.Tick();
        Assert.AreEqual(3d, rig.Player.Elapsed);
        rig.Player.OpenFile("local"); await rig.Tick();
        rig.RemoteIds = ["two"]; rig.RemotePosition = 7000; await rig.Tick();
        Assert.AreEqual("local", rig.Player.CurrentPath); Assert.AreEqual(0d, rig.Engine.Position);
    }
    [TestMethod]
    public async Task ListeningThresholdSurvivesAnInFlightQueueRequestAndSeeksDoNotCount()
    {
        using var rig = new Rig(); await rig.Create(true, true); await rig.Play("one");
        rig.Hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Time.Advance(); rig.Player.SampleMedia();
        await Pump.Wait(() => rig.RequestCount("getPlayQueueByIndex") == 1);
        for (var i = 0; i < 5; i++) { rig.Time.Advance(); rig.Player.SampleMedia(); }
        rig.Hold.SetResult(); await rig.Dispatcher.Until(() => rig.Completions > 0);
        await rig.Tick();
        Assert.AreEqual(1, rig.Handler.Requests.Count(x => x.Endpoint == "scrobble" && x.Parameters["submission"][0] == "true"));
        rig.Player.SeekAbsolute(9); await rig.Tick();
        Assert.AreEqual(1, rig.Handler.Requests.Count(x => x.Endpoint == "scrobble" && x.Parameters["submission"][0] == "true"));
    }
    private sealed class Clock : TimeProvider
    {
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _ticks;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(_ticks);
        internal void Advance() => _ticks += TimeSpan.TicksPerSecond;
    }
    private sealed class Rig : IDisposable
    {
        private readonly TemporaryDirectory _directory = new();
        internal Handler Handler { get; }
        private readonly HttpClient _http;
        internal Connections Connections { get; }
        internal Profile Profile { get; private set; } = null!;
        internal readonly FakeEngine Engine = new();
        internal readonly Player Player;
        internal readonly Pump Dispatcher = new();
        internal readonly Clock Time = new();
        private readonly Synchronization _sync;
        internal string[] RemoteIds = [];
        internal int RemoteIndex;
        internal long RemotePosition;
        internal int Completions;
        internal TaskCompletionSource? Hold;
        internal Rig()
        {
            Handler = new Handler(async (sent, token) =>
            {
                if (sent.Endpoint == "getPlayQueueByIndex")
                {
                    if (Hold is { } held) await held.Task.WaitAsync(token);
                    var entries = string.Join(",", RemoteIds.Select(id => "{\"id\":\"" + id + "\",\"title\":\"" + id + "\",\"duration\":10}"));
                    return Fixture.Json("\"playQueueByIndex\":{\"currentIndex\":" + RemoteIndex + ",\"position\":" + RemotePosition + ",\"entry\":[" + entries + "]}");
                }
                if (sent.Endpoint == "savePlayQueueByIndex")
                {
                    var values = sent.Parameters; RemoteIds = values.GetValueOrDefault("id") ?? [];
                    RemoteIndex = values.TryGetValue("currentIndex", out var current) ? int.Parse(current[0], CultureInfo.InvariantCulture) : 0;
                    RemotePosition = values.TryGetValue("position", out var position) ? long.Parse(position[0], CultureInfo.InvariantCulture) : 0;
                }
                return await Fixture.Reply(sent, token);
            });
            _http = new HttpClient(Handler);
            Connections = new(new Store(Path.Combine(_directory.Path, "servers.json")), profile => new Client(profile.ConnectionOptions(), _http));
            Player = new(Engine); Player.SetMediaResolver(Connections, Dispatcher.Post);
            _sync = new(Connections, Player, Dispatcher, Time);
            _sync.Status += (_, error) => { Assert.IsNull(error); Completions++; };
        }
        internal async Task Create(bool report, bool queue)
        {
            Profile = await Connections.SaveAsync(new Profile { Name = "Test", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials,
                ReportListening = report, SynchronizeQueue = queue });
            _sync.ProfilesChanged();
        }
        internal async Task Play(string id)
        {
            Player.InsertMedia([(new Reference(Profile.Id, id).Address, LunaPlayer.Media.MediaTags.None)], true);
            await Dispatcher.Until(() => Player.IsLoaded);
        }
        internal int RequestCount(string endpoint) { lock (Handler.Requests) return Handler.Requests.Count(x => x.Endpoint == endpoint); }
        internal async Task Tick()
        {
            var count = Completions; Time.Advance(); Player.SampleMedia();
            await Dispatcher.Until(() => Completions > count);
        }
        public void Dispose() { _sync.Dispose(); Player.Dispose(); Connections.Dispose(); _http.Dispose(); Handler.Dispose(); _directory.Dispose(); }
    }
}
