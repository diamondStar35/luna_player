namespace LunaPlayer.Configuration.Models;

internal sealed class SpotifySettings
{
    /// <summary>The user's own Spotify developer Client ID, used for catalog browsing via the Web API.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Preferred Spotify stream quality: NORMAL (96 kbps), HIGH (160 kbps) or VERY_HIGH (320 kbps).
    /// A lower quality downloads faster on weaker connections.</summary>
    public string AudioQuality { get; set; } = "VERY_HIGH";

    internal SpotifySettings Copy() => (SpotifySettings)MemberwiseClone();

    internal void Apply(SpotifySettings source)
    {
        ClientId = source.ClientId;
        AudioQuality = source.AudioQuality;
    }
}
