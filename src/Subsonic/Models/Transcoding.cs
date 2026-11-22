using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record TranscodeDecision
{
    public bool? CanDirectPlay { get; init; }
    public bool? CanTranscode { get; init; }
    public string TranscodeParams { get; init; } = "";
    public string ErrorReason { get; init; } = "";
    public SourceStream? SourceStream { get; init; }
    [JsonPropertyName("transcodeReason")]
    public string[] TranscodeReasons { get; init; } = [];
    public SourceStream? TranscodeStream { get; init; }
}

internal sealed record SourceStream
{
    public string Protocol { get; init; } = "";
    public string Codec { get; init; } = "";
    public string Container { get; init; } = "";
    public string AudioCodec { get; init; } = "";
    public string AudioProfile { get; init; } = "";
    [JsonPropertyName("audioBitrate")]
    public int? AudioBitRate { get; init; }
    [JsonPropertyName("audioSamplerate")]
    public int? AudioSampleRate { get; init; }
    public int? AudioChannels { get; init; }
    [JsonPropertyName("audioBitdepth")]
    public int? AudioBitDepth { get; init; }
    public double? Duration { get; init; }
    public long? Size { get; init; }
}

internal sealed record ClientInfo
{
    public int? MaxAudioBitrate { get; init; }
    public int? MaxTranscodingAudioBitrate { get; init; }
    public CodecProfile[] CodecProfiles { get; init; } = [];
    public string Name { get; init; } = "";
    public string Platform { get; init; } = "";
    [JsonConverter(typeof(Transport.OneOrMany<DirectPlayProfile>))]
    public DirectPlayProfile[] DirectPlayProfiles { get; init; } = [];
    [JsonConverter(typeof(Transport.OneOrMany<TranscodingProfile>))]
    public TranscodingProfile[] TranscodingProfiles { get; init; } = [];
}

internal sealed record DirectPlayProfile
{
    public string[] Containers { get; init; } = [];
    public string[] AudioCodecs { get; init; } = [];
    public string[] Protocols { get; init; } = [];
    public int? MaxAudioChannels { get; init; }
}

internal sealed record TranscodingProfile
{
    public string Container { get; init; } = "";
    public string AudioCodec { get; init; } = "";
    public string Protocol { get; init; } = "";
    public int? MaxAudioChannels { get; init; }
}

internal sealed record SonicMatch
{
    [JsonPropertyName("entry")]
    public Entry? Song { get; init; }
    public double? Distance { get; init; }
    public double? Similarity { get; init; }
}

internal sealed record CodecProfile
{
    public string Type { get; init; } = "AudioCodec";
    public string Name { get; init; } = "";
    public Limitation[] Limitations { get; init; } = [];
}
internal sealed record Limitation
{
    public string Name { get; init; } = "";
    public string Comparison { get; init; } = "";
    public string[] Values { get; init; } = [];
    public bool Required { get; init; }
}
