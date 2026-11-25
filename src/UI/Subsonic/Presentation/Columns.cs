using System.Globalization;
using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.UI.Subsonic;

internal enum Column { Title, Name, Artist, Album, Duration, Quality, Rating, Favorite, Track, Year, Plays,
    Albums, Songs, Owner, Description, Position, Status, Published }

/// <summary>Column selection and values are shared by browsers and song pickers without querying a server.</summary>
internal static class Columns
{
    internal static readonly Column[] Tracks = [Column.Title, Column.Artist, Column.Album, Column.Duration,
        Column.Quality, Column.Rating, Column.Favorite, Column.Track, Column.Year, Column.Plays];

    internal static Column[] For(Location location, IReadOnlyList<Row> rows, bool searching)
    {
        if (searching) return Tracks;
        if (rows.Count > 0)
        {
            if (rows.All(row => row.Kind is RowKind.Song or RowKind.Unavailable))
                return rows.Any(row => !row.Available || row.Kind == RowKind.Unavailable) ? [.. Tracks, Column.Status] : Tracks;
            if (rows.All(row => row.Kind == RowKind.Artist))
                return [Column.Name, Column.Albums, Column.Rating, Column.Favorite];
            if (rows.All(row => row.Kind == RowKind.Album))
                return [Column.Name, Column.Artist, Column.Songs, Column.Duration, Column.Year, Column.Rating, Column.Favorite];
        }
        if (location.Container is RowKind.Album or RowKind.Playlist) return Tracks;
        if (location.Container == RowKind.PodcastChannel) return [Column.Title, Column.Duration, Column.Status, Column.Published, Column.Description];
        if (location.Container == RowKind.Artist) return [Column.Name, Column.Artist, Column.Songs, Column.Duration, Column.Year, Column.Rating, Column.Favorite];
        return location.Category switch
        {
            Category.Artists => [Column.Name, Column.Albums, Column.Rating, Column.Favorite],
            Category.Albums or Category.Added or Category.Recent or Category.Frequent or Category.RandomAlbums
                => [Column.Name, Column.Artist, Column.Songs, Column.Duration, Column.Year, Column.Rating, Column.Favorite],
            Category.Playlists => [Column.Name, Column.Owner, Column.Songs, Column.Duration, Column.Description],
            Category.Bookmarks => [Column.Title, Column.Artist, Column.Album, Column.Position, Column.Rating, Column.Favorite, Column.Description],
            Category.Radio => [Column.Name, Column.Description],
            Category.Podcasts => [Column.Name, Column.Status, Column.Description],
            Category.NowPlaying => [Column.Title, Column.Artist, Column.Album, Column.Duration, Column.Owner, Column.Description],
            Category.Genres when location.Container is null => [Column.Name],
            _ => Tracks
        };
    }

    internal static string Heading(Column column) => column switch
    {
        Column.Title => Words.TrackTitle, Column.Name => Words.Name, Column.Artist => Words.Artist,
        Column.Album => Words.Album, Column.Duration => Words.Duration, Column.Quality => Words.Quality,
        Column.Rating => Words.Rating, Column.Favorite => Words.Favorite, Column.Track => Words.Track,
        Column.Year => Words.Year, Column.Plays => Words.Plays, Column.Albums => Words.Albums,
        Column.Songs => Words.Songs, Column.Owner => Words.Owner, Column.Position => Words.CurrentPosition,
        Column.Status => Words.Status, Column.Published => Words.Published, _ => Words.Description
    };

    internal static int Width(Column column) => column switch
    {
        Column.Title or Column.Name or Column.Description => 240,
        Column.Quality => 250, Column.Artist or Column.Album or Column.Owner => 160, _ => 90
    };

    internal static string Cell(Row row, Column column) => column switch
    {
        Column.Title or Column.Name => row.Name, Column.Artist => row.Artist, Column.Album => row.Album,
        Column.Duration => row.Duration is { } duration ? Time(duration) : "",
        Column.Position => Time(row.Position / 1000d), Column.Quality => Quality(row),
        Column.Rating => row.Rating is > 0 and <= 5 ? Words.Stars(row.Rating.Value) : Words.NotRated,
        Column.Favorite => row.Favorite ? Words.Yes : "", Column.Track => Number(row.Track),
        Column.Year => Number(row.Year), Column.Plays => Number(row.Plays),
        Column.Albums or Column.Songs => Number(row.Count), Column.Owner => row.Owner,
        Column.Status => row.Kind == RowKind.Unavailable ? Words.UnavailableItem : row.Status,
        Column.Published => DateTimeOffset.TryParse(row.Added, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) : "",
        _ => row.Kind == RowKind.Unavailable ? Words.UnavailableItem : row.Description
    };

    internal static string Quality(Row row)
    {
        var quality = LunaPlayer.Subsonic.Browsing.Quality.From(row.Song);
        var values = new List<string>();
        if (quality.Format.Length > 0) values.Add(quality.Format);
        if (quality.BitRate is { } rate) values.Add(Words.Kilobits(rate));
        if (quality.SampleRate is { } samples) values.Add(Words.SampleRate(samples / 1000d));
        if (quality.BitDepth is { } depth) values.Add(Words.BitDepth(depth));
        if (quality.Channels is { } channels) values.Add(Words.Channels(channels));
        return string.Join(", ", values);
    }
    private static string Number<T>(T? value) where T : struct, IFormattable => value?.ToString(null, CultureInfo.CurrentCulture) ?? "";
    private static string Time(double seconds) => double.IsFinite(seconds) && seconds >= 0 && seconds < TimeSpan.MaxValue.TotalSeconds - 1
        ? TimeSpan.FromSeconds(seconds).ToString(seconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture) : "";
}
