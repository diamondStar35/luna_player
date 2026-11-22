using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record TranscodeDecision
{
    public bool? CanDirectPlay { get; set; }
    public bool? CanTranscode { get; set; }
    public string TranscodeParams { get; set; } = "";
    public string ErrorReason { get; set; } = "";
    public SourceStream? SourceStream { get; set; }
    [JsonPropertyName("transcodeReason")]
    public string[] TranscodeReasons { get; set; } = [];
    public SourceStream? TranscodeStream { get; set; }
}

internal sealed record SourceStream
{
    public string Protocol { get; set; } = "";
    public string Codec { get; set; } = "";
    public string Container { get; set; } = "";
    public string AudioCodec { get; set; } = "";
    public string AudioProfile { get; set; } = "";
    [JsonPropertyName("audioBitrate")]
    public int? AudioBitRate { get; set; }
    [JsonPropertyName("audioSamplerate")]
    public int? AudioSampleRate { get; set; }
    public int? AudioChannels { get; set; }
    [JsonPropertyName("audioBitdepth")]
    public int? AudioBitDepth { get; set; }
    public double? Duration { get; set; }
    public long? Size { get; set; }
}

internal sealed record ClientInfo
{
    public int? MaxAudioBitrate { get; set; }
    public int? MaxTranscodingAudioBitrate { get; set; }
    public CodecProfile[] CodecProfiles { get; set; } = [];
    public string Name { get; set; } = "";
    public string Platform { get; set; } = "";
    [JsonConverter(typeof(Transport.OneOrMany<DirectPlayProfile>))]
    public DirectPlayProfile[] DirectPlayProfiles { get; set; } = [];
    [JsonConverter(typeof(Transport.OneOrMany<TranscodingProfile>))]
    public TranscodingProfile[] TranscodingProfiles { get; set; } = [];
}

internal sealed record DirectPlayProfile
{
    public string[] Containers { get; set; } = [];
    public string[] AudioCodecs { get; set; } = [];
    public string[] Protocols { get; set; } = [];
    public int? MaxAudioChannels { get; set; }
}

internal sealed record TranscodingProfile
{
    public string Container { get; set; } = "";
    public string AudioCodec { get; set; } = "";
    public string Protocol { get; set; } = "";
    public int? MaxAudioChannels { get; set; }
}

internal sealed record SonicMatch
{
    [JsonPropertyName("entry")]
    public Entry? Song { get; set; }
    public double? Distance { get; set; }
    public double? Similarity { get; set; }
}

internal sealed record CodecProfile
{
    public string Type { get; set; } = "AudioCodec";
    public string Name { get; set; } = "";
    public Limitation[] Limitations { get; set; } = [];
}
internal sealed record Limitation
{
    public string Name { get; set; } = "";
    public string Comparison { get; set; } = "";
    public string[] Values { get; set; } = [];
    public bool Required { get; set; }
}
