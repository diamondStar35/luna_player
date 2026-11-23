using System.Runtime.CompilerServices;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic.Browsing;

internal sealed class Library(Session session, Func<string, int, CancellationToken, Task> require) : Feature(session)
{
    internal async Task<MusicFolder[]> GetMusicFoldersAsync(CancellationToken token = default)
        => Required((await Session.ReadAsync("getMusicFolders", token: token).ConfigureAwait(false)).MusicFolders, "getMusicFolders").MusicFolders;
    internal async Task<Artists> GetArtistsAsync(string? musicFolderId = null, CancellationToken token = default)
        => Required((await Session.ReadAsync("getArtists", new Parameters().With("musicFolderId", musicFolderId), token).ConfigureAwait(false)).Artists, "getArtists");
    internal async Task<Artists> GetIndexesAsync(string? musicFolderId = null, long? ifModifiedSince = null, CancellationToken token = default)
        => Required((await Session.ReadAsync("getIndexes", new Parameters().With("musicFolderId", musicFolderId).With("ifModifiedSince", ifModifiedSince), token).ConfigureAwait(false)).Indexes, "getIndexes");
    internal async Task<Models.Directory> GetDirectoryAsync(string id, CancellationToken token = default)
        => Required((await Session.ReadAsync("getMusicDirectory", new Parameters().With("id", Id(id)), token).ConfigureAwait(false)).Directory, "getMusicDirectory");
    internal async Task<Artist> GetArtistAsync(string id, CancellationToken token = default)
        => Required((await Session.ReadAsync("getArtist", new Parameters().With("id", Id(id)), token).ConfigureAwait(false)).Artist, "getArtist");
    internal async Task<Album> GetAlbumAsync(string id, CancellationToken token = default)
        => Required((await Session.ReadAsync("getAlbum", new Parameters().With("id", Id(id)), token).ConfigureAwait(false)).Album, "getAlbum");
    internal async Task<Entry> GetSongAsync(string id, CancellationToken token = default)
        => Required((await Session.ReadAsync("getSong", new Parameters().With("id", Id(id)), token).ConfigureAwait(false)).Song, "getSong");
    internal async Task<Genre[]> GetGenresAsync(CancellationToken token = default)
        => Required((await Session.ReadAsync("getGenres", token: token).ConfigureAwait(false)).Genres, "getGenres").Genres;
    internal async Task<Album[]> GetAlbumsAsync(AlbumQuery? query = null, bool id3 = true, CancellationToken token = default)
    {
        var method = id3 ? "getAlbumList2" : "getAlbumList";
        var result = await Session.ReadAsync(method, (query ?? new()).Parameters(), token).ConfigureAwait(false);
        return Required(id3 ? result.AlbumList2 : result.AlbumList, method).Albums;
    }
    internal async Task<SearchResult> SearchAsync(string query, SearchOptions? options = null, bool id3 = true, CancellationToken token = default)
    {
        var method = id3 ? "search3" : "search2";
        var result = await Session.ReadAsync(method, (options ?? new()).Parameters(query), token).ConfigureAwait(false);
        return Required(id3 ? result.SearchResult3 : result.SearchResult2, method);
    }
    internal async Task<SearchResult> GetStarredAsync(string? musicFolderId = null, bool id3 = true, CancellationToken token = default)
    {
        var method = id3 ? "getStarred2" : "getStarred";
        var result = await Session.ReadAsync(method, new Parameters().With("musicFolderId", musicFolderId), token).ConfigureAwait(false);
        return Required(id3 ? result.Starred2 : result.Starred, method);
    }
    internal async Task<Entry[]> GetRandomSongsAsync(int count = 50, string? genre = null, int? fromYear = null,
        int? toYear = null, string? musicFolderId = null, CancellationToken token = default)
        => Required((await Session.ReadAsync("getRandomSongs", new Parameters().With("size", Count(count)).With("genre", genre)
            .With("fromYear", fromYear).With("toYear", toYear).With("musicFolderId", musicFolderId), token).ConfigureAwait(false)).RandomSongs, "getRandomSongs").Songs;
    internal async Task<Entry[]> GetSongsByGenreAsync(string genre, int count = 100, int offset = 0,
        string? musicFolderId = null, CancellationToken token = default)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        return Required((await Session.ReadAsync("getSongsByGenre", new Parameters().With("genre", Id(genre))
            .With("count", Count(count)).With("offset", offset).With("musicFolderId", musicFolderId), token).ConfigureAwait(false)).SongsByGenre, "getSongsByGenre").Songs;
    }
    internal async Task<ArtistInfo> GetArtistInfoAsync(string id, int count = 20, bool includeNotPresent = false,
        bool id3 = true, CancellationToken token = default)
    {
        var method = id3 ? "getArtistInfo2" : "getArtistInfo";
        var response = await Session.ReadAsync(method, new Parameters().With("id", Id(id)).With("count", Count(count))
            .With("includeNotPresent", includeNotPresent), token).ConfigureAwait(false);
        return Required(id3 ? response.ArtistInfo2 : response.ArtistInfo, method);
    }
    internal async Task<AlbumInfo> GetAlbumInfoAsync(string id, bool id3 = true, CancellationToken token = default)
    {
        var method = id3 ? "getAlbumInfo2" : "getAlbumInfo";
        var response = await Session.ReadAsync(method, new Parameters().With("id", Id(id)), token).ConfigureAwait(false);
        return Required(id3 ? response.AlbumInfo2 : response.AlbumInfo, method);
    }
    internal async Task<Entry[]> GetTopSongsAsync(string artist, int count = 50, bool byId = false, CancellationToken token = default)
    {
        if (byId) await require("topSongsByArtistId", 1, token).ConfigureAwait(false);
        return Required((await Session.ReadAsync("getTopSongs", new Parameters().With(byId ? "id" : "artist", Id(artist))
            .With("count", Count(count)), token).ConfigureAwait(false)).TopSongs, "getTopSongs").Songs;
    }
    internal async Task<Entry[]> GetSimilarSongsAsync(string id, int count = 50, bool id3 = true, CancellationToken token = default)
    {
        var method = id3 ? "getSimilarSongs2" : "getSimilarSongs";
        var response = await Session.ReadAsync(method, new Parameters().With("id", Id(id)).With("count", Count(count)), token).ConfigureAwait(false);
        return Required(id3 ? response.SimilarSongs2 : response.SimilarSongs, method).Songs;
    }
    internal async Task<SonicMatch[]> GetSonicSimilarTracksAsync(string id, int count = 10, CancellationToken token = default)
    {
        await require("sonicSimilarity", 1, token).ConfigureAwait(false);
        return (await Session.ReadAsync("getSonicSimilarTracks", new Parameters().With("id", Id(id))
            .With("count", Count(count)), token).ConfigureAwait(false)).SonicMatches;
    }
    internal async Task<SonicMatch[]> FindSonicPathAsync(string startSongId, string endSongId, int count = 25, CancellationToken token = default)
    {
        await require("sonicSimilarity", 1, token).ConfigureAwait(false);
        return (await Session.ReadAsync("findSonicPath", new Parameters().With("startSongId", Id(startSongId))
            .With("endSongId", Id(endSongId)).With("count", Count(count)), token).ConfigureAwait(false)).SonicMatches;
    }
    internal async IAsyncEnumerable<Entry> EnumerateSongsAsync(string? musicFolderId = null, int pageSize = 500,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        Count(pageSize);
        var offset = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            var result = await SearchAsync("", new SearchOptions { ArtistCount = 0, AlbumCount = 0,
                SongCount = pageSize, SongOffset = offset, MusicFolderId = musicFolderId }, token: token).ConfigureAwait(false);
            var added = 0;
            foreach (var song in result.Songs)
                if (seen.Add(song.Id)) { token.ThrowIfCancellationRequested(); added++; yield return song; }
            if (result.Songs.Length < pageSize || added == 0) yield break;
            offset = checked(offset + result.Songs.Length);
        }
    }
    internal static int Count(int count)
    {
        if (count is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(count));
        return count;
    }
}
