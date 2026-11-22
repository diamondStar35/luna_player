using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Browsing;

internal sealed partial class Catalog
{
    private readonly Dictionary<string, (DateTimeOffset Loaded, HashSet<string> Albums)> _libraryAlbums = [];
    private readonly SemaphoreSlim _libraryGate = new(1, 1);

    private static bool IsLibraryView(Location location, string query) => query.Length > 0
        || location.Container is RowKind.Artist or RowKind.Album or RowKind.Genre
        || location.Container is null && location.Category is Category.Artists or Category.Albums or Category.Songs
            or Category.Favorites or Category.Added or Category.Recent or Category.Frequent or Category.Random or Category.RandomAlbums;

    private async Task<Row[]> ReadLibrariesAsync(Location location, string query, int type, IReadOnlyList<string> libraries,
        IProgress<int>? progress, CancellationToken token)
    {
        var result = new List<Row>(); var seen = new HashSet<(RowKind, string)>();
        foreach (var library in libraries.Distinct(StringComparer.Ordinal))
        {
            var rows = await ReadAsync(location, query, type, library, null, token).ConfigureAwait(false);
            foreach (var row in rows) if (seen.Add((row.Kind, row.Id))) result.Add(row);
            progress?.Report(result.Count);
        }
        return result.ToArray();
    }

    private async Task<Album[]> ArtistAlbumsAsync(string artist, string? library, CancellationToken token)
    {
        var albums = (await client.Library.GetArtistAsync(artist, token).ConfigureAwait(false)).Albums;
        if (library is null) return albums;
        // getArtist has no library parameter. Intersect its album IDs with a library-scoped index,
        // rather than allowing an artist present in two libraries to bypass the user's selection.
        var allowed = await LibraryAlbumsAsync(library, token).ConfigureAwait(false);
        return albums.Where(album => allowed.Contains(album.Id)).ToArray();
    }

    private async Task<HashSet<string>> LibraryAlbumsAsync(string library, CancellationToken token)
    {
        await _libraryGate.WaitAsync(token).ConfigureAwait(false);
        HashSet<string> allowed;
        try
        {
            if (_libraryAlbums.TryGetValue(library, out var cached) && DateTimeOffset.UtcNow - cached.Loaded < TimeSpan.FromMinutes(30))
                allowed = cached.Albums;
            else
            {
                allowed = [];
                for (var offset = 0; ; offset += 500)
                {
                    var page = await client.Library.GetAlbumsAsync(new() { Size = 500, Offset = offset,
                        MusicFolderId = library, Order = AlbumOrder.AlphabeticalByName }, token: token).ConfigureAwait(false);
                    var before = allowed.Count;
                    foreach (var album in page) allowed.Add(album.Id);
                    if (page.Length < 500 || allowed.Count == before) break;
                }
                _libraryAlbums[library] = (DateTimeOffset.UtcNow, allowed);
            }
        }
        finally { _libraryGate.Release(); }
        return allowed;
    }

    internal async Task<Row[]> FilterAsync(Row[] rows, IReadOnlyList<string> libraries, CancellationToken token)
    {
        if (libraries.Count == 0) return rows;
        var allowed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var library in libraries) allowed.UnionWith(await LibraryAlbumsAsync(library, token).ConfigureAwait(false));
        var filtered = new List<Row>();
        foreach (var row in rows)
        {
            token.ThrowIfCancellationRequested();
            if (row.Kind == RowKind.Artist)
            {
                var artist = await client.Library.GetArtistAsync(row.Id, token).ConfigureAwait(false);
                if (artist.Albums.Any(album => allowed.Contains(album.Id))) filtered.Add(row);
            }
            else if (row.Kind == RowKind.Album) { if (allowed.Contains(row.Id)) filtered.Add(row); }
            else if (row.Kind is RowKind.Song or RowKind.Bookmark)
            {
                var song = row.Song ?? await client.Library.GetSongAsync(row.Id, token).ConfigureAwait(false);
                if (allowed.Contains(song.AlbumId.Length > 0 ? song.AlbumId : song.Parent)) filtered.Add(row);
            }
            else filtered.Add(row);
        }
        return filtered.ToArray();
    }

    internal async Task InvalidateAsync(CancellationToken token = default)
    {
        await _libraryGate.WaitAsync(token).ConfigureAwait(false);
        try { _libraryAlbums.Clear(); }
        finally { _libraryGate.Release(); }
    }
}
