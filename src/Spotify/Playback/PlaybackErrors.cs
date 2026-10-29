namespace LunaPlayer.Spotify.Playback;

internal sealed class AudioKeyUnavailableException(ushort? code = null)
    : Exception("Spotify did not provide an audio key for this file.")
{
    internal ushort? Code { get; } = code;
}

internal sealed class TrackUnavailableException(Exception? cause = null)
    : Exception("No playable Spotify audio file is available for this track.", cause);

internal sealed class PlaybackDecodeException()
    : Exception("Spotify audio could not be decoded after retrying.");
