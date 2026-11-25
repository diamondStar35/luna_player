using LunaPlayer.UI.Services;

namespace LunaPlayer.Tests.Presentation;

[TestClass, TestCategory("Unit")]
public sealed class LifetimeTests
{
    [TestMethod]
    public void CloseAndFinalTeardownCancelOnceAndKeepTheTokenReadable()
    {
        var lifetime = new Lifetime(); var token = lifetime.Token; var cancelled = 0;
        using var registration = token.Register(() => cancelled++);
        lifetime.Dispose(); lifetime.Dispose();
        Assert.IsTrue(token.IsCancellationRequested);
        Assert.IsTrue(lifetime.Token.IsCancellationRequested);
        Assert.AreEqual(1, cancelled);
    }

    [TestMethod]
    public void QueuedCompletionCannotTouchAClosedDialog()
    {
        var pending = new Queue<Action>(); var lifetime = new Lifetime(); var touched = false;
        lifetime.Post(pending.Enqueue, () => touched = true);
        lifetime.Dispose(); pending.Dequeue()();
        lifetime.Post(pending.Enqueue, () => touched = true);
        Assert.IsFalse(touched); Assert.IsEmpty(pending);
    }

    [TestMethod]
    public void DispatcherShutdownIsIgnoredOnlyAfterCancellation()
    {
        using var lifetime = new Lifetime();
        Assert.Throws<InvalidOperationException>(() => lifetime.Post(_ => throw new InvalidOperationException(), () => { }));
        lifetime.Post(_ => { lifetime.Dispose(); throw new InvalidOperationException(); }, () => Assert.Fail("Closed work ran."));
    }
}
