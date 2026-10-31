using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback.Engine;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class SessionTests
{
    [TestMethod]
    public async Task AReplayCannotDiscardAPendingSofaConfiguration()
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        using var session = new MpvSession(0, new Lock(), new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var audio = new AudioProcessor(session);
        try
        {
            session.TryDo(_ => { started.SetResult(); release.Wait(TimeSpan.FromSeconds(5)); });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            audio.SetSofa(AudioFilters.SofaGraph(new() { SofaEnabled = true, SofaFile = SofaFixture.Write(workspace) }));
            audio.SetSofaDirection(-45, 90);
            audio.ReplaySofaDirection();
            session.TryDo(_ => finished.SetResult());
        }
        finally { release.Set(); }
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await NativeHost.Until(() => session.ReadObject("af") is IEnumerable<object?> filters
            && filters.OfType<IDictionary<string, object?>>()
                .Any(filter => filter.TryGetValue("label", out var label) && label as string == AudioFilters.SofaLabel));
    }

    [TestMethod]
    public async Task NativeRequestsAndStateReadsRemainAvailableWhileAFilterIsBusy()
    {
        NativeHost.Initialize();
        using var session = new MpvSession(0, new Lock(), new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var audio = new AudioProcessor(session);
        try
        {
            session.QueueFilter(AudioFilter.Equalizer, () => { started.SetResult(); return release.Wait(TimeSpan.FromSeconds(5)); });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await Task.Run(() =>
            {
                Assert.IsTrue(audio.SetSofa("sofalizer=sofa=not-validated.anything:type=freq"));
                Assert.IsTrue(session.TrySetProperty("volume", 50));
                Assert.IsTrue(session.TryDo(_ => finished.SetResult()));
                Assert.AreEqual(1d, session.ReadDouble("speed"));
                Assert.IsFalse(finished.Task.IsCompleted);
            }).WaitAsync(TimeSpan.FromSeconds(1));
        }
        finally { release.Set(); }
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
