using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record Envelope
{
    [JsonPropertyName("subsonic-response")] public Response? Response { get; set; }
}
internal sealed record Response
{
    public string Status { get; set; } = "";
    public string Version { get; set; } = "";
    public string Type { get; set; } = "";
    public string ServerVersion { get; set; } = "";
    public bool OpenSubsonic { get; set; }
    public Error? Error { get; set; }
    public FolderList? MusicFolders { get; set; }
    public Artists? Artists { get; set; }
    public Artists? Indexes { get; set; }
    public Artist? Artist { get; set; }
    public Album? Album { get; set; }
    public Entry? Song { get; set; }
    public Directory? Directory { get; set; }
    public GenreList? Genres { get; set; }
    public ArtistInfo? ArtistInfo { get; set; }
    public ArtistInfo? ArtistInfo2 { get; set; }
    public AlbumInfo? AlbumInfo { get; set; }
    public AlbumInfo? AlbumInfo2 { get; set; }
    public AlbumList? AlbumList { get; set; }
    public AlbumList? AlbumList2 { get; set; }
    public SongList? RandomSongs { get; set; }
    public SongList? SongsByGenre { get; set; }
    public SongList? TopSongs { get; set; }
    public SongList? SimilarSongs { get; set; }
    public SongList? SimilarSongs2 { get; set; }
    public SearchResult? SearchResult2 { get; set; }
    public SearchResult? SearchResult3 { get; set; }
    public SearchResult? Starred { get; set; }
    public SearchResult? Starred2 { get; set; }
    public PlaylistList? Playlists { get; set; }
    public Playlist? Playlist { get; set; }
    public BookmarkList? Bookmarks { get; set; }
    public PlayQueue? PlayQueue { get; set; }
    public PlayQueue? PlayQueueByIndex { get; set; }
    public RadioList? InternetRadioStations { get; set; }
    public PodcastList? Podcasts { get; set; }
    public EpisodeList? NewestPodcasts { get; set; }
    public PodcastEpisode? PodcastEpisode { get; set; }
    public NowPlayingList? NowPlaying { get; set; }
    public Lyrics? Lyrics { get; set; }
    public LyricsList? LyricsList { get; set; }
    public ScanStatus? ScanStatus { get; set; }
    public User? User { get; set; }
    public License? License { get; set; }
    public TranscodeDecision? TranscodeDecision { get; set; }
    [JsonPropertyName("sonicMatch"), JsonConverter(typeof(Transport.OneOrMany<SonicMatch>))]
    public SonicMatch[] SonicMatches { get; set; } = [];
    public TokenInfo? TokenInfo { get; set; }
    [JsonConverter(typeof(Transport.OneOrMany<Extension>))]
    public Extension[] OpenSubsonicExtensions { get; set; } = [];
}
internal sealed record TokenInfo
{
    public string Username { get; set; } = "";
}
internal sealed record FolderList
{
    [JsonPropertyName("musicFolder"), JsonConverter(typeof(Transport.OneOrMany<MusicFolder>))]
    public MusicFolder[] MusicFolders { get; set; } = [];
}
internal sealed record GenreList
{
    [JsonPropertyName("genre"), JsonConverter(typeof(Transport.OneOrMany<Genre>))]
    public Genre[] Genres { get; set; } = [];
}
internal sealed record AlbumList
{
    [JsonPropertyName("album"), JsonConverter(typeof(Transport.OneOrMany<Album>))]
    public Album[] Albums { get; set; } = [];
}
internal sealed record SongList
{
    [JsonPropertyName("song"), JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Songs { get; set; } = [];
}
internal sealed record PlaylistList
{
    [JsonPropertyName("playlist"), JsonConverter(typeof(Transport.OneOrMany<Playlist>))]
    public Playlist[] Playlists { get; set; } = [];
}
internal sealed record BookmarkList
{
    [JsonPropertyName("bookmark"), JsonConverter(typeof(Transport.OneOrMany<Bookmark>))]
    public Bookmark[] Bookmarks { get; set; } = [];
}
internal sealed record RadioList
{
    [JsonPropertyName("internetRadioStation"), JsonConverter(typeof(Transport.OneOrMany<RadioStation>))]
    public RadioStation[] Stations { get; set; } = [];
}
internal sealed record PodcastList
{
    [JsonPropertyName("channel"), JsonConverter(typeof(Transport.OneOrMany<PodcastChannel>))]
    public PodcastChannel[] Channels { get; set; } = [];
}
internal sealed record EpisodeList
{
    [JsonPropertyName("episode"), JsonConverter(typeof(Transport.OneOrMany<PodcastEpisode>))]
    public PodcastEpisode[] Episodes { get; set; } = [];
}
internal sealed record NowPlayingList
{
    [JsonPropertyName("entry"), JsonConverter(typeof(Transport.OneOrMany<NowPlayingEntry>))]
    public NowPlayingEntry[] Entries { get; set; } = [];
}
internal sealed record LyricsList
{
    [JsonPropertyName("structuredLyrics"), JsonConverter(typeof(Transport.OneOrMany<StructuredLyrics>))]
    public StructuredLyrics[] Lyrics { get; set; } = [];
}
internal sealed record SearchResult
{
    [JsonPropertyName("artist"), JsonConverter(typeof(Transport.OneOrMany<Artist>))]
    public Artist[] Artists { get; set; } = [];
    [JsonPropertyName("album"), JsonConverter(typeof(Transport.OneOrMany<Album>))]
    public Album[] Albums { get; set; } = [];
    [JsonPropertyName("song"), JsonConverter(typeof(Transport.OneOrMany<Entry>))]
    public Entry[] Songs { get; set; } = [];
}
