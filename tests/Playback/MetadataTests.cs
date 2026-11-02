using System.Diagnostics;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class MetadataTests
{
    [TestMethod]
    [DataRow(".wav")]
    [DataRow(".mp3")]
    [DataRow(".flac")]
    [DataRow(".m4a")]
    public async Task SwitchingToAnUntaggedFileDoesNotRetainThePreviousMetadata(string extension)
    {
        using var workspace = new Workspace();
        using var engine = NativeHost.Create();
        using var player = new Player(engine);
        var first = TaggedWave.Write(workspace, "first.wav", "First title", "First artist");
        var second = NativeHost.Wave(workspace, "second.wav");
        if (extension != ".wav")
        {
            first = await Convert(first, Path.ChangeExtension(first, extension));
            second = await Convert(second, Path.ChangeExtension(second, extension));
        }
        // Presentation code asks for the new entry's name as soon as the selection changes,
        // before asynchronous loading has replaced the outgoing decoder's properties.
        player.CurrentChanged += () => _ = player.CurrentDisplayName;
        player.OpenFiles([first, second]);
        await NativeHost.Until(() => !engine.IsLoading && engine.GetMetadata().Display.Title == "First title");
        Assert.AreEqual("First title", player.CurrentTitle);
        Assert.AreEqual("First artist", player.CurrentArtist);
        for (var visit = 0; visit < 2; visit++)
        {
            Assert.IsTrue(player.Next(false));
            await NativeHost.Until(() => !engine.IsLoading && engine.MediaTitle == Path.GetFileName(second));
            Assert.IsTrue(string.IsNullOrEmpty(player.CurrentTitle), $"Retained title: {player.CurrentTitle}");
            Assert.IsTrue(string.IsNullOrEmpty(player.CurrentArtist), $"Retained artist: {player.CurrentArtist}");
            Assert.IsTrue(player.Previous(false));
            await NativeHost.Until(() => !engine.IsLoading && engine.GetMetadata().Display.Title == "First title");
            Assert.AreEqual("First title", player.CurrentTitle);
            Assert.AreEqual("First artist", player.CurrentArtist);
        }
    }

    private static async Task<string> Convert(string source, string destination)
    {
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "lib", "ffmpeg.exe"))
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true,
        };
        foreach (var argument in new[] { "-v", "error", "-y", "-i", source, destination })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
        Assert.AreEqual(0, process.ExitCode, await error);
        return destination;
    }
}
