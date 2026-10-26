using System.Buffers.Binary;

namespace LunaPlayer.Spotify.Playback.AccessPoint;

/// <summary>Frames and authenticates packets on Spotify's encrypted access point connection.</summary>
internal sealed class Codec(Stream connection, ReadOnlySpan<byte> sendKey,
    ReadOnlySpan<byte> receiveKey) : IDisposable
{
    private readonly Stream _connection = connection;
    private readonly ShannonCipher _sender = new(sendKey);
    private readonly ShannonCipher _receiver = new(receiveKey);
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private uint _sendNonce;
    private uint _receiveNonce;
    private int _disposed;

    internal async Task SendAsync(byte kind, ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        if (payload.Length > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(payload));
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            cancellationToken.ThrowIfCancellationRequested();
            var packet = new byte[3 + payload.Length + 4];
            packet[0] = kind;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(1, 2), (ushort)payload.Length);
            payload.CopyTo(packet.AsMemory(3));
            _sender.Nonce(_sendNonce++);
            _sender.Encrypt(packet.AsSpan(0, packet.Length - 4));
            _sender.Finish(packet.AsSpan(packet.Length - 4));
            try { await _connection.WriteAsync(packet, cancellationToken).ConfigureAwait(false); }
            catch
            {
                // Once encryption advances the nonce, a canceled or partial write cannot
                // be retried on this connection without corrupting packet framing.
                Dispose();
                throw;
            }
        }
        finally { _sendGate.Release(); }
    }

    internal async Task<(byte Kind, byte[] Payload)> ReceiveAsync(CancellationToken cancellationToken)
    {
        var header = new byte[3];
        await _connection.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        _receiver.Nonce(_receiveNonce++);
        _receiver.Decrypt(header);
        var length = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(1));
        var packet = new byte[length + 4];
        await _connection.ReadExactlyAsync(packet, cancellationToken).ConfigureAwait(false);
        _receiver.Decrypt(packet.AsSpan(0, length));
        if (!_receiver.CheckMac(packet.AsSpan(length, 4))) throw new InvalidDataException();
        return (header[0], packet.AsSpan(0, length).ToArray());
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _connection.Dispose();
        // Pending senders must still release the managed gate after the socket closes.
        // No wait handle is allocated, so the gate can be collected with this codec.
    }
}
