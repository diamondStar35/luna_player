using System.Buffers.Binary;

namespace LunaPlayer.Spotify;

/// <summary>Serializes playback requests on one authenticated Spotify access point.</summary>
internal sealed class AccessPointRequests : IDisposable
{
    private readonly AccessPointCodec _connection;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _stop = new();
    private readonly object _stateLock = new();
    private TaskCompletionSource<byte[]>? _pending;
    private uint _pendingSequence;
    private Exception? _failure;
    private uint _keySequence;

    // Spotify sends keepalive packets even while a track is playing. Read them throughout the
    // session so the access point remains usable when the next track needs its audio key.
    internal AccessPointRequests(AccessPointCodec connection)
    {
        _connection = connection;
        _ = Task.Run(ReceiveLoopAsync);
    }

    internal async Task<byte[]> GetAudioKeyAsync(TrackId track, ReadOnlyMemory<byte> fileId,
        CancellationToken cancellationToken)
    {
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
                await _connection.SendAsync(AudioKeyRequest.PacketType, request, cancellationToken)
                    .ConfigureAwait(false);
                return await response.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
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
            while (!_stop.IsCancellationRequested)
            {
                var (kind, packet) = await _connection.ReceiveAsync(_stop.Token).ConfigureAwait(false);
                if (kind == 0x04)
                {
                    await _connection.SendAsync(0x49, new byte[4], _stop.Token).ConfigureAwait(false);
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
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
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
        _stop.Cancel();
        _connection.Dispose();
        _stop.Dispose();
        _gate.Dispose();
    }
}
