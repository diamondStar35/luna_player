using LunaPlayer.Configuration.Models;
using LunaPlayer.Tests.Infrastructure;

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
        using var workspace = new Workspace();
        using var engine = NativeHost.Create();
        engine.ConfigurePlaybackEffects(new AudioSettings
        {
            FadeSeeking = fadeSeek, FadePauseResume = fadeResume,
            FadeIntervalMilliseconds = 80, SeekFadeIntervalMilliseconds = 80,
        });
        var source = NativeHost.Wave(workspace);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            engine.Load(source, paused: true);
            await NativeHost.Until(() => !engine.IsLoading && engine.Duration is > 0);
            if (relative) engine.SeekRelative(2);
            else engine.SeekAbsolute(2);
            engine.Play();
            await NativeHost.Until(() => !engine.IsPaused && engine.Elapsed is > 0.1);
            Assert.IsGreaterThanOrEqualTo(1.9, engine.Elapsed!.Value,
                $"Resume replaced the requested seek on attempt {attempt + 1}: position {engine.Elapsed}.");
        }
    }
}
