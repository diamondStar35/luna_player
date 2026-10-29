using LunaPlayer.Application;
using LunaPlayer.Application.Presentation;

namespace LunaPlayer.Tests.Presentation;

[TestClass, TestCategory("Unit")]
public sealed class ClockTests
{
    private sealed class Dispatcher : IApplicationDispatcher
    {
        internal readonly List<Registration> Timers = [];
        public void Post(Action action) => action();
        public IDisposable Repeat(TimeSpan interval, Action action)
        {
            Assert.AreEqual(TimeSpan.FromSeconds(1), interval);
            var registration = new Registration(action);
            Timers.Add(registration);
            return registration;
        }
    }
    private sealed class Registration(Action action) : IDisposable
    {
        internal bool Disposed;
        internal void Tick() { if (!Disposed) action(); }
        public void Dispose() => Disposed = true;
    }

    [TestMethod]
    public void MultiplePresentersShareOneTickAndStopTogether()
    {
        var dispatcher = new Dispatcher();
        var overlay = 0;
        var discord = 0;
        using var clock = new Clock(dispatcher, () => { overlay++; discord++; });
        clock.Update(false);
        Assert.HasCount(0, dispatcher.Timers);
        clock.Update(true);
        clock.Update(true);
        Assert.HasCount(1, dispatcher.Timers);
        dispatcher.Timers[0].Tick();
        Assert.AreEqual(1, overlay);
        Assert.AreEqual(1, discord);
        clock.Update(false);
        dispatcher.Timers[0].Tick();
        Assert.AreEqual(1, discord);
        clock.Update(true);
        Assert.HasCount(2, dispatcher.Timers);
        clock.Dispose();
        clock.Update(true);
        dispatcher.Timers[1].Tick();
        Assert.AreEqual(1, discord);
        Assert.HasCount(2, dispatcher.Timers);
    }
}
