using LunaPlayer.Discord.Publishing;

namespace LunaPlayer.Tests.Discord.Publishing;

[TestClass, TestCategory("Unit")]
public sealed class SendBudgetTests
{
    [TestMethod]
    public void StateChangesCanBurstWithoutAFourSecondDelay()
    {
        var budget = new SendBudget();
        budget.Record(TimeSpan.Zero);
        Assert.AreEqual(TimeSpan.Zero, budget.Delay(TimeSpan.FromMilliseconds(1), true));
        budget.Record(TimeSpan.FromMilliseconds(1));
        Assert.AreEqual(TimeSpan.Zero, budget.Delay(TimeSpan.FromMilliseconds(2), true));
    }

    [TestMethod]
    public void OrdinaryUpdatesLeaveASlotForPauseAndAFullWindowEventuallyExpires()
    {
        var budget = new SendBudget();
        for (var i = 0; i < 4; i++) budget.Record(TimeSpan.FromSeconds(i));
        Assert.AreEqual(TimeSpan.FromSeconds(16), budget.Delay(TimeSpan.FromSeconds(4), false));
        Assert.AreEqual(TimeSpan.Zero, budget.Delay(TimeSpan.FromSeconds(4), true));
        budget.Record(TimeSpan.FromSeconds(4));
        Assert.AreEqual(TimeSpan.FromSeconds(15), budget.Delay(TimeSpan.FromSeconds(5), true));
        Assert.AreEqual(TimeSpan.Zero, budget.Delay(TimeSpan.FromSeconds(20), true));
        Assert.AreEqual(TimeSpan.Zero, budget.Delay(TimeSpan.FromSeconds(24), false));
    }
}
