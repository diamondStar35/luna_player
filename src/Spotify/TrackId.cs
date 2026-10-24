using System.Buffers.Binary;

namespace LunaPlayer.Spotify;

/// <summary>The 128-bit ID in a Spotify track URI, decoded from its 22-character base62 form.</summary>
internal readonly record struct TrackId(UInt128 Value)
{
    private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Prefix = "spotify:track:";

    internal static bool TryParse(string? uri, out TrackId id)
    {
        id = default;
        if (uri is null || !uri.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        var encoded = uri.AsSpan(Prefix.Length);
        if (encoded.Length != 22) return false;
        UInt128 value = 0;
        foreach (var character in encoded)
        {
            var digit = Digits.IndexOf(character);
            if (digit < 0 || value > (UInt128.MaxValue - (uint)digit) / 62) return false;
            value = value * 62 + (uint)digit;
        }
        id = new TrackId(value);
        return true;
    }

    internal void WriteRaw(Span<byte> destination)
    {
        if (destination.Length < 16) throw new ArgumentException(null, nameof(destination));
        BinaryPrimitives.WriteUInt64BigEndian(destination, (ulong)(Value >> 64));
        BinaryPrimitives.WriteUInt64BigEndian(destination[8..], (ulong)Value);
    }

    internal static TrackId ReadRaw(ReadOnlySpan<byte> value)
    {
        if (value.Length != 16) throw new InvalidDataException();
        return new TrackId(((UInt128)BinaryPrimitives.ReadUInt64BigEndian(value) << 64)
            | BinaryPrimitives.ReadUInt64BigEndian(value[8..]));
    }
}
