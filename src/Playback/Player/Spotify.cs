namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    // Called only while Spotify owns the session. Shared CloseAll retains the ordinary
    // YouTube behavior: its suspended local playlist can still be restored later.
    internal void DiscardSpotifySession() => _suspendedPlaylist = null;

    /// <summary>Opens a decrypted Spotify file through the same mpv audio chain as local music.</summary>
    internal bool OpenSpotifyAudio(LunaPlayer.Spotify.AudioDecryptStream audio, string title,
        string source, double? startPosition = null)
    {
        if (_engine is not Engine mpv) { audio.Dispose(); return false; }
        string? url = null;
        var opened = false;
        try
        {
            url = mpv.RegisterSpotifyAudio(audio);
            EnterSession();
            if (!_playlist.OpenFile(url)) return false;
            Describe(url, title, source, null, audioOnly: true);
            opened = LoadCurrent(startPosition, paused: false);
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

    // Spotify uses the same position store rather than racing another store's writes to the same file.
    internal double? ReadSavedPosition(string link) => _positions?.Get(link);
    internal void WriteSavedPosition(string link, double seconds) => _positions?.Set(link, seconds);
}
