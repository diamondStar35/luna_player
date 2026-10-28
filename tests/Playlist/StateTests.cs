using LunaPlayer.Playlist;
using LunaPlayer.Media;
namespace LunaPlayer.Tests.Playlist;

[TestClass, TestCategory("Unit")]
public sealed class StateTests
{
    [TestMethod]
    public void EmptyOpenDoesNotReplaceTheCurrentList()
    {
        var state = new PlaylistState();
        state.OpenFiles(["one.mp3", "two.mp3"]);
        Assert.IsFalse(state.OpenFiles([]));
        Assert.AreEqual(2, state.Count);
        Assert.AreEqual("one.mp3", state.CurrentPath);
    }
    [TestMethod]
    public void DuplicatePathsHaveDistinctOccurrenceIdentities()
    {
        var state = new PlaylistState();
        state.OpenFiles(["one.mp3", "one.mp3"]);
        Assert.AreEqual(2, state.Count);
        Assert.AreNotEqual(state.Items[0].Id, state.Items[1].Id);
    }
    [TestMethod]
    [DataRow(0, false, false, 0)]
    [DataRow(0, true, false, 2)]
    [DataRow(2, false, true, 2)]
    [DataRow(2, true, true, 0)]
    public void NavigationRespectsWrap(int index, bool wrap, bool next, int expected)
    {
        var state = new PlaylistState();
        state.OpenFiles(["one", "two", "three"]);
        state.GoToIndex(index);
        if (next) state.MoveNext(wrap); else state.MovePrevious(wrap);
        Assert.AreEqual(expected, state.CurrentIndex);
    }
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AppendRetainsCurrentIdentityAndExistingOrder(bool shuffle)
    {
        var state = new PlaylistState();
        state.OpenFiles(["one", "two", "three"]);
        if (shuffle) state.ToggleShuffle();
        var current = state.CurrentItem!.Id;
        var order = state.Items.Select(item => item.Id).ToArray();
        state.Append("four", jump: false);
        Assert.AreEqual(current, state.CurrentItem!.Id);
        CollectionAssert.AreEqual(order, state.Items.Where(item => order.Contains(item.Id)).Select(item => item.Id).ToArray());
    }
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RemovingTheCurrentItemChoosesItsSuccessor(bool shuffle)
    {
        var state = new PlaylistState();
        state.OpenFiles(["one", "two", "three", "four"]);
        if (shuffle) state.ToggleShuffle();
        state.GoToIndex(1);
        var next = state.Items[2].Id;
        state.RemoveCurrent();
        Assert.AreEqual(next, state.CurrentItem!.Id);
        Assert.AreEqual(3, state.Count);
    }
    [TestMethod]
    public void MarksArePerPathAndClearedWithTheList()
    {
        var state = new PlaylistState();
        state.OpenFiles(["one", "one", "two"]);
        state.ToggleAllMarked();
        Assert.AreEqual(2, state.MarkedCount);
        state.RemovePaths(["one"]);
        Assert.AreEqual(1, state.MarkedCount);
        state.ClearAll();
        Assert.AreEqual(0, state.Count);
        Assert.AreEqual(0, state.MarkedCount);
    }
    [TestMethod]
    public void ChaptersExpandAndCollapseWithoutLosingTheSource()
    {
        var state = new PlaylistState();
        state.OpenFiles(["book.m4b", "other.mp3"]);
        state.SetChapters("book.m4b", [new Chapter(0, "One", 0, 10), new Chapter(1, "Two", 10, 20)], 0);
        state.SetAudiobookMode(true, 0);
        Assert.AreEqual(3, state.Count);
        Assert.IsTrue(state.GoToChapter(1));
        Assert.AreEqual(1, state.CurrentItem!.Chapter!.Index);
        state.SetAudiobookMode(false, 12);
        Assert.AreEqual(2, state.Count);
        Assert.AreEqual("book.m4b", state.CurrentPath);
    }
    [TestMethod]
    public void PendingStartIsConsumedOnce()
    {
        var state = new PlaylistState();
        state.OpenFiles(["one", "two"], "two", 12.5);
        Assert.AreEqual(12.5, state.TakePendingStart());
        Assert.IsNull(state.TakePendingStart());
    }
}
