using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;
using MpvNet;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, DoNotParallelize, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class NativeRangeTests
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task LoadingAQueuePlaysFromTheSavedPositionEvenAfterTheCurrentSongEnds(bool offset, bool atEnd)
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create(); using var player = new Player(engine);
        var pump = new Pump();
        player.SetMediaResolver(new Resolver(NativeHost.Wave(workspace), offset, 10), pump.Post);
        var server = Guid.NewGuid();
        if (atEnd)
        {
            player.SetEndBehavior(EndBehavior.None);
            player.InsertMedia([(new Reference(server, "song").Address, MediaTags.None)], true);
            await NativeHost.Until(() => { pump.Drain(); return player.IsLoaded && !engine.IsLoading; });
            engine.SeekAbsolute(10);
            await NativeHost.Until(() => engine.EndReached && engine.IsPaused);
        }
        player.ApplyServerQueue(server, new LunaPlayer.Subsonic.Models.PlayQueue
        { Entries = [new() { Id = "song" }], Current = "song", Position = 3000 }, play: true);
        Assert.AreEqual(3d, player.SourceElapsed);
        Assert.IsFalse(player.IsPaused);
        await NativeHost.Until(() =>
        {
            pump.Drain();
            return player.IsPlaying && !engine.IsLoading && engine.Elapsed is { } elapsed
                && Math.Abs(elapsed - (offset ? 0 : 3)) < 0.5;
        });
        Assert.AreEqual(3d, player.SourceElapsed!.Value, 0.5);
        await NativeHost.Until(() => player.IsPlaying && player.SourceElapsed > 3.1);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task OffsetSelectionStopsAtItsSongBoundaryAndLeavesNoFalseTrackEnd(bool crossfade)
    {
        using var rig = new Rig(crossfade);
        await rig.Open();
        double? selectionEnd = null; var fileEnds = 0;
        rig.Player.SelectionEnded += end => selectionEnd = end;
        rig.Player.Ended += _ => fileEnds++;
        Assert.IsTrue(rig.Player.PlaySelectionRange(10.1, 10.4, false));
        await rig.Until(() => selectionEnd.HasValue);
        Assert.AreEqual(10.4, selectionEnd!.Value, 0.001);
        Assert.AreEqual(0, fileEnds);
        Assert.IsTrue(rig.Player.SelectionRangeCompleted);
        Assert.AreEqual(10.4, rig.Player.SourceElapsed!.Value, 0.1);
        Assert.IsTrue(rig.Player.ExitSelectionRange(10.4, false));
        await rig.Until(() => !rig.Engine.SelectionRangeCompleted && rig.Engine.IsPaused);
        Assert.AreEqual(10.4, rig.Player.SourceElapsed!.Value, 0.1);
        rig.Player.Play(); await rig.Until(() => rig.Player.SourceElapsed > 10.5);
        Assert.AreEqual(0, fileEnds);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task LoopSurvivesAReopenedOffsetStreamAndClearsNormally(bool selection)
    {
        using var rig = new Rig(false); await rig.Open();
        if (selection)
        {
            Assert.IsTrue(rig.Player.PlaySelectionRange(5, 5.4, true));
            await rig.Until(() => rig.Player.IsLoaded && !rig.Engine.IsLoading);
            rig.Player.Pause();
        }
        else
        {
            Assert.IsTrue(rig.Player.SetLoopStart(10.1));
            Assert.IsTrue(rig.Player.SetLoopEnd(10.5));
        }
        var start = selection ? 5 : 10.1;
        rig.Player.SeekAbsolute(start + 0.1);
        await rig.Until(() => rig.Player.IsLoaded && !rig.Engine.IsLoading);
        Assert.AreEqual(0d, Convert.ToDouble(rig.Mpv.GetProperty("ab-loop-a")), 0.001);
        Assert.AreEqual(0.4d, Convert.ToDouble(rig.Mpv.GetProperty("ab-loop-b")), 0.001);
        rig.Player.Play();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        await rig.Until(() => elapsed.ElapsedMilliseconds > 650);
        Assert.IsTrue(rig.Player.SourceElapsed >= start && rig.Player.SourceElapsed < start + 0.5);
        if (selection) rig.Player.ClearSelectionRange(); else rig.Player.ClearLoop();
        await rig.Until(() => rig.Player.SourceElapsed > start + 0.6);
    }

    [TestMethod]
    public async Task ClearingAnOpeningSelectionOverridesInitialLoadOptions()
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create();
        engine.Load(NativeHost.Wave(workspace), paused: true,
            boundaries: new(Selection: new(0, 0.2), RepeatSelection: true));
        engine.ClearSelectionRange();
        await NativeHost.Until(() => !engine.IsLoading);
        engine.Play(); await NativeHost.Until(() => engine.Elapsed > 0.4);
        Assert.IsFalse(engine.SelectionRangeCompleted);
    }

    private sealed class Rig : IDisposable
    {
        private readonly Workspace _workspace = new();
        private readonly Pump _pump = new();
        internal readonly Engine Engine = NativeHost.Create();
        internal readonly Player Player;
        internal MPV Mpv { get; }
        internal Rig(bool crossfade)
        {
            Player = new(Engine);
            Engine.ConfigurePlaybackEffects(new AudioSettings { Crossfade = crossfade });
            var file = NativeHost.Wave(_workspace);
            Player.SetMediaResolver(new Resolver(file), _pump.Post);
            var coordinator = NativeFields.Read(typeof(Engine), Engine, "_coordinator")!;
            var session = NativeFields.Read(typeof(PlaybackCoordinator), coordinator, "_session")!;
            Mpv = (MPV)NativeFields.Read(typeof(MpvSession), session, "_mpv")!;
        }
        internal async Task Open()
        {
            Player.InsertMedia([(new Reference(Guid.NewGuid(), "song").Address, MediaTags.None)], true, 10);
            await Until(() => Player.IsLoaded && !Engine.IsLoading); Player.Pause();
        }
        internal Task Until(Func<bool> condition) => NativeHost.Until(() => { _pump.Drain(); return condition(); });
        public void Dispose() { Player.Dispose(); _workspace.Dispose(); }
    }
    private sealed class Resolver(string file, bool offset = true, double duration = 100) : IMediaResolver
    {
        public bool Handles(string address) => Reference.IsAddress(address);
        public Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
            => Task.FromResult(new MediaInput(file, MediaTags.None, duration, Offset: offset ? position ?? 0 : 0, SeekByReopening: offset));
    }
}
