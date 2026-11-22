using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback;

[TestClass, TestCategory("Unit")]
public sealed class MediaClockTests
{
    [TestMethod]
    public void OptionalViewsShareTheExistingClockAndReleaseTheirSubscriptionsOnce()
    {
        using var player = new Player(new FakeEngine()); var first = 0; var second = 0;
        var one = player.ObserveMediaTick(() => first++); var two = player.ObserveMediaTick(() => second++);
        Assert.IsTrue(player.RequiresMediaTick); player.SampleMedia();
        Assert.AreEqual(1, first); Assert.AreEqual(1, second);
        one.Dispose(); one.Dispose(); player.SampleMedia();
        Assert.AreEqual(1, first); Assert.AreEqual(2, second); Assert.IsTrue(player.RequiresMediaTick);
        two.Dispose(); Assert.IsFalse(player.RequiresMediaTick);
        player.RequiresMediaTick = true; Assert.IsTrue(player.RequiresMediaTick);
    }
}
