using System.Globalization;
using LunaPlayer.Media;
using LunaPlayer.Subsonic.Models;
using Reference = LunaPlayer.Subsonic.Playback.Reference;

namespace LunaPlayer.Subsonic.Browsing;

internal enum Category { Artists, Albums, Songs, Genres, Favorites, Added, Recent, Frequent, Random, Playlists, Bookmarks, NowPlaying, Radio, Podcasts, RandomAlbums }
internal enum RowKind { Artist, Album, Song, Genre, Playlist, Bookmark, Radio, PodcastChannel, Podcast, Unavailable }
internal enum SortField { Default, Name, Artist, Album, Track, Duration, Year, Added, Plays, Rating, Format, BitRate, SampleRate, BitDepth }
internal sealed record Location(Category Category, RowKind? Container = null, string? Id = null, string? Name = null);
internal sealed record Row(RowKind Kind, string Id, string Name, string Artist = "", string Album = "",
    double? Duration = null, int? Year = null, int? Disc = null, int? Track = null, string? Added = null,
    long? Plays = null, int? Rating = null, bool Favorite = false, long Position = 0, string Description = "",
    Entry? Song = null, string Owner = "", int? Count = null, int? PlaylistIndex = null, bool Available = true, string Status = "")
{
    internal bool Playable => Available && Kind is RowKind.Song or RowKind.Bookmark or RowKind.Podcast or RowKind.Radio;
    internal Reference Reference(Guid server) => new(server, Id, Kind == RowKind.Podcast ? Playback.MediaKind.Podcast
        : Kind == RowKind.Radio ? Playback.MediaKind.Radio : Playback.MediaKind.Song);
    internal MediaTags Tags => new(Name, Artist, Album);
    internal static Row From(Entry song) => new(RowKind.Song, song.Id, song.Title, song.DisplayArtist.Length > 0 ? song.DisplayArtist : song.Artist,
        song.Album, song.Duration, song.Year, song.DiscNumber, song.Track, song.Created, song.PlayCount, song.UserRating,
        !string.IsNullOrEmpty(song.Starred), Song: song);
    internal static Row From(Artist artist) => new(RowKind.Artist, artist.Id, artist.Name, Rating: artist.UserRating,
        Favorite: !string.IsNullOrEmpty(artist.Starred), Count: artist.AlbumCount);
    internal static Row From(Album album) => new(RowKind.Album, album.Id, album.Name.Length > 0 ? album.Name : album.Title, album.Artist,
        Duration: album.Duration, Year: album.Year, Added: album.Created, Plays: album.PlayCount, Rating: album.UserRating,
        Favorite: !string.IsNullOrEmpty(album.Starred), Count: album.SongCount);
}
internal static class Ordering
{
    internal static Row[] Sort(IEnumerable<Row> rows, SortField field, bool descending)
    {
        if (field == SortField.Default) return rows.ToArray();
        object? Key(Row row) => field switch
        {
            SortField.Artist => row.Artist, SortField.Album => row.Album, SortField.Track => row.Track.HasValue ? (row.Disc ?? 0) * 100000L + row.Track : null,
            SortField.Duration => row.Duration, SortField.Year => row.Year, SortField.Added => DateTimeOffset.TryParse(row.Added, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null,
            SortField.Plays => row.Plays, SortField.Rating => row.Rating,
            SortField.Format => row.Song?.Suffix, SortField.BitRate => row.Song?.BitRate is > 0 ? row.Song.BitRate : null,
            SortField.SampleRate => row.Song?.SamplingRate is > 0 ? row.Song.SamplingRate : null,
            SortField.BitDepth => row.Song?.BitDepth is > 0 ? row.Song.BitDepth : null, _ => row.Name
        };
        var comparer = Comparer<object?>.Create((a, b) =>
        {
            if (a is null || a is string { Length: 0 }) return b is null || b is string { Length: 0 } ? 0 : 1;
            if (b is null || b is string { Length: 0 }) return -1;
            var value = a is string left && b is string right ? CultureInfo.CurrentCulture.CompareInfo.Compare(left, right, CompareOptions.IgnoreCase)
                : ((IComparable)a).CompareTo(b);
            return descending ? -value : value;
        });
        return rows.OrderBy(Key, comparer).ToArray();
    }
}
