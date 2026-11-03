namespace LunaPlayer.Spotify.Playback;

internal sealed class AudioKeyUnavailableException(ushort? code = null)
    : Exception("Spotify did not provide an audio key for this file.")
{
    internal ushort? Code { get; } = code;
}
