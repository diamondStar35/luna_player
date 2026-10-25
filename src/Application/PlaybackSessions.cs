using LunaPlayer.Playback;
using LunaPlayer.YouTube.Playback;

namespace LunaPlayer.Application;

/// <summary>Routes session commands to their owner instead of chaining competing Escape or navigation handlers.</summary>
internal sealed class PlaybackSessions(Sessions youTube, Radio.Playback radio, Player player)
{
    internal Radio.Playback Radio => radio;
    internal bool CanMoveNext => radio.IsActive ? radio.CanMoveNext : youTube.CanMoveNext;
    internal bool CanMovePrevious => radio.IsActive ? radio.CanMovePrevious : youTube.CanMovePrevious;
    internal NavigationOutcome TryNext() => radio.IsActive ? radio.TryMove(1) : youTube.TryNext();
    internal NavigationOutcome TryPrevious() => radio.IsActive ? radio.TryMove(-1) : youTube.TryPrevious();
    internal bool HandleEscape() => radio.IsActive ? radio.HandleEscape() : youTube.HandleEscape();
    internal void AcceptQueuedAdvance() { if (!radio.IsActive) youTube.AcceptQueuedAdvance(); }
    internal bool IsShuffleEnabled => radio.IsActive ? radio.IsShuffleEnabled : player.IsShuffleEnabled;
    internal bool ToggleShuffle() => radio.IsActive ? radio.ToggleShuffle() : player.ToggleShuffle();
}
