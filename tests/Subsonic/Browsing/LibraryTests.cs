using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Listening;

namespace LunaPlayer.Tests.Subsonic.Browsing;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class LibraryTests
{
    [TestMethod]
    public async Task MusicAndMetadataOperationsDecodeTheirDocumentedContainers()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        Assert.AreEqual("1.16.1", (await client.PingAsync()).Version);
        Assert.IsTrue((await client.GetLicenseAsync()).Valid);
        Assert.AreEqual("user", (await client.GetUserAsync("user")).Username);
        await client.GetTokenInfoAsync();
        Assert.AreEqual("0", (await client.Library.GetMusicFoldersAsync())[0].Id);
        Assert.AreEqual("artist", (await client.Library.GetArtistsAsync("0")).Indexes[0].Artists[0].Id);
        await client.Library.GetIndexesAsync("0", 123);
        await client.Library.GetDirectoryAsync("dir");
        Assert.AreEqual("album", (await client.Library.GetArtistAsync("artist")).Albums[0].Id);
        Assert.AreEqual(42.5, (await client.Library.GetAlbumAsync("album")).Songs[0].Duration);
        Assert.AreEqual("s", (await client.Library.GetSongAsync("s")).Id);
        await client.Library.GetGenresAsync();
        foreach (var id3 in new[] { false, true })
        {
            await client.Library.GetAlbumsAsync(id3: id3);
            await client.Library.SearchAsync("title & artist", id3: id3);
            await client.Library.GetStarredAsync("0", id3);
            await client.Library.GetArtistInfoAsync("artist", id3: id3);
            await client.Library.GetAlbumInfoAsync("album", id3);
            await client.Library.GetSimilarSongsAsync("s", id3: id3);
        }
        await client.Library.GetRandomSongsAsync(10, "Rock", 1980, 2000, "0");
        await client.Library.GetSongsByGenreAsync("Rock", 10, 20, "0");
        await client.Library.GetTopSongsAsync("Name");
        await client.Library.GetTopSongsAsync("artist", byId: true);
        Assert.AreEqual("s", (await client.Library.GetSonicSimilarTracksAsync("s"))[0].Song!.Id);
        Assert.AreEqual(0.8, (await client.Library.FindSonicPathAsync("a", "b"))[0].Similarity);
        var indexes = handler.Requests.Single(x => x.Endpoint == "getIndexes").Parameters;
        Assert.AreEqual("123", indexes["ifModifiedSince"][0]);
        Assert.AreEqual("0", indexes["musicFolderId"][0]);
        Assert.AreEqual("title & artist", handler.Requests.First(x => x.Endpoint == "search3").Parameters["query"][0]);
        Assert.AreEqual("artist", handler.Requests.Last(x => x.Endpoint == "getTopSongs").Parameters["id"][0]);
        Assert.IsFalse(handler.Requests.Last(x => x.Endpoint == "getTopSongs").Parameters.ContainsKey("artistId"));
    }

    [TestMethod]
    public async Task PagingPreservesCaseSensitiveIdsAndStopsWhenServerRepeatsAPage()
    {
        using var handler = new Handler((sent, _) => Task.FromResult(Fixture.Json(
            "\"searchResult3\":{\"song\":[{\"id\":\"a\"},{\"id\":\"A\"}]}")));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var ids = new List<string>();
        await foreach (var song in client.Library.EnumerateSongsAsync("lib & 1", 2)) ids.Add(song.Id);
        CollectionAssert.AreEqual(new[] { "a", "A" }, ids);
        CollectionAssert.AreEqual(new[] { "0", "2" }, handler.Requests.Select(x => x.Parameters["songOffset"][0]).ToArray());
        Assert.IsTrue(handler.Requests.All(x => x.Parameters["artistCount"][0] == "0"
            && x.Parameters["albumCount"][0] == "0" && x.Parameters["musicFolderId"][0] == "lib & 1"));
    }

    [TestMethod]
    public async Task SelectionExpansionPreservesPlaylistOccurrencesButRemovesOverlaps()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var ids = new List<string>();
        await foreach (var song in client.Selections.ResolveAsync([
            new(SelectionKind.Playlist, "p"), new(SelectionKind.Artist, "artist"), new(SelectionKind.Song, "s")]))
            ids.Add(song.Id);
        CollectionAssert.AreEqual(new[] { "s", "s" }, ids);
        Assert.IsTrue(handler.Requests.Any(x => x.Endpoint == "getArtist"));
        Assert.IsTrue(handler.Requests.Any(x => x.Endpoint == "getAlbum"));
    }

    [TestMethod]
    public async Task PlaylistMutationsRetainOrderDuplicatesAndOccurrenceIndexes()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await client.Playlists.GetAllAsync();
        Assert.AreEqual(2, (await client.Playlists.GetAsync("p")).Entries.Length);
        await client.Playlists.CreateAsync("Evening", ["s", "b", "s"]);
        await client.Playlists.ReplaceAsync("p", ["b", "s", "b"]);
        await client.Playlists.UpdateAsync("p", comment: "", isPublic: false, add: ["x", "x"], removeIndexes: [2, 0]);
        await client.Playlists.DeleteAsync("p");
        var creates = handler.Requests.Where(x => x.Endpoint == "createPlaylist").ToArray();
        CollectionAssert.AreEqual(new[] { "s", "b", "s" }, creates[0].Parameters["songId"]);
        Assert.IsFalse(creates[1].Parameters.ContainsKey("name"));
        Assert.AreEqual("p", creates[1].Parameters["playlistId"][0]);
        var update = handler.Requests.Single(x => x.Endpoint == "updatePlaylist").Parameters;
        CollectionAssert.AreEqual(new[] { "2", "0" }, update["songIndexToRemove"]);
        CollectionAssert.AreEqual(new[] { "x", "x" }, update["songIdToAdd"]);
        Assert.AreEqual("false", update["public"][0]);
        Assert.AreEqual("", update["comment"][0]);
    }

    [TestMethod]
    public async Task ListeningRequestsKeepMillisecondsAndDisambiguateDuplicateQueueEntries()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await client.Listening.SetStarredAsync(["s", "a"], true);
        await client.Listening.SetStarredAsync(["album"], false, StarKind.Album);
        await client.Listening.SetStarredAsync(["artist"], true, StarKind.Artist);
        await client.Listening.SetRatingAsync("s", 0);
        await client.Listening.ScrobbleAsync("s", true, DateTimeOffset.FromUnixTimeMilliseconds(123456));
        await client.Listening.ReportPlaybackAsync("s", 2345, PlaybackState.Paused, 1.25);
        await client.Listening.GetNowPlayingAsync();
        await client.Listening.GetBookmarksAsync();
        await client.Listening.CreateBookmarkAsync("s", 3456, "Here");
        await client.Listening.DeleteBookmarkAsync("s");
        await client.Listening.GetQueueAsync();
        await client.Listening.GetQueueAsync(true);
        await client.Listening.SaveQueueAsync(["a", "b"], "b", 1234);
        await client.Listening.SaveQueueAsync(["s", "s"], positionMs: 4567, currentIndex: 1, byIndex: true);
        await client.Listening.SaveQueueAsync([], byIndex: true);
        Assert.AreEqual("Lyrics", (await client.Listening.GetLyricsAsync("Artist", "Title")).Value);
        Assert.AreEqual("Line", (await client.Listening.GetLyricsBySongIdAsync("s"))[0].Lines[0].Value);
        var report = handler.Requests.Single(x => x.Endpoint == "reportPlayback");
        Assert.AreEqual("", report.Body);
        Assert.AreEqual("paused", report.Parameters["state"][0]);
        Assert.AreEqual("2345", report.Parameters["positionMs"][0]);
        Assert.AreEqual("1.25", report.Parameters["playbackRate"][0]);
        Assert.AreEqual("true", report.Parameters["ignoreScrobble"][0]);
        Assert.AreEqual("123456", handler.Requests.Single(x => x.Endpoint == "scrobble").Parameters["time"][0]);
        var saves = handler.Requests.Where(x => x.Endpoint == "savePlayQueueByIndex").ToArray();
        Assert.AreEqual("1", saves[0].Parameters["currentIndex"][0]);
        CollectionAssert.AreEqual(new[] { "s", "s" }, saves[0].Parameters["id"]);
        Assert.AreEqual("4567", saves[0].Parameters["position"][0]);
        Assert.IsFalse(saves[1].Parameters.ContainsKey("currentIndex"));
        Assert.IsFalse(saves[1].Parameters.ContainsKey("id"));
    }

    [TestMethod]
    public void ListeningThresholdUsesActualListeningAndSubmitsOnlyOnce()
    {
        var tracker = new PlayTracker();
        Assert.IsFalse(tracker.AddListeningTime(TimeSpan.FromSeconds(49), TimeSpan.FromSeconds(100)));
        Assert.IsTrue(tracker.AddListeningTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(100)));
        Assert.IsFalse(tracker.AddListeningTime(TimeSpan.FromHours(1), null));
        tracker.Reset();
        Assert.IsFalse(tracker.AddListeningTime(TimeSpan.FromSeconds(239), null));
        Assert.IsTrue(tracker.AddListeningTime(TimeSpan.FromSeconds(1), null));
    }
}
