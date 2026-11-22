using LunaPlayer.Actions;
using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.UI.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class BindingsTests
{
    [TestMethod]
    public void ContextLabelsAndInspectionAgreeForSongsAndCollections()
    {
        Row[] songs = [new(RowKind.Song, "s", "Song")];
        Assert.AreEqual(Command.Details, Bindings.Inspect(songs));
        Assert.AreEqual(ActionId.ServerPlay, Bindings.ForMenu(Command.Play, songs));
        Assert.AreEqual(ActionId.ServerDetails, Bindings.ForMenu(Command.Details, songs));
        foreach (var kind in new[] { RowKind.Artist, RowKind.Album, RowKind.Playlist })
        {
            Row[] rows = [new(kind, "id", "Collection")];
            Assert.AreEqual(Command.Play, Bindings.Inspect(rows));
            Assert.AreEqual(ActionId.ServerDetails, Bindings.ForMenu(Command.Play, rows));
            Assert.IsNull(Bindings.ForMenu(Command.Details, rows));
        }
    }
}
