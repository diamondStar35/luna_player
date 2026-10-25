using LunaPlayer.Playback;
using LunaPlayer.YouTube.Playback;

namespace LunaPlayer.Application;

/// <summary>Routes session commands to their owner instead of chaining competing Escape or navigation handlers.</summary>
internal sealed class PlaybackSessions(Sessions youTube, Radio.Playback radio, Player player,
    Func<Spotify.Playback?> getSpotify, Func<bool> returnToSpotify)
{
    internal Radio.Playback Radio => radio;
    internal Spotify.Playback? Spotify => getSpotify();
    internal bool CanMoveNext => Spotify is { HasSession: true } spotify ? spotify.CanMoveNext
        : radio.IsActive ? radio.CanMoveNext : youTube.CanMoveNext;
    internal bool CanMovePrevious => Spotify is { HasSession: true } spotify ? spotify.CanMovePrevious
        : radio.IsActive ? radio.CanMovePrevious : youTube.CanMovePrevious;
    internal NavigationOutcome TryNext() => Spotify is { HasSession: true } spotify
        ? spotify.Next() ? NavigationOutcome.Pending : NavigationOutcome.Exhausted
        : radio.IsActive ? radio.TryMove(1) : youTube.TryNext();
    internal NavigationOutcome TryPrevious() => Spotify is { HasSession: true } spotify
        ? spotify.Previous() ? NavigationOutcome.Pending : NavigationOutcome.Exhausted
        : radio.IsActive ? radio.TryMove(-1) : youTube.TryPrevious();
    internal bool HandleEscape() => Spotify is { HasSession: true } ? returnToSpotify()
        : radio.IsActive ? radio.HandleEscape() : youTube.HandleEscape();
    internal void AcceptQueuedAdvance() { if (!radio.IsActive && Spotify?.HasSession != true) youTube.AcceptQueuedAdvance(); }
    internal bool IsShuffleEnabled => Spotify is { HasSession: true } spotify ? spotify.IsShuffleEnabled
        : radio.IsActive ? radio.IsShuffleEnabled : player.IsShuffleEnabled;
    internal bool ToggleShuffle() => Spotify is { HasSession: true } spotify ? spotify.ToggleShuffle()
        : radio.IsActive ? radio.ToggleShuffle() : player.ToggleShuffle();
}
