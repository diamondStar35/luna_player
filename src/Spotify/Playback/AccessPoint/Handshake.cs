using System.Buffers.Binary;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;

namespace LunaPlayer.Spotify.Playback.AccessPoint;

/// <summary>Establishes the signed Diffie-Hellman session used before Spotify login.</summary>
internal static class Handshake
{
    private const ulong SpotifyVersion = 124200290;
    private static readonly byte[] Prime = Convert.FromHexString(
        "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74" +
        "020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F" +
        "14374FE1356D6D51C245E485B576625E7EC6F44C42E9A63A3620FFFFFFFF" +
        "FFFFFFFF");
    private static readonly byte[] ServerKey = Convert.FromHexString(
        "ACE0460BFFC230AFF46BFEC3BFBF863DA191C6CC336C93A14FB3B01612ACAC6A" +
        "F180E7F614D9429DBE2E346643E362D2327A1A0D923BAEDD1402B18155056104" +
        "D52C96A44C1ECC024AD4B20C001F17EDC22FC43521C8F0CBAED2ADD72B0F9DB3" +
        "C5321A2AFE59F35A0DAC68F1FA621EFB2C8D0CB7392D9247E3D7351A6DBD24C2" +
        "AE255B88FFAB73298A0BCCCD0C58673189E8BD3480784A5FC96B899D956BFC86" +
        "D74F33A6781796C9C32D0D32A5ABCD0527E2F710A39613C42F99C027BFED049C" +
        "3C275804B6B219F9C12F02E94863ECA1B642A09D4825F8B39DD0E86AF9484DA1" +
        "C2BA863042EA9DB3086C190E48B39D66EB0006A25AEEA11B13873CD719E655BD");

    static Handshake()
    {
        if (Prime.Length != 96 || ServerKey.Length != 256) throw new InvalidDataException();
    }

    internal static async Task<Codec> ConnectAsync(string host, int port,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        cancellationToken = timeout.Token;
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            var stream = client.GetStream();
            var privateBytes = RandomNumberGenerator.GetBytes(95);
            var privateKey = new BigInteger(privateBytes, isUnsigned: true, isBigEndian: false);
            CryptographicOperations.ZeroMemory(privateBytes);
            var modulus = new BigInteger(Prime, isUnsigned: true, isBigEndian: true);
            var publicKey = BigInteger.ModPow(2, privateKey, modulus)
                .ToByteArray(isUnsigned: true, isBigEndian: true);
            var hello = CreateHello(publicKey);
            await stream.WriteAsync(hello, cancellationToken).ConfigureAwait(false);

            var lengthBytes = new byte[4];
            await stream.ReadExactlyAsync(lengthBytes, cancellationToken).ConfigureAwait(false);
            var length = BinaryPrimitives.ReadUInt32BigEndian(lengthBytes);
            if (length is < 4 or > 1_048_576) throw new InvalidDataException();
            var response = new byte[length - 4];
            await stream.ReadExactlyAsync(response, cancellationToken).ConfigureAwait(false);

            var challenge = FieldBytes(response, 10);
            var crypto = FieldBytes(challenge, 10);
            var dh = FieldBytes(crypto, 10);
            var serverPublic = FieldBytes(dh, 10);
            var signature = FieldBytes(dh, 30);
            if (serverPublic.IsEmpty || signature.IsEmpty) throw new InvalidDataException();
            using (var verifier = RSA.Create())
            {
                verifier.ImportParameters(new RSAParameters
                {
                    Modulus = ServerKey,
                    Exponent = [1, 0, 1],
                });
                if (!verifier.VerifyData(serverPublic, signature, HashAlgorithmName.SHA1,
                        RSASignaturePadding.Pkcs1)) throw new InvalidDataException();
            }

            var remote = new BigInteger(serverPublic, isUnsigned: true, isBigEndian: true);
            var secret = BigInteger.ModPow(remote, privateKey, modulus)
                .ToByteArray(isUnsigned: true, isBigEndian: true);
            using var transcript = new MemoryStream();
            transcript.Write(hello);
            transcript.Write(lengthBytes);
            transcript.Write(response);
            var (proof, sendKey, receiveKey) = DeriveKeys(secret, transcript.ToArray());
            CryptographicOperations.ZeroMemory(secret);
            try
            {
                var confirmation = CreateConfirmation(proof);
                await stream.WriteAsync(confirmation, cancellationToken).ConfigureAwait(false);
                return new Codec(stream, sendKey, receiveKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sendKey);
                CryptographicOperations.ZeroMemory(receiveKey);
            }
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static byte[] CreateHello(ReadOnlySpan<byte> publicKey)
    {
        using var build = new MemoryStream();
        ProtoWire.WriteNumber(build, 10, 0); // Product client
        ProtoWire.WriteNumber(build, 20, 0); // Release build
        ProtoWire.WriteNumber(build, 30, 39); // Win32 x64
        ProtoWire.WriteNumber(build, 40, SpotifyVersion);

        using var dh = new MemoryStream();
        ProtoWire.WriteBytes(dh, 10, publicKey);
        ProtoWire.WriteNumber(dh, 20, 1);
        using var crypto = new MemoryStream();
        ProtoWire.WriteBytes(crypto, 10, dh.ToArray());
        using var hello = new MemoryStream();
        ProtoWire.WriteBytes(hello, 10, build.ToArray());
        ProtoWire.WriteNumber(hello, 30, 0); // Shannon
        ProtoWire.WriteBytes(hello, 50, crypto.ToArray());
        ProtoWire.WriteBytes(hello, 60, RandomNumberGenerator.GetBytes(16));
        ProtoWire.WriteBytes(hello, 70, [0x1e]);
        var message = hello.ToArray();
        var frame = new byte[6 + message.Length];
        frame[1] = 4;
        BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(2), (uint)frame.Length);
        message.CopyTo(frame, 6);
        return frame;
    }

    private static byte[] CreateConfirmation(ReadOnlySpan<byte> proof)
    {
        using var dh = new MemoryStream();
        ProtoWire.WriteBytes(dh, 10, proof);
        using var crypto = new MemoryStream();
        ProtoWire.WriteBytes(crypto, 10, dh.ToArray());
        using var response = new MemoryStream();
        ProtoWire.WriteBytes(response, 10, crypto.ToArray());
        ProtoWire.WriteBytes(response, 20, []);
        ProtoWire.WriteBytes(response, 30, []);
        var message = response.ToArray();
        var frame = new byte[4 + message.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frame, (uint)frame.Length);
        message.CopyTo(frame, 4);
        return frame;
    }

    private static ReadOnlySpan<byte> FieldBytes(ReadOnlySpan<byte> message, int requested)
    {
        var reader = new ProtoReader(message);
        while (reader.TryRead(out var field, out _, out var bytes))
        {
            if (field == requested) return bytes;
        }
        return default;
    }

    private static (byte[] Proof, byte[] SendKey, byte[] ReceiveKey) DeriveKeys(
        ReadOnlySpan<byte> secret, ReadOnlySpan<byte> transcript)
    {
        var material = new byte[100];
        var input = new byte[transcript.Length + 1];
        transcript.CopyTo(input);
        for (byte index = 1; index <= 5; index++)
        {
            input[^1] = index;
            HMACSHA1.HashData(secret, input).CopyTo(material, (index - 1) * 20);
        }
        var proof = HMACSHA1.HashData(material.AsSpan(0, 20), transcript);
        var send = material.AsSpan(20, 32).ToArray();
        var receive = material.AsSpan(52, 32).ToArray();
        CryptographicOperations.ZeroMemory(material);
        CryptographicOperations.ZeroMemory(input);
        return (proof, send, receive);
    }
}
