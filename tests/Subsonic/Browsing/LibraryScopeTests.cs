using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.Tests.Subsonic.Browsing;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class LibraryScopeTests
{
    [TestMethod]
    public async Task ArtistDrilldownAndExpansionRespectLibraryBoundaries()
    {
        using var handler = new Handler((sent, _) => Task.FromResult(Fixture.Json(sent.Endpoint switch
        {
            "getArtist" => "\"artist\":{\"id\":\"artist\",\"album\":[{\"id\":\"a\"},{\"id\":\"b\"}]}",
            "getAlbumList2" => "\"albumList2\":{\"album\":[{\"id\":\"" + sent.Parameters["musicFolderId"][0] + "\"}]}",
            "getAlbum" => "\"album\":{\"id\":\"" + sent.Parameters["id"][0] + "\",\"song\":[{\"id\":\"song-" + sent.Parameters["id"][0] + "\"}]}",
            _ => ""
        })));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var catalog = new Catalog(client); var token = CancellationToken.None;
        var albums = await catalog.ReadAsync(new(Category.Artists, RowKind.Artist, "artist"), "", 0, null, null, token, ["a"]);
        CollectionAssert.AreEqual(new[] { "a" }, albums.Select(row => row.Id).ToArray());
        var songs = await catalog.ExpandAsync([new(RowKind.Artist, "artist", "Artist")], token, ["a"]);
        CollectionAssert.AreEqual(new[] { "song-a" }, songs.Select(row => row.Id).ToArray());
        Assert.AreEqual(1, handler.Requests.Count(request => request.Endpoint == "getAlbumList2"));
        await catalog.InvalidateAsync();
        await catalog.ReadAsync(new(Category.Artists, RowKind.Artist, "artist"), "", 0, null, null, token, ["a"]);
        Assert.AreEqual(2, handler.Requests.Count(request => request.Endpoint == "getAlbumList2"));
    }

    [TestMethod]
    public async Task MultipleLibrariesMergeWithoutDuplicatingSharedSearchResults()
    {
        using var handler = new Handler((sent, _) => Task.FromResult(Fixture.Json("\"searchResult3\":{\"song\":[{\"id\":\"shared\"},{\"id\":\""
            + sent.Parameters["musicFolderId"][0] + "\"}]}")));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var rows = await new Catalog(client).ReadAsync(new(Category.Songs), "query", 3, null, null, CancellationToken.None, ["a", "b", "a"]);
        CollectionAssert.AreEqual(new[] { "shared", "a", "b" }, rows.Select(row => row.Id).ToArray());
        Assert.AreEqual(2, handler.Requests.Count);
    }

    [TestMethod]
    public async Task ExplicitPlaylistOccurrencesKeepTheirPositionsAndDuplicates()
    {
        using var handler = new Handler(Fixture.Reply); using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var catalog = new Catalog(client);
        var rows = await catalog.ReadAsync(new(Category.Playlists, RowKind.Playlist, "p"), "", 0, null, null, CancellationToken.None);
        Assert.AreEqual(0, rows[0].PlaylistIndex); Assert.AreEqual(1, rows[1].PlaylistIndex);
        var expanded = await catalog.ExpandAsync(rows, CancellationToken.None);
        Assert.AreEqual(2, expanded.Length);
    }
}
