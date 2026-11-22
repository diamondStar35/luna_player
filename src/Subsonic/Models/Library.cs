using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record MusicFolder
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
}

internal sealed record Genre
{
    public string Value { get; init; } = "";
    public int SongCount { get; init; }
    public int AlbumCount { get; init; }
}

internal sealed record Artist
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int AlbumCount { get; init; }
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; init; } = "";
    public string ArtistImageUrl { get; init; } = "";
    public string? Starred { get; init; }
    public int? UserRating { get; init; }
    public string MusicBrainzId { get; init; } = "";
    public string SortName { get; init; } = "";
    [JsonPropertyName("album")]
    [JsonConverter(typeof(Transport.OneOrMany<Album>))]
    public Album[] Albums { get; init; } = [];
}

internal sealed record ArtistIndex
{
    public string Name { get; init; } = "";
    [JsonPropertyName("artist")]
    [JsonConverter(typeof(Transport.OneOrMany<Artist>))]
    public Artist[] Artists { get; init; } = [];
}

internal sealed record Artists
{
    public string IgnoredArticles { get; init; } = "";
    public long? LastModified { get; init; }
    [JsonPropertyName("index")]
    [JsonConverter(typeof(Transport.OneOrMany<ArtistIndex>))]
    public ArtistIndex[] Indexes { get; init; } = [];
}

internal sealed record Entry
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Parent { get; init; } = "";
    public bool IsDir { get; init; }
    public string Title { get; init; } = "";
    public string Name { get; init; } = "";
    public string Album { get; init; } = "";
    public string Artist { get; init; } = "";
    public int? Track { get; init; }
    public int? Year { get; init; }
    public string Genre { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; init; } = "";
    public long? Size { get; init; }
    public string ContentType { get; init; } = "";
    public string Suffix { get; init; } = "";
    public string TranscodedContentType { get; init; } = "";
    public string TranscodedSuffix { get; init; } = "";
    public double? Duration { get; init; }
    public int? BitRate { get; init; }
    public int? BitDepth { get; init; }
    public int? SamplingRate { get; init; }
    public int? ChannelCount { get; init; }
    public string Path { get; init; } = "";
    public bool IsVideo { get; init; }
    public int? DiscNumber { get; init; }
    public string? Created { get; init; }
    public string? Starred { get; init; }
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string AlbumId { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string ArtistId { get; init; } = "";
    public int? UserRating { get; init; }
    public double? AverageRating { get; init; }
    public long? PlayCount { get; init; }
    public string? Played { get; init; }
    public int? Bpm { get; init; }
    public string Comment { get; init; } = "";
    public string SortName { get; init; } = "";
    public string MusicBrainzId { get; init; } = "";
    [JsonConverter(typeof(Transport.OneOrMany<ArtistCredit>))]
    public ArtistCredit[] Artists { get; init; } = [];
    [JsonConverter(typeof(Transport.OneOrMany<NamedGenre>))]
    public NamedGenre[] Genres { get; init; } = [];
    public ReplayGain? ReplayGain { get; init; }
    public string Type { get; init; } = "";
    public string MediaType { get; init; } = "";
    public long? BookmarkPosition { get; init; }
    public string[] Isrc { get; init; } = [];
    public string DisplayArtist { get; init; } = "";
    public ArtistCredit[] AlbumArtists { get; init; } = [];
    public string DisplayAlbumArtist { get; init; } = "";
    public Contributor[] Contributors { get; init; } = [];
    public string DisplayComposer { get; init; } = "";
    public string[] Moods { get; init; } = [];
    public string ExplicitStatus { get; init; } = "";
    public Work[] Works { get; init; } = [];
    public Movement[] Movements { get; init; } = [];
    public string[] Groupings { get; init; } = [];
}

internal sealed record ArtistCredit
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
}

internal sealed record NamedGenre
{
    public string Name { get; init; } = "";
}

internal sealed record Contributor
{
    public string Role { get; init; } = "";
    public string SubRole { get; init; } = "";
    public ArtistCredit? Artist { get; init; }
}
internal sealed record Work
{
    public string Name { get; init; } = "";
    public string MusicBrainzId { get; init; } = "";
}
internal sealed record Movement
{
    public string Name { get; init; } = "";
    public int? Number { get; init; }
    public int? Count { get; init; }
}

internal sealed record ReplayGain
{
    public double? TrackGain { get; init; }
    public double? AlbumGain { get; init; }
    public double? TrackPeak { get; init; }
    public double? AlbumPeak { get; init; }
    public double? BaseGain { get; init; }
    public double? FallbackGain { get; init; }
}

internal sealed record Album
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Title { get; init; } = "";
    public string Artist { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string ArtistId { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; init; } = "";
    public int SongCount { get; init; }
    public double Duration { get; init; }
    public string? Created { get; init; }
    public int? Year { get; init; }
    public string Genre { get; init; } = "";
    public string? Starred { get; init; }
    public int? UserRating { get; init; }
    public long? PlayCount { get; init; }
    public string? Played { get; init; }
    public string MusicBrainzId { get; init; } = "";
    public string SortName { get; init; } = "";
    [JsonPropertyName("song")]
    [JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Songs { get; init; } = [];
    [JsonConverter(typeof(Transport.OneOrMany<ArtistCredit>))]
    public ArtistCredit[] Artists { get; init; } = [];
    [JsonConverter(typeof(Transport.OneOrMany<NamedGenre>))]
    public NamedGenre[] Genres { get; init; } = [];
}

internal sealed record Directory
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Parent { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Starred { get; init; }
    public int? UserRating { get; init; }
    public long? PlayCount { get; init; }
    [JsonPropertyName("child")]
    [JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Children { get; init; } = [];
}

internal sealed record ArtistInfo
{
    public string Biography { get; init; } = "";
    public string MusicBrainzId { get; init; } = "";
    public string LastFmUrl { get; init; } = "";
    public string SmallImageUrl { get; init; } = "";
    public string MediumImageUrl { get; init; } = "";
    public string LargeImageUrl { get; init; } = "";
    [JsonPropertyName("similarArtist")]
    [JsonConverter(typeof(Transport.OneOrMany<Artist>))]
    public Artist[] SimilarArtists { get; init; } = [];
}

internal sealed record AlbumInfo
{
    public string Notes { get; init; } = "";
    public string MusicBrainzId { get; init; } = "";
    public string LastFmUrl { get; init; } = "";
    public string SmallImageUrl { get; init; } = "";
    public string MediumImageUrl { get; init; } = "";
    public string LargeImageUrl { get; init; } = "";
}
