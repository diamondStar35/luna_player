using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.UI.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class ListPositionTests
{
    [TestMethod]
    public void MetadataRefreshKeepsTheExistingVirtualSourceWhenRowOrderIsUnchanged()
    {
        Row[] before = [new(RowKind.Song, "a", "A"), new(RowKind.Song, "b", "B")];
        Row[] after = [before[0] with { Rating = 5 }, before[1] with { Name = "Updated B" }];
        Assert.IsTrue(ListPosition.SameRows(before, after));
        Assert.IsFalse(ListPosition.SameRows(before, after.Reverse().ToArray()));
        Assert.IsFalse(ListPosition.SameRows(before, [after[0]]));
    }

    [TestMethod]
    public void SortingRestoresKeyboardFocusSelectionAndVisibleItemByIdentity()
    {
        Row[] before = [new(RowKind.Song, "a", "A"), new(RowKind.Song, "b", "B"), new(RowKind.Song, "c", "C")];
        var position = ListPosition.Capture(before, [1, 2], 2, 1);
        var restored = position.Restore([before[2], before[0], before[1]]);
        CollectionAssert.AreEqual(new[] { 0, 2 }, restored.Selected);
        Assert.AreEqual(0, restored.Focused); Assert.AreEqual(2, restored.Top);
    }

    [TestMethod]
    public void DuplicatePlaylistOccurrencesAndRemovedRowsAreHandledIndependently()
    {
        Row[] before = [new(RowKind.Song, "a", "A", PlaylistIndex: 0), new(RowKind.Song, "a", "A", PlaylistIndex: 1)];
        var position = ListPosition.Capture(before, [1], 1, 0);
        var restored = position.Restore([before[1], before[0]]);
        CollectionAssert.AreEqual(new[] { 0 }, restored.Selected); Assert.AreEqual(0, restored.Focused); Assert.AreEqual(1, restored.Top);
        restored = position.Restore([]);
        Assert.IsEmpty(restored.Selected); Assert.AreEqual(-1, restored.Focused); Assert.AreEqual(-1, restored.Top);
        Assert.IsFalse(ListPosition.SameRows(before, [before[1], before[0]]));
    }
}
