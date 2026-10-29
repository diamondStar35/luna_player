namespace LunaPlayer.Configuration.Models;

internal sealed class DiscordSettings
{
    // Opt in: even a local file name can reveal private listening habits.
    public bool Enabled { get; set; }
    public bool ShowDetails { get; set; } = true;
    public bool ShowButtons { get; set; } = true;
    public bool ShowIdle { get; set; } = true;

    internal DiscordSettings Copy() => (DiscordSettings)MemberwiseClone();

    internal void Apply(DiscordSettings source)
    {
        Enabled = source.Enabled;
        ShowDetails = source.ShowDetails;
        ShowButtons = source.ShowButtons;
        ShowIdle = source.ShowIdle;
    }
}
