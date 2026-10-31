using System.Collections.Concurrent;
using LunaPlayer.Equalizer;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback.Engine;

[TestClass, DoNotParallelize]
public sealed class EqualizerTests
{
    private static readonly Preset Preset = new("test", "Test", [new(1000, 1, 6)]);

    [TestMethod, TestCategory("Unit")]
    [DataRow(null)]
    [DataRow(0d)]
    [DataRow(-1d)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    public void UnknownOrInvalidSampleRateDoesNotProduceFilters(double? rate)
        => Assert.AreEqual(string.Empty, AudioFilters.EqualizerGraph(Bands.Slots(Preset), rate));

    [TestMethod, TestCategory("Unit")]
    public void EightKilohertzAudioNeverIncludesBandsAtOrAboveNyquist()
    {
        var graph = AudioFilters.EqualizerGraph(Bands.Slots(Preset), 8000);
        StringAssert.Contains(graph, "f=2500:");
        foreach (var frequency in new[] { 4000, 6000, 6300, 10000, 16000 })
            Assert.DoesNotContain($"f={frequency}:", graph);
    }

    [TestMethod, TestCategory("Integration")]
    public async Task RestoredPresetWaitsForTheFirstDecodedSampleRate()
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        using var session = new MpvSession(0, new Lock(), new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
        var audio = new AudioProcessor(session);
        var failures = new ConcurrentQueue<AudioFilter>();
        session.FilterFailed += failures.Enqueue;
        using var rate = session.ObserveProperty("audio-params/samplerate", audio.HandleSampleRateChange);
        Assert.IsTrue(audio.SetEqualizer(Preset));
        Assert.IsNull(await ReadGraph(session));
        session.TrySetProperty("pause", true);
        session.TryDo(mpv => mpv.Command("loadfile", NativeHost.Wave(workspace, sampleRate: 8000)));
        await NativeHost.Until(() => Graph(session)?.Contains("f=1000:", StringComparison.Ordinal) == true);
        Assert.DoesNotContain("f=4000:", Graph(session)!);
        Assert.DoesNotContain("f=16000:", Graph(session)!);
        Assert.IsEmpty(failures);
    }

    [TestMethod, TestCategory("Integration")]
    public async Task ColdStartAtEightKilohertzRendersTheSignalWithoutAnOscillatingTail()
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        var source = NativeHost.Wave(workspace, sampleRate: 8000);
        // A short tone followed by silence exposes an unstable high-frequency band:
        // its output continues ringing after the actual signal has ended.
        using (var stream = new FileStream(source, FileMode.Open, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            stream.Position = 44;
            for (var sample = 0; sample < 800; sample++)
                writer.Write((short)(Math.Sin(2 * Math.PI * 1000 * sample / 8000) * 2000));
        }
        var output = Path.Combine(workspace.Root, "output.f32");
        using (var engine = new LunaPlayer.Playback.Engine(0, new Dictionary<string, object?>
        {
            ["ao"] = "pcm", ["vo"] = "null", ["ao-pcm-file"] = output,
            ["ao-pcm-waveheader"] = false, ["audio-format"] = "float", ["audio-channels"] = "mono",
        }))
        {
            var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            engine.Ended += reason => { if (reason == EndReason.EndOfFile) ended.TrySetResult(); };
            engine.SetEqualizer(new("test", "Test", [new(4000, 1, 6), new(6300, 1, 6), new(16000, 1, 6)]));
            engine.Load(source);
            await ended.Task.WaitAsync(TimeSpan.FromSeconds(8));
        }
        var bytes = File.ReadAllBytes(output);
        Assert.IsGreaterThan(8000 * sizeof(float), bytes.Length);
        double energy = 0;
        for (var offset = 0; offset < bytes.Length; offset += sizeof(float))
        {
            var sample = BitConverter.ToSingle(bytes, offset);
            Assert.IsTrue(float.IsFinite(sample), "The equalizer produced an invalid sample.");
            if (offset < 1600 * sizeof(float)) energy += sample * sample;
            if (offset >= 4000 * sizeof(float))
                Assert.IsLessThan(0.0001f, Math.Abs(sample), "The equalizer rang into the silent tail.");
        }
        Assert.IsGreaterThan(0.01, energy, "The opening tone was lost while the equalizer initialized.");
    }

    [TestMethod, TestCategory("Integration")]
    public async Task RateChangesRemoveUnsafeBandsAndRestoreThemForHigherRateAudio()
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        using var session = new MpvSession(0, new Lock(), new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
        var audio = new AudioProcessor(session);
        using var rate = session.ObserveProperty("audio-params/samplerate", audio.HandleSampleRateChange);
        audio.SetEqualizer(Preset);
        session.TrySetProperty("pause", true);
        foreach (var sampleRate in new[] { 48000, 8000, 48000 })
        {
            session.TryDo(mpv =>
            {
                audio.ClearEqualizerForLoad();
                mpv.Command("loadfile", NativeHost.Wave(workspace, $"{sampleRate}.wav", sampleRate));
            });
            await NativeHost.Until(() => session.ReadDouble("audio-params/samplerate") == sampleRate);
            session.TryDo(_ => Assert.IsTrue(audio.CompleteEqualizerLoad()));
            await NativeHost.Until(() => Graph(session) == AudioFilters.EqualizerGraph(Bands.Slots(Preset), sampleRate));
        }
    }

    private static string? Graph(MpvSession session)
        => Graph(session.ReadObject("af"));

    private static string? Graph(object? value)
        => (value as IEnumerable<object?> ?? [])
            .OfType<IDictionary<string, object?>>()
            .Where(filter => filter.TryGetValue("label", out var label) && label as string == AudioFilters.EqualizerLabel)
            .Select(filter => filter.TryGetValue("params", out var parameters)
                && parameters is IDictionary<string, object?> values && values.TryGetValue("graph", out var graph) ? graph as string : null)
            .SingleOrDefault();

    private static async Task<string?> ReadGraph(MpvSession session)
    {
        var completed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.TryDo(_ => completed.SetResult(Graph(session.ReadLiveObject("af"))));
        return await completed.Task.WaitAsync(TimeSpan.FromSeconds(8));
    }
}
