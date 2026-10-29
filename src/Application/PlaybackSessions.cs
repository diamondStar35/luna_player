using LunaPlayer.Playback;
using LunaPlayer.YouTube.Playback;

namespace LunaPlayer.Application;

/// <summary>Routes session commands to their owner instead of chaining competing Escape or navigation handlers.</summary>
internal sealed class PlaybackSessions(Sessions youTube, Radio.Playback radio, Player player,
    Spotify.Playback.Sessions spotify, Func<bool> returnToSpotify)
{
    internal YouTube.YouTubeResult? CurrentYouTubeItem => youTube.CurrentVideo;
    internal Radio.Playback Radio => radio;
    internal Spotify.Playback.Sessions Spotify => spotify;
    internal bool CanMoveNext => spotify.HasSession ? spotify.CanMoveNext
        : radio.IsActive ? radio.CanMoveNext : youTube.CanMoveNext;
    internal bool CanMovePrevious => spotify.HasSession ? spotify.CanMovePrevious
        : radio.IsActive ? radio.CanMovePrevious : youTube.CanMovePrevious;
    internal NavigationOutcome TryNext() => spotify.HasSession
        ? spotify.Next() ? NavigationOutcome.Pending : NavigationOutcome.Exhausted
        : radio.IsActive ? radio.TryMove(1) : youTube.TryNext();
    internal NavigationOutcome TryPrevious() => spotify.HasSession
        ? spotify.Previous() ? NavigationOutcome.Pending : NavigationOutcome.Exhausted
        : radio.IsActive ? radio.TryMove(-1) : youTube.TryPrevious();
    internal bool HandleEscape() => spotify.HasSession ? returnToSpotify()
        : radio.IsActive ? radio.HandleEscape() : youTube.HandleEscape();
    internal void AcceptQueuedAdvance() { if (!radio.IsActive && !spotify.HasSession) youTube.AcceptQueuedAdvance(); }
    internal bool IsShuffleEnabled => spotify.HasSession ? spotify.IsShuffleEnabled
        : radio.IsActive ? radio.IsShuffleEnabled : player.IsShuffleEnabled;
    internal bool ToggleShuffle() => spotify.HasSession ? spotify.ToggleShuffle()
        : radio.IsActive ? radio.ToggleShuffle() : player.ToggleShuffle();
}
