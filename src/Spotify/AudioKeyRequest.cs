using System.Buffers.Binary;

namespace LunaPlayer.Spotify;

/// <summary>The payload of librespot's request-key access point packet.</summary>
internal static class AudioKeyRequest
{
    internal const byte PacketType = 0x0c;

    internal static byte[] Create(TrackId track, ReadOnlySpan<byte> fileId, uint sequence)
    {
        if (fileId.Length != 20) throw new ArgumentException(null, nameof(fileId));
        var packet = new byte[42];
        fileId.CopyTo(packet);
        track.WriteRaw(packet.AsSpan(20, 16));
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(36, 4), sequence);
        return packet;
    }
}
