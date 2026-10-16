using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace LunaPlayer.Spotify;

/// <summary>Buffers encrypted Spotify ranges in a seekable file and caches a complete download.</summary>
internal sealed class SpotifyRangeStream : Stream
{
    private const int BlockSize = 64 * 1024;
    private static readonly TimeSpan ReadWaitTimeout = TimeSpan.FromSeconds(8);
    private readonly HttpClient? _http;
    private readonly string[] _urls;
    private readonly string? _temporaryPath;
    private readonly string? _cachePath;
    private readonly FileStream _file;
    private readonly CancellationTokenSource _cancellation;
    private readonly object _gate = new();
    private readonly bool[] _available;
    private readonly long _length;
    private long _position;
    private int _urgentBlock = -1;
    private int _throughputBytesPerSecond = 8 * 1024;
    private Exception? _error;
    private bool _disposed;

    private SpotifyRangeStream(HttpClient? http, string[] urls, string? temporaryPath,
        string? cachePath, FileStream file, long length, CancellationToken cancellationToken)
    {
        _http = http;
        _urls = urls;
        _temporaryPath = temporaryPath;
        _cachePath = cachePath;
        _file = file;
        _length = length;
        _available = new bool[checked((int)((length + BlockSize - 1) / BlockSize))];
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    internal static SpotifyRangeStream? OpenCached(string cachePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(cachePath)) return null;
        try
        {
            var file = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                BlockSize, FileOptions.RandomAccess);
            if (file.Length <= 167) { file.Dispose(); return null; }
            var result = new SpotifyRangeStream(null, [], null, null, file, file.Length,
                cancellationToken);
            Array.Fill(result._available, true);
            return result;
        }
        catch (IOException) { return null; }
    }

    internal static async Task<SpotifyRangeStream> OpenAsync(HttpClient http, string[] urls,
        string cachePath, CancellationToken cancellationToken)
    {
        if (urls.Length == 0) throw new InvalidDataException();
        using var firstTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        firstTimeout.CancelAfter(TimeSpan.FromSeconds(10));
        var (first, length) = await FetchAsync(http, urls, 0, BlockSize - 1,
            firstTimeout.Token).ConfigureAwait(false);
        if (length <= 167) throw new InvalidDataException();
        var path = Path.Combine(Path.GetTempPath(), "luna-spotify-" + Guid.NewGuid().ToString("N") + ".tmp");
        FileStream? file = null;
        try
        {
            file = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite | FileShare.Delete, BlockSize, FileOptions.RandomAccess);
            file.SetLength(length);
            RandomAccess.Write(file.SafeFileHandle, first, 0);
            var result = new SpotifyRangeStream(http, urls, path, cachePath, file, length,
                cancellationToken);
            result._available[0] = true;
            _ = Task.Run(result.DownloadRemainingAsync);
            return result;
        }
        catch
        {
            file?.Dispose();
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    private static async Task<(byte[] Data, long Total)> FetchAsync(HttpClient http, string[] urls,
        long start, long end, CancellationToken cancellationToken)
    {
        foreach (var url in urls)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new RangeHeaderValue(start, end);
                using var response = await http.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.PartialContent
                    || response.Content.Headers.ContentRange is not { From: { } from, To: { } to,
                        Length: { } total } || from != start || total <= 0
                    || to != Math.Min(end, total - 1)) continue;
                var data = await response.Content.ReadAsByteArrayAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (data.LongLength != to - from + 1) continue;
                return (data, total);
            }
            catch (HttpRequestException) { }
            catch (IOException) { }
        }
        throw new IOException();
    }

    private async Task DownloadRemainingAsync()
    {
        try
        {
            while (!_cancellation.IsCancellationRequested)
            {
                int block;
                int blocks;
                lock (_gate)
                {
                    block = NextBlock();
                    if (block < 0) break;
                    blocks = Math.Max(1, _throughputBytesPerSecond / BlockSize);
                    blocks = Math.Min(blocks, _available.Length - block);
                    for (var next = block + 1; next < block + blocks; next++)
                        if (_available[next]) { blocks = next - block; break; }
                }
                var start = (long)block * BlockSize;
                var started = Stopwatch.GetTimestamp();
                var (data, total) = await FetchAsync(_http!, _urls, start,
                    Math.Min(_length - 1, start + (long)blocks * BlockSize - 1), _cancellation.Token)
                    .ConfigureAwait(false);
                if (total != _length) throw new InvalidDataException();
                RandomAccess.Write(_file.SafeFileHandle, data, start);
                var elapsed = Stopwatch.GetElapsedTime(started).TotalSeconds;
                lock (_gate)
                {
                    for (var next = block; next < block + blocks; next++) _available[next] = true;
                    if (elapsed > 0)
                    {
                        var measured = (int)Math.Clamp(data.Length / elapsed, 8 * 1024,
                            int.MaxValue);
                        _throughputBytesPerSecond = (int)(((long)_throughputBytesPerSecond + measured) / 2);
                    }
                    Monitor.PulseAll(_gate);
                }
            }
            if (!_cancellation.IsCancellationRequested && _cachePath is { } cachePath)
                SaveCache(cachePath);
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { }
        catch (Exception error)
        {
            lock (_gate)
            {
                _error = error;
                Monitor.PulseAll(_gate);
            }
        }
    }

    private void SaveCache(string cachePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        var pending = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(pending, FileMode.CreateNew, FileAccess.Write))
            {
                for (var offset = 0L; offset < _length; offset += BlockSize)
                {
                    _cancellation.Token.ThrowIfCancellationRequested();
                    var buffer = new byte[(int)Math.Min(BlockSize, _length - offset)];
                    var read = 0;
                    while (read < buffer.Length)
                    {
                        var received = RandomAccess.Read(_file.SafeFileHandle,
                            buffer.AsSpan(read), offset + read);
                        if (received == 0) throw new EndOfStreamException();
                        read += received;
                    }
                    output.Write(buffer);
                }
            }
            File.Move(pending, cachePath, overwrite: true);
        }
        finally { if (File.Exists(pending)) File.Delete(pending); }
    }

    private int NextBlock()
    {
        if (_urgentBlock >= 0 && !_available[_urgentBlock])
        {
            var urgent = _urgentBlock;
            _urgentBlock = -1;
            return urgent;
        }
        var positionBlock = Math.Min((int)(_position / BlockSize), _available.Length - 1);
        for (var block = positionBlock; block < _available.Length; block++)
            if (!_available[block]) return block;
        for (var block = 0; block < positionBlock; block++)
            if (!_available[block]) return block;
        return -1;
    }

    public override bool CanRead => !_disposed;
    public override bool CanSeek => !_disposed;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_position >= _length || buffer.IsEmpty) return 0;
        var block = (int)(_position / BlockSize);
        lock (_gate)
        {
            while (!_available[block])
            {
                _cancellation.Token.ThrowIfCancellationRequested();
                if (_error is { } error) throw new IOException(null, error);
                _urgentBlock = block;
                if (!Monitor.Wait(_gate, ReadWaitTimeout)) throw new TimeoutException();
            }
        }
        var length = Math.Min(buffer.Length,
            (int)Math.Min(_length - _position, BlockSize - _position % BlockSize));
        var read = RandomAccess.Read(_file.SafeFileHandle, buffer[..length], _position);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(_position + offset),
            SeekOrigin.End => checked(_length + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        if (target < 0) throw new IOException();
        lock (_gate) _position = target;
        return target;
    }

    internal void Cancel()
    {
        if (_disposed) return;
        _cancellation.Cancel();
        lock (_gate) Monitor.PulseAll(_gate);
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            Cancel();
            _file.Dispose();
            if (_temporaryPath is { } path && File.Exists(path)) File.Delete(path);
            _cancellation.Dispose();
        }
        _disposed = true;
        base.Dispose(disposing);
    }
}
