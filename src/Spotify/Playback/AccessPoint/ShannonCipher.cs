using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace LunaPlayer.Spotify.Playback.AccessPoint;

/// <summary>The Shannon packet cipher used by Spotify's access point connection.</summary>
/// <remarks>Ported from the MIT-licensed shannon 0.2.0 crate used by Luna's pinned librespot.</remarks>
internal sealed class ShannonCipher
{
    private const uint InitialConstant = 0x6996c53a;
    private const int Size = 16;
    private const int KeyPosition = 13;
    private readonly uint[] _register = new uint[Size];
    private readonly uint[] _checksum = new uint[Size];
    private readonly uint[] _initial = new uint[Size];
    private uint _constant = InitialConstant;
    private uint _stream;
    private uint _partial;
    private int _remaining;

    internal ShannonCipher(ReadOnlySpan<byte> key)
    {
        _register[0] = _register[1] = 1;
        for (var i = 2; i < Size; i++)
            _register[i] = unchecked(_register[i - 1] + _register[i - 2]);
        LoadKey(key);
        _constant = _register[0];
        _register.CopyTo(_initial, 0);
    }

    internal void Nonce(uint value)
    {
        Span<byte> nonce = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(nonce, value);
        _initial.CopyTo(_register, 0);
        _constant = InitialConstant;
        LoadKey(nonce);
        _constant = _register[0];
        _remaining = 0;
    }

    internal void Encrypt(Span<byte> data) => Process(data, decrypt: false);
    internal void Decrypt(Span<byte> data) => Process(data, decrypt: true);

    internal void Finish(Span<byte> mac)
    {
        if (_remaining != 0) Mac(_partial);
        Cycle();
        _register[KeyPosition] ^= InitialConstant ^ ((uint)_remaining << 3);
        _remaining = 0;
        for (var i = 0; i < Size; i++) _register[i] ^= _checksum[i];
        Diffuse();
        Span<byte> word = stackalloc byte[4];
        for (var offset = 0; offset < mac.Length; offset += 4)
        {
            Cycle();
            BinaryPrimitives.WriteUInt32LittleEndian(word, _stream);
            word[..Math.Min(4, mac.Length - offset)].CopyTo(mac[offset..]);
        }
    }

    internal bool CheckMac(ReadOnlySpan<byte> expected)
    {
        Span<byte> actual = expected.Length <= 64 ? stackalloc byte[expected.Length] : new byte[expected.Length];
        Finish(actual);
        var valid = CryptographicOperations.FixedTimeEquals(actual, expected);
        CryptographicOperations.ZeroMemory(actual);
        return valid;
    }

    private static uint Sbox1(uint word)
    {
        word ^= BitOperations.RotateLeft(word, 5) | BitOperations.RotateLeft(word, 7);
        word ^= BitOperations.RotateLeft(word, 19) | BitOperations.RotateLeft(word, 22);
        return word;
    }

    private static uint Sbox2(uint word)
    {
        word ^= BitOperations.RotateLeft(word, 7) | BitOperations.RotateLeft(word, 22);
        word ^= BitOperations.RotateLeft(word, 5) | BitOperations.RotateLeft(word, 19);
        return word;
    }

    private void Cycle()
    {
        var feedback = Sbox1(_register[12] ^ _register[13] ^ _constant)
            ^ BitOperations.RotateLeft(_register[0], 1);
        Array.Copy(_register, 1, _register, 0, Size - 1);
        _register[15] = feedback;
        var nonlinear = Sbox2(_register[2] ^ _register[15]);
        _register[0] ^= nonlinear;
        _stream = nonlinear ^ _register[8] ^ _register[12];
    }

    private void Diffuse()
    {
        for (var i = 0; i < Size; i++) Cycle();
    }

    private void LoadKey(ReadOnlySpan<byte> key)
    {
        Span<byte> word = stackalloc byte[4];
        for (var offset = 0; offset < key.Length; offset += 4)
        {
            word.Clear();
            key.Slice(offset, Math.Min(4, key.Length - offset)).CopyTo(word);
            _register[KeyPosition] ^= BinaryPrimitives.ReadUInt32LittleEndian(word);
            Cycle();
        }
        _register[KeyPosition] ^= (uint)key.Length;
        Cycle();
        _register.CopyTo(_checksum, 0);
        Diffuse();
        for (var i = 0; i < Size; i++) _register[i] ^= _checksum[i];
    }

    private void Checksum(uint value)
    {
        var feedback = _checksum[0] ^ _checksum[2] ^ _checksum[15] ^ value;
        Array.Copy(_checksum, 1, _checksum, 0, Size - 1);
        _checksum[15] = feedback;
    }

    private void Mac(uint value)
    {
        Checksum(value);
        _register[KeyPosition] ^= value;
    }

    private void Process(Span<byte> data, bool decrypt)
    {
        var offset = 0;
        if (_remaining != 0)
        {
            while (_remaining > 0 && offset < data.Length)
            {
                Partial(ref data[offset++], decrypt);
                _remaining -= 8;
            }
            if (_remaining > 0) return;
            // A word contributes to the MAC once, after all four bytes have arrived.
            // Network reads can split it at any byte, not only after the packet header.
            Mac(_partial);
        }
        while (data.Length - offset >= 4)
        {
            Cycle();
            var word = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            if (decrypt) word ^= _stream;
            Mac(word);
            if (!decrypt) word ^= _stream;
            BinaryPrimitives.WriteUInt32LittleEndian(data[offset..], word);
            offset += 4;
        }
        if (offset == data.Length) return;
        Cycle();
        _partial = 0;
        _remaining = 32;
        while (offset < data.Length)
        {
            Partial(ref data[offset++], decrypt);
            _remaining -= 8;
        }
    }

    private void Partial(ref byte value, bool decrypt)
    {
        var shift = 32 - _remaining;
        if (decrypt) value ^= (byte)(_stream >> shift);
        _partial ^= (uint)value << shift;
        if (!decrypt) value ^= (byte)(_stream >> shift);
    }
}
