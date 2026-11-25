namespace LunaPlayer.Subsonic.Servers;

internal readonly record struct TrackReference(Guid ServerId, string SongId);
internal sealed record AudioMuseSettings
{
    public Uri? Server { get; set; }
    public string Token { get; set; } = "";
    public string? MusicServer { get; set; }
    public int ResultCount { get; set; } = 50;
}
internal sealed record Profile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public required Uri Server { get; set; }
    public Credentials Credentials { get; set; } = new();
    public StreamOptions Streaming { get; set; } = new();
    public TimingSettings Timing { get; set; } = new();
    public string[] LibraryIds { get; set; } = [];
    public string[] HiddenSections { get; set; } = [];
    public bool RefreshMetadataOnStartup { get; set; } = true;
    public bool ReportListening { get; set; }
    public bool SynchronizeQueue { get; set; }
    public DownloadSettings Downloads { get; set; } = new();
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public AudioMuseSettings? AudioMuse { get; set; }
    internal Options ConnectionOptions() => new() { Server = Server, Credentials = Credentials, Headers = Headers,
        RequestTimeout = TimeSpan.FromSeconds(Timing.RequestTimeoutSeconds), LibraryCacheDuration = TimeSpan.FromSeconds(Timing.LibraryCacheSeconds) };
}

internal sealed record DownloadSettings
{
    public string Folder { get; set; } = "";
    public bool AlbumFolders { get; set; }
}
