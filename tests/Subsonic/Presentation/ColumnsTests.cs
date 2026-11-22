using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.UI.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class ColumnsTests
{
    [TestMethod]
    public void SourceQualityUsesAvailableMetadataAndNeverInventsTranscodeValues()
    {
        var row = Row.From(new Entry { Id = "song", Suffix = "flac", SamplingRate = 96000, BitDepth = 24, ChannelCount = 2 });
        var quality = Quality.From(row.Song);
        Assert.AreEqual("FLAC", quality.Format); Assert.AreEqual(96000, quality.SampleRate); Assert.AreEqual(24, quality.BitDepth);
        Assert.IsNull(quality.BitRate); Assert.AreEqual(2, quality.Channels);
        var missing = Quality.From(null);
        Assert.AreEqual("", missing.Format); Assert.IsNull(missing.SampleRate); Assert.IsNull(missing.BitDepth);
    }

    [TestMethod]
    public void CollectionsAndUnavailableItemsHaveRelevantColumns()
    {
        var artists = Columns.For(new(Category.Artists), [new(RowKind.Artist, "a", "Artist", Count: 2)], false);
        Assert.Contains(Column.Albums, artists); Assert.DoesNotContain(Column.Quality, artists);
        var albums = Columns.For(new(Category.Artists, RowKind.Artist, "a"), [new(RowKind.Album, "b", "Album")], false);
        Assert.Contains(Column.Songs, albums); Assert.DoesNotContain(Column.Albums, albums);
        var songs = Columns.For(new(Category.Artists), [new(RowKind.Song, "s", "Song", Available: false)], false);
        Assert.Contains(Column.Quality, songs); Assert.Contains(Column.Status, songs);
        var bookmarks = Columns.For(new(Category.Bookmarks), [new(RowKind.Bookmark, "s", "Song", Position: 90000)], false);
        Assert.Contains(Column.Position, bookmarks);
        Assert.AreEqual("1:30", Columns.Cell(new(RowKind.Bookmark, "s", "Song", Position: 90000), Column.Position));
    }

    [TestMethod]
    public void QualitySortingKeepsZeroAndMissingValuesLast()
    {
        Row[] rows = [Row.From(new Entry { Id = "zero", BitRate = 0 }), Row.From(new Entry { Id = "high", BitRate = 320 }),
            Row.From(new Entry { Id = "low", BitRate = 128 }), new(RowKind.Song, "unknown", "Unknown")];
        CollectionAssert.AreEqual(new[] { "low", "high", "zero", "unknown" }, Ordering.Sort(rows, SortField.BitRate, false).Select(row => row.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "high", "low", "zero", "unknown" }, Ordering.Sort(rows, SortField.BitRate, true).Select(row => row.Id).ToArray());
    }
}
