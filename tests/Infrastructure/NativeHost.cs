using LunaPlayer.Playback;
using MpvNet;
namespace LunaPlayer.Tests.Infrastructure;
internal static class NativeHost
{
    private static readonly Lock Gate = new();
    private static bool _initialized;
    internal static void Initialize()
    {
        lock (Gate)
        {
            if (!_initialized) { NativeLibraryBootstrap.Initialize(); _initialized = true; }
        }
    }
    internal static Engine Create()
    {
        Initialize();
        return new Engine(0, new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
    }
    internal static MpvSession Session(Engine engine)
    {
        var coordinator = NativeFields.Read(typeof(Engine), engine, "_coordinator")!;
        return (MpvSession)NativeFields.Read(typeof(PlaybackCoordinator), coordinator, "_session")!;
    }
    internal static MPV Handle(Engine engine) => (MPV)NativeFields.Read(typeof(MpvSession), Session(engine), "_mpv")!;
    internal static WorkQueue Queue(Engine engine) => (WorkQueue)NativeFields.Read(typeof(MpvSession), Session(engine), "_work")!;
    internal sealed class SeekCompletion : IDisposable
    {
        private readonly IDisposable _seek, _restart;
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _seeking;
        internal SeekCompletion(Engine engine)
        {
            var mpv = Handle(engine);
            _seek = mpv.OnEvent(_ => _seeking = true, MpvEventId.Seek);
            _restart = mpv.OnEvent(_ => { if (_seeking) _completed.TrySetResult(); }, MpvEventId.PlaybackRestart);
        }
        internal Task Wait() => _completed.Task.WaitAsync(TimeSpan.FromSeconds(8));
        public void Dispose() { _seek.Dispose(); _restart.Dispose(); }
    }
    internal static async Task Flush(Engine engine)
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.IsTrue(Queue(engine).Post(() => { completed.SetResult(); return true; }));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(8));
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
    internal static string Wave(Workspace workspace, string name = "fixture.wav", int sampleRate = 16000)
    {
        var path = Path.Combine(workspace.Root, name);
        using var writer = new BinaryWriter(File.Create(path));
        var size = sampleRate * 2 * 10;
        writer.Write("RIFF"u8); writer.Write(size + 36); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(sampleRate);
        writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(size); writer.Write(new byte[size]);
        return path;
    }
}
