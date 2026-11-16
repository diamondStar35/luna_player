using System.Diagnostics;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback;

[TestClass, TestCategory("Integration")]
public sealed class SpatializationRenderingTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task NormalizationDoesNotDelayAudibleDirectionChanges(bool normalizationFirst)
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        using var session = new MpvSession(0, new Lock(), new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
        var audio = new AudioProcessor(session);
        var sofa = AudioFilters.SofaGraph(new AudioSettings
        {
            SofaEnabled = true, SofaFile = SofaFixture.Write(workspace), SofaRotation = -90,
        });
        if (normalizationFirst) audio.SetNormalization(true);
        audio.SetSofa(sofa);
        if (!normalizationFirst) audio.SetNormalization(true);
        var ready = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.TryDo(_ => ready.SetResult(string.Join(',',
            ((IEnumerable<object?>)session.ReadLiveObject("af")!).OfType<IDictionary<string, object?>>()
            .Select(filter => (string)((IDictionary<string, object?>)filter["params"]!)["graph"]!))));
        var graph = await ready.Task.WaitAsync(TimeSpan.FromSeconds(8));
        // Issue the command at the output end of the chain, where its timestamp matches
        // what the listener currently hears rather than the normalizer's lookahead input.
        graph += ",asendcmd=c='3 sofalizer rotation -90;4 sofalizer rotation 90;5 sofalizer rotation -90'";
        var samples = await RenderSamples(graph, "aevalsrc=0.05*sin(2*PI*440*t):s=48000:d=8");
        double initialLeft = 0, initialRight = 0;
        for (var sample = 48000 * 2; sample < 48000 * 2 + 960; sample++)
        {
            var l = BitConverter.ToSingle(samples, sample * 8);
            var r = BitConverter.ToSingle(samples, sample * 8 + 4);
            initialLeft += l * l;
            initialRight += r * r;
        }
        Assert.IsTrue(initialLeft > initialRight * 20, "The initial direction was not audible on the left.");
        const int window = 960;
        foreach (var (requestedAt, onRight) in new[] { (3, true), (4, false), (5, true) })
        {
            double? responseTime = null;
            for (var first = 48000 * requestedAt; (first + window) * 8 <= samples.Length; first += window)
            {
                double left = 0, right = 0;
                for (var sample = first; sample < first + window; sample++)
                {
                    var l = BitConverter.ToSingle(samples, sample * 8);
                    var r = BitConverter.ToSingle(samples, sample * 8 + 4);
                    left += l * l;
                    right += r * r;
                }
                var desired = onRight ? right : left;
                var other = onRight ? left : right;
                if (desired > other * 20 && desired > 0.0001)
                {
                    responseTime = first / 48000d - requestedAt;
                    break;
                }
            }
            Assert.IsNotNull(responseTime, "The new direction never reached the rendered audio.");
            Assert.IsLessThan(0.25, responseTime.Value,
                $"Normalization delayed the direction requested at {requestedAt}s by {responseTime.Value:F3} seconds.");
        }
    }

    [TestMethod]
    [DataRow(-90d, true)]
    [DataRow(90d, false)]
    public async Task RotationMovesRenderedSoundInTheRequestedDirection(double delta, bool left)
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        Assert.IsTrue(player.ConfigureSofa(new AudioSettings
        { SofaEnabled = true, SofaFile = SofaFixture.Write(workspace) }));
        Assert.IsTrue(player.ChangeSofaRotation(delta));
        var energy = await Render(engine.SofaGraph, engine.SofaRotation, engine.SofaElevation);
        Assert.IsTrue(left ? energy.Left > energy.Right * 20 : energy.Right > energy.Left * 20,
            $"Expected sound on the {(left ? "left" : "right")}; energies were L={energy.Left}, R={energy.Right}.");
    }

    [TestMethod]
    [DataRow(90d, true)]
    [DataRow(-90d, false)]
    public async Task MaximumElevationStepDoesNotWrapRenderedSoundToTheHorizon(double delta, bool above)
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        Assert.IsTrue(player.ConfigureSofa(new AudioSettings
        { SofaEnabled = true, SofaFile = SofaFixture.Write(workspace) }));
        Assert.IsTrue(player.ChangeSofaElevation(delta));
        var energy = await Render(engine.SofaGraph, engine.SofaRotation, engine.SofaElevation);
        // The fixture gives the upper and lower measurements different ear amplitudes;
        // its horizontal measurement has equal energy in both ears.
        Assert.IsTrue(above ? energy.Left > energy.Right * 3 : energy.Right > energy.Left * 3,
            $"Expected {(above ? "upper" : "lower")} response; energies were L={energy.Left}, R={energy.Right}.");
    }

    private static async Task<(double Left, double Right)> Render(string graph, double rotation, double elevation)
    {
        var commands = FormattableString.Invariant($"0.02 sofalizer rotation {rotation};0.02 sofalizer elevation {elevation}");
        graph = $"asendcmd=c='{commands}',{graph}";
        var samples = await RenderSamples(graph, "sine=frequency=440:sample_rate=48000:duration=0.1");
        Assert.IsTrue(samples.Length > 0 && samples.Length % 8 == 0);
        double left = 0, right = 0;
        for (var index = 48000 * 2 * sizeof(float) * 6 / 100; index < samples.Length; index += 8)
        {
            var l = BitConverter.ToSingle(samples, index);
            var r = BitConverter.ToSingle(samples, index + 4);
            left += l * l;
            right += r * r;
        }
        return (left, right);
    }

    private static async Task<byte[]> RenderSamples(string graph, string input)
    {
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "lib", "ffmpeg.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in new[] { "-v", "error", "-f", "lavfi", "-i",
            input, "-af", graph, "-f", "f32le", "-" })
            start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        Assert.IsTrue(process.Start());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        using var output = new MemoryStream();
        try
        {
            await process.StandardOutput.BaseStream.CopyToAsync(output, timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
        Assert.AreEqual(0, process.ExitCode, await error);
        return output.ToArray();
    }
}
