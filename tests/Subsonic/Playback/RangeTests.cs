using System.Collections.Concurrent;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class RangeTests
{
    [TestMethod]
    public async Task LoopMarkersUseTheReturnedStreamsTimeline()
    {
        using var rig = new Rig(); await rig.Open(12);
        Assert.IsTrue(rig.Player.SetLoopStart(14));
        Assert.IsTrue(rig.Player.SetLoopEnd(18));
        Assert.AreEqual(2d, rig.Engine.LoopStart);
        Assert.AreEqual(6d, rig.Engine.LoopEnd);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SelectionBoundariesAndCompletionUseDifferentTimeCoordinates(bool repeat)
    {
        using var rig = new Rig(); await rig.Open(12);
        double? ended = null; rig.Player.SelectionEnded += value => ended = value;
        Assert.IsTrue(rig.Player.SetSelectionRange(14, 18, repeat));
        Assert.AreEqual(new PlaybackRange(2, 6), rig.Engine.Selection);
        Assert.AreEqual(repeat, rig.Engine.SelectionRepeats);
        rig.Engine.CompleteSelection(6);
        Assert.AreEqual(18d, ended);
        Assert.IsTrue(rig.Player.ExitSelectionRange(18, false));
        await rig.Resolve(18);
        Assert.AreEqual(18d, rig.Player.SourceElapsed);
        Assert.IsNull(rig.Engine.Selection);
        Assert.IsTrue(rig.Player.IsPaused);
    }

    [TestMethod]
    public async Task SeekingRetainsLoopBoundsOnTheNewStream()
    {
        using var rig = new Rig(); await rig.Open(12);
        rig.Player.SetLoopStart(14); rig.Player.SetLoopEnd(18);
        rig.Player.SeekAbsolute(16);
        await rig.Resolve(14);
        Assert.AreEqual(2d, rig.Engine.Position);
        Assert.AreEqual(0d, rig.Engine.LoopStart);
        Assert.AreEqual(4d, rig.Engine.LoopEnd);
        Assert.AreEqual(16d, rig.Player.SourceElapsed);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SelectingAnEarlierRangeReopensAtItsStartAndKeepsItsEnd(bool repeat)
    {
        using var rig = new Rig(); await rig.Open(12);
        Assert.IsTrue(rig.Player.PlaySelectionRange(5, 8, repeat));
        await rig.Resolve(5);
        Assert.AreEqual(new PlaybackRange(0, 3), rig.Engine.Selection);
        Assert.AreEqual(repeat, rig.Engine.SelectionRepeats);
        Assert.AreEqual(5d, rig.Player.SourceElapsed);
        Assert.IsFalse(rig.Player.IsPaused);
        rig.Player.Pause(); rig.Player.SeekAbsolute(7);
        await rig.Resolve(5);
        Assert.AreEqual(2d, rig.Engine.Position);
        Assert.AreEqual(new PlaybackRange(0, 3), rig.Engine.Selection);
        Assert.IsTrue(rig.Player.IsPaused);
    }

    [TestMethod]
    public async Task ClearingControlsWhileAReplacementIsResolvingDoesNotRestoreThem()
    {
        using var rig = new Rig(); await rig.Open(12);
        rig.Player.SetLoopStart(14); rig.Player.SetLoopEnd(18);
        rig.Player.SeekAbsolute(16); rig.Player.ClearLoop();
        await rig.Resolve(14);
        Assert.IsNull(rig.Engine.LoopStart); Assert.IsNull(rig.Engine.LoopEnd);
        Assert.IsTrue(rig.Player.PlaySelectionRange(5, 8, false));
        rig.Player.ClearSelectionRange();
        await rig.Resolve(5);
        Assert.IsNull(rig.Engine.Selection);
        rig.Player.OpenFile("local");
        Assert.IsNull(rig.Engine.LoopStart); Assert.IsNull(rig.Engine.Selection);
    }

    [TestMethod]
    public async Task InvalidRangeDoesNotReplaceAnActiveSelection()
    {
        using var rig = new Rig(); await rig.Open(12);
        rig.Player.SetSelectionRange(14, 18, false);
        Assert.IsFalse(rig.Player.SetSelectionRange(20, 19, true));
        Assert.IsFalse(rig.Player.SetSelectionRange(double.NaN, 19, true));
        Assert.AreEqual(new PlaybackRange(2, 6), rig.Engine.Selection);
    }

    private sealed class Rig : IDisposable
    {
        internal readonly FakeEngine Engine = new();
        internal readonly Player Player;
        private readonly Pump _pump = new();
        private readonly Resolver _resolver = new();
        internal Rig() { Player = new(Engine); Player.SetMediaResolver(_resolver, _pump.Post); }
        internal async Task Open(double offset)
        {
            Player.InsertMedia([(new Reference(Guid.NewGuid(), "song").Address, MediaTags.None)], true, offset);
            await Resolve(offset);
        }
        internal async Task Resolve(double expectedOffset)
        {
            await Pump.Wait(() => !_resolver.Calls.IsEmpty);
            Assert.IsTrue(_resolver.Calls.TryDequeue(out var call));
            Assert.AreEqual(expectedOffset, call.Position);
            call.Result.SetResult(new MediaInput("https://invalid/stream", MediaTags.None, 100, Offset: expectedOffset, SeekByReopening: true));
            await _pump.Until(() => Player.IsLoaded);
        }
        public void Dispose() => Player.Dispose();
    }
    private sealed class Resolver : IMediaResolver
    {
        internal readonly record struct Call(double? Position, TaskCompletionSource<MediaInput> Result);
        internal ConcurrentQueue<Call> Calls { get; } = new();
        public bool Handles(string address) => Reference.IsAddress(address);
        public Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
        {
            var result = new TaskCompletionSource<MediaInput>(TaskCreationOptions.RunContinuationsAsynchronously);
            Calls.Enqueue(new(position, result)); return result.Task;
        }
    }
}
