using System.Net;
using System.Net.Http.Headers;

namespace LunaPlayer.Spotify;

/// <summary>Fetches requested encrypted audio blocks with bounded, memory-only read-ahead.</summary>
/// <remarks>Demand reads use their own request, cancelling speculative work before a miss or seek.
/// The worker stops four MiB ahead of the reader; it never downloads the remainder of a track.</remarks>
internal sealed class BufferedAudioStream : Stream
{
    private const int BlockSize = 64 * 1024;
    private const int ReadAheadBlocks = 4 * 1024 * 1024 / BlockSize;
    private const int MaximumBlocks = 20 * 1024 * 1024 / BlockSize;
    private const int FetchBlocks = 4;
    private readonly HttpClient _http;
    private readonly string[] _urls;
    private readonly long _length;
    private readonly object _gate = new();
    private readonly Dictionary<long, Block> _blocks = [];
    private readonly LinkedList<long> _recent = new();
    private readonly CancellationTokenSource _lifetime;
    private readonly CancellationToken _token;
    private readonly CancellationTokenRegistration _stoppingRegistration;
    private readonly SemaphoreSlim _wake = new(0, 1);
    private readonly Task _worker;
    private CancellationTokenSource? _speculation;
    private long _position;
    private int _demand;
    private bool _readingStarted;
    private bool _stopped;
    private bool _disposed;

    private sealed record Block(byte[] Data, LinkedListNode<long> Recent);

    private BufferedAudioStream(HttpClient http, string[] urls, byte[] first, long length,
        CancellationToken cancellationToken)
    {
        _http = http;
        _urls = urls;
        _length = length;
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _token = _lifetime.Token;
        Store(0, first);
        // A new selection cancels the host token before mpv closes the old stream. Release
        // retained audio at that point as well as aborting its outstanding HTTP requests.
        _stoppingRegistration = _token.UnsafeRegister(static state =>
        {
            var stream = (BufferedAudioStream)state!;
            lock (stream._gate)
            {
                stream._stopped = true;
                stream._blocks.Clear();
                stream._recent.Clear();
            }
        }, this);
        _worker = Task.Run(ReadAheadAsync);
    }

    internal static async Task<BufferedAudioStream> OpenAsync(HttpClient http, string[] urls,
        CancellationToken cancellationToken)
    {
        if (urls.Length == 0) throw new InvalidDataException();
        var (first, length) = await FetchAsync(http, urls, 0, BlockSize - 1,
            cancellationToken).ConfigureAwait(false);
        if (length <= 167) throw new InvalidDataException();
        cancellationToken.ThrowIfCancellationRequested();
        return new BufferedAudioStream(http, urls, first, length, cancellationToken);
    }

    private static async Task<(byte[] Data, long Total)> FetchAsync(HttpClient http, string[] urls,
        long start, long end, CancellationToken cancellationToken)
    {
        Exception? failure = null;
        foreach (var url in urls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new RangeHeaderValue(start, end);
                using var response = await http.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.PartialContent
                    || response.Content.Headers.ContentRange is not { From: { } from, To: { } to,
                        Length: { } total } || from != start || total <= 0
                    || to != Math.Min(end, total - 1))
                    throw new InvalidDataException("Invalid audio range response.");
                // Read only the validated range, even if a server sends an unbounded response body.
                var data = new byte[checked((int)(to - from + 1))];
                using var body = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                await body.ReadExactlyAsync(data, timeout.Token).ConfigureAwait(false);
                var extra = new byte[1];
                if (await body.ReadAsync(extra, timeout.Token).ConfigureAwait(false) != 0)
                    throw new InvalidDataException("Audio range response is too long.");
                return (data, total);
            }
            catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException or ObjectDisposedException
                || error is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                // CDN addresses are alternatives. Retain the cause if none serves this range.
                failure = error;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        throw new IOException("Could not fetch the requested audio range.", failure);
    }

    private void Store(long firstBlock, byte[] data)
    {
        for (var offset = 0; offset < data.Length; offset += BlockSize)
        {
            var number = firstBlock + offset / BlockSize;
            if (_blocks.ContainsKey(number)) continue;
            while (_blocks.Count >= MaximumBlocks)
            {
                _blocks.Remove(_recent.First!.Value);
                _recent.RemoveFirst();
            }
            var bytes = data.AsSpan(offset, Math.Min(BlockSize, data.Length - offset)).ToArray();
            _blocks.Add(number, new Block(bytes, _recent.AddLast(number)));
        }
    }

    private void Wake()
    {
        if (_wake.CurrentCount == 0) _wake.Release();
    }

    private async Task ReadAheadAsync()
    {
        try
        {
            while (true)
            {
                await _wake.WaitAsync(_token).ConfigureAwait(false);
                while (true)
                {
                    long block;
                    long end;
                    CancellationTokenSource speculation;
                    lock (_gate)
                    {
                        _token.ThrowIfCancellationRequested();
                        if (_demand > 0 || !_readingStarted || _position >= _length) break;
                        block = _position / BlockSize;
                        var limit = Math.Min((_length - 1) / BlockSize, block + ReadAheadBlocks);
                        while (block <= limit && _blocks.ContainsKey(block)) block++;
                        if (block > limit) break;
                        var last = Math.Min(limit, block + FetchBlocks - 1);
                        for (var next = block + 1; next <= last; next++)
                            if (_blocks.ContainsKey(next)) { last = next - 1; break; }
                        // Cap the byte window as well as its block count at unaligned read positions.
                        end = Math.Min(_length - 1, Math.Min((last + 1) * BlockSize - 1,
                            (_position / BlockSize + ReadAheadBlocks) * BlockSize - 1));
                        if (end < block * BlockSize) break;
                        speculation = CancellationTokenSource.CreateLinkedTokenSource(_token);
                        _speculation = speculation;
                    }
                    try
                    {
                        var (data, total) = await FetchAsync(_http, _urls, block * BlockSize, end,
                            speculation.Token).ConfigureAwait(false);
                        if (total != _length) throw new InvalidDataException("Audio length changed.");
                        lock (_gate)
                        {
                            if (!speculation.IsCancellationRequested) Store(block, data);
                        }
                    }
                    catch (OperationCanceledException) when (speculation.IsCancellationRequested)
                    {
                        // A seek or demand miss has priority; it will wake the worker at the new position.
                        break;
                    }
                    catch (Exception error) when (error is IOException or InvalidDataException)
                    {
                        // Read-ahead is optional. A demand read retries this block and reports any failure.
                        break;
                    }
                    finally
                    {
                        lock (_gate)
                        {
                            _speculation = null;
                            speculation.Dispose();
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (_token.IsCancellationRequested)
        {
            // Track changes and mpv cancellation terminate the worker and its current HTTP request.
        }
    }

    public override bool CanRead => !_disposed;
    public override bool CanSeek => !_disposed;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position
    {
        get { lock (_gate) return _position; }
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        long position;
        long block;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _token.ThrowIfCancellationRequested();
            if (_position >= _length || buffer.IsEmpty) return 0;
            position = _position;
            block = position / BlockSize;
            if (!_blocks.ContainsKey(block))
            {
                _demand++;
                _speculation?.Cancel();
            }
            else return Copy(block, buffer);
        }
        try
        {
            // This request is independent of the speculative worker, so a slow read-ahead cannot queue it.
            var (data, total) = FetchAsync(_http, _urls, block * BlockSize,
                Math.Min(_length - 1, (block + 1) * BlockSize - 1), _token).GetAwaiter().GetResult();
            if (total != _length) throw new InvalidDataException("Audio length changed.");
            lock (_gate)
            {
                _token.ThrowIfCancellationRequested();
                if (_position != position) throw new IOException("Audio position changed during a read.");
                Store(block, data);
                return Copy(block, buffer);
            }
        }
        finally
        {
            lock (_gate)
            {
                _demand--;
                if (!_stopped) Wake();
            }
        }
    }

    private int Copy(long number, Span<byte> buffer)
    {
        var block = _blocks[number];
        var offset = (int)(_position % BlockSize);
        var count = Math.Min(buffer.Length, block.Data.Length - offset);
        block.Data.AsSpan(offset, count).CopyTo(buffer);
        _recent.Remove(block.Recent);
        _recent.AddLast(block.Recent);
        _position += count;
        _readingStarted = true;
        Wake();
        return count;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _token.ThrowIfCancellationRequested();
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => checked(_position + offset),
                SeekOrigin.End => checked(_length + offset),
                _ => throw new ArgumentOutOfRangeException(nameof(origin)),
            };
            if (target < 0) throw new IOException();
            if (target != _position) _speculation?.Cancel();
            _position = target;
            // Probe seeks alone must not trigger four MiB downloads at the end of every track.
            _readingStarted = false;
            return target;
        }
    }

    internal void Cancel()
    {
        lock (_gate)
        {
            if (_stopped) return;
            _stopped = true;
            _lifetime.Cancel();
            _blocks.Clear();
            _recent.Clear();
        }
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_gate)
            {
                if (_disposed) return;
                Cancel();
                _disposed = true;
            }
            // Disposal must not wait on network I/O from mpv's close callback.
            _ = ReleaseResourcesAsync();
        }
        base.Dispose(disposing);
    }

    private async Task ReleaseResourcesAsync()
    {
        await _worker.ConfigureAwait(false);
        _stoppingRegistration.Dispose();
        _lifetime.Dispose();
        _wake.Dispose();
    }
}
