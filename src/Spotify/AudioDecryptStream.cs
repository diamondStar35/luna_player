using System.Security.Cryptography;

namespace LunaPlayer.Spotify;

/// <summary>Exposes the audio body of a seekable Spotify file after AES-128-CTR decryption.</summary>
/// <remarks>The cipher counter follows the underlying file position, so seeking does not require
/// decrypting all preceding bytes. The body offset hides Spotify's nonstandard Ogg header from mpv.</remarks>
internal sealed class AudioDecryptStream : Stream
{
    private static readonly byte[] AudioIv =
    [
        0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77,
        0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93,
    ];

    private readonly Stream _source;
    private readonly Aes _aes;
    private readonly byte[] _iv;
    private readonly long _bodyOffset;
    private bool _disposed;

    internal AudioDecryptStream(Stream source, ReadOnlySpan<byte> key, long bodyOffset = 0)
        : this(source, key, AudioIv, bodyOffset) { }

    internal AudioDecryptStream(Stream source, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv,
        long bodyOffset)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead || !source.CanSeek)
            throw new ArgumentException(null, nameof(source));
        if (key.Length != 16) throw new ArgumentException(null, nameof(key));
        if (iv.Length != 16) throw new ArgumentException(null, nameof(iv));
        if (bodyOffset < 0 || bodyOffset > source.Length)
            throw new ArgumentOutOfRangeException(nameof(bodyOffset));

        _source = source;
        _iv = iv.ToArray();
        _bodyOffset = bodyOffset;
        _aes = Aes.Create();
        _aes.Key = key.ToArray();
        _aes.Mode = CipherMode.ECB;
        _aes.Padding = PaddingMode.None;
        _source.Position = bodyOffset;
    }

    public override bool CanRead => !_disposed;
    public override bool CanSeek => !_disposed;
    public override bool CanWrite => false;
    public override long Length => _source.Length - _bodyOffset;
    public override long Position
    {
        get => _source.Position - _bodyOffset;
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
        var position = _source.Position;
        var read = _source.Read(buffer);
        if (read == 0) return 0;

        Span<byte> counter = stackalloc byte[16];
        Span<byte> keystream = stackalloc byte[16];
        var block = (ulong)(position / 16);
        var intraBlock = (int)(position % 16);
        var changed = 0;
        while (changed < read)
        {
            _iv.CopyTo(counter);
            var carry = block;
            for (var index = 15; index >= 0 && carry != 0; index--)
            {
                carry += counter[index];
                counter[index] = (byte)carry;
                carry >>= 8;
            }
            _aes.EncryptEcb(counter, keystream, PaddingMode.None);
            for (var index = intraBlock; index < 16 && changed < read; index++)
                buffer[changed++] ^= keystream[index];
            block++;
            intraBlock = 0;
        }
        CryptographicOperations.ZeroMemory(counter);
        CryptographicOperations.ZeroMemory(keystream);
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(Position + offset),
            SeekOrigin.End => checked(Length + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        if (target < 0) throw new IOException();
        _source.Position = checked(_bodyOffset + target);
        return target;
    }

    public override void Flush() { }
    internal void Cancel()
    {
        if (_source is BufferedAudioStream range) range.Cancel();
    }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            _aes.Dispose();
            _source.Dispose();
            CryptographicOperations.ZeroMemory(_iv);
        }
        base.Dispose(disposing);
    }
}
