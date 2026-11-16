using LunaPlayer.Application;
using LunaPlayer.UI.Contracts;

namespace LunaPlayer.Tests.Presentation;

[TestClass, DoNotParallelize, TestCategory("Unit")]
public sealed class BackgroundProgressTests
{
    [TestMethod]
    public async Task CompletionReleasesTheWindowAndTimerBeforeOpeningTheResult()
    {
        var dispatcher = new Dispatcher();
        var progress = new Progress();
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbacks = new List<string>();
        BackgroundProgress.Start(progress, dispatcher, Prompt(), (_, _) => release.Task.GetAwaiter().GetResult(),
            result =>
            {
                Assert.IsTrue(progress.Disposed);
                Assert.IsTrue(dispatcher.TimerDisposed);
                Assert.AreEqual(42, result);
                callbacks.Add("completed");
            }, () => callbacks.Add("finished"));
        Assert.IsFalse(progress.Disposed);
        Assert.IsEmpty(callbacks);
        release.SetResult(42);
        await Until(dispatcher, () => callbacks.Count == 2);
        CollectionAssert.AreEqual(new[] { "finished", "completed" }, callbacks);
        dispatcher.Tick();
        Assert.HasCount(2, callbacks);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CancellationAndShutdownReleaseTheOperationWithoutOpeningItsResult(bool shutdown)
    {
        var dispatcher = new Dispatcher();
        var progress = new Progress();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = 0;
        var finished = 0;
        BackgroundProgress.Start<int>(progress, dispatcher, Prompt(), (_, token) =>
        {
            started.SetResult();
            token.WaitHandle.WaitOne();
            token.ThrowIfCancellationRequested();
            return 0;
        }, _ => completed++, () => finished++);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (shutdown) progress.Dispose(); else progress.UserCancelled = true;
        await Until(dispatcher, () => finished == 1);
        Assert.AreEqual(0, completed);
        Assert.IsTrue(progress.Disposed);
        Assert.IsTrue(dispatcher.TimerDisposed);
    }

    [TestMethod]
    public async Task AWorkerReturningAfterCancellationDoesNotOpenTheResult()
    {
        var dispatcher = new Dispatcher();
        var progress = new Progress();
        var completed = 0;
        var finished = 0;
        BackgroundProgress.Start(progress, dispatcher, Prompt(), (_, token) =>
        {
            token.WaitHandle.WaitOne();
            return 42;
        }, _ => completed++, () => finished++);
        progress.UserCancelled = true;
        await Until(dispatcher, () => finished == 1);
        Assert.AreEqual(0, completed);
        Assert.IsTrue(progress.Disposed);
    }

    private static ProgressPrompt Prompt() => new("Progress", "Working", update => update.Name) { Modal = false };

    private static async Task Until(Dispatcher dispatcher, Func<bool> done)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!done())
        {
            timeout.Token.ThrowIfCancellationRequested();
            dispatcher.Tick();
            await Task.Delay(5, timeout.Token);
        }
    }

    private sealed class Progress : IProgressView
    {
        internal bool Disposed;
        internal bool UserCancelled;
        public bool Cancelled => UserCancelled || Disposed;
        public void Update(int percent, string message) { }
        public void Pulse() { }
        public void Dispose() => Disposed = true;
    }

    private sealed class Dispatcher : IApplicationDispatcher
    {
        private readonly Queue<Action> _posted = [];
        private Action? _tick;
        internal bool TimerDisposed;
        public void Post(Action action) => _posted.Enqueue(action);
        public IDisposable Repeat(TimeSpan interval, Action action)
        {
            _tick = action;
            return new Registration(() => { TimerDisposed = true; _tick = null; });
        }
        internal void Tick()
        {
            _tick?.Invoke();
            while (_posted.TryDequeue(out var action)) action();
        }
    }

    private sealed class Registration(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
