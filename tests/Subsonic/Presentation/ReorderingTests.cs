using LunaPlayer.UI.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class ReorderingTests
{
    [TestMethod]
    public void MovingAGroupPreservesItsOrderAndFocusedSong()
    {
        var entries = new List<string> { "a", "b", "c", "d" };
        var moved = Reordering.Move(entries, [1, 2], -1, 2)!;
        CollectionAssert.AreEqual(new[] { "b", "c", "a", "d" }, entries);
        CollectionAssert.AreEqual(new[] { 0, 1 }, moved.Selected); Assert.AreEqual(1, moved.Focused);
        var restored = Reordering.Move(entries, moved.Selected, 1, moved.Focused)!;
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, entries); Assert.AreEqual(2, restored.Focused);
    }

    [TestMethod]
    public void AnUnselectedFocusedRowFollowsItsSwapWithoutResettingToTheFirstItem()
    {
        var entries = new List<string> { "a", "same", "c", "same" };
        var moved = Reordering.Move(entries, [1, 3], -1, 2)!;
        CollectionAssert.AreEqual(new[] { "same", "a", "same", "c" }, entries);
        Assert.AreEqual(3, moved.Focused); CollectionAssert.AreEqual(new[] { 0, 2 }, moved.Selected);
    }

    [TestMethod]
    public void MovingPastTheBoundaryDoesNotChangeEntriesOrSelection()
    {
        var entries = new List<string> { "a", "b" };
        Assert.IsNull(Reordering.Move(entries, [0], -1, 0));
        Assert.IsNull(Reordering.Move(entries, [1], 1, 1));
        CollectionAssert.AreEqual(new[] { "a", "b" }, entries);
    }
}
