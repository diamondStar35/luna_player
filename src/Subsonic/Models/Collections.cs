using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record Playlist
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Comment { get; init; } = "";
    public string Owner { get; init; } = "";
    public bool Public { get; init; }
    public int SongCount { get; init; }
    public double Duration { get; init; }
    public string? Created { get; init; }
    public string? Changed { get; init; }
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; init; } = "";
    [JsonPropertyName("allowedUser")]
    public string[] AllowedUsers { get; init; } = [];
    [JsonPropertyName("entry")]
    [JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Entries { get; init; } = [];
}

internal sealed record Bookmark
{
    public long Position { get; init; }
    public string Username { get; init; } = "";
    public string Comment { get; init; } = "";
    public string? Created { get; init; }
    public string? Changed { get; init; }
    public Entry? Entry { get; init; }
}

internal sealed record PlayQueue
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Current { get; init; } = "";
    public int? CurrentIndex { get; init; }
    public double Position { get; init; }
    public string Username { get; init; } = "";
    public string? Changed { get; init; }
    public string ChangedBy { get; init; } = "";
    [JsonPropertyName("entry")]
    [JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Entries { get; init; } = [];
}

internal sealed record RadioStation
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string StreamUrl { get; init; } = "";
    public string HomePageUrl { get; init; } = "";
}

internal sealed record PodcastChannel
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    public string Url { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; init; } = "";
    public string OriginalImageUrl { get; init; } = "";
    public string Status { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    [JsonPropertyName("episode")]
    [JsonConverter(typeof(Transport.OneOrMany<PodcastEpisode>))]
    public PodcastEpisode[] Episodes { get; init; } = [];
}

internal sealed record PodcastEpisode
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string StreamId { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string ChannelId { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string? PublishDate { get; init; }
    public string Status { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    public double? Duration { get; init; }
    public long? Size { get; init; }
    public string Suffix { get; init; } = "";
    public string ContentType { get; init; } = "";
}

internal sealed record NowPlayingEntry
{
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Artist { get; init; } = "";
    public string Album { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string AlbumId { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string ArtistId { get; init; } = "";
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string CoverArt { get; init; } = "";
    public double? Duration { get; init; }
    public string Username { get; init; } = "";
    public int MinutesAgo { get; init; }
    [JsonConverter(typeof(Transport.IdentifierConverter))]
    public string PlayerId { get; init; } = "";
    public string PlayerName { get; init; } = "";
}
