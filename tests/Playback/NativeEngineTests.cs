using System.Collections.Concurrent;
using LunaPlayer.Playback;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Tests.Infrastructure;
namespace LunaPlayer.Tests.Playback;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class NativeEngineTests
{
    [TestMethod]
    public async Task LoadSeekPauseAndDiscoverAudio()
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create();
        Assert.IsTrue(engine.Load(NativeHost.Wave(workspace), paused: true));
        await NativeHost.Until(() => !engine.IsLoading && engine.Duration is > 0);
        Assert.AreEqual(10d, engine.Duration!.Value, 0.1);
        Assert.IsFalse(engine.HasVideo);
        Assert.IsNotEmpty(engine.GetAudioTracks());
        Assert.AreEqual(65d, engine.SetVolume(65));
        engine.SetSpeed(1.25); Assert.AreEqual(1.25, engine.Speed, 0.01);
        engine.SeekAbsolute(2); await NativeHost.Until(() => engine.Elapsed is > 1.8);
        engine.Play(); await NativeHost.Until(() => !engine.IsPaused);
        engine.Pause(); await NativeHost.Until(() => engine.IsPaused);
        engine.Stop();
    }
    [TestMethod]
    public async Task OneShotSelectionNeverReportsRealEndOfFile()
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create();
        var ends = new ConcurrentQueue<EndReason>();
        var selections = new ConcurrentQueue<double>();
        engine.Ended += ends.Enqueue; engine.SelectionEnded += selections.Enqueue;
        engine.Load(NativeHost.Wave(workspace), paused: true);
        await NativeHost.Until(() => !engine.IsLoading && engine.Duration is > 0);
        Assert.IsTrue(engine.PlaySelectionRange(0.1, 0.35, false));
        await NativeHost.Until(() => !selections.IsEmpty);
        Assert.IsFalse(ends.Contains(EndReason.EndOfFile));
        Assert.IsTrue(engine.ClearSelectionRange());
    }
    [TestMethod]
    public async Task MissingMediaReportsAnError()
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create();
        var ends = new ConcurrentQueue<EndReason>(); engine.Ended += ends.Enqueue;
        engine.Load(Path.Combine(workspace.Root, "missing.mp3"));
        await NativeHost.Until(() => ends.Contains(EndReason.Error));
    }
    [TestMethod]
    public async Task ChapterStopIsDistinctFromEof()
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create();
        var ends = new ConcurrentQueue<EndReason>(); engine.Ended += ends.Enqueue;
        engine.Load(NativeHost.Wave(workspace), chapterStop: 0.3);
        await NativeHost.Until(() => ends.Contains(EndReason.ChapterStop));
        Assert.IsFalse(ends.Contains(EndReason.EndOfFile));
    }
    [TestMethod]
    public async Task GaplessSuccessorRetainsItsIdentity()
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create();
        var first = NativeHost.Wave(workspace); var second = NativeHost.Wave(workspace, "next.wav");
        engine.ConfigurePlaybackEffects(new AudioSettings { GaplessPlayback = GaplessPlaybackMode.AlwaysWhenPossible });
        engine.Load(first, paused: true, itemId: "first");
        await NativeHost.Until(() => !engine.IsLoading && engine.Duration is > 0);
        Assert.IsTrue(engine.QueueNext(second, itemId: "second"));
        engine.SeekAbsolute(9.8); engine.Play();
        QueuedAdvance? advance = null;
        await NativeHost.Until(() => (advance = engine.TakeQueuedAdvance()) is not null);
        Assert.AreEqual("second", advance!.Value.ItemId);
    }
    [TestMethod]
    public void DisposeIsIdempotent()
    {
        var engine = NativeHost.Create(); engine.Dispose(); engine.Dispose();
    }
}
