using System.Diagnostics;
using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.YouTube;

namespace LunaPlayer.Media;

internal readonly record struct SelectionExportOutcome(bool Success, string? Error);

/// <summary>Writes one precise selection with FFmpeg, retaining audio for audio and both picture and sound
/// for video. Encoder defaults supply every setting the user did not choose, matching the source where the
/// output container permits it.</summary>
internal sealed class SelectionExporter
{
    private static readonly string FfmpegPath = Path.Combine(Utils.NativeDirectory, "ffmpeg.exe");

    internal SelectionExportOutcome Export(
        string source, string destination, SelectionRange range, bool video,
        Action<ProgressUpdate> report, CancellationToken token)
    {
        var temporary = TemporaryPath(destination);
        var duration = Math.Max(0.001, range.End - range.Start);
        var totalMilliseconds = (int)Math.Clamp(Math.Ceiling(duration * 1000), 1, int.MaxValue);
        try
        {
            Paths.EnsureDirectoryFor(destination);
            Discard(temporary);
            using var process = Utils.Start(FfmpegPath, BuildArguments(source, temporary, range, video));
            using var abort = token.Register(() => Stop(process));
            var errors = Task.Run(() => LastError(process), CancellationToken.None);
            while (process.StandardOutput.ReadLine() is string line)
            {
                token.ThrowIfCancellationRequested();
                if (!line.StartsWith("out_time=", StringComparison.Ordinal)
                    || !TimeSpan.TryParse(line[9..], CultureInfo.InvariantCulture, out var elapsed))
                    continue;
                report(new ProgressUpdate(
                    (int)Math.Clamp(elapsed.TotalMilliseconds, 0, totalMilliseconds),
                    totalMilliseconds,
                    Path.GetFileName(destination)));
            }
            process.WaitForExit();
            var error = errors.GetAwaiter().GetResult();
            token.ThrowIfCancellationRequested();
            if (process.ExitCode != 0)
            {
                Discard(temporary);
                return new SelectionExportOutcome(false, error);
            }
            File.Move(temporary, destination, overwrite: true);
            report(new ProgressUpdate(1, 1, Path.GetFileName(destination)));
            return new SelectionExportOutcome(true, null);
        }
        catch (OperationCanceledException)
        {
            Discard(temporary);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Discard(temporary);
            LunaPlayer.Application.CrashReport.Note(exception);
            return new SelectionExportOutcome(false, exception.Message);
        }
    }

    private static IReadOnlyList<string> BuildArguments(
        string source, string destination, SelectionRange range, bool video)
    {
        var duration = Precision.Normalize(range.End - range.Start);
        var arguments = new List<string>
        {
            "-nostdin", "-y", "-hide_banner",
            "-i", source,
            // Input-side seeking is fast but begins video on a keyframe. Output-side seeking decodes up to
            // the exact requested timestamp, which is why millisecond markers remain meaningful.
            "-ss", range.Start.ToString("0.###", CultureInfo.InvariantCulture),
            "-t", duration.ToString("0.###", CultureInfo.InvariantCulture),
            "-map_metadata", "0",
        };
        var extension = Path.GetExtension(destination).ToLowerInvariant();
        if (video)
        {
            arguments.AddRange(["-map", "0:v:0", "-map", "0:a?"]);
            arguments.AddRange(extension switch
            {
                ".webm" => ["-c:v", "libvpx-vp9", "-c:a", "libopus"],
                ".avi" => ["-c:v", "mpeg4", "-c:a", "libmp3lame"],
                // The bundled FFmpeg deliberately has no GPL libx264 encoder. Its native MPEG-4 encoder
                // is software-only, shipped on every machine, and supported by MP4, MOV and Matroska.
                _ => ["-c:v", "mpeg4", "-c:a", "aac"],
            });
        }
        else
        {
            arguments.AddRange(["-vn", "-map", "0:a:0", "-c:a", AudioCodec(extension)]);
        }
        arguments.AddRange(["-progress", "pipe:1", "-nostats", destination]);
        return arguments;
    }

    private static string AudioCodec(string extension) => extension switch
    {
        ".m4a" => "aac",
        ".flac" => "flac",
        ".wav" => "pcm_s16le",
        ".ogg" => "libvorbis",
        ".opus" => "libopus",
        _ => "libmp3lame",
    };

    private static string TemporaryPath(string destination)
    {
        var directory = Path.GetDirectoryName(destination) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(destination);
        return Path.Combine(directory, $"{name}.selection-{Guid.NewGuid():N}{Path.GetExtension(destination)}");
    }

    private static string? LastError(Process process)
    {
        string? last = null;
        while (process.StandardError.ReadLine() is string line)
            if (!string.IsNullOrWhiteSpace(line))
                last = line.Trim();
        return last;
    }

    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
        {
            // The process finished between HasExited and Kill, which is the outcome cancellation wanted.
        }
    }

    private static void Discard(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A best-effort cleanup must not replace the useful export or cancellation result with another error.
        }
    }
}
