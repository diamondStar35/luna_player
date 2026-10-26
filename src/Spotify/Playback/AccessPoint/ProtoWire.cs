namespace LunaPlayer.Spotify.Playback.AccessPoint;

/// <summary>The small protobuf wire subset needed by Spotify's playback messages.</summary>
internal static class ProtoWire
{
    internal static void WriteNumber(Stream output, int field, ulong value)
    {
        WriteVarint(output, ((ulong)field << 3) | 0);
        WriteVarint(output, value);
    }

    internal static void WriteBytes(Stream output, int field, ReadOnlySpan<byte> value)
    {
        WriteVarint(output, ((ulong)field << 3) | 2);
        WriteVarint(output, (ulong)value.Length);
        output.Write(value);
    }

    internal static void WriteVarint(Stream output, ulong value)
    {
        while (value >= 0x80)
        {
            output.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }
        output.WriteByte((byte)value);
    }
}

internal ref struct ProtoReader(ReadOnlySpan<byte> input)
{
    private ReadOnlySpan<byte> _input = input;

    internal bool TryRead(out int field, out ulong number, out ReadOnlySpan<byte> bytes)
    {
        field = 0;
        number = 0;
        bytes = default;
        if (_input.IsEmpty) return false;
        var tag = ReadVarint();
        field = checked((int)(tag >> 3));
        if (field == 0) throw new InvalidDataException();
        switch (tag & 7)
        {
            case 0:
                number = ReadVarint();
                break;
            case 2:
                var length = checked((int)ReadVarint());
                if (length > _input.Length) throw new InvalidDataException();
                bytes = _input[..length];
                _input = _input[length..];
                break;
            case 1:
                if (_input.Length < 8) throw new InvalidDataException();
                bytes = _input[..8];
                _input = _input[8..];
                break;
            case 5:
                if (_input.Length < 4) throw new InvalidDataException();
                bytes = _input[..4];
                _input = _input[4..];
                break;
            default:
                throw new InvalidDataException();
        }
        return true;
    }

    private ulong ReadVarint()
    {
        ulong result = 0;
        for (var shift = 0; shift < 64; shift += 7)
        {
            if (_input.IsEmpty) throw new InvalidDataException();
            var next = _input[0];
            _input = _input[1..];
            if (shift == 63 && next > 1) throw new InvalidDataException();
            result |= (ulong)(next & 0x7f) << shift;
            if ((next & 0x80) == 0) return result;
        }
        throw new InvalidDataException();
    }
}
