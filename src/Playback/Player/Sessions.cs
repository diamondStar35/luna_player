using LunaPlayer.Playlist;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    /// <summary>Whether a list of videos is playing in front of the playlist the user opened.</summary>
    internal bool InSession => _suspendedPlaylist is not null;

    /// <summary>Puts an empty playlist in front of the one the user opened, remembering where it was.</summary>
    ///
    /// <remarks>
    /// This keeps a YouTube session separate from the user's playlist. Merging the two would discard the
    /// shuffle order and leave the local playlist on the wrong item when the session closes.
    ///
    /// The loaded file and its exact position are restored by <see cref="LeaveSession"/>.
    /// </remarks>
    internal void EnterSession()
    {
        if (_suspendedPlaylist is not null)
            return;
        // Written to the store as well as remembered, so a session that ends in a crash still leaves the
        // position behind for the next launch.
        SavePosition();
        _suspendedPlaylist = new SuspendedPlaylist(_playlist, _playlist.CurrentPath, Elapsed ?? 0, _loaded);
        _playlist = new PlaylistState();
        _loaded = false;
        // Nothing is announced here. The list is empty for the moment between this and the video being
        // loaded, and telling the rest of the player that nothing is playing - only to tell it again a
        // moment later - shows a state that never really existed and puts every handler through a
        // half-built one. Whoever calls this loads something next, and that announces itself.
    }

    /// <summary>Takes the list of videos away and puts the playlist the user opened back, on the file it
    /// was on and at the point it had reached.</summary>
    /// <remarks>
    /// It comes back paused rather than playing. The user pressed something to leave the videos, not to
    /// start music, and a player that begins playing on its own is worse than one that waits to be asked.
    /// </remarks>
    internal void LeaveSession()
    {
        _radioSession = false;
        _spotifySession = false;
        if (_suspendedPlaylist is not SuspendedPlaylist suspended)
            return;
        _suspendedPlaylist = null;
        _playlist = suspended.Playlist;
        // Reloaded rather than resumed, because the video that played in the meantime replaced it.
        // LoadCurrent raises the two events itself when it succeeds.
        if (suspended.WasLoaded && suspended.Path is not null && LoadCurrent(suspended.Position, paused: true))
            return;
        // Either nothing was loaded before the videos, or what was is no longer there to load - a file
        // renamed or deleted while they played. Either way the video has to stop, or it would carry on
        // playing with no list in front of it.
        StopEngine();
        CurrentChanged?.Invoke();
        StateChanged?.Invoke();
    }

    /// <summary>Plays a resolved video in front of the playlist the user opened.</summary>
    internal bool PlaySessionStream(string url, string? title, string? source, string? audioFile,
        bool audioOnly = false, bool fadeTransition = false)
    {
        if (_radioSession || _spotifySession) LeaveSession();
        EnterSession();
        // The YouTube session owns its complete, durable order. Keep only the stream currently given to
        // mpv here: stream addresses expire, while the session can resolve any previous or next row again.
        if (!_playlist.OpenFile(url))
            return false;
        Describe(url, title, source, audioFile, audioOnly);
        return LoadCurrent(fadeTransition);
    }

    /// <summary>The playlist a session was opened in front of, and the point playback had reached in it.
    /// </summary>
    /// <param name="WasLoaded">Whether a file was open at all, so an empty player is not sent to reload
    /// nothing.</param>
    private sealed record SuspendedPlaylist(PlaylistState Playlist, string? Path, double Position, bool WasLoaded);
}
