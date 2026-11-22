using LunaPlayer.Configuration.Models;
using LunaPlayer.Tests.Infrastructure;
using NativeEngine = LunaPlayer.Playback.Engine;

namespace LunaPlayer.Tests.Playback.Engine;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class TransportTests
{
    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, true, false)]
    [DataRow(true, true, false)]
    [DataRow(false, true, true)]
    public async Task ImmediateResumePreservesThePausedSeekTarget(bool fadeSeek, bool fadeResume, bool relative)
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create();
        Configure(engine, fadeSeek, fadeResume);
        var source = NativeHost.Wave(workspace);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            engine.Load(source, paused: true);
            await NativeHost.Until(() => !engine.IsLoading && engine.Duration is > 0);
            using var completion = new NativeHost.SeekCompletion(engine);
            if (relative) engine.SeekRelative(2); else engine.SeekAbsolute(2);
            engine.Play();
            await AssertCompletedTarget(engine, completion);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ResumeQueuedBeforeLoadPreparationCompletesRetainsTheSeek(bool fadeSeek)
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create();
        Configure(engine, fadeSeek, fadeResume: true);
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var pending = 0;
        engine.MetadataChanged += () =>
        {
            if (engine.IsLoading || Interlocked.Exchange(ref pending, 0) == 0) return;
            // Hold the worker before final load preparation, so Seek and Play are already queued
            // when it reads the latest pause intent. No timing-based sleep controls the race.
            NativeHost.Queue(engine).Post(() => { entered.Set(); return release.Wait(TimeSpan.FromSeconds(5)); });
        };
        var source = NativeHost.Wave(workspace);
        for (var attempt = 0; attempt < 25; attempt++)
        {
            entered.Reset(); release.Reset(); pending = 1;
            NativeHost.SeekCompletion? completion = null;
            try
            {
                engine.Load(source, paused: true);
                await NativeHost.Until(() => !engine.IsLoading && entered.IsSet);
                completion = new NativeHost.SeekCompletion(engine);
                engine.SeekAbsolute(2); engine.Play();
                release.Set();
                await AssertCompletedTarget(engine, completion);
            }
            finally { release.Set(); completion?.Dispose(); }
        }
    }

    private static async Task AssertCompletedTarget(NativeEngine engine, NativeHost.SeekCompletion completion)
    {
        // Play updates logical pause intent immediately, and a queued native seek can still report
        // seeking=false before it starts. Wait for its seek/restart events before judging position.
        await NativeHost.Flush(engine);
        await completion.Wait();
        var mpv = NativeHost.Handle(engine);
        await NativeHost.Until(() => mpv.GetProperty("audio-fade/transport-active") is false
            && mpv.GetProperty("seeking") is false && mpv.GetProperty("pause") is false);
        var position = Convert.ToDouble(mpv.GetProperty("time-pos"));
        Assert.IsGreaterThanOrEqualTo(1.9, position, "The completed seek lost its requested destination.");
        Assert.IsLessThan(2.6, position, "Playback consumed too much of the destination before resuming.");
        await NativeHost.Until(() => engine.Elapsed is >= 1.9 && !engine.IsPaused);
    }

    private static void Configure(NativeEngine engine, bool fadeSeek, bool fadeResume)
        => engine.ConfigurePlaybackEffects(new AudioSettings
        {
            FadeSeeking = fadeSeek, FadePauseResume = fadeResume,
            FadeIntervalMilliseconds = 80, SeekFadeIntervalMilliseconds = 80,
        });
}
