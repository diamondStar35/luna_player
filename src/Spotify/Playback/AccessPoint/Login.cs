using System.Text;

namespace LunaPlayer.Spotify.Playback.AccessPoint;

internal sealed class AuthenticationException : Exception;

/// <summary>Authenticates a Spotify access point and returns credentials usable by keymaster.</summary>
internal static class Login
{
    internal readonly record struct Credentials(string? Username, int Type, byte[] Data)
    {
        internal static Credentials FromToken(string token) =>
            new(null, 3, Encoding.UTF8.GetBytes(token));
    }

    internal static async Task<Credentials> LoginAsync(Codec connection,
        Credentials credentials, string deviceId, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        cancellationToken = timeout.Token;
        using var login = new MemoryStream();
        using var identity = new MemoryStream();
        if (!string.IsNullOrEmpty(credentials.Username))
            ProtoWire.WriteBytes(identity, 10, Encoding.UTF8.GetBytes(credentials.Username));
        ProtoWire.WriteNumber(identity, 20, (ulong)credentials.Type);
        ProtoWire.WriteBytes(identity, 30, credentials.Data);
        ProtoWire.WriteBytes(login, 10, identity.ToArray());

        using var system = new MemoryStream();
        ProtoWire.WriteNumber(system, 10, 2); // x64
        ProtoWire.WriteNumber(system, 60, 1); // Windows
        ProtoWire.WriteBytes(system, 90, Encoding.UTF8.GetBytes("Luna Player"));
        ProtoWire.WriteBytes(system, 100, Encoding.UTF8.GetBytes(deviceId));
        ProtoWire.WriteBytes(login, 50, system.ToArray());
        ProtoWire.WriteBytes(login, 70, Encoding.UTF8.GetBytes("Luna Player"));
        await connection.SendAsync(0xab, login.ToArray(), cancellationToken).ConfigureAwait(false);

        var (kind, payload) = await connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        if (kind == 0xad) throw new AuthenticationException();
        if (kind != 0xac) throw new InvalidDataException();
        var reader = new ProtoReader(payload);
        string? username = null;
        int type = -1;
        byte[]? reusable = null;
        while (reader.TryRead(out var field, out var number, out var bytes))
        {
            switch (field)
            {
                case 10: username = Encoding.UTF8.GetString(bytes); break;
                case 30: type = checked((int)number); break;
                case 40: reusable = bytes.ToArray(); break;
            }
        }
        if (string.IsNullOrEmpty(username) || type < 0 || reusable is not { Length: > 0 })
            throw new InvalidDataException();
        return new Credentials(username, type, reusable);
    }
}
