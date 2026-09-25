using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.YouTube;

namespace LunaPlayer.Media;

/// <summary>How Opus is written, which no other format needs.</summary>
/// <param name="VariableBitrate">Whether the rate varies with the sound (true) or is held steady (false).</param>
/// <param name="Application">What libopus tunes for: "audio", "voip" or "lowdelay".</param>
internal readonly record struct OpusSettings(bool VariableBitrate, string Application);

/// <summary>The encoder settings a conversion is written with.</summary>
/// <param name="Codec">The ffmpeg encoder that writes the format, e.g. "libmp3lame".</param>
/// <param name="SampleRate">The sample rate in hertz, or null to keep each file's own.</param>
/// <param name="Channels">1 or 2, or null to keep each file's own channel count.</param>
/// <param name="Bitrate">The target bitrate in kbps for a lossy format, or null to leave it to the encoder
/// - which is the case for a lossless format and for "Preserve original".</param>
/// <param name="Opus">The Opus-only settings, present only when the codec is libopus.</param>
internal sealed record ConversionSettings(
    string Codec, int? SampleRate, int? Channels, int? Bitrate, OpusSettings? Opus);

/// <summary>One file to convert and where its result is to go.</summary>
/// <param name="Source">The file read from.</param>
/// <param name="Destination">Where the converted file is to be written. The final name may differ, so a
/// file already there - the source among them - is never overwritten.</param>
internal readonly record struct ConversionJob(string Source, string Destination);

/// <summary>A batch of files to convert, with the settings and where they go.</summary>
/// <param name="MaxConcurrency">How many files may be converted at once. Each file is handled by exactly one
/// worker, so no two ever touch the same file.</param>
/// <param name="DeleteOriginals">Whether each source is removed once its conversion has succeeded.</param>
internal sealed record ConversionRequest(
    IReadOnlyList<ConversionJob> Jobs, ConversionSettings Settings, int MaxConcurrency, bool DeleteOriginals);

/// <summary>What a finished batch produced.</summary>
/// <param name="Converted">How many files were written.</param>
/// <param name="Failures">The names of the files that could not be converted.</param>
internal sealed record ConversionOutcome(int Converted, IReadOnlyList<string> Failures)
{
    internal int Total => Converted + Failures.Count;
}

/// <summary>A snapshot of how far a running batch has got, for the converter's progress window.</summary>
/// <param name="FileNumber">The 1-based place of the file this update is about in the batch.</param>
/// <param name="TotalFiles">How many files the batch holds.</param>
/// <param name="CurrentName">The display name of the file this update is about.</param>
/// <param name="FilePercent">How far that file is through, 0 to 100.</param>
/// <param name="RemainingFiles">How many files have not finished yet.</param>
/// <param name="TotalPercent">How far the whole batch is through, 0 to 100, each file weighed the same.</param>
/// <param name="Elapsed">How long the batch has been running.</param>
/// <param name="EstimatedRemaining">A guess at the time left, or null while there is too little to guess
/// from.</param>
internal readonly record struct ConversionProgress(
    int FileNumber, int TotalFiles, string CurrentName, double FilePercent,
    int RemainingFiles, double TotalPercent, TimeSpan Elapsed, TimeSpan? EstimatedRemaining);

/// <summary>Converts a batch of media files to an audio format with the bundled FFmpeg.</summary>
///
/// <remarks>
/// Stateless: the window gathers the settings into a <see cref="ConversionRequest"/> and the Tools handler
/// runs it here behind a progress window. Each file is one FFmpeg invocation, and the files are spread over
/// as many workers as the request allows.
///
/// No two workers ever touch the same file. The sources are distinct - they are the list the user built -
/// and the destinations are made distinct from one another and from everything already on disk once, up
/// front and single-threaded, before any worker starts. A worker therefore reads a file no other worker
/// reads and writes a file no other worker writes, so nothing has to be locked while the batch runs.
/// </remarks>
internal sealed class MediaConverter
{
    /// <summary>The FFmpeg beside the player, in the same native-library folder yt-dlp is pointed at.</summary>
    private static readonly string FfmpegPath = Path.Combine(Tools.NativeDirectory, "ffmpeg.exe");

    /// <summary>How long to wait for a pipe-draining thread to notice its process has gone. It has almost
    /// always finished the instant the process exits; this only bounds the wait if it has not.</summary>
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Converts every file in <paramref name="request"/>, reporting as each one moves along.</summary>
    /// <param name="report">Called from a worker thread with a fresh snapshot whenever a file starts, makes
    /// progress or finishes. Several workers may call it at once, so it must be safe to call from any thread.
    /// </param>
    /// <param name="token">Set when the user aborts; the run stops and throws
    /// <see cref="OperationCanceledException"/>, leaving no half-written file behind.</param>
    internal ConversionOutcome Run(
        ConversionRequest request, Action<ConversionProgress> report, CancellationToken token)
    {
        var jobs = ResolveDestinations(request.Jobs);
        var total = jobs.Count;
        var failures = new ConcurrentBag<string>();
        // One slot per file, holding its fraction from 0 to 1. The total is their mean, so a file that is
        // half done counts half whether it is the biggest in the batch or the smallest. Their running sum is
        // kept beside them so a report costs nothing extra however many files the batch holds - a folder sent
        // here can hold a hundred thousand.
        var fractions = new double[total];
        var sum = 0.0;
        var stopwatch = Stopwatch.StartNew();
        var completed = 0;
        var gate = new object();
        // Builds and sends a snapshot. Reads and updates the shared totals under the lock so two workers
        // reporting at once cannot see each other half-written; the arithmetic and the report happen outside.
        void Emit(int index, string name, double fileFraction)
        {
            double totalFraction;
            int done;
            lock (gate)
            {
                sum += fileFraction - fractions[index];
                fractions[index] = fileFraction;
                totalFraction = sum / total;
                done = completed;
            }
            var elapsed = stopwatch.Elapsed;
            // Only worth a guess once enough is done that the rate has settled; before that it swings wildly.
            TimeSpan? remaining = totalFraction > 0.02
                ? TimeSpan.FromSeconds(elapsed.TotalSeconds * (1 - totalFraction) / totalFraction)
                : null;
            report(new ConversionProgress(
                index + 1, total, name, Math.Clamp(fileFraction, 0, 1) * 100,
                total - done, Math.Clamp(totalFraction, 0, 1) * 100, elapsed, remaining));
        }
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, request.MaxConcurrency),
            CancellationToken = token,
        };
        Parallel.ForEach(Enumerable.Range(0, total), options, index =>
        {
            var job = jobs[index];
            var name = MediaLibrary.DisplayName(job.Source);
            Emit(index, name, 0);
            if (ConvertOne(job.Source, job.Destination, request.Settings,
                    fraction => Emit(index, name, fraction), token))
            {
                if (request.DeleteOriginals)
                    DeleteOriginal(job.Source);
            }
            else
            {
                failures.Add(name);
            }
            lock (gate)
            {
                completed++;
            }
            Emit(index, name, 1);
        });
        return new ConversionOutcome(total - failures.Count, failures.ToArray());
    }

    /// <summary>Gives each job a destination nothing else will write and nothing already holds.</summary>
    ///
    /// <remarks>
    /// Done here rather than in each worker because the answer depends on the other jobs: two files of the
    /// same name headed for one folder, or a job whose chosen name matches one an earlier job took, must be
    /// told apart before the writing starts. The claimed set stands in for the files that do not exist yet;
    /// <see cref="File.Exists"/> stands in for the ones that do, the sources among them.
    /// </remarks>
    private static IReadOnlyList<ConversionJob> ResolveDestinations(IReadOnlyList<ConversionJob> jobs)
    {
        var resolved = new List<ConversionJob>(jobs.Count);
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var job in jobs)
        {
            var destination = UniqueDestination(job.Destination, claimed);
            claimed.Add(destination);
            resolved.Add(job with { Destination = destination });
        }
        return resolved;
    }

    /// <summary>The preferred path, or the first " (n)" variant free of both the claimed set and the disk.
    /// </summary>
    private static string UniqueDestination(string preferred, HashSet<string> claimed)
    {
        if (!claimed.Contains(preferred) && !File.Exists(preferred))
            return preferred;
        var directory = Path.GetDirectoryName(preferred) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(preferred);
        var extension = Path.GetExtension(preferred);
        for (var number = 2; number < 100000; number++)
        {
            var candidate = Path.Combine(directory, $"{name} ({number}){extension}");
            if (!claimed.Contains(candidate) && !File.Exists(candidate))
                return candidate;
        }
        return preferred;
    }

    /// <summary>Runs FFmpeg once, turning <paramref name="source"/> into <paramref name="destination"/>.
    /// </summary>
    /// <returns>True when FFmpeg wrote the file and exited cleanly; false when it failed, the partial output
    /// discarded either way.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when the user aborts, after killing FFmpeg
    /// and removing whatever it had written. A failure of one file is caught and returned as false so the
    /// rest of the batch carries on; only an abort stops everything.</remarks>
    private static bool ConvertOne(
        string source, string destination, ConversionSettings settings,
        Action<double> onProgress, CancellationToken token)
    {
        Paths.EnsureDirectoryFor(destination);
        try
        {
            using var process = Tools.Start(FfmpegPath, BuildArguments(source, destination, settings));
            // Killed the moment the token is set, not at FFmpeg's next line of output: a stalled or very
            // long conversion must answer the Cancel button at once.
            using var abort = token.Register(() => Stop(process));
            // FFmpeg writes its progress and its complaints to stderr and can fill the pipe; drain both on
            // threads of their own so a full pipe never stops it. The stderr side is read for the file's
            // duration and its running time, which is where the per-file percentage comes from.
            var draining = Task.Run(() => Drain(process, onProgress), CancellationToken.None);
            process.WaitForExit();
            draining.Wait(DrainTimeout);
            // A killed FFmpeg exits like any other, so the abort is read from the token rather than mistaken
            // for a program that failed.
            token.ThrowIfCancellationRequested();
            if (process.ExitCode == 0)
                return true;
            Discard(destination);
            return false;
        }
        catch (OperationCanceledException)
        {
            Discard(destination);
            throw;
        }
        catch (Exception failure)
        {
            // One file that will not convert - a codec that chokes on it, a name the disk rejects - is a
            // failure to report, not a crash to end the batch with. Noted so the cause is not lost.
            LunaPlayer.Application.CrashReport.Note(failure);
            Discard(destination);
            return false;
        }
    }

    /// <summary>The FFmpeg command line for one file: sound only, metadata carried across, then whatever of
    /// the sample rate, channels, bitrate and Opus settings the request pinned down.</summary>
    private static IReadOnlyList<string> BuildArguments(
        string source, string destination, ConversionSettings settings)
    {
        var arguments = new List<string>
        {
            // No console input, never prompt to overwrite (the destination is one nothing holds), no banner.
            "-nostdin", "-y", "-hide_banner",
            "-i", source,
            // Drop any video stream; this only ever writes audio. Cover art would otherwise stall some
            // encoders or be copied as a video track the container will not take.
            "-vn",
            // Carry titles, artist and the rest across rather than dropping them on the floor.
            "-map_metadata", "0",
            "-c:a", settings.Codec,
        };
        if (settings.SampleRate is int rate)
            arguments.AddRange(["-ar", rate.ToString()]);
        if (settings.Channels is int channels)
            arguments.AddRange(["-ac", channels.ToString()]);
        if (settings.Bitrate is int bitrate)
            arguments.AddRange(["-b:a", $"{bitrate}k"]);
        if (settings.Opus is OpusSettings opus)
            arguments.AddRange(["-vbr", opus.VariableBitrate ? "on" : "off", "-application", opus.Application]);
        arguments.Add(destination);
        return arguments;
    }

    /// <summary>Reads a process's output to the end so its pipes never fill and stall it, watching stderr
    /// for the file's duration and running time and turning them into a fraction from 0 to 1.</summary>
    /// <remarks>
    /// FFmpeg prints "Duration: HH:MM:SS.ss" once near the top and then a stream of "time=HH:MM:SS.ss"
    /// lines as it works, each overwriting the last with a lone carriage return. A .NET line reader treats
    /// that carriage return as a line break, so every one of those status writes arrives here as its own
    /// line and the percentage climbs smoothly. Stdout carries nothing we need but is drained anyway.
    /// </remarks>
    private static void Drain(Process process, Action<double> onProgress)
    {
        var output = Task.Run(
            () => { while (process.StandardOutput.ReadLine() is not null) { } }, CancellationToken.None);
        var duration = 0.0;
        string? line;
        while ((line = process.StandardError.ReadLine()) is not null)
        {
            if (duration <= 0)
            {
                var parsed = ParseClockAfter(line, "Duration:", ',');
                if (parsed > 0)
                {
                    duration = parsed;
                    continue;
                }
            }
            if (duration > 0)
            {
                var time = ParseClockAfter(line, "time=", ' ');
                if (time >= 0)
                    onProgress(Math.Clamp(time / duration, 0, 1));
            }
        }
        output.Wait(DrainTimeout);
    }

    /// <summary>Reads the "HH:MM:SS.ss" clock that follows <paramref name="marker"/> on a line, up to
    /// <paramref name="terminator"/> or the next space, in seconds. Returns -1 when the marker is absent or
    /// what follows is not a clock (FFmpeg writes "time=N/A" before the first frame).</summary>
    private static double ParseClockAfter(string line, string marker, char terminator)
    {
        var start = line.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return -1;
        start += marker.Length;
        while (start < line.Length && line[start] == ' ')
            start++;
        var end = start;
        while (end < line.Length && line[end] != terminator && line[end] != ' ')
            end++;
        return ParseClock(line[start..end]);
    }

    /// <summary>Turns "HH:MM:SS.ss" into seconds, or -1 when it is not that shape.</summary>
    private static double ParseClock(string token)
    {
        if (token.Length == 0 || token[0] == '-' || !token.Contains(':'))
            return -1;
        var parts = token.Split(':');
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)
            || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            return -1;
        return hours * 3600 + minutes * 60 + seconds;
    }

    /// <summary>Kills FFmpeg, tolerating its having already exited between the test and the kill.</summary>
    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
        {
            // It finished on its own, which is the outcome the kill was after.
        }
    }

    /// <summary>Removes a half-written or rejected output, so a failed or aborted conversion leaves nothing
    /// behind that looks like a finished file.</summary>
    private static void Discard(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Nothing more can be done about it here, and it is not worth ending the batch over.
        }
    }

    /// <summary>Removes a source once its conversion has succeeded, when the user asked for that.</summary>
    private static void DeleteOriginal(string source)
    {
        try
        {
            File.Delete(source);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The converted file is written; failing to remove the original is not worth failing the file.
        }
    }
}
