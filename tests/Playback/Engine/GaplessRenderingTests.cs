using System.Collections.Concurrent;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback.Engine;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class GaplessRenderingTests
{
    [TestMethod]
    [DataRow(1, false, false)]
    [DataRow(2, false, false)]
    [DataRow(1, false, true)]
    [DataRow(2, false, true)]
    [DataRow(1, true, false)]
    [DataRow(2, true, false)]
    public async Task QueuedFilesPreserveSamplesAndSmoothBoundaries(int mode, bool normalize, bool equalize)
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        const int boundary = 48000 * 3 + 1031;
        var samples = AudioSignal.Tone(boundary * 2);
        var first = AudioSignal.Wave(workspace, "first.wav", samples.AsSpan(0, boundary));
        var second = AudioSignal.Wave(workspace, "second.wav", samples.AsSpan(boundary));
        var whole = AudioSignal.Wave(workspace, "whole.wav", samples);
        float[] reference;
        if (normalize)
        {
            var firstTrack = await Render(workspace, "first.f32", first, null, (GaplessPlaybackMode)mode, true, false);
            var secondTrack = await Render(workspace, "second.f32", second, null, (GaplessPlaybackMode)mode, true, false);
            reference = [.. firstTrack, .. secondTrack];
        }
        else
            reference = await Render(workspace, "whole.f32", whole, null, (GaplessPlaybackMode)mode, false, equalize);
        var joined = await Render(workspace, "joined.f32", first, second, (GaplessPlaybackMode)mode, normalize, equalize);
        Assert.AreEqual(samples.Length, reference.Length, "The reference recording lost samples.");
        Assert.AreEqual(reference.Length, joined.Length, "The file boundary dropped or inserted samples.");
        for (var sample = boundary - 512; sample < boundary + 512; sample++)
        {
            // A fresh normalizer continues the preceding output gain through
            // its first frame. Compare analysis after that interpolation settles.
            var position = normalize && sample >= boundary ? sample + 24000 : sample;
            // Integer EQ processing can round a block's final sample differently.
            Assert.AreEqual(reference[position], joined[position], 2f / 32768,
                $"Gapless processing changed sample {position - boundary} relative to the boundary.");
        }
        var referenceStep = Enumerable.Range(boundary + 480, 512)
            .Max(sample => Math.Abs(reference[sample] - reference[sample - 1]));
        var boundaryStep = Enumerable.Range(boundary - 3, 7)
            .Max(sample => Math.Abs(joined[sample] - joined[sample - 1]));
        Assert.IsLessThanOrEqualTo(referenceStep * 1.5f, boundaryStep,
            "The track boundary introduced an abrupt sample discontinuity.");
    }

    private static async Task<float[]> Render(Workspace workspace, string outputName, string first,
        string? next, GaplessPlaybackMode mode, bool normalize, bool equalize)
    {
        var output = Path.Combine(workspace.Root, outputName);
        using (var engine = new LunaPlayer.Playback.Engine(0, new Dictionary<string, object?>
        {
            ["ao"] = "pcm", ["vo"] = "null", ["ao-pcm-file"] = output,
            ["ao-pcm-waveheader"] = false, ["ao-pcm-append"] = true,
            ["audio-format"] = "float", ["audio-channels"] = "mono", ["audio-samplerate"] = 48000,
            // PCM export is untimed. Pace the source so file events describe
            // the active track instead of an already-finished export.
            ["af"] = "@testclock:lavfi=[arealtime=speed=4]",
        }))
        {
            var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var reasons = new ConcurrentQueue<EndReason>();
            engine.Ended += reason =>
            {
                reasons.Enqueue(reason);
                if (reason == EndReason.EndOfFile) ended.TrySetResult();
            };
            engine.ConfigurePlaybackEffects(new AudioSettings { GaplessPlayback = mode });
            engine.SetNormalization(normalize);
            if (equalize) engine.SetEqualizer(new("gapless", "Gapless", [new(1000, 1, 10)]));
            Assert.IsTrue(engine.Load(first, paused: true, itemId: "first"));
            await NativeHost.Until(() => !engine.IsLoading && engine.Duration is > 0);
            if (next is not null) Assert.IsTrue(engine.QueueNext(next, itemId: "second"));
            engine.Play();
            try { await ended.Task.WaitAsync(TimeSpan.FromSeconds(8)); }
            catch (TimeoutException)
            {
                Assert.Fail($"Output did not finish: bytes={new FileInfo(output).Length}, paused={engine.IsPaused}, "
                    + $"loading={engine.IsLoading}, eof={engine.EndReached}, elapsed={engine.Elapsed}, "
                    + $"events={string.Join(',', reasons)}, advance={engine.TakeQueuedAdvance()?.ItemId}.");
            }
        }
        return AudioSignal.ReadFloats(output);
    }
}
