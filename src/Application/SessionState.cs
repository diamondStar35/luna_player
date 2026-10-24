using LunaPlayer.Configuration.Models;
using LunaPlayer.Spotify;

namespace LunaPlayer.Application;

internal static class SessionState
{
    internal static void Remember(Settings settings, string? path, double? elapsed,
        Track? spotifyTrack, double spotifyPosition)
    {
        if (!settings.General.RememberLastPosition) return;
        var playback = settings.Playback;
        if (spotifyTrack is { } track)
        {
            playback.LastSpotifyUri = track.Uri;
            playback.LastSpotifyName = track.Name;
            playback.LastSpotifyArtists = track.Artists;
            playback.LastSpotifyDurationMs = track.DurationMs;
            playback.LastSpotifyPosition = Math.Max(0, spotifyPosition);
        }
        else if (path is not null)
            playback.LastSpotifyUri = string.Empty;

        // Network streams must leave the last remembered local file and its position intact.
        if (path is not null && File.Exists(path))
        {
            playback.LastFile = path;
            playback.LastPosition = Math.Max(0, elapsed ?? 0);
        }
    }
}
