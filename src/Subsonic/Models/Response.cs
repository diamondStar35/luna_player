using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record Envelope
{
    [JsonPropertyName("subsonic-response")] public Response? Response { get; init; }
}
internal sealed record Response
{
    public string Status { get; init; } = "";
    public string Version { get; init; } = "";
    public string Type { get; init; } = "";
    public string ServerVersion { get; init; } = "";
    public bool OpenSubsonic { get; init; }
    public Error? Error { get; init; }
    public FolderList? MusicFolders { get; init; }
    public Artists? Artists { get; init; }
    public Artists? Indexes { get; init; }
    public Artist? Artist { get; init; }
    public Album? Album { get; init; }
    public Entry? Song { get; init; }
    public Directory? Directory { get; init; }
    public GenreList? Genres { get; init; }
    public ArtistInfo? ArtistInfo { get; init; }
    public ArtistInfo? ArtistInfo2 { get; init; }
    public AlbumInfo? AlbumInfo { get; init; }
    public AlbumInfo? AlbumInfo2 { get; init; }
    public AlbumList? AlbumList { get; init; }
    public AlbumList? AlbumList2 { get; init; }
    public SongList? RandomSongs { get; init; }
    public SongList? SongsByGenre { get; init; }
    public SongList? TopSongs { get; init; }
    public SongList? SimilarSongs { get; init; }
    public SongList? SimilarSongs2 { get; init; }
    public SearchResult? SearchResult2 { get; init; }
    public SearchResult? SearchResult3 { get; init; }
    public SearchResult? Starred { get; init; }
    public SearchResult? Starred2 { get; init; }
    public PlaylistList? Playlists { get; init; }
    public Playlist? Playlist { get; init; }
    public BookmarkList? Bookmarks { get; init; }
    public PlayQueue? PlayQueue { get; init; }
    public PlayQueue? PlayQueueByIndex { get; init; }
    public RadioList? InternetRadioStations { get; init; }
    public PodcastList? Podcasts { get; init; }
    public EpisodeList? NewestPodcasts { get; init; }
    public PodcastEpisode? PodcastEpisode { get; init; }
    public NowPlayingList? NowPlaying { get; init; }
    public Lyrics? Lyrics { get; init; }
    public LyricsList? LyricsList { get; init; }
    public ScanStatus? ScanStatus { get; init; }
    public User? User { get; init; }
    public License? License { get; init; }
    public TranscodeDecision? TranscodeDecision { get; init; }
    [JsonPropertyName("sonicMatch"), JsonConverter(typeof(Transport.OneOrMany<SonicMatch>))]
    public SonicMatch[] SonicMatches { get; init; } = [];
    public TokenInfo? TokenInfo { get; init; }
    [JsonConverter(typeof(Transport.OneOrMany<Extension>))]
    public Extension[] OpenSubsonicExtensions { get; init; } = [];
}
internal sealed record TokenInfo
{
    public string Username { get; init; } = "";
}
internal sealed record FolderList
{
    [JsonPropertyName("musicFolder"), JsonConverter(typeof(Transport.OneOrMany<MusicFolder>))]
    public MusicFolder[] MusicFolders { get; init; } = [];
}
internal sealed record GenreList
{
    [JsonPropertyName("genre"), JsonConverter(typeof(Transport.OneOrMany<Genre>))]
    public Genre[] Genres { get; init; } = [];
}
internal sealed record AlbumList
{
    [JsonPropertyName("album"), JsonConverter(typeof(Transport.OneOrMany<Album>))]
    public Album[] Albums { get; init; } = [];
}
internal sealed record SongList
{
    [JsonPropertyName("song"), JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Songs { get; init; } = [];
}
internal sealed record PlaylistList
{
    [JsonPropertyName("playlist"), JsonConverter(typeof(Transport.OneOrMany<Playlist>))]
    public Playlist[] Playlists { get; init; } = [];
}
internal sealed record BookmarkList
{
    [JsonPropertyName("bookmark"), JsonConverter(typeof(Transport.OneOrMany<Bookmark>))]
    public Bookmark[] Bookmarks { get; init; } = [];
}
internal sealed record RadioList
{
    [JsonPropertyName("internetRadioStation"), JsonConverter(typeof(Transport.OneOrMany<RadioStation>))]
    public RadioStation[] Stations { get; init; } = [];
}
internal sealed record PodcastList
{
    [JsonPropertyName("channel"), JsonConverter(typeof(Transport.OneOrMany<PodcastChannel>))]
    public PodcastChannel[] Channels { get; init; } = [];
}
internal sealed record EpisodeList
{
    [JsonPropertyName("episode"), JsonConverter(typeof(Transport.OneOrMany<PodcastEpisode>))]
    public PodcastEpisode[] Episodes { get; init; } = [];
}
internal sealed record NowPlayingList
{
    [JsonPropertyName("entry"), JsonConverter(typeof(Transport.OneOrMany<NowPlayingEntry>))]
    public NowPlayingEntry[] Entries { get; init; } = [];
}
internal sealed record LyricsList
{
    [JsonPropertyName("structuredLyrics"), JsonConverter(typeof(Transport.OneOrMany<StructuredLyrics>))]
    public StructuredLyrics[] Lyrics { get; init; } = [];
}
internal sealed record SearchResult
{
    [JsonPropertyName("artist"), JsonConverter(typeof(Transport.OneOrMany<Artist>))]
    public Artist[] Artists { get; init; } = [];
    [JsonPropertyName("album"), JsonConverter(typeof(Transport.OneOrMany<Album>))]
    public Album[] Albums { get; init; } = [];
    [JsonPropertyName("song"), JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Songs { get; init; } = [];
}
