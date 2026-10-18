using System.Globalization;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.YouTube.Download;

/// <summary>Builds the yt-dlp arguments for a request. The engine adds the cookie source and the URL.</summary>
internal static class Arguments
{
    /// <summary>The prefix a progress line carries.</summary>
    internal const string ProgressSentinel = "LUNA";

    /// <summary>The prefix a post-processing update carries.</summary>
    internal const string PostProcessSentinel = "LUNAPOST";

    /// <summary>The prefix the final-path line carries.</summary>
    internal const string FileSentinel = "LUNAFILE";

    internal static List<string> Build(Request request)
    {
        var arguments = new List<string>
        {
            "--no-playlist",
            "--no-warnings",
            "--newline",
            "--progress",
            "--force-overwrites",
            "--remote-components", "ejs:github",
            "--progress-template",
            $"download:{ProgressSentinel}|%(progress.status)s|%(progress.downloaded_bytes)s|%(progress.total_bytes)s|%(progress.total_bytes_estimate)s|%(progress.speed)s|%(progress.eta)s",
            "--progress-template",
            $"postprocess:{PostProcessSentinel}|%(progress.status)s|%(progress.postprocessor)s",
            "--print",
            $"after_move:{FileSentinel}|%(filepath)s",
            "-P", request.SaveFolder(),
            "-o", OutputTemplate(request.FileName),
        };
        if (request.ConcurrentFragments > 1)
            arguments.AddRange(["--concurrent-fragments", request.ConcurrentFragments.ToString(CultureInfo.InvariantCulture)]);

        if (request.Kind == DownloadKind.Audio)
        {
            arguments.AddRange(["-f", AudioSelector(request.AudioQuality)]);
            arguments.AddRange(["-x", "--audio-format", AudioFormatName(request.AudioFormat)]);
            if (request.AudioQuality is not (AudioQuality.Best or AudioQuality.Worst))
                arguments.AddRange(["--audio-quality", $"{(int)request.AudioQuality}K"]);
        }
        else
        {
            arguments.AddRange(["-f", VideoSelector(request.VideoQuality)]);
            arguments.AddRange(["--merge-output-format", ContainerName(request.Container)]);
        }

        if (request.EmbedMetadata) arguments.Add("--embed-metadata");
        if (request.EmbedThumbnail) arguments.Add("--embed-thumbnail");
        if (request.EmbedChapters) arguments.Add("--embed-chapters");
        if (request.EmbedSubtitles) arguments.Add("--embed-subs");
        if (request.SponsorBlockRemove) arguments.AddRange(["--sponsorblock-remove", "default"]);

        arguments.AddRange(["--ffmpeg-location", Utils.NativeDirectory]);
        if (Utils.DenoRuntime is string runtime)
            arguments.AddRange(["--js-runtimes", runtime]);
        return arguments;
    }

    /// <summary>The user's name with yt-dlp placeholders disabled, or the title template when no name was given.</summary>
    private static string OutputTemplate(string fileName)
        => string.IsNullOrEmpty(fileName)
            ? "%(title)s.%(ext)s"
            : fileName.Replace("%", "%%") + ".%(ext)s";

    /// <summary>The video format selector; a 0 cap means best, a negative cap means worst.</summary>
    private static string VideoSelector(VideoQuality quality)
        => (int)quality switch
        {
            0 => "bv*+ba/b",
            < 0 => "wv*+wa/w",
            var height => $"bv*[height<=?{height}]+ba/b[height<=?{height}]/bv*+ba/b",
        };

    private static string AudioSelector(AudioQuality quality)
        => quality == AudioQuality.Worst ? "worstaudio/worst" : "bestaudio/best";

    private static string ContainerName(VideoContainer container)
        => container == VideoContainer.Mkv ? "mkv" : "mp4";

    private static string AudioFormatName(AudioFileFormat format)
        => format switch
        {
            AudioFileFormat.M4a => "m4a",
            AudioFileFormat.Opus => "opus",
            AudioFileFormat.Flac => "flac",
            AudioFileFormat.Wav => "wav",
            _ => "mp3",
        };
}
