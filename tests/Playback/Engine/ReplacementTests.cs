using System.Collections.Concurrent;
using System.Diagnostics;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback.Engine;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class ReplacementTests
{
    [TestMethod]
    public async Task ManualCrossfadeAcrossDifferentSampleRatesKeepsPlaying()
    {
        using var workspace = new Workspace();
        using var engine = NativeHost.Create();
        var failures = new ConcurrentQueue<AudioFilter>();
        engine.AudioFilterFailed += failures.Enqueue;
        engine.SetEqualizer(new("test", "Test", [new(1000, 1, 6)]));
        engine.ConfigurePlaybackEffects(new AudioSettings
        {
            Crossfade = true, CrossfadeManualNavigation = true, CrossfadeMilliseconds = 80,
        });
        engine.Load(NativeHost.Wave(workspace, "first.wav", 48000));
        await NativeHost.Until(() => !engine.IsLoading && engine.Elapsed is > 0.1);
        engine.Load(NativeHost.Wave(workspace, "next.wav", 8000), fadeTransition: true);
        await NativeHost.Until(() => !engine.IsLoading && engine.MediaTitle == "next.wav" && engine.Elapsed is > 0.1);
        Assert.IsFalse(engine.IsPaused);
        Assert.IsEmpty(failures);
    }

    [TestMethod]
    public async Task RapidNavigationReachesOnlyTheLatestRequestedDestination()
    {
        using var workspace = new Workspace();
        using var engine = NativeHost.Create();
        var failures = new ConcurrentQueue<AudioFilter>();
        engine.AudioFilterFailed += failures.Enqueue;
        engine.SetEqualizer(new("test", "Test", [new(1000, 1, 6)]));
        var first = NativeHost.Wave(workspace, "first.wav", 48000);
        var second = NativeHost.Wave(workspace, "second.wav", 8000);
        var last = NativeHost.Wave(workspace, "last.wav", 16000);
        engine.Load(first);
        await NativeHost.Until(() => !engine.IsLoading && engine.Elapsed is > 0.1);
        var clock = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
            Assert.IsTrue(engine.Load(i == 99 ? last : i % 2 == 0 ? first : second, fadeTransition: true));
        Assert.IsLessThan(TimeSpan.FromSeconds(1), clock.Elapsed, "Navigation waited for native fades or filter initialization.");
        await NativeHost.Until(() => !engine.IsLoading && engine.MediaTitle == "last.wav" && engine.Elapsed is > 0.1);
        Assert.IsFalse(engine.IsPaused);
        Assert.IsEmpty(failures);
    }

    [TestMethod]
    public async Task PauseAndSeekDuringTheOutgoingRampApplyToTheDestination()
    {
        using var workspace = new Workspace();
        using var engine = NativeHost.Create();
        engine.Load(NativeHost.Wave(workspace));
        await NativeHost.Until(() => !engine.IsLoading && engine.Elapsed is > 0.1);
        engine.Load(NativeHost.Wave(workspace, "next.wav", 8000), fadeTransition: true);
        engine.SeekAbsolute(3);
        engine.Pause();
        await NativeHost.Until(() => !engine.IsLoading && engine.MediaTitle == "next.wav" && engine.IsPaused);
        Assert.AreEqual(3d, engine.Elapsed!.Value, 0.1);
        engine.Play();
        await NativeHost.Until(() => engine.Elapsed is > 3.1 && !engine.IsPaused);
    }

    [TestMethod]
    public async Task StopCancelsAPendingReplacement()
    {
        using var workspace = new Workspace();
        using var engine = NativeHost.Create();
        engine.Load(NativeHost.Wave(workspace));
        await NativeHost.Until(() => !engine.IsLoading && engine.Elapsed is > 0.1);
        engine.Load(NativeHost.Wave(workspace, "next.wav"), fadeTransition: true);
        engine.Stop();
        await NativeHost.Until(() => engine.Duration is null);
        Assert.IsFalse(engine.IsLoading);
    }
}
