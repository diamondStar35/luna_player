using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback.Engine;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class NormalizationTests
{
    [TestMethod]
    [DataRow(48000)]
    [DataRow(8000)]
    public async Task NextAndPreviousKeepNormalizingTheRenderedSignal(int nextRate)
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        var first = Tone(workspace, "first.wav", 48000);
        var next = Tone(workspace, "next.wav", nextRate);
        var output = Path.Combine(workspace.Root, "output.f32");
        using var engine = new LunaPlayer.Playback.Engine(0, new Dictionary<string, object?>
        {
            ["ao"] = "pcm", ["vo"] = "null", ["ao-pcm-file"] = output,
            ["ao-pcm-append"] = true, ["ao-pcm-waveheader"] = false,
            ["audio-format"] = "float", ["audio-channels"] = "mono", ["audio-samplerate"] = 48000,
        });
        using var player = new Player(engine);
        var ends = 0;
        engine.Ended += reason => { if (reason == EndReason.EndOfFile) Interlocked.Increment(ref ends); };
        Assert.IsTrue(player.SetNormalization(true));
        Assert.IsTrue(player.OpenFiles([first, next]));
        for (var selection = 0; selection < 3; selection++)
        {
            var expected = selection + 1;
            await NativeHost.Until(() => Volatile.Read(ref ends) >= expected);
            var rms = MiddleRms(output);
            Assert.IsGreaterThan(0.05, rms,
                $"Normalization lost its audible gain on selection {selection}: RMS={rms}.");
            if (selection == 0) Assert.IsTrue(player.Next(false));
            if (selection == 1) Assert.IsTrue(player.Previous(false));
        }
    }

    private static string Tone(Workspace workspace, string name, int rate)
    {
        var path = NativeHost.Wave(workspace, name, rate);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write);
        using var writer = new BinaryWriter(stream);
        stream.Position = 44;
        for (var sample = 0; sample < rate * 10; sample++)
            writer.Write((short)(Math.Sin(2 * Math.PI * 440 * sample / rate) * 800));
        return path;
    }

    private static double MiddleRms(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream);
        var start = stream.Length - 48000 * sizeof(float) * 7;
        Assert.IsGreaterThanOrEqualTo(0L, start, "The destination did not render enough audio.");
        stream.Position = start;
        double energy = 0;
        const int samples = 48000 * 3;
        for (var sample = 0; sample < samples; sample++)
        {
            var value = reader.ReadSingle();
            Assert.IsTrue(float.IsFinite(value));
            energy += value * value;
        }
        return Math.Sqrt(energy / samples);
    }
}
