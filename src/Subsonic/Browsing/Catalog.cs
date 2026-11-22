using System.Runtime.CompilerServices;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Browsing;

/// <summary>Fetches complete view collections. Paging and presentation sorting never change a server playlist.</summary>
internal sealed partial class Catalog(Client client)
{
    internal async Task<Row[]> ReadAsync(Location location, string query, int type, string? library,
        IProgress<int>? progress, CancellationToken token, IReadOnlyList<string>? libraries = null)
    {
        if (library is null && libraries is { Count: > 0 } && IsLibraryView(location, query))
            return await ReadLibrariesAsync(location, query, type, libraries, progress, token).ConfigureAwait(false);
        var rows = new List<Row>();
        void Add(IEnumerable<Row> added) { rows.AddRange(added); progress?.Report(rows.Count); token.ThrowIfCancellationRequested(); }
        if (query.Length > 0)
        {
            var seen = new HashSet<(RowKind, string)>();
            for (var offset = 0; ; offset += 500)
            {
                var page = await client.Library.SearchAsync(query, new()
                {
                    ArtistCount = type is 0 or 1 ? 500 : 0, AlbumCount = type is 0 or 2 ? 500 : 0,
                    SongCount = type is 0 or 3 ? 500 : 0, ArtistOffset = offset, AlbumOffset = offset,
                    SongOffset = offset, MusicFolderId = library
                }, token: token).ConfigureAwait(false);
                var next = page.Artists.Select(Row.From).Concat(page.Albums.Select(Row.From)).Concat(page.Songs.Select(Row.From)).ToArray();
                var fresh = next.Where(x => seen.Add((x.Kind, x.Id))).ToArray();
                Add(fresh);
                if (fresh.Length == 0 || page.Artists.Length < 500 && page.Albums.Length < 500 && page.Songs.Length < 500) break;
            }
            return rows.ToArray();
        }
        if (location.Container is RowKind.Artist)
            return (await ArtistAlbumsAsync(location.Id!, library, token).ConfigureAwait(false)).Select(Row.From).ToArray();
        if (location.Container is RowKind.Album)
        {
            if (library is not null && !(await LibraryAlbumsAsync(library, token).ConfigureAwait(false)).Contains(location.Id!)) return [];
            return (await client.Library.GetAlbumAsync(location.Id!, token).ConfigureAwait(false)).Songs.Select(Row.From).ToArray();
        }
        if (location.Container is RowKind.Playlist)
            return (await client.Playlists.GetAsync(location.Id!, token).ConfigureAwait(false)).Entries.Select((entry, index) => Row.From(entry) with { PlaylistIndex = index }).ToArray();
        if (location.Container is RowKind.PodcastChannel)
            return (await client.Podcasts.GetChannelsAsync(location.Id, token: token).ConfigureAwait(false)).SelectMany(x => x.Episodes)
                .Select(Episode).ToArray();
        if (location.Container is RowKind.Genre)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var offset = 0; ; offset += 500)
            {
                var page = await client.Library.GetSongsByGenreAsync(location.Id!, 500, offset, library, token).ConfigureAwait(false);
                var fresh = page.Where(x => seen.Add(x.Id)).Select(Row.From).ToArray();
                Add(fresh); if (page.Length < 500 || fresh.Length == 0) break;
            }
            return rows.ToArray();
        }
        switch (location.Category)
        {
            case Category.Artists:
                Add((await client.Library.GetArtistsAsync(library, token).ConfigureAwait(false)).Indexes.SelectMany(x => x.Artists).Select(Row.From)); break;
            case Category.Albums: case Category.Added: case Category.Recent: case Category.Frequent:
                var order = location.Category switch { Category.Added => AlbumOrder.Newest, Category.Recent => AlbumOrder.Recent,
                    Category.Frequent => AlbumOrder.Frequent, _ => AlbumOrder.AlphabeticalByName };
                var seenAlbums = new HashSet<string>(StringComparer.Ordinal);
                for (var offset = 0; ; offset += 500)
                {
                    var page = await client.Library.GetAlbumsAsync(new() { Order = order, Size = 500, Offset = offset, MusicFolderId = library }, token: token).ConfigureAwait(false);
                    var next = page.Where(x => seenAlbums.Add(x.Id)).Select(Row.From).ToArray();
                    Add(next); if (page.Length < 500 || next.Length == 0) break;
                }
                break;
            case Category.Songs:
                await foreach (var song in client.Library.EnumerateSongsAsync(library, token: token).ConfigureAwait(false))
                { rows.Add(Row.From(song)); if (rows.Count % 500 == 0) progress?.Report(rows.Count); }
                break;
            case Category.Genres:
                Add((await client.Library.GetGenresAsync(token).ConfigureAwait(false)).Select(x => new Row(RowKind.Genre, x.Value, x.Value))); break;
            case Category.Favorites:
                var favorites = await client.Library.GetStarredAsync(library, token: token).ConfigureAwait(false);
                Add(favorites.Artists.Select(Row.From).Concat(favorites.Albums.Select(Row.From)).Concat(favorites.Songs.Select(Row.From))); break;
            case Category.RandomAlbums:
                Add((await client.Library.GetAlbumsAsync(new() { Order = AlbumOrder.Random, Size = 50, MusicFolderId = library }, token: token).ConfigureAwait(false)).Select(Row.From)); break;
            case Category.Random:
                Add((await client.Library.GetRandomSongsAsync(musicFolderId: library, token: token).ConfigureAwait(false)).Select(Row.From)); break;
            case Category.Playlists:
                Add((await client.Playlists.GetAllAsync(token: token).ConfigureAwait(false)).Select(x => new Row(RowKind.Playlist, x.Id, x.Name,
                    Duration: x.Duration, Description: x.Comment, Owner: x.Owner, Count: x.SongCount))); break;
            case Category.Bookmarks:
                Add((await client.Listening.GetBookmarksAsync(token).ConfigureAwait(false)).Where(x => x.Entry is not null)
                    .Select(x => Row.From(x.Entry!) with { Kind = RowKind.Bookmark, Position = x.Position, Description = x.Comment })); break;
            case Category.NowPlaying:
                Add((await client.Listening.GetNowPlayingAsync(token).ConfigureAwait(false)).Select(x => new Row(RowKind.Song, x.Id, x.Title,
                    x.Artist, x.Album, x.Duration, Description: x.PlayerName, Owner: x.Username))); break;
            case Category.Radio:
                Add((await client.Radio.GetAllAsync(token).ConfigureAwait(false)).Select(x => new Row(RowKind.Radio, x.Id, x.Name, Description: x.HomePageUrl))); break;
            case Category.Podcasts:
                Add((await client.Podcasts.GetChannelsAsync(includeEpisodes: false, token: token).ConfigureAwait(false)).Select(x => new Row(RowKind.PodcastChannel, x.Id, x.Title, Description: x.Description, Status: x.Status))); break;
        }
        return rows.ToArray();
    }
    private static Row Episode(PodcastEpisode episode) => new(RowKind.Podcast, episode.Id, episode.Title,
        Duration: episode.Duration, Description: episode.Description, Added: episode.PublishDate, Status: episode.Status,
        Available: episode.StreamId.Length > 0 && episode.Status is not ("error" or "downloading" or "new" or "skipped"));

    internal async Task<Row[]> ExpandAsync(IEnumerable<Row> selected, CancellationToken token, IReadOnlyList<string>? libraries = null)
    {
        var result = new List<Row>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in selected)
        {
            if (row.Playable) { if (row.PlaylistIndex.HasValue || seen.Add(row.Id)) result.Add(row); seen.Add(row.Id); continue; }
            if (row.Kind == RowKind.Artist && libraries is { Count: > 0 })
            {
                var albums = await ReadLibrariesAsync(new(Category.Artists, RowKind.Artist, row.Id), "", 0, libraries, null, token).ConfigureAwait(false);
                foreach (var album in albums)
                    foreach (var song in (await client.Library.GetAlbumAsync(album.Id, token).ConfigureAwait(false)).Songs)
                        if (seen.Add(song.Id)) result.Add(Row.From(song));
                continue;
            }
            SelectionKind kind = row.Kind switch { RowKind.Artist => SelectionKind.Artist, RowKind.Album => SelectionKind.Album,
                RowKind.Playlist => SelectionKind.Playlist, _ => throw new ArgumentException(nameof(selected)) };
            var current = new HashSet<string>(StringComparer.Ordinal);
            await foreach (var song in client.Selections.ResolveAsync([new(kind, row.Id)], token).ConfigureAwait(false))
                if (!seen.Contains(song.Id)) { result.Add(Row.From(song)); current.Add(song.Id); }
            seen.UnionWith(current);
        }
        return result.ToArray();
    }
}
