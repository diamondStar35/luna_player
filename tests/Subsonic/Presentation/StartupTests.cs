using LunaPlayer.Application;
using LunaPlayer.UI.Subsonic;
using Startup = LunaPlayer.UI.Subsonic.Startup;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class StartupTests
{
    [TestMethod]
    public void OpeningReturnsBeforeBuildingControlsAndEachStepYieldsToTheEventLoop()
    {
        var dispatcher = new Dispatcher(); var steps = new List<int>();
        using var startup = new Startup(dispatcher, () => true,
            [() => steps.Add(1), () => steps.Add(2), () => steps.Add(3)], error => Assert.Fail(error.Message));
        startup.Start(); startup.Start();
        Assert.IsEmpty(steps); Assert.AreEqual(1, dispatcher.Active);
        dispatcher.Tick(); CollectionAssert.AreEqual(new[] { 1 }, steps); Assert.AreEqual(1, dispatcher.Active);
        dispatcher.Tick(); CollectionAssert.AreEqual(new[] { 1, 2 }, steps);
        dispatcher.Tick(); CollectionAssert.AreEqual(new[] { 1, 2, 3 }, steps); Assert.AreEqual(0, dispatcher.Active);
        startup.Start(); Assert.AreEqual(0, dispatcher.Active);
    }

    [TestMethod]
    public void ClosingDuringConstructionPausesUntilTheBrowserIsOpenedAgain()
    {
        var dispatcher = new Dispatcher(); var visible = true; var built = 0;
        using var startup = new Startup(dispatcher, () => visible,
            [() => built++, () => built++], error => Assert.Fail(error.Message));
        startup.Start(); dispatcher.Tick(); visible = false; dispatcher.Tick();
        Assert.AreEqual(1, built); Assert.AreEqual(0, dispatcher.Active);
        visible = true; startup.Start(); dispatcher.Tick();
        Assert.AreEqual(2, built); Assert.AreEqual(0, dispatcher.Active);
    }

    [TestMethod]
    public void DisposalCancelsPendingConstructionAndLateTimerCallbacks()
    {
        var dispatcher = new Dispatcher(); var built = false;
        var startup = new Startup(dispatcher, () => true, [() => built = true], error => Assert.Fail(error.Message));
        startup.Start(); startup.Dispose(); startup.Dispose(); dispatcher.Callbacks[0](); startup.Start();
        Assert.IsFalse(built); Assert.AreEqual(0, dispatcher.Active);
    }

    [TestMethod]
    public void AConstructionFailureIsReportedOnceAndStopsFurtherSteps()
    {
        var dispatcher = new Dispatcher(); var failed = 0; var built = false;
        using var startup = new Startup(dispatcher, () => true,
            [() => throw new InvalidOperationException(), () => built = true], _ => failed++);
        startup.Start(); dispatcher.Tick(); startup.Start(); dispatcher.Tick();
        Assert.AreEqual(1, failed); Assert.IsFalse(built); Assert.AreEqual(0, dispatcher.Active);
    }

    private sealed class Dispatcher : IApplicationDispatcher
    {
        private readonly List<Timer> _timers = [];
        internal readonly List<Action> Callbacks = [];
        internal int Active => _timers.Count(timer => !timer.Disposed);
        public void Post(Action action) => throw new NotSupportedException();
        public IDisposable Repeat(TimeSpan interval, Action action)
        { var timer = new Timer(action); _timers.Add(timer); Callbacks.Add(action); return timer; }
        internal void Tick() { foreach (var timer in _timers.ToArray()) if (!timer.Disposed) timer.Action(); }
        private sealed class Timer(Action action) : IDisposable
        {
            internal Action Action => action;
            internal bool Disposed;
            public void Dispose() => Disposed = true;
        }
    }
}
