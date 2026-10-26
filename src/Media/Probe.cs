using System.Collections.Concurrent;
using System.Globalization;
using MpvNet;

namespace LunaPlayer.Media;

/// <summary>Reads demuxer metadata without opening an audio device. Read runs on a worker;
/// cached results can be inspected without waiting for the demuxer.</summary>
internal sealed class Probe : IDisposable
{
    internal readonly record struct Result(double? Duration, bool HasAudio, bool HasVideo, IReadOnlyList<Chapter>? Chapters = null)
    {
        internal bool AudioOnly => HasAudio && !HasVideo;
    }

    private sealed record Cached(Result Value, long Length, DateTime Modified, DateTime Expires);
    private readonly ConcurrentDictionary<string, Cached> _cache = new(StringComparer.Ordinal);
    private readonly Lock _sync = new();
    private MPV? _mpv;
    private SemaphoreSlim? _loaded;
    private IDisposable? _registration;
    private bool _disposed;

    internal Result? CachedResult(string path)
    {
        if (!_cache.TryGetValue(path, out var cached) || cached.Expires <= DateTime.UtcNow)
            return null;
        if (!IsRemote(path))
        {
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists || file.Length != cached.Length || file.LastWriteTimeUtc != cached.Modified)
                    return null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                or ArgumentException or NotSupportedException)
            {
                return null;
            }
        }
        return cached.Value;
    }

    internal Result? Read(string path, CancellationToken cancellationToken, Func<bool>? isCurrent = null)
    {
        if (CachedResult(path) is Result cached)
            return cached;
        lock (_sync)
        {
            if (_disposed || cancellationToken.IsCancellationRequested || isCurrent?.Invoke() == false)
                return null;
            try
            {
                if (_mpv is null)
                {
                    _mpv = new MPV(options: new Dictionary<string, object?>
                    {
                        ["vo"] = "null", ["ao"] = "null", ["vid"] = "no",
                        ["pause"] = true, ["terminal"] = false, ["idle"] = "yes",
                        ["load_scripts"] = false, ["sub_auto"] = "no", ["network_timeout"] = 2,
                    });
                    var loaded = new SemaphoreSlim(0);
                    _loaded = loaded;
                    _registration = _mpv.OnEvent(_ =>
                    {
                        try { loaded.Release(); }
                        catch (ObjectDisposedException)
                        {
                            // A callback already being delivered may finish during probe disposal.
                        }
                    }, MpvEventId.FileLoaded);
                }
                while (_loaded!.CurrentCount > 0)
                    _loaded.Wait(0, CancellationToken.None);
                _mpv.LoadFileAsync(path);
                if (!_loaded.Wait(2000, cancellationToken))
                    return null;
                if (!string.Equals(Convert.ToString(_mpv.GetProperty("path"), CultureInfo.InvariantCulture),
                        path, StringComparison.Ordinal))
                    return null;
                if (_mpv.GetProperty("track-list") is not IEnumerable<object?> tracks)
                    return null;
                var result = Tracks(tracks, _mpv.GetProperty("duration"));
                result = result with { Chapters = Chapters.Read(_mpv.GetProperty("chapter-list"), result.Duration) };
                long length = 0;
                var modified = DateTime.MinValue;
                if (!IsRemote(path))
                {
                    var file = new FileInfo(path);
                    length = file.Length;
                    modified = file.LastWriteTimeUtc;
                }
                if (_cache.Count >= 128)
                    _cache.Clear();
                _cache[path] = new Cached(result, length, modified, DateTime.UtcNow.AddMinutes(2));
                return result;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception exception) when (exception is MpvException or InvalidOperationException
                or FormatException or InvalidCastException or OverflowException or IOException
                or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return null;
            }
            finally
            {
                try { _mpv?.Command("stop"); }
                catch (Exception exception) when (exception is MpvException or InvalidOperationException)
                {
                    // A failed or canceled demuxer may already have stopped.
                }
            }
        }
    }

    internal static Result Tracks(IEnumerable<object?> tracks, object? duration = null)
    {
        var audio = false;
        var video = false;
        foreach (var value in tracks)
        {
            if (value is not IDictionary<string, object?> track || !track.TryGetValue("type", out var type))
                continue;
            audio |= type as string == "audio";
            // Attached cover art belongs to audio files. An unselected real video track still excludes
            // the file, even when the caller asked mpv to play only its sound.
            video |= type as string == "video"
                && !(track.TryGetValue("albumart", out var art) && art is true);
        }
        var seconds = duration is null ? null : (double?)Convert.ToDouble(duration, CultureInfo.InvariantCulture);
        return new Result(seconds is > 0 ? seconds : null, audio, video);
    }

    private static bool IsRemote(string path)
        => Uri.TryCreate(path, UriKind.Absolute, out var uri) && !uri.IsFile;

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            _registration?.Dispose();
            _mpv?.Dispose();
            _loaded?.Dispose();
            _registration = null;
            _mpv = null;
            _loaded = null;
            _cache.Clear();
        }
    }
}
