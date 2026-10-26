using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playlist;

namespace LunaPlayer.Media;

/// <summary>What a scan of the playlist added up to. Numbers only: the strings a user reads are built from
/// these on the UI thread, because the translation lookup is a wxWidgets object and may not be touched from
/// the thread the scan runs on.</summary>
internal readonly record struct PlaylistTotals(
    int FileCount,
    long TotalBytes,
    double TotalDuration,
    double Elapsed,
    double Remaining);

internal sealed class PlaylistInfoService
{
    /// <summary>Reads every file in the playlist and adds up its size and length. Runs on a worker thread.
    /// </summary>
    internal PlaylistTotals Build(
        IReadOnlyList<PlaylistItem> files,
        string? currentPath,
        int currentIndex,
        double? currentDuration,
        double? currentElapsed,
        double? currentRemaining,
        Action<ProgressUpdate> report,
        CancellationToken cancellationToken)
    {
        var durations = new double?[files.Count];
        long totalSize = 0;
        using var probe = new Probe();
        var measuredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = files[index];
            var path = item.Path;
            try { if (measuredPaths.Add(Paths.Key(path))) totalSize += new FileInfo(path).Length; }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // An inaccessible size does not prevent the file's duration from contributing to the totals.
            }
            // What is playing already knows its own length; asking mpv beats reading the file again.
            durations[index] = item.Chapter is Chapter chapter ? chapter.Duration
                : index == currentIndex && string.Equals(path, currentPath, StringComparison.Ordinal) && currentDuration is > 0
                ? currentDuration
                : Duration(probe, path, cancellationToken);
            report(new ProgressUpdate(index + 1, files.Count, MediaLibrary.DisplayName(path)));
        }

        var totalDuration = durations.Where(value => value.HasValue).Sum(value => value!.Value);
        var elapsed = Math.Max(0, currentElapsed ?? 0);
        for (var index = 0; index < currentIndex && index < durations.Length; index++) elapsed += durations[index] ?? 0;
        var remaining = Math.Max(0, currentRemaining ?? ((currentDuration ?? 0) - (currentElapsed ?? 0)));
        for (var index = currentIndex + 1; index < durations.Length; index++) remaining += durations[index] ?? 0;
        return new PlaylistTotals(
            files.Count,
            totalSize,
            Precision.Normalize(totalDuration),
            Precision.Normalize(elapsed),
            Precision.Normalize(remaining));
    }

    /// <summary>How long one file is, from its header where that can say and from a demuxer where it cannot.
    /// </summary>
    /// <remarks>
    /// A file the header does not recognise as media at all is passed over rather than handed to mpv. The
    /// extension is only a guess about what a file holds - <c>.ts</c> means a transport stream here and a
    /// TypeScript source everywhere else - and a playlist built from a folder of source code would otherwise
    /// spend a demuxer start and a two second timeout on every one of them.
    /// </remarks>
    private static double? Duration(Probe probe, string path, CancellationToken cancellationToken)
        => MediaHeader.Read(path, out var duration) switch
        {
            MediaHeaderVerdict.Duration => duration,
            MediaHeaderVerdict.NeedsDemuxer => probe.Read(path, cancellationToken)?.Duration,
            _ => null,
        };
}
