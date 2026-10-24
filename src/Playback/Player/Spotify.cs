namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    /// <summary>Opens a decrypted Spotify file through the same mpv audio chain as local music.</summary>
    internal bool OpenSpotifyAudio(LunaPlayer.Spotify.AudioDecryptStream audio, string title,
        string source, double? startPosition = null)
    {
        if (_engine is not Engine mpv) { audio.Dispose(); return false; }
        var url = mpv.RegisterSpotifyAudio(audio);
        EnterSession();
        if (!_playlist.OpenFile(url)) { mpv.ReleaseSpotifyAudio(url); return false; }
        Describe(url, title, source, null, audioOnly: true);
        if (LoadCurrent(startPosition, paused: false)) return true;
        mpv.ReleaseSpotifyAudio(url);
        return false;
    }

    // Spotify uses the same position store rather than racing another store's writes to the same file.
    internal double? ReadSavedPosition(string link) => _positions?.Get(link);
    internal void WriteSavedPosition(string link, double seconds) => _positions?.Set(link, seconds);
}
