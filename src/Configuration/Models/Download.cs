using System.Text.Json.Serialization;

namespace LunaPlayer.Configuration.Models;

/// <summary>Whether a download saves video or audio.</summary>
internal enum DownloadKind { Video, Audio }

/// <summary>The container a downloaded video is muxed into.</summary>
internal enum VideoContainer { Mp4, Mkv }

/// <summary>The format a downloaded audio track is written as.</summary>
internal enum AudioFileFormat { Mp3, M4a, Opus, Flac, Wav }

/// <summary>Defaults for a direct download, and the starting values for the download dialog.</summary>
internal sealed class DownloadSettings
{
    /// <summary>Whether a download saves video or audio.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<DownloadKind>))]
    public DownloadKind Kind { get; set; } = DownloadKind.Video;

    /// <summary>Where downloads are saved. Empty means the default folder.</summary>
    public string Folder { get; set; } = "";

    /// <summary>Whether downloads are sorted into 'videos' and 'audios' subfolders of the folder.</summary>
    public bool SplitByType { get; set; } = true;

    /// <summary>The height a video download is capped at.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<VideoQuality>))]
    public VideoQuality VideoQuality { get; set; } = VideoQuality.P1080;

    /// <summary>The container a downloaded video is muxed into.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<VideoContainer>))]
    public VideoContainer Container { get; set; } = VideoContainer.Mp4;

    /// <summary>The format a downloaded audio track is converted to.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<AudioFileFormat>))]
    public AudioFileFormat AudioFormat { get; set; } = AudioFileFormat.Mp3;

    /// <summary>The bitrate a downloaded audio track is converted at.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<AudioQuality>))]
    public AudioQuality AudioQuality { get; set; } = AudioQuality.Kbps256;

    /// <summary>How many fragments yt-dlp fetches at once.</summary>
    public int ConcurrentFragments { get; set; } = 4;

    /// <summary>How many videos the download queue may download at the same time.</summary>
    public int SimultaneousDownloads { get; set; } = 2;

    /// <summary>Write the title and other tags into the file.</summary>
    public bool EmbedMetadata { get; set; } = true;

    /// <summary>Embed the thumbnail as cover art.</summary>
    public bool EmbedThumbnail { get; set; }

    /// <summary>Write chapter marks into the file.</summary>
    public bool EmbedChapters { get; set; }

    /// <summary>Embed available subtitles.</summary>
    public bool EmbedSubtitles { get; set; }

    /// <summary>Cut out SponsorBlock segments.</summary>
    public bool SponsorBlockRemove { get; set; }

    internal DownloadSettings Copy() => (DownloadSettings)MemberwiseClone();

    internal void Apply(DownloadSettings source)
    {
        Kind = source.Kind;
        Folder = source.Folder;
        SplitByType = source.SplitByType;
        VideoQuality = source.VideoQuality;
        Container = source.Container;
        AudioFormat = source.AudioFormat;
        AudioQuality = source.AudioQuality;
        ConcurrentFragments = source.ConcurrentFragments;
        SimultaneousDownloads = source.SimultaneousDownloads;
        EmbedMetadata = source.EmbedMetadata;
        EmbedThumbnail = source.EmbedThumbnail;
        EmbedChapters = source.EmbedChapters;
        EmbedSubtitles = source.EmbedSubtitles;
        SponsorBlockRemove = source.SponsorBlockRemove;
    }
}
