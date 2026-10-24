using System.Buffers.Binary;

namespace LunaPlayer.Spotify;

/// <summary>Serializes playback requests on one authenticated Spotify access point.</summary>
internal sealed class AccessPointRequests : IDisposable
{
    private readonly AccessPointCodec _connection;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _stop = new();
    private readonly CancellationToken _stopToken;
    private readonly Task _receiver;
    private readonly object _stateLock = new();
    private TaskCompletionSource<byte[]>? _pending;
    private uint _pendingSequence;
    private Exception? _failure;
    private uint _keySequence;
    private int _disposed;

    // Spotify sends keepalive packets even while a track is playing. Read them throughout the
    // session so the access point remains usable when the next track needs its audio key.
    internal AccessPointRequests(AccessPointCodec connection)
    {
        _connection = connection;
        _stopToken = _stop.Token;
        _receiver = Task.Run(ReceiveLoopAsync);
    }

    internal async Task<byte[]> GetAudioKeyAsync(TrackId track, ReadOnlyMemory<byte> fileId,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var sequence = _keySequence++;
            var request = AudioKeyRequest.Create(track, fileId.Span, sequence);
            var response = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_stateLock)
            {
                if (_failure is { } failure) throw failure;
                _pendingSequence = sequence;
                _pending = response;
            }
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                await _connection.SendAsync(AudioKeyRequest.PacketType, request, timeout.Token)
                    .ConfigureAwait(false);
                try { return await response.Task.WaitAsync(timeout.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested
                    && !_stopToken.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }
            }
            finally
            {
                lock (_stateLock)
                    if (ReferenceEquals(_pending, response)) _pending = null;
            }
        }
        finally { _gate.Release(); }
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (!_stopToken.IsCancellationRequested)
            {
                var (kind, packet) = await _connection.ReceiveAsync(_stopToken).ConfigureAwait(false);
                if (kind == 0x04)
                {
                    await _connection.SendAsync(0x49, new byte[4], _stopToken).ConfigureAwait(false);
                    continue;
                }
                if (kind is not (0x0d or 0x0e) || packet.Length < 4) continue;
                TaskCompletionSource<byte[]>? pending;
                lock (_stateLock)
                    pending = BinaryPrimitives.ReadUInt32BigEndian(packet) == _pendingSequence
                        ? _pending : null;
                if (pending is null) continue;
                if (kind == 0x0e || packet.Length != 20)
                    pending.TrySetException(new InvalidDataException());
                else pending.TrySetResult(packet.AsSpan(4, 16).ToArray());
            }
        }
        catch (OperationCanceledException) when (_stopToken.IsCancellationRequested)
        {
            // Disposing the access point intentionally cancels the receive loop.
        }
        catch (Exception error)
        {
            TaskCompletionSource<byte[]>? pending;
            lock (_stateLock)
            {
                _failure = error;
                pending = _pending;
            }
            pending?.TrySetException(error);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        lock (_stateLock)
        {
            _failure = new ObjectDisposedException(nameof(AccessPointRequests));
            _pending?.TrySetException(_failure);
        }
        _stop.Cancel();
        _connection.Dispose();
        _ = ReleaseStopAsync();
        // Queued key requests still release this managed gate while unwinding cancellation.
    }

    private async Task ReleaseStopAsync()
    {
        await _receiver.ConfigureAwait(false);
        _stop.Dispose();
    }
}
