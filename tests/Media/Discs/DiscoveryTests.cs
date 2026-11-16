using System.Collections.Concurrent;
using LunaPlayer.Media.Discs;

namespace LunaPlayer.Tests.Media.Discs;

[TestClass, TestCategory("Unit")]
public sealed class DiscoveryTests
{
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
