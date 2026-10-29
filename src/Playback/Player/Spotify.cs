namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    private bool _spotifySession;
    internal bool InSpotifySession => _spotifySession;

    internal void BeginSpotifySession()
    {
        if (_spotifySession) return;
        if (_radioSession) LeaveSession();
        EnterSession();
        _spotifySession = true;
        _engine.ClearQueuedNext();
    }

    // Called only while Spotify owns the session. Shared CloseAll retains the ordinary
    // YouTube behavior: its suspended local playlist can still be restored later.
    internal void DiscardSpotifySession()
    {
        if (!_spotifySession) return;
        _spotifySession = false;
        _suspendedPlaylist = null;
    }

    /// <summary>Opens a decrypted Spotify file through the same mpv audio chain as local music.</summary>
    internal bool OpenSpotifyAudio(LunaPlayer.Spotify.Playback.AudioDecryptStream audio, string title,
        string source, double? startPosition = null, bool paused = false, bool fadeTransition = false)
    {
        if (_engine is not Engine mpv) { audio.Dispose(); return false; }
        string? url = null;
        var opened = false;
        try
        {
            url = mpv.RegisterSpotifyAudio(audio);
            BeginSpotifySession();
            if (!_playlist.OpenFile(url)) return false;
            Describe(url, title, source, null, audioOnly: true);
            opened = LoadCurrent(startPosition, paused, fadeTransition);
            return opened;
        }
        finally
        {
            if (!opened)
            {
                if (url is not null) mpv.ReleaseSpotifyAudio(url);
                audio.Cancel();
                audio.Dispose();
            }
        }
    }

    internal string? QueueSpotifyAudio(string expectedSource, LunaPlayer.Spotify.Playback.AudioDecryptStream audio,
        string title, string source, double startPosition)
    {
        if (_engine is not Engine mpv) { audio.Dispose(); return null; }
        var url = mpv.RegisterSpotifyAudio(audio);
        var queued = false;
        try
        {
            queued = QueueSessionStream(expectedSource, url, title, source, null, true, startPosition);
            return queued ? url : null;
        }
        finally
        {
            if (!queued) mpv.ReleaseSpotifyAudio(url);
        }
    }

    internal bool OpenPreparedSpotifyAudio(string url, string title, string source,
        double startPosition, bool paused, bool fadeTransition)
    {
        if (!_spotifySession || !_playlist.OpenFile(url)) return false;
        Describe(url, title, source, null, audioOnly: true);
        return LoadCurrent(startPosition, paused, fadeTransition);
    }

    internal void ClearSpotifySuccessor(string? url)
    {
        if (_queuedSessionStream?.Url == url && url is not null)
        {
            _queuedSessionStream = null;
            _engine.ClearQueuedNext();
        }
        if (url is not null && _engine is Engine mpv) mpv.ReleaseSpotifyAudio(url);
    }

    // Spotify uses the same position store rather than racing another store's writes to the same file.
    internal double? ReadSavedPosition(string link) => _positions?.Get(link);
    internal void WriteSavedPosition(string link, double seconds) => _positions?.Set(link, seconds);
}
