using System.Reflection;
using System.Runtime.ExceptionServices;
using LunaPlayer.Application;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Tests.Infrastructure;
using LunaPlayer.UI.YouTube;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.Tests.Presentation;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class DownloadProgressTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void FinishedDownloadClosesEvenWhenAModalWindowHasDisabledTheMainFrame(bool success)
    {
        OnUiThread(() =>
        {
            using var frame = new Frame(title: "Download test");
            frame.Enabled = false;
            var expected = success ? Downloads.Outcome.Saved("video.mp4") : Downloads.Outcome.Failed("Download failed");
            using var queue = new Downloads.DownloadQueue(new Settings(), (_, _, _) => expected);
            var id = queue.StartSingle("Video", new Downloads.Request { Url = "https://www.youtube.com/watch?v=abcdefghijk", Folder = "." });
            Assert.IsTrue(SpinWait.SpinUntil(() => queue.Snapshot(id)?.Status is
                Downloads.QueueItemStatus.Completed or Downloads.QueueItemStatus.Failed, TimeSpan.FromSeconds(5)));
            using var dialog = new DownloadProgressDialog(new Dispatcher(), queue, id);
            var closed = 0;
            Downloads.Outcome? actual = null;
            dialog.Closed += (_, outcome) =>
            {
                closed++;
                actual = outcome;
                Assert.IsNull(queue.Snapshot(id));
            };

            var tick = typeof(DownloadProgressDialog).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)!;
            tick.Invoke(dialog, null);
            tick.Invoke(dialog, null);

            Assert.AreEqual(1, closed, "Completion must close progress without waiting for the search window to exit.");
            Assert.AreEqual(expected, actual);
            Assert.IsFalse(frame.Enabled, "Completing a download must not re-enable the modal window's owner.");
        });
    }

    [TestMethod]
    public void PostProcessingKeepsProgressOpenUntilTheQueueHasFinished()
    {
        OnUiThread(() =>
        {
            using var frame = new Frame(title: "Download test");
            frame.Enabled = false;
            using var finish = new ManualResetEventSlim();
            using var queue = new Downloads.DownloadQueue(new Settings(), (_, report, token) =>
            {
                report(new("finished", 100, 100, 100, 0, 0, "MoveFiles"));
                finish.Wait(token);
                return Downloads.Outcome.Saved("video.mp4");
            });
            var id = queue.StartSingle("Video", new Downloads.Request { Url = "https://www.youtube.com/watch?v=abcdefghijk", Folder = "." });
            using var dialog = new DownloadProgressDialog(new Dispatcher(), queue, id);
            var closed = 0;
            dialog.Closed += (_, _) => closed++;
            var tick = typeof(DownloadProgressDialog).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assert.IsTrue(SpinWait.SpinUntil(() => queue.Snapshot(id)?.Status == Downloads.QueueItemStatus.Downloaded,
                TimeSpan.FromSeconds(5)));
            tick.Invoke(dialog, null);
            Assert.AreEqual(0, closed);
            finish.Set();
            Assert.IsTrue(SpinWait.SpinUntil(() => queue.Snapshot(id)?.Status == Downloads.QueueItemStatus.Completed,
                TimeSpan.FromSeconds(5)));
            tick.Invoke(dialog, null);
            Assert.AreEqual(1, closed);
        });
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CompletionUsesSearchResultsInsteadOfADisabledMainFrameOrAnotherProgressWindow(bool progressActive)
    {
        OnUiThread(() =>
        {
            using var frame = new Frame(title: "Main");
            using var results = new Dialog(frame, title: "Search results");
            using var progress = new Dialog(parent: null, style: DialogStyle.Default | DialogStyle.NoParent);
            frame.Enabled = false;
            Assert.IsTrue(results.Enabled);
            Assert.AreSame(results, DownloadOutcomes.SelectOwner(progressActive ? progress : null, results, frame, [progress]));
            Assert.IsFalse(frame.Enabled);
        });
    }

    [TestMethod]
    public void CompletionUsesAnEnabledNestedDialogAndFallsBackToMainAfterSearchCloses()
    {
        OnUiThread(() =>
        {
            using var frame = new Frame(title: "Main");
            using var results = new Dialog(frame, title: "Search results");
            using var nested = new Dialog(results, title: "Options");
            frame.Enabled = false;
            results.Enabled = false;
            Assert.IsTrue(nested.Enabled);
            Assert.AreSame(nested, DownloadOutcomes.SelectOwner(nested, results, frame, []));
            Assert.IsNull(DownloadOutcomes.SelectOwner(null, results, frame, []));
            frame.Enabled = true;
            Assert.AreSame(frame, DownloadOutcomes.SelectOwner(null, null, frame, []));
        });
    }

    [TestMethod]
    public void CompletionMessagesAreDeferredUntilProgressClosingReturnsAndAreNeverNested()
    {
        OnUiThread(() =>
        {
            using var frame = new Frame(title: "Main");
            var dispatcher = new Dispatcher();
            var first = Downloads.Outcome.Saved("first.mp4");
            var second = Downloads.Outcome.Failed("Network error");
            var shown = new List<Downloads.Outcome>();
            using var messages = new DownloadOutcomes(dispatcher, () => frame, (result, parent) =>
            {
                Assert.AreSame(frame, parent);
                shown.Add(result);
                if (result == first)
                {
                    dispatcher.Pump();
                    Assert.HasCount(1, shown, "The second message must wait until the first one closes.");
                }
            });
            messages.Add(first);
            dispatcher.Post(() => messages.Add(second));
            Assert.HasCount(0, shown);
            dispatcher.Pump();
            CollectionAssert.AreEqual(new[] { first, second }, shown);
        });
    }

    [TestMethod]
    public void OnlyTheNotificationWaitsWhileEveryOwnerIsDisabled()
    {
        OnUiThread(() =>
        {
            using var frame = new Frame(title: "Main");
            frame.Enabled = false;
            var dispatcher = new Dispatcher();
            var shown = new List<Downloads.Outcome>();
            var outcome = Downloads.Outcome.Saved("video.mp4");
            using var messages = new DownloadOutcomes(dispatcher, () => frame.Enabled ? frame : null,
                (result, _) => shown.Add(result));
            messages.Add(outcome);
            dispatcher.Pump();
            Assert.HasCount(0, shown);
            Assert.AreEqual(1, dispatcher.ActiveTimers);
            frame.Enabled = true;
            dispatcher.Tick();
            CollectionAssert.AreEqual(new[] { outcome }, shown);
            Assert.AreEqual(0, dispatcher.ActiveTimers);
        });
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ClearingOrDisposingPreventsLateCompletionMessages(bool dispose)
    {
        OnUiThread(() =>
        {
            using var frame = new Frame(title: "Main");
            var dispatcher = new Dispatcher();
            var shown = new List<Downloads.Outcome>();
            using var messages = new DownloadOutcomes(dispatcher, () => frame.Enabled ? frame : null,
                (result, _) => shown.Add(result));
            frame.Enabled = false;
            messages.Add(Downloads.Outcome.Saved("old.mp4"));
            dispatcher.Pump();
            if (dispose) messages.Dispose(); else messages.Clear();
            frame.Enabled = true;
            dispatcher.Tick();
            dispatcher.Pump();
            Assert.HasCount(0, shown);
            Assert.AreEqual(0, dispatcher.ActiveTimers);
            messages.Add(null);
            messages.Add(Downloads.Outcome.Saved("new.mp4"));
            dispatcher.Pump();
            Assert.AreEqual(dispose ? 0 : 1, shown.Count);
        });
    }

    private static void OnUiThread(Action verify)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                NativeHost.Initialize();
                using var app = new App();
                verify();
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private sealed class Dispatcher : IApplicationDispatcher
    {
        private readonly Queue<Action> _posts = [];
        private readonly List<Repeater> _timers = [];
        internal int ActiveTimers => _timers.Count(timer => !timer.Disposed);
        public void Post(Action action) => _posts.Enqueue(action);
        public IDisposable Repeat(TimeSpan interval, Action action)
        {
            var timer = new Repeater(action);
            _timers.Add(timer);
            return timer;
        }
        internal void Pump() { while (_posts.TryDequeue(out var action)) action(); }
        internal void Tick() { foreach (var timer in _timers.ToArray()) timer.Tick(); }

        private sealed class Repeater(Action action) : IDisposable
        {
            internal bool Disposed { get; private set; }
            internal void Tick() { if (!Disposed) action(); }
            public void Dispose() => Disposed = true;
        }
    }
}
