using LunaPlayer.Discord;

namespace LunaPlayer.Tests.Discord;

internal static class Examples
{
    internal static PresenceText English { get; } = new(
        "Idle",
        "Media playback",
        "Untitled media",
        "music on Luna",
        "video on Luna",
        "YouTube on Luna",
        "radio on Luna",
        "Spotify on Luna",
        "an audiobook on Luna",
        "Watch online",
        "Watch on YouTube",
        "Listen on YouTube",
        "Open in Spotify",
        "Download Luna Player",
        "By {artist}",
        "[Paused]: {app}");
}
