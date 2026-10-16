namespace LunaPlayer.Configuration.Models;

internal sealed class PlaybackSettings
{
    public string LastFile { get; set; } = string.Empty;
    public double LastPosition { get; set; }
    public string LastSpotifyUri { get; set; } = string.Empty;
    public string LastSpotifyName { get; set; } = string.Empty;
    public string LastSpotifyArtists { get; set; } = string.Empty;
    public int LastSpotifyDurationMs { get; set; }
    public double LastSpotifyPosition { get; set; }
    internal PlaybackSettings Copy() => (PlaybackSettings)MemberwiseClone();
    internal void Apply(PlaybackSettings source)
    {
        LastFile = source.LastFile;
        LastPosition = source.LastPosition;
        LastSpotifyUri = source.LastSpotifyUri;
        LastSpotifyName = source.LastSpotifyName;
        LastSpotifyArtists = source.LastSpotifyArtists;
        LastSpotifyDurationMs = source.LastSpotifyDurationMs;
        LastSpotifyPosition = source.LastSpotifyPosition;
    }
}
