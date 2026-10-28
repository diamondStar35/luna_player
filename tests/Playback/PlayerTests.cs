using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;
namespace LunaPlayer.Tests.Playback;
[TestClass, TestCategory("Unit")]
public sealed class PlayerTests
{
    [TestMethod]
    public void OpeningFilesLoadsOnlyTheSelectedItem()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        Assert.IsTrue(player.OpenFiles(["one", "two"], "two"));
        CollectionAssert.AreEqual(new[] { "two" }, engine.Loads);
        Assert.AreEqual("two", player.CurrentPath);
    }
    [TestMethod]
    public void SessionRestoresThePreviousItemAndPositionPaused()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        player.OpenFiles(["one", "two"], "two"); player.SeekAbsolute(3);
        player.PlaySessionStream("https://example.invalid/audio", "Online", null, null);
        player.LeaveSession();
        Assert.AreEqual("two", player.CurrentPath);
        Assert.AreEqual(3d, engine.Position);
        Assert.IsTrue(player.IsPaused);
    }
    [TestMethod]
    public void NavigationLoadsTheSuccessorAndPreservesOrder()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        player.OpenFiles(["one", "two", "three"]);
        Assert.IsTrue(player.Next(false));
        Assert.AreEqual("two", player.CurrentPath);
        Assert.IsTrue(player.Previous(false));
        Assert.AreEqual("one", player.CurrentPath);
    }
    [TestMethod]
    public void CloseAllStopsPlaybackAndClearsTheList()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        player.OpenFiles(["one", "two"]); player.CloseAll();
        Assert.AreEqual(0, player.Count);
        Assert.IsFalse(player.IsLoaded);
        Assert.IsTrue(engine.Stops > 0);
    }
    [TestMethod]
    public void EngineCompletionIsForwardedOnce()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var reasons = new List<EndReason>(); player.Ended += reasons.Add;
        player.OpenFile("one"); engine.End(EndReason.EndOfFile);
        CollectionAssert.AreEqual(new[] { EndReason.EndOfFile }, reasons);
    }
}
