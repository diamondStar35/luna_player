namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string PlaybackStream(string format) =>
        // Translators: Audio format of the stream currently playing, which may differ from the source file. {format} contains codec, bitrate, sample rate and other available audio properties.
        TrFormat("Playback stream: {format}", format);
}
