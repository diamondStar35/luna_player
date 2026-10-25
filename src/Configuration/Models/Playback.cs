namespace LunaPlayer.Configuration.Models;

internal sealed class PlaybackSettings
{
    public string LastFile { get; set; } = string.Empty;
    public double LastPosition { get; set; }
    internal PlaybackSettings Copy() => (PlaybackSettings)MemberwiseClone();
    internal void Apply(PlaybackSettings source)
    {
        LastFile = source.LastFile;
        LastPosition = source.LastPosition;
    }
}
