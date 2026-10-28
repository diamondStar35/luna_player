using LunaPlayer.Playback;
namespace LunaPlayer.Tests.Infrastructure;
internal static class NativeHost
{
    private static readonly Lock Gate = new();
    private static bool _initialized;
    internal static Engine Create()
    {
        lock (Gate)
        {
            if (!_initialized) { NativeLibraryBootstrap.Initialize(); _initialized = true; }
        }
        return new Engine(0, new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
    }
    internal static async Task Until(Func<bool> condition)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(20));
        while (!condition())
        {
            try { await timer.WaitForNextTickAsync(deadline.Token); }
            catch (OperationCanceledException) { Assert.Fail("Timed out waiting for the native playback state."); }
        }
    }
    internal static string Wave(Workspace workspace, string name = "fixture.wav")
    {
        var path = Path.Combine(workspace.Root, name);
        using var writer = new BinaryWriter(File.Create(path));
        const int size = 16000 * 2 * 10;
        writer.Write("RIFF"u8); writer.Write(size + 36); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(16000);
        writer.Write(32000); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(size); writer.Write(new byte[size]);
        return path;
    }
}
