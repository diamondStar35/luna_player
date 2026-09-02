using System.Globalization;
using MpvNet;

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
        IReadOnlyList<string> files,
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
        using var probe = new MpvProbe();
        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = files[index];
            try { totalSize += new FileInfo(path).Length; }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
            // What is playing already knows its own length; asking mpv beats reading the file again.
            durations[index] = string.Equals(path, currentPath, StringComparison.Ordinal) && currentDuration is > 0
                ? currentDuration
                // The header answers for every format that states a length. Only what it turns down - an
                // MPEG transport or program stream, raw ADTS, a file too damaged to read - costs a demuxer.
                : MediaHeader.ReadDuration(path) ?? probe.Read(path, cancellationToken);
            report(new ProgressUpdate(index + 1, files.Count, MediaLibrary.DisplayName(path)));
        }

        var totalDuration = durations.Where(value => value.HasValue).Sum(value => value!.Value);
        var elapsed = Math.Max(0, currentElapsed ?? 0);
        for (var index = 0; index < currentIndex && index < durations.Length; index++) elapsed += durations[index] ?? 0;
        var remaining = Math.Max(0, currentRemaining ?? ((currentDuration ?? 0) - (currentElapsed ?? 0)));
        for (var index = currentIndex + 1; index < durations.Length; index++) remaining += durations[index] ?? 0;
        return new PlaylistTotals(files.Count, totalSize, totalDuration, elapsed, remaining);
    }

    /// <summary>The fallback: loads a file into mpv to find out how long it is.</summary>
    ///
    /// <remarks>
    /// Only reached for the formats whose header states no length, and for a file the header reader could
    /// not make sense of. That is a small enough share of any real library that the cost of a demuxer per
    /// file stops mattering, which is the whole reason the header reader exists.
    ///
    /// The instance is created on first need, so a scan that never falls back never pays for it, and it
    /// waits on mpv's own file-loaded event rather than polling for the duration to appear.
    /// </remarks>
    private sealed class MpvProbe : IDisposable
    {
        private const int TimeoutMilliseconds = 2000;
        private readonly Lock _sync = new();
        private MPV? _probe;
        private SemaphoreSlim? _loaded;
        private IDisposable? _registration;
        private bool _disposed;

        internal double? Read(string path, CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (_disposed)
                    return null;
                try
                {
                    if (_probe is null)
                    {
                        _probe = new MPV(options: new Dictionary<string, object?>
                        {
                            ["vo"] = "null", ["ao"] = "null", ["vid"] = "no", ["terminal"] = false,
                            ["idle"] = "yes",
                        });
                        _loaded = new SemaphoreSlim(0);
                        _registration = _probe.OnEvent(_ => _loaded.Release(), MpvEventId.FileLoaded);
                    }
                    // Anything left over from the file before would otherwise be read as this one's signal.
                    while (_loaded!.CurrentCount > 0) _loaded.Wait(0, CancellationToken.None);
                    _probe.LoadFile(path);
                    if (!_loaded.Wait(TimeoutMilliseconds, cancellationToken))
                        return null;
                    var raw = _probe.GetProperty("duration");
                    var duration = raw is null ? null : (double?)Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                    return duration is > 0 ? duration : null;
                }
                catch (Exception exception) when (exception is MpvException or InvalidOperationException or FormatException)
                {
                    return null;
                }
                finally
                {
                    try { _probe?.Command("stop"); }
                    catch (Exception exception) when (exception is MpvException or InvalidOperationException) { }
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _disposed = true;
                _registration?.Dispose();
                _registration = null;
                _probe?.Dispose();
                _probe = null;
                _loaded?.Dispose();
                _loaded = null;
            }
        }
    }
}
