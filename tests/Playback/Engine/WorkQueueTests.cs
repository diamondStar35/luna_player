using System.Collections.Concurrent;
using LunaPlayer.Playback;

namespace LunaPlayer.Tests.Playback.Engine;

[TestClass, TestCategory("Unit")]
public sealed class WorkQueueTests
{
    [TestMethod]
    public async Task SubmissionDoesNotWaitAndOnlyTheLatestPendingFilterRuns()
    {
        using var queue = new WorkQueue();
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applied = new List<int>();
        try
        {
            Assert.IsTrue(queue.Post(() => { started.SetResult(); return release.Wait(TimeSpan.FromSeconds(5)); }));
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            foreach (var value in new[] { 1, 2, 3 })
                Assert.IsTrue(queue.Post(() => { applied.Add(value); if (value == 3) finished.SetResult(); return true; }, AudioFilter.Spatialization));
            Assert.IsEmpty(applied);
            release.Set();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(2));
            CollectionAssert.AreEqual(new[] { 3 }, applied);
        }
        finally { release.Set(); queue.Dispose(); await queue.Completion; }
    }

    [TestMethod]
    public async Task SupersededFailuresDoNotReportAnError()
    {
        using var queue = new WorkQueue();
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new ConcurrentQueue<AudioFilter>();
        queue.Failed += errors.Enqueue;
        try
        {
            queue.Post(() => { started.SetResult(); release.Wait(TimeSpan.FromSeconds(5)); return false; }, AudioFilter.Spatialization);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            queue.Post(() => { finished.SetResult(); return true; }, AudioFilter.Spatialization);
            release.Set();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.IsEmpty(errors);
        }
        finally { release.Set(); queue.Dispose(); await queue.Completion; }
    }

    [TestMethod]
    public async Task AReplayDoesNotSuppressTheFailureOfTheRequestedState()
    {
        using var queue = new WorkQueue();
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new ConcurrentQueue<AudioFilter>();
        queue.Failed += errors.Enqueue;
        try
        {
            queue.Post(() => { started.SetResult(); release.Wait(TimeSpan.FromSeconds(5)); return false; }, AudioFilter.Spatialization);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            queue.Post(() => true, AudioFilter.Spatialization, replay: true);
            queue.Post(() => { finished.SetResult(); return true; });
            release.Set();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(2));
            CollectionAssert.AreEqual(new[] { AudioFilter.Spatialization }, errors.ToArray());
        }
        finally { release.Set(); queue.Dispose(); await queue.Completion; }
    }

    [TestMethod]
    public async Task AFailedFilterDoesNotStopFollowingWork()
    {
        using var queue = new WorkQueue();
        var errors = new ConcurrentQueue<AudioFilter>();
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.Failed += errors.Enqueue;
        queue.Post(() => false, AudioFilter.Spatialization);
        queue.Post(() => { finished.SetResult(); return true; });
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(2));
        CollectionAssert.AreEqual(new[] { AudioFilter.Spatialization }, errors.ToArray());
        queue.Dispose();
        await queue.Completion;
    }

    [TestMethod]
    public async Task DisposalDropsPendingWorkWithoutWaitingForTheRunningFilter()
    {
        using var queue = new WorkQueue();
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ran = false;
        try
        {
            queue.Post(() => { started.SetResult(); return release.Wait(TimeSpan.FromSeconds(5)); });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            queue.Post(() => ran = true);
            queue.Dispose();
            Assert.IsFalse(queue.Post(() => true));
            Assert.IsFalse(ran);
        }
        finally { release.Set(); await queue.Completion; }
        Assert.IsFalse(ran);
    }
}
