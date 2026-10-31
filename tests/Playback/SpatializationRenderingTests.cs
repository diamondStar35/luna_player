using System.Diagnostics;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback;

[TestClass, TestCategory("Integration")]
public sealed class SpatializationRenderingTests
{
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
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "lib", "ffmpeg.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in new[] { "-v", "error", "-f", "lavfi", "-i",
            "sine=frequency=440:sample_rate=48000:duration=0.1", "-af", graph, "-f", "f32le", "-" })
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
        var samples = output.ToArray();
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
}
