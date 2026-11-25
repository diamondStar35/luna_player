using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Playback;

internal static class StreamCapabilities
{
    internal static ClientInfo For(StreamOptions options)
    {
        var targets = new TranscodingProfile[]
        {
            new() { Container = "ogg", AudioCodec = "opus", Protocol = "http" },
            new() { Container = "mp3", AudioCodec = "mp3", Protocol = "http" },
            new() { Container = "flac", AudioCodec = "flac", Protocol = "http" },
            new() { Container = "aac", AudioCodec = "aac", Protocol = "http" },
            new() { Container = "wav", AudioCodec = "pcm_s16le", Protocol = "http" }
        };
        if (options.Mode == StreamingMode.Transcode)
            targets = targets.Where(target => target.AudioCodec == options.Format
                || options.Format == "wav" && target.Container == "wav").ToArray();
        return new()
        {
            Name = "LunaPlayer", Platform = "Windows",
            MaxTranscodingAudioBitrate = options.MaximumBitRate is > 0 and var rate ? checked(rate * 1000) : null,
            DirectPlayProfiles = options.Mode == StreamingMode.Transcode ? [] :
            [new() { Protocols = ["http"], Containers = ["mp3", "flac", "ogg", "opus", "mp4", "m4a", "aac", "wav", "aiff", "wma", "asf", "ape", "wv"],
                AudioCodecs = ["mp3", "mp2", "flac", "opus", "vorbis", "aac", "alac", "pcm_s16le", "pcm_s24le", "pcm_s32le", "pcm_f32le", "pcm_f64le", "pcm_s16be", "pcm_s24be", "wmav1", "wmav2", "wmapro", "ape", "wavpack"] }],
            TranscodingProfiles = targets
        };
    }

    internal static StreamFormat Format(StreamDetails stream, bool transcoded)
    {
        if (stream.Protocol != "http" || stream.Codec.Length == 0 || stream.Container.Length == 0)
            throw new RequestException(FailureKind.InvalidResponse, "getTranscodeDecision");
        return new(stream.Codec, stream.Container, Positive(stream.AudioBitRate), Positive(stream.AudioSampleRate),
            Positive(stream.AudioBitDepth), Positive(stream.AudioChannels), transcoded);
    }
    internal static StreamFormat Source(Entry song) => new(Container: song.Suffix.Length > 0 ? song.Suffix : null,
        BitRate: song.BitRate is > 0 and <= int.MaxValue / 1000 ? song.BitRate * 1000 : null,
        SampleRate: Positive(song.SamplingRate), BitDepth: Positive(song.BitDepth), Channels: Positive(song.ChannelCount), Transcoded: false);
    private static int? Positive(int? value) => value is > 0 ? value : null;
}
