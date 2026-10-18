using System.Diagnostics;
using System.Globalization;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.YouTube.Download;

/// <summary>Runs one yt-dlp download on a worker thread, reporting progress and returning the saved file or an error.</summary>
internal sealed class Engine
{
    private const int DownloadAttempts = 3;
    private const int ErrorTailLines = 12;

    private readonly Settings _settings;

    /// <param name="settings">Read live so a cookie-source change takes effect on the next download.</param>
    internal Engine(Settings settings) => _settings = settings;

    /// <summary>Downloads the request's video, reporting as bytes arrive. Returns the saved file or the
    /// failure reason; throws <see cref="OperationCanceledException"/> on cancel.</summary>
    internal Outcome Run(Request request, Action<Progress> report, CancellationToken token)
    {
        try
        {
            Directory.CreateDirectory(request.SaveFolder());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return Outcome.Failed(exception.Message);
        }

        Outcome outcome = default;
        for (var attempt = 0; attempt < DownloadAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();
            outcome = RunOnce(request, report, token);
            if (outcome.Success)
                return outcome;
        }
        return outcome;
    }

    private Outcome RunOnce(Request request, Action<Progress> report, CancellationToken token)
    {
        var arguments = Arguments.Build(request);
        var hasCookies = _settings.YouTube.CookiesFromFirefox
            || (!string.IsNullOrEmpty(_settings.YouTube.CookiesPath)
                && File.Exists(_settings.YouTube.CookiesPath));
        // The Android client currently avoids YouTube's SABR-only URLs, which can fail with HTTP 403.
        // Authenticated downloads retain yt-dlp's default client because Android is not cookie-compatible.
        if (!hasCookies)
            arguments.AddRange(["--extractor-args", "youtube:player_client=android;js_variant=main"]);
        Utils.AddCookies(arguments, _settings.YouTube);
        arguments.Add(request.Url);

        using var process = Utils.Start(Utils.YtDlpPath, arguments);
        using var abort = token.Register(() => Stop(process));

        var sync = new object();
        var finalPath = string.Empty;
        var errorTail = new List<string>();

        void Handle(string line)
        {
            if (line.Length == 0)
                return;
            lock (sync)
            {
                if (line.StartsWith(Arguments.FileSentinel + "|", StringComparison.Ordinal))
                {
                    finalPath = line[(Arguments.FileSentinel.Length + 1)..].Trim();
                    return;
                }
                if (TryParseProgress(line, out var progress))
                {
                    report(progress);
                    return;
                }
                errorTail.Add(line);
                if (errorTail.Count > ErrorTailLines)
                    errorTail.RemoveAt(0);
            }
        }

        // Progress and the final-path print can arrive on either stream, so both are read the same way.
        var readingErrors = Task.Run(() =>
        {
            string? line;
            while ((line = process.StandardError.ReadLine()) is not null)
                Handle(line.Trim());
        }, CancellationToken.None);

        try
        {
            string? line;
            while ((line = process.StandardOutput.ReadLine()) is not null)
            {
                token.ThrowIfCancellationRequested();
                Handle(line.Trim());
            }
            process.WaitForExit();
            token.ThrowIfCancellationRequested();
        }
        catch (Exception)
        {
            Stop(process);
            throw;
        }
        finally
        {
            readingErrors.Wait(TimeSpan.FromSeconds(2));
        }

        return process.ExitCode == 0
            ? Outcome.Saved(finalPath)
            : Outcome.Failed(Diagnostic(errorTail, process.ExitCode));
    }

    private static bool TryParseProgress(string line, out Progress progress)
    {
        progress = default;
        if (line.StartsWith(Arguments.PostProcessSentinel + "|", StringComparison.Ordinal))
        {
            var postProcess = line.Split('|', 3);
            if (postProcess.Length < 3)
                return false;
            progress = new Progress(
                postProcess[1], 0, 0, 100, 0, 0,
                postProcess[2].Trim());
            return true;
        }
        if (!line.StartsWith(Arguments.ProgressSentinel + "|", StringComparison.Ordinal))
            return false;
        // Sentinel, status, downloaded, total, total-estimate, speed, eta.
        var parts = line.Split('|');
        if (parts.Length < 7)
            return false;
        var downloaded = ParseLong(parts[2]);
        var total = ParseLong(parts[3]);
        if (total <= 0)
            total = ParseLong(parts[4]);
        var percent = total > 0 ? Math.Clamp(downloaded * 100.0 / total, 0, 100) : 0;
        progress = new Progress(parts[1], downloaded, total, percent, ParseDouble(parts[5]), ParseDouble(parts[6]));
        return true;
    }

    /// <summary>A yt-dlp number field as bytes; "NA" or anything unparseable reads as 0.</summary>
    private static long ParseLong(string text)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? (long)value : 0;

    private static double ParseDouble(string text)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value : 0;

    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
        {
        }
    }

    /// <summary>The failure reason, preferring the last line that looks like a yt-dlp error.</summary>
    private static string Diagnostic(IReadOnlyList<string> errorTail, int exitCode)
    {
        var complaint = string.Empty;
        for (var index = errorTail.Count - 1; index >= 0; index--)
        {
            if (errorTail[index].StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                complaint = errorTail[index].Trim();
                break;
            }
        }
        if (complaint.Length == 0)
            complaint = errorTail.LastOrDefault(line => line.Length > 0)?.Trim() ?? string.Empty;
        if (complaint.Length == 0)
            return $"yt-dlp exited with code {exitCode}.";
        return complaint.Length <= 220 ? complaint : string.Concat(complaint.AsSpan(0, 220).TrimEnd(), "...");
    }
}

