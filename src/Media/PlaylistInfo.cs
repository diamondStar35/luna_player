using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using MpvNet;

namespace LunaPlayer.Media;

internal sealed class PlaylistInfoService
{
    private readonly ConcurrentDictionary<string, (DateTime Modified, double Duration)> _durationCache = new(StringComparer.OrdinalIgnoreCase);

    internal string Build(
        IReadOnlyList<string> files,
        string? currentPath,
        int currentIndex,
        double? currentDuration,
        double? currentElapsed,
        double? currentRemaining,
        Action<ProgressUpdate> report,
        CancellationToken cancellationToken)
    {
        var durations = new List<double?>(files.Count);
        long totalSize = 0;
        using var probe = CreateProbe();
        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = files[index];
            try { totalSize += new FileInfo(path).Length; } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
            var duration = string.Equals(path, currentPath, StringComparison.Ordinal) && currentDuration is > 0
                ? currentDuration
                : ProbeDuration(probe, path, cancellationToken);
            durations.Add(duration);
            report(new(index + 1, files.Count, MediaLibrary.DisplayName(path)));
        }

        var totalDuration = durations.Where(value => value.HasValue).Sum(value => value!.Value);
        var elapsed = Math.Max(0, currentElapsed ?? 0);
        for (var index = 0; index < currentIndex && index < durations.Count; index++) elapsed += durations[index] ?? 0;
        var remaining = Math.Max(0, currentRemaining ?? ((currentDuration ?? 0) - (currentElapsed ?? 0)));
        for (var index = currentIndex + 1; index < durations.Count; index++) remaining += durations[index] ?? 0;
        return string.Join(Environment.NewLine,
            // Translators: The playlist summary. {count} is how many files are loaded.
            TrFormat("Number of files: {count}", files.Count),
            // Translators: The playlist summary. {value} is a size such as "12.5 MB".
            TrFormat("Total size: {value}", FormatSize(totalSize)),
            // Translators: The playlist summary. {value} is a duration as hours:minutes:seconds.
            TrFormat("Total duration: {value}", FormatTime(totalDuration)),
            // Translators: The playlist summary: how much of the whole playlist has already played.
            TrFormat("Elapsed: {value}", FormatTime(elapsed)),
            // Translators: The playlist summary: how much of the whole playlist is left to play.
            TrFormat("Remaining: {value}", FormatTime(remaining)));
    }

    private double? ProbeDuration(MPV probe, string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return null;
        var modified = File.GetLastWriteTimeUtc(path);
        if (_durationCache.TryGetValue(path, out var cached) && cached.Modified == modified) return cached.Duration;
        try
        {
            probe.LoadFile(path);
            var timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < 1200)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var raw = probe.GetProperty("duration");
                if (raw is not null && Convert.ToDouble(raw, CultureInfo.InvariantCulture) is > 0 and var duration)
                {
                    _durationCache[path] = (modified, duration);
                    probe.Command("stop");
                    return duration;
                }
                Thread.Sleep(20);
            }
            probe.Command("stop");
        }
        catch (Exception exception) when (exception is MpvException or InvalidOperationException or FormatException)
        {
        }
        return null;
    }

    private static MPV CreateProbe() => new(options: new Dictionary<string, object?>
    {
        ["vo"] = "null", ["ao"] = "null", ["vid"] = "no", ["terminal"] = false,
    });

    private static string FormatTime(double seconds)
    {
        var total = Math.Max(0, (long)seconds);
        return $"{total / 3600:00}:{total % 3600 / 60:00}:{total % 60:00}";
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.##} {units[unit]}";
    }
}
