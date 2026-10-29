using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Discord;

internal readonly record struct PresenceOptions(bool Enabled, bool ShowDetails, bool ShowButtons, bool ShowIdle)
{
    internal static PresenceOptions Capture(DiscordSettings settings)
        => new(settings.Enabled, settings.ShowDetails, settings.ShowButtons, settings.ShowIdle);
}
