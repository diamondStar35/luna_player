using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic.Browsing;

internal sealed record SearchOptions
{
    public int ArtistCount { get; init; } = 20;
    public int ArtistOffset { get; init; }
    public int AlbumCount { get; init; } = 20;
    public int AlbumOffset { get; init; }
    public int SongCount { get; init; } = 20;
    public int SongOffset { get; init; }
    public string? MusicFolderId { get; init; }
    internal Parameters Parameters(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (new[] { ArtistCount, ArtistOffset, AlbumCount, AlbumOffset, SongCount, SongOffset }.Any(x => x < 0))
            throw new ArgumentOutOfRangeException(nameof(SearchOptions));
        return new Parameters().With("query", query).With("artistCount", ArtistCount).With("artistOffset", ArtistOffset)
            .With("albumCount", AlbumCount).With("albumOffset", AlbumOffset).With("songCount", SongCount)
            .With("songOffset", SongOffset).With("musicFolderId", MusicFolderId);
    }
}
internal enum AlbumOrder { Random, Newest, Highest, Frequent, Recent, AlphabeticalByName, AlphabeticalByArtist, Starred, ByYear, ByGenre }
internal sealed record AlbumQuery
{
    public AlbumOrder Order { get; init; } = AlbumOrder.Newest;
    public int Size { get; init; } = 100;
    public int Offset { get; init; }
    public int? FromYear { get; init; }
    public int? ToYear { get; init; }
    public string? Genre { get; init; }
    public string? MusicFolderId { get; init; }
    internal Parameters Parameters()
    {
        if (!Enum.IsDefined(Order) || Size is < 1 or > 500 || Offset < 0) throw new ArgumentOutOfRangeException(nameof(AlbumQuery));
        if (Order == AlbumOrder.ByYear && (!FromYear.HasValue || !ToYear.HasValue)
            || Order == AlbumOrder.ByGenre && string.IsNullOrWhiteSpace(Genre)) throw new ArgumentException(nameof(AlbumQuery));
        var name = Order.ToString();
        return new Parameters().With("type", char.ToLowerInvariant(name[0]) + name[1..]).With("size", Size)
            .With("offset", Offset).With("fromYear", FromYear).With("toYear", ToYear).With("genre", Genre).With("musicFolderId", MusicFolderId);
    }
}
internal enum SelectionKind { Song, Album, Artist, Playlist }
internal readonly record struct Selection(SelectionKind Kind, string Id);
