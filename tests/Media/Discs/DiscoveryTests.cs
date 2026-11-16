using System.Collections.Concurrent;
using LunaPlayer.Media.Discs;

namespace LunaPlayer.Tests.Media.Discs;

[TestClass, TestCategory("Unit")]
public sealed class DiscoveryTests
{
    [TestMethod]
    public async Task AcceptingDuringInspectionTransfersTheReadWithoutCancellingOrRestartingIt()
    {
        var posted = new ConcurrentQueue<Action>();
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        using var discovery = new Discovery(posted.Enqueue, (path, format, token) =>
        {
            Interlocked.Increment(ref reads);
            started.SetResult(token);
            Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(5)));
            return Info(path, format);
        });
        discovery.Completed += _ => Assert.Fail("The accepted inspection must not update its closed picker.");
        discovery.Request("disc", DiscFormat.Audio);
        var token = await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var accepted = discovery.Take("disc", DiscFormat.Audio);
        discovery.Dispose();
        Assert.IsFalse(accepted.Result.IsCompleted);
        Assert.IsFalse(token.IsCancellationRequested);
        release.Set();
        var result = await accepted.Result.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual("disc", result.Disc!.Name);
        Assert.AreEqual(1, reads);
        while (posted.TryDequeue(out var callback)) callback();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AcceptingACompletedReadReusesItsResultBeforeTheUiReceivesIt(bool fail)
    {
        var posted = new ConcurrentQueue<Action>();
        var reads = 0;
        var info = Info("disc", DiscFormat.Video);
        using var discovery = new Discovery(posted.Enqueue, (_, _, _) =>
        {
            Interlocked.Increment(ref reads);
            return fail ? throw new DiscException(Failure.NoVideoTitles) : info;
        });
        discovery.Completed += _ => Assert.Fail("A transferred result must not return to its picker.");
        discovery.Request("disc", DiscFormat.Video);
        await WaitFor(() => posted.Count == 1);
        using var accepted = discovery.Take("DISC", DiscFormat.Video);
        discovery.Dispose();
        Assert.IsTrue(accepted.Result.IsCompletedSuccessfully);
        var result = await accepted.Result;
        if (fail) Assert.AreEqual(Failure.NoVideoTitles, result.Error!.Failure);
        else Assert.AreSame(info, result.Disc);
        Assert.AreEqual(1, reads);
        while (posted.TryDequeue(out var callback)) callback();
    }

    [TestMethod]
    public async Task AcceptingADifferentSelectionStartsOnlyTheAcceptedRead()
    {
        var posted = new ConcurrentQueue<Action>();
        var read = new ConcurrentQueue<string>();
        using var discovery = new Discovery(posted.Enqueue, (path, format, _) =>
        {
            read.Enqueue(path);
            return Info(path, format);
        });
        discovery.Request("old", DiscFormat.Video);
        await WaitFor(() => posted.Count == 1);
        using var accepted = discovery.Take("new", DiscFormat.Audio);
        discovery.Dispose();
        var result = await accepted.Result.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(DiscFormat.Audio, result.Disc!.Format);
        Assert.AreEqual("new", result.Disc.Name);
        CollectionAssert.AreEqual(new[] { "old", "new" }, read.ToArray());
    }

    [TestMethod]
    public async Task AcceptedReadStillWaitsForAnOlderUninterruptibleReadToExit()
    {
        var posted = new ConcurrentQueue<Action>();
        using var release = new ManualResetEventSlim();
        using var entered = new ManualResetEventSlim();
        var newRead = false;
        using var discovery = new Discovery(posted.Enqueue, (path, format, _) =>
        {
            if (path == "old")
            {
                entered.Set();
                Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(5)));
            }
            else newRead = true;
            return Info(path, format);
        });
        discovery.Request("old", DiscFormat.Audio);
        await WaitFor(() => entered.IsSet);
        discovery.Request("new", DiscFormat.Video);
        using var accepted = discovery.Take("new", DiscFormat.Video);
        discovery.Dispose();
        Assert.IsFalse(newRead);
        release.Set();
        var result = await accepted.Result.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual("new", result.Disc!.Name);
        Assert.IsTrue(newRead);
    }

    [TestMethod]
    public async Task NewSelectionInvalidatesAResultAlreadyQueuedForTheUi()
    {
        var posted = new ConcurrentQueue<Action>();
        using var discovery = new Discovery(posted.Enqueue, (path, format, _) => Info(path, format));
        var names = new List<string>();
        discovery.Completed += result => names.Add(result.Disc!.Name);
        discovery.Request("old", DiscFormat.Video);
        await WaitFor(() => posted.Count == 1);
        discovery.Request("new", DiscFormat.Audio);
        await WaitFor(() => posted.Count == 2);
        while (posted.TryDequeue(out var callback)) callback();
        CollectionAssert.AreEqual(new[] { "new" }, names);
    }

    [TestMethod]
    public async Task SelectionChangesCancelAndSerializeAnUninterruptibleRead()
    {
        var posted = new ConcurrentQueue<Action>();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        CancellationToken oldToken = default;
        var running = 0;
        var concurrent = false;
        using var discovery = new Discovery(posted.Enqueue, (path, format, token) =>
        {
            if (Interlocked.Increment(ref running) > 1) concurrent = true;
            try
            {
                if (path == "old")
                {
                    oldToken = token;
                    entered.Set();
                    Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(5)));
                }
                return Info(path, format);
            }
            finally { Interlocked.Decrement(ref running); }
        });
        var received = new List<string>();
        discovery.Completed += result => received.Add(result.Disc!.Name);
        discovery.Request("old", DiscFormat.Video);
        await WaitFor(() => entered.IsSet);
        discovery.Request("new", DiscFormat.Audio);
        Assert.IsTrue(oldToken.IsCancellationRequested);
        release.Set();
        await WaitFor(() => posted.Count == 1);
        while (posted.TryDequeue(out var callback)) callback();
        Assert.IsFalse(concurrent);
        CollectionAssert.AreEqual(new[] { "new" }, received);
    }

    [TestMethod]
    public async Task ClosingPickerDiscardsQueuedResults()
    {
        var posted = new ConcurrentQueue<Action>();
        using var discovery = new Discovery(posted.Enqueue, (path, format, _) => Info(path, format));
        discovery.Completed += _ => Assert.Fail("A closed picker must not be updated.");
        discovery.Request("disc", DiscFormat.Video);
        await WaitFor(() => posted.Count == 1);
        discovery.Dispose();
        while (posted.TryDequeue(out var callback)) callback();
        Assert.Throws<ObjectDisposedException>(() => discovery.Request("other", DiscFormat.Audio));
    }

    [TestMethod]
    public async Task ReadFailureReturnsItsSpecificReason()
    {
        var posted = new ConcurrentQueue<Action>();
        using var discovery = new Discovery(posted.Enqueue, (_, _, _) => throw new DiscException(Failure.NoAudioTitles));
        DiscoveryResult? received = null;
        discovery.Completed += value => received = value;
        discovery.Request("video disc", DiscFormat.Audio);
        await WaitFor(() => posted.Count == 1);
        Assert.IsTrue(posted.TryDequeue(out var callback));
        callback();
        Assert.AreEqual(Failure.NoAudioTitles, received!.Error!.Failure);
    }

    private static DiscInfo Info(string name, DiscFormat format) => new(name, format, name, name, [new Title(0)], 0);
    private static async Task WaitFor(Func<bool> ready)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!ready()) await Task.Delay(10, timeout.Token);
    }
}
