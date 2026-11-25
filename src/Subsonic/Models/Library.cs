using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record MusicFolder
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

internal sealed record Genre
{
    public string Value { get; set; } = "";
    public int SongCount { get; set; }
    public int AlbumCount { get; set; }
}

internal sealed record Artist
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int AlbumCount { get; set; }
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; set; } = "";
    public string ArtistImageUrl { get; set; } = "";
    public string? Starred { get; set; }
    public int? UserRating { get; set; }
    public string MusicBrainzId { get; set; } = "";
    public string SortName { get; set; } = "";
    [JsonPropertyName("album")]
    [JsonConverter(typeof(Transport.OneOrMany<Album>))]
    public Album[] Albums { get; set; } = [];
}

internal sealed record ArtistIndex
{
    public string Name { get; set; } = "";
    [JsonPropertyName("artist")]
    [JsonConverter(typeof(Transport.OneOrMany<Artist>))]
    public Artist[] Artists { get; set; } = [];
}

internal sealed record Artists
{
    public string IgnoredArticles { get; set; } = "";
    public long? LastModified { get; set; }
    [JsonPropertyName("index")]
    [JsonConverter(typeof(Transport.OneOrMany<ArtistIndex>))]
    public ArtistIndex[] Indexes { get; set; } = [];
}

internal sealed record Entry
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Parent { get; set; } = "";
    public bool IsDir { get; set; }
    public string Title { get; set; } = "";
    public string Name { get; set; } = "";
    public string Album { get; set; } = "";
    public string Artist { get; set; } = "";
    public int? Track { get; set; }
    public int? Year { get; set; }
    public string Genre { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; set; } = "";
    public long? Size { get; set; }
    public string ContentType { get; set; } = "";
    public string Suffix { get; set; } = "";
    public string TranscodedContentType { get; set; } = "";
    public string TranscodedSuffix { get; set; } = "";
    public double? Duration { get; set; }
    public int? BitRate { get; set; }
    public int? BitDepth { get; set; }
    public int? SamplingRate { get; set; }
    public int? ChannelCount { get; set; }
    public string Path { get; set; } = "";
    public bool IsVideo { get; set; }
    public int? DiscNumber { get; set; }
    public string? Created { get; set; }
    public string? Starred { get; set; }
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string AlbumId { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string ArtistId { get; set; } = "";
    public int? UserRating { get; set; }
    public double? AverageRating { get; set; }
    public long? PlayCount { get; set; }
    public string? Played { get; set; }
    public int? Bpm { get; set; }
    public string Comment { get; set; } = "";
    public string SortName { get; set; } = "";
    public string MusicBrainzId { get; set; } = "";
    [JsonConverter(typeof(Transport.OneOrMany<ArtistCredit>))]
    public ArtistCredit[] Artists { get; set; } = [];
    [JsonConverter(typeof(Transport.OneOrMany<NamedGenre>))]
    public NamedGenre[] Genres { get; set; } = [];
    public ReplayGain? ReplayGain { get; set; }
    public string Type { get; set; } = "";
    public string MediaType { get; set; } = "";
    public long? BookmarkPosition { get; set; }
    public string[] Isrc { get; set; } = [];
    public string DisplayArtist { get; set; } = "";
    public ArtistCredit[] AlbumArtists { get; set; } = [];
    public string DisplayAlbumArtist { get; set; } = "";
    public Contributor[] Contributors { get; set; } = [];
    public string DisplayComposer { get; set; } = "";
    public string[] Moods { get; set; } = [];
    public string ExplicitStatus { get; set; } = "";
    public Work[] Works { get; set; } = [];
    public Movement[] Movements { get; set; } = [];
    public string[] Groupings { get; set; } = [];
}

internal sealed record ArtistCredit
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

internal sealed record NamedGenre
{
    public string Name { get; set; } = "";
}

internal sealed record Contributor
{
    public string Role { get; set; } = "";
    public string SubRole { get; set; } = "";
    public ArtistCredit? Artist { get; set; }
}
internal sealed record Work
{
    public string Name { get; set; } = "";
    public string MusicBrainzId { get; set; } = "";
}
internal sealed record Movement
{
    public string Name { get; set; } = "";
    public int? Number { get; set; }
    public int? Count { get; set; }
}

internal sealed record ReplayGain
{
    public double? TrackGain { get; set; }
    public double? AlbumGain { get; set; }
    public double? TrackPeak { get; set; }
    public double? AlbumPeak { get; set; }
    public double? BaseGain { get; set; }
    public double? FallbackGain { get; set; }
}

internal sealed record Album
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string ArtistId { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; set; } = "";
    public int SongCount { get; set; }
    public double Duration { get; set; }
    public string? Created { get; set; }
    public int? Year { get; set; }
    public string Genre { get; set; } = "";
    public string? Starred { get; set; }
    public int? UserRating { get; set; }
    public long? PlayCount { get; set; }
    public string? Played { get; set; }
    public string MusicBrainzId { get; set; } = "";
    public string SortName { get; set; } = "";
    [JsonPropertyName("song")]
    [JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Songs { get; set; } = [];
    [JsonConverter(typeof(Transport.OneOrMany<ArtistCredit>))]
    public ArtistCredit[] Artists { get; set; } = [];
    [JsonConverter(typeof(Transport.OneOrMany<NamedGenre>))]
    public NamedGenre[] Genres { get; set; } = [];
}

internal sealed record Directory
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Parent { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Starred { get; set; }
    public int? UserRating { get; set; }
    public long? PlayCount { get; set; }
    [JsonPropertyName("child")]
    [JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Children { get; set; } = [];
}

internal sealed record ArtistInfo
{
    public string Biography { get; set; } = "";
    public string MusicBrainzId { get; set; } = "";
    public string LastFmUrl { get; set; } = "";
    public string SmallImageUrl { get; set; } = "";
    public string MediumImageUrl { get; set; } = "";
    public string LargeImageUrl { get; set; } = "";
    [JsonPropertyName("similarArtist")]
    [JsonConverter(typeof(Transport.OneOrMany<Artist>))]
    public Artist[] SimilarArtists { get; set; } = [];
}

internal sealed record AlbumInfo
{
    public string Notes { get; set; } = "";
    public string MusicBrainzId { get; set; } = "";
    public string LastFmUrl { get; set; } = "";
    public string SmallImageUrl { get; set; } = "";
    public string MediumImageUrl { get; set; } = "";
    public string LargeImageUrl { get; set; } = "";
}
