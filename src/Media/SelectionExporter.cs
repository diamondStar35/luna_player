using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.YouTube;

namespace LunaPlayer.Media;

internal readonly record struct SelectionExportOutcome(bool Success, string? Error);

/// <summary>Everything needed to write the completed selection slots of one source file.</summary>
internal sealed record SelectionExportBatchRequest(
    string Source,
    IReadOnlyList<SelectionRange> Ranges,
    string Folder,
    string Name,
    string Extension,
    bool Video,
    int MaxConcurrency);

/// <summary>One selection that could not be written, with its intended file and FFmpeg's reason.</summary>
internal readonly record struct SelectionExportFailure(int Number, string File, string Error);

/// <summary>The result of writing every selection in a batch.</summary>
internal sealed record SelectionExportBatchOutcome(
    int Saved,
    int Total,
    IReadOnlyList<SelectionExportFailure> Failures);

/// <summary>Writes precise selections with FFmpeg, retaining audio for audio and both picture and sound for
/// video. Single and concurrent batch saves share the same per-selection path, including progress parsing,
/// cancellation and temporary-file cleanup.</summary>
internal sealed class SelectionExporter
{
    private const int ProgressScale = 1000;
    private static readonly string FfmpegPath = Path.Combine(Utils.NativeDirectory, "ffmpeg.exe");

    internal SelectionExportOutcome Export(
        string source, string destination, SelectionRange range, bool video,
        Action<ProgressUpdate> report, CancellationToken token)
        => ExportOne(
            source, destination, range, video,
            fraction => report(new ProgressUpdate(
                (int)Math.Round(fraction * ProgressScale), ProgressScale, Path.GetFileName(destination))),
            token);

    /// <summary>Writes every range with one FFmpeg process per selection, running no more than the chosen
    /// number at once. Each selection contributes an equal share to the aggregate progress bar.</summary>
    internal SelectionExportBatchOutcome ExportBatch(
        SelectionExportBatchRequest request,
        Action<ProgressUpdate> report,
        CancellationToken token)
    {
        var total = request.Ranges.Count;
        if (total == 0)
            return new SelectionExportBatchOutcome(0, 0, []);

        // Resolve every destination before starting workers. The numbered preferred names are already
        // distinct from one another; Unused protects files left by an earlier batch from being overwritten.
        var destinations = Enumerable.Range(0, total)
            .Select(index => Paths.Unused(Path.Combine(
                request.Folder, $"{request.Name}{index + 1}{request.Extension}")))
            .ToArray();
        var failures = new ConcurrentBag<SelectionExportFailure>();
        var fractions = new double[total];
        var sum = 0.0;
        var gate = new object();

        void Emit(int index, double fraction)
        {
            fraction = Math.Clamp(fraction, 0, 1);
            double aggregate;
            lock (gate)
            {
                sum += fraction - fractions[index];
                fractions[index] = fraction;
                aggregate = sum / total;
            }
            report(new ProgressUpdate(
                (int)Math.Round(aggregate * ProgressScale),
                ProgressScale,
                Path.GetFileName(destinations[index]),
                ItemNumber: index + 1,
                ItemValue: (int)Math.Round(fraction * ProgressScale),
                ItemTotal: ProgressScale));
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(request.MaxConcurrency, 1, 16),
            CancellationToken = token,
        };
        Parallel.ForEach(Enumerable.Range(0, total), options, index =>
        {
            Emit(index, 0);
            var outcome = ExportOne(
                request.Source, destinations[index], request.Ranges[index], request.Video,
                fraction => Emit(index, fraction), token);
            if (!outcome.Success)
            {
                failures.Add(new SelectionExportFailure(
                    index + 1,
                    destinations[index],
                    outcome.Error ?? string.Empty));
                // A failed selection is finished work too; otherwise the total bar could never reach 100%.
                Emit(index, 1);
            }
        });
        var orderedFailures = failures.OrderBy(failure => failure.Number).ToArray();
        return new SelectionExportBatchOutcome(total - orderedFailures.Length, total, orderedFailures);
    }

    /// <summary>The single FFmpeg path shared by one-file and batch saves. Progress is a fraction from zero
    /// to one, leaving each caller to decide whether it drives a file bar or one share of an aggregate.</summary>
    private static SelectionExportOutcome ExportOne(
        string source,
        string destination,
        SelectionRange range,
        bool video,
        Action<double> report,
        CancellationToken token)
    {
        var temporary = TemporaryPath(destination);
        var duration = Math.Max(0.001, range.End - range.Start);
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
                report(Math.Clamp(elapsed.TotalSeconds / duration, 0, 1));
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
            report(1);
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
