namespace LunaPlayer.Subsonic.Servers;

internal readonly record struct TrackReference(Guid ServerId, string SongId);
internal sealed record AudioMuseSettings
{
    public Uri? Server { get; init; }
    public string Token { get; init; } = "";
    public string? MusicServer { get; init; }
    public int ResultCount { get; init; } = 50;
}
internal sealed record Profile
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public required Uri Server { get; init; }
    public Credentials Credentials { get; init; } = new();
    public StreamOptions Streaming { get; init; } = new();
    public string[] LibraryIds { get; init; } = [];
    public bool ReportListening { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public AudioMuseSettings? AudioMuse { get; init; }
    internal Options ConnectionOptions() => new() { Server = Server, Credentials = Credentials, Headers = Headers };
}
