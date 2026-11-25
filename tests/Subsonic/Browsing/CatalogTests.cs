using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.Tests.Subsonic.Browsing;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class CatalogTests
{
    [TestMethod]
    public async Task SearchCollectsAllPagesBeforeSortingNumerically()
    {
        using var handler = new Handler((sent, _) =>
        {
            var offset = int.Parse(sent.Parameters["songOffset"][0]);
            var songs = Enumerable.Range(offset, offset == 0 ? 500 : 1).Select(i => "{\"id\":\"" + i + "\",\"title\":\"Song " + i + "\",\"duration\":" + (501 - i) + "}");
            return Task.FromResult(Fixture.Json("\"searchResult3\":{\"song\":[" + string.Join(",", songs) + "]}"));
        });
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var rows = await new Catalog(client).ReadAsync(new(Category.Songs), "song", 3, null, null, CancellationToken.None);
        Assert.AreEqual(501, rows.Length); Assert.AreEqual(2, handler.Requests.Count);
        var sorted = Ordering.Sort(rows, SortField.Duration, false);
        Assert.AreEqual("500", sorted[0].Id); Assert.AreEqual("0", sorted[^1].Id);
        Assert.IsTrue(handler.Requests.All(x => x.Parameters["artistCount"][0] == "0" && x.Parameters["albumCount"][0] == "0"));
    }
    [TestMethod]
    public async Task GenrePagingStopsWhenServerIgnoresOffset()
    {
        var songs = string.Join(",", Enumerable.Range(0, 500).Select(i => "{\"id\":\"" + i + "\"}"));
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Json("\"songsByGenre\":{\"song\":[" + songs + "]}")));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var rows = await new Catalog(client).ReadAsync(new(Category.Genres, RowKind.Genre, "Jazz"), "", 0, null, null, CancellationToken.None);
        Assert.AreEqual(500, rows.Length); Assert.AreEqual(2, handler.Requests.Count);
    }
    [TestMethod]
    public async Task CancellationPreventsFetchingTheNextPage()
    {
        using var cancellation = new CancellationTokenSource();
        var songs = string.Join(",", Enumerable.Range(0, 500).Select(i => "{\"id\":\"" + i + "\"}"));
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Json("\"searchResult3\":{\"song\":[" + songs + "]}")));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        await Assert.ThrowsAsync<OperationCanceledException>(() => new Catalog(client).ReadAsync(new(Category.Songs), "test", 3, null,
            new InlineProgress<int>(_ => cancellation.Cancel()), cancellation.Token));
        Assert.AreEqual(1, handler.Requests.Count);
    }
    [TestMethod]
    public async Task ExpandingPlaylistRetainsDuplicatesAndAvoidsEarlierOverlaps()
    {
        using var handler = new Handler(Fixture.Reply); using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var catalog = new Catalog(client);
        var duplicates = await catalog.ExpandAsync([new(RowKind.Playlist, "p", "Playlist")], CancellationToken.None);
        Assert.AreEqual(2, duplicates.Length);
        var overlaps = await catalog.ExpandAsync([new(RowKind.Song, "s", "Song"), new(RowKind.Playlist, "p", "Playlist")], CancellationToken.None);
        Assert.AreEqual(1, overlaps.Length);
    }
    [TestMethod]
    public async Task MissingAndNullOptionalMetadataKeepSafeDefaults()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Json("\"song\":{\"id\":\"s\",\"artist\":null,\"displayArtist\":null}")));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var song = await client.Library.GetSongAsync("s"); var row = Row.From(song);
        Assert.AreEqual("", row.Name); Assert.AreEqual("", row.Artist); Assert.AreEqual("", row.Album);
        Assert.IsEmpty(song.Artists); Assert.IsEmpty(song.Genres); Assert.IsEmpty(song.Isrc);
    }
    [TestMethod]
    public void AddedDatesAreComparedAsInstantsAcrossTimeZones()
    {
        Row[] rows = [new(RowKind.Album, "earlier", "First", Added: "2026-11-22T10:00:00+02:00"),
            new(RowKind.Album, "later", "Second", Added: "2026-11-22T09:00:00Z"), new(RowKind.Album, "unknown", "Unknown")];
        CollectionAssert.AreEqual(new[] { "earlier", "later", "unknown" }, Ordering.Sort(rows, SortField.Added, false).Select(x => x.Id).ToArray());
    }
    [TestMethod]
    public void SortingKeepsUnknownsLastAndDefaultPlaylistOrderUnchanged()
    {
        Row[] rows = [new(RowKind.Song, "unknown", "Unknown"), new(RowKind.Song, "two", "Two", Duration: 2), new(RowKind.Song, "ten", "Ten", Duration: 10)];
        CollectionAssert.AreEqual(new[] { "ten", "two", "unknown" }, Ordering.Sort(rows, SortField.Duration, true).Select(x => x.Id).ToArray());
        CollectionAssert.AreEqual(rows, Ordering.Sort(rows, SortField.Default, false));
        Row[] tracks = [new(RowKind.Song, "disc2", "D2", Disc: 2, Track: 1), new(RowKind.Song, "disc1", "D1", Disc: 1, Track: 12)];
        Assert.AreEqual("disc1", Ordering.Sort(tracks, SortField.Track, false)[0].Id);
    }
}
