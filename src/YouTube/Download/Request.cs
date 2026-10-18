using LunaPlayer.Configuration.Models;

namespace LunaPlayer.YouTube.Download;

/// <summary>One decided download: the video and every choice needed to save it.</summary>
internal sealed record Request
{
    /// <summary>The canonical watch URL.</summary>
    public required string Url { get; init; }

    /// <summary>Whether to save video or audio.</summary>
    public DownloadKind Kind { get; init; }

    /// <summary>The folder to save into.</summary>
    public required string Folder { get; init; }

    /// <summary>Whether to sort into 'videos'/'audios' subfolders of the folder.</summary>
    public bool SplitByType { get; init; } = true;

    /// <summary>The name without extension, or empty to use the video's title.</summary>
    public string FileName { get; init; } = "";

    /// <summary>The height cap for a video save.</summary>
    public VideoQuality VideoQuality { get; init; } = VideoQuality.P1080;

    /// <summary>The container for a video save.</summary>
    public VideoContainer Container { get; init; } = VideoContainer.Mp4;

    /// <summary>The format for an audio save.</summary>
    public AudioFileFormat AudioFormat { get; init; } = AudioFileFormat.Mp3;

    /// <summary>The bitrate for an audio save.</summary>
    public AudioQuality AudioQuality { get; init; } = AudioQuality.Kbps256;

    /// <summary>How many fragments to fetch at once.</summary>
    public int ConcurrentFragments { get; init; } = 1;

    public bool EmbedMetadata { get; init; }
    public bool EmbedThumbnail { get; init; }
    public bool EmbedChapters { get; init; }
    public bool EmbedSubtitles { get; init; }
    public bool SponsorBlockRemove { get; init; }

    /// <summary>The request a direct download makes from the saved settings.</summary>
    internal static Request FromSettings(DownloadSettings settings, string url, string fileName = "")
        => new()
        {
            Url = url,
            Kind = settings.Kind,
            Folder = settings.Folder,
            SplitByType = settings.SplitByType,
            FileName = fileName,
            VideoQuality = settings.VideoQuality,
            Container = settings.Container,
            AudioFormat = settings.AudioFormat,
            AudioQuality = settings.AudioQuality,
            ConcurrentFragments = settings.ConcurrentFragments,
            EmbedMetadata = settings.EmbedMetadata,
            EmbedThumbnail = settings.EmbedThumbnail,
            EmbedChapters = settings.EmbedChapters,
            EmbedSubtitles = settings.EmbedSubtitles,
            SponsorBlockRemove = settings.SponsorBlockRemove,
        };

    /// <summary>The folder the file is written into: the base folder, plus a per-type subfolder when
    /// <see cref="SplitByType"/> is set.</summary>
    internal string SaveFolder()
        => SplitByType ? Path.Combine(Folder, Kind == DownloadKind.Audio ? "audios" : "videos") : Folder;
}

/// <summary>The choices returned by the download dialog and whether to run them now or queue them.</summary>
internal readonly record struct OptionsResult(Request Request, bool AddToQueue);
