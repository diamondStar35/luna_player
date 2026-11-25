using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record Playlist
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Comment { get; set; } = "";
    public string Owner { get; set; } = "";
    public bool Public { get; set; }
    public int SongCount { get; set; }
    public double Duration { get; set; }
    public string? Created { get; set; }
    public string? Changed { get; set; }
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; set; } = "";
    [JsonPropertyName("allowedUser")]
    public string[] AllowedUsers { get; set; } = [];
    [JsonPropertyName("entry")]
    [JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Entries { get; set; } = [];
}

internal sealed record Bookmark
{
    public long Position { get; set; }
    public string Username { get; set; } = "";
    public string Comment { get; set; } = "";
    public string? Created { get; set; }
    public string? Changed { get; set; }
    public Entry? Entry { get; set; }
}

internal sealed record PlayQueue
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Current { get; set; } = "";
    public int? CurrentIndex { get; set; }
    public double Position { get; set; }
    public string Username { get; set; } = "";
    public string? Changed { get; set; }
    public string ChangedBy { get; set; } = "";
    [JsonPropertyName("entry")]
    [JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Entries { get; set; } = [];
}

internal sealed record RadioStation
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string StreamUrl { get; set; } = "";
    public string HomePageUrl { get; set; } = "";
}

internal sealed record PodcastChannel
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; set; } = "";
    public string OriginalImageUrl { get; set; } = "";
    public string Status { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    [JsonPropertyName("episode")]
    [JsonConverter(typeof(Transport.OneOrMany<PodcastEpisode>))]
    public PodcastEpisode[] Episodes { get; set; } = [];
}

internal sealed record PodcastEpisode
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string StreamId { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string ChannelId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? PublishDate { get; set; }
    public string Status { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public double? Duration { get; set; }
    public long? Size { get; set; }
    public string Suffix { get; set; } = "";
    public string ContentType { get; set; } = "";
}

internal sealed record NowPlayingEntry
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string AlbumId { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string ArtistId { get; set; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; set; } = "";
    public double? Duration { get; set; }
    public string Username { get; set; } = "";
    public int MinutesAgo { get; set; }
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
}
