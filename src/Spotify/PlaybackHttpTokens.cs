using System.Net.Http.Headers;
using System.Text;

namespace LunaPlayer.Spotify;

/// <summary>Obtains the HTTP tokens used by the Spotify metadata and storage endpoints.</summary>
internal sealed class PlaybackHttpTokens(HttpClient http, string clientId, string deviceId)
{
    private readonly HttpClient _http = http;
    private readonly string _clientId = clientId;
    private readonly string _deviceId = deviceId;
    private string? _clientToken;
    private DateTimeOffset _clientExpires;
    private string? _accessToken;
    private DateTimeOffset _accessExpires;
    private readonly SemaphoreSlim _clientGate = new(1, 1);
    private readonly SemaphoreSlim _accessGate = new(1, 1);

    internal async Task<string> ClientTokenAsync(CancellationToken cancellationToken)
    {
        await _clientGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { return await LoadClientTokenAsync(cancellationToken).ConfigureAwait(false); }
        finally { _clientGate.Release(); }
    }

    private async Task<string> LoadClientTokenAsync(CancellationToken cancellationToken)
    {
        if (_clientToken is not null && DateTimeOffset.UtcNow < _clientExpires) return _clientToken;
        using var windows = new MemoryStream();
        ProtoWire.WriteNumber(windows, 1, 10);
        ProtoWire.WriteNumber(windows, 3, (ulong)Environment.OSVersion.Version.Build);
        ProtoWire.WriteNumber(windows, 4, 2);
        ProtoWire.WriteNumber(windows, 6, 9);
        var machine = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => 34404UL,
            System.Runtime.InteropServices.Architecture.Arm64 => 43620UL,
            System.Runtime.InteropServices.Architecture.Arm => 448UL,
            _ => 332UL
        };
        ProtoWire.WriteNumber(windows, 7, machine == 43620 ? 452UL : machine);
        ProtoWire.WriteNumber(windows, 8, machine);
        ProtoWire.WriteNumber(windows, 10, 1);
        using var platform = new MemoryStream();
        ProtoWire.WriteBytes(platform, 4, windows.ToArray());
        using var connectivity = new MemoryStream();
        ProtoWire.WriteBytes(connectivity, 1, platform.ToArray());
        ProtoWire.WriteBytes(connectivity, 2, Encoding.UTF8.GetBytes(_deviceId));
        using var clientData = new MemoryStream();
        ProtoWire.WriteBytes(clientData, 1, "1.2.52.442"u8);
        ProtoWire.WriteBytes(clientData, 2, Encoding.UTF8.GetBytes(_clientId));
        ProtoWire.WriteBytes(clientData, 3, connectivity.ToArray());
        using var body = new MemoryStream();
        ProtoWire.WriteNumber(body, 1, 1);
        ProtoWire.WriteBytes(body, 2, clientData.ToArray());
        var response = await PostAsync("https://clienttoken.spotify.com/v1/clienttoken",
            body.ToArray(), null, cancellationToken).ConfigureAwait(false);
        var reader = new ProtoReader(response);
        while (reader.TryRead(out var field, out _, out var bytes))
        {
            if (field != 2) continue;
            var granted = new ProtoReader(bytes);
            var expires = 0;
            string? token = null;
            while (granted.TryRead(out var item, out var number, out var value))
            {
                if (item == 1) token = Encoding.UTF8.GetString(value);
                if (item == 2) expires = checked((int)number);
            }
            if (token is null || expires < 1) break;
            _clientToken = token;
            _clientExpires = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, expires - 60));
            return token;
        }
        throw new InvalidDataException();
    }

    internal async Task<string> AccessTokenAsync(AccessPointLogin.Credentials credentials,
        CancellationToken cancellationToken)
    {
        await _accessGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { return await LoadAccessTokenAsync(credentials, cancellationToken).ConfigureAwait(false); }
        finally { _accessGate.Release(); }
    }

    private async Task<string> LoadAccessTokenAsync(AccessPointLogin.Credentials credentials,
        CancellationToken cancellationToken)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _accessExpires) return _accessToken;
        using var info = new MemoryStream();
        ProtoWire.WriteBytes(info, 1, Encoding.UTF8.GetBytes(_clientId));
        ProtoWire.WriteBytes(info, 2, Encoding.UTF8.GetBytes(_deviceId));
        using var stored = new MemoryStream();
        ProtoWire.WriteBytes(stored, 1, Encoding.UTF8.GetBytes(credentials.Username ?? string.Empty));
        ProtoWire.WriteBytes(stored, 2, credentials.Data);
        using var body = new MemoryStream();
        ProtoWire.WriteBytes(body, 1, info.ToArray());
        ProtoWire.WriteBytes(body, 100, stored.ToArray());
        var clientToken = await ClientTokenAsync(cancellationToken).ConfigureAwait(false);
        var response = await PostAsync("https://login5.spotify.com/v3/login", body.ToArray(),
            clientToken, cancellationToken).ConfigureAwait(false);
        var reader = new ProtoReader(response);
        while (reader.TryRead(out var field, out var number, out var bytes))
        {
            if (field == 2) throw new Login5StatusException((int)number);
            if (field != 1) continue;
            var ok = new ProtoReader(bytes);
            string? token = null;
            var expires = 0;
            while (ok.TryRead(out var item, out var value, out var data))
            {
                if (item == 2) token = Encoding.UTF8.GetString(data);
                if (item == 4) expires = checked((int)value);
            }
            if (token is null || expires < 1) break;
            _accessToken = token;
            _accessExpires = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, expires - 60));
            return token;
        }
        throw new InvalidDataException();
    }

    private async Task<byte[]> PostAsync(string uri, byte[] body, string? clientToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
        request.Headers.UserAgent.ParseAdd("Spotify/124200290 Win32/0 (librespot)");
        if (clientToken is not null) request.Headers.TryAddWithoutValidation("client-token", clientToken);
        request.Content = new ByteArrayContent(body);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class Login5StatusException(int status) : Exception
{
    internal int Status { get; } = status;
}
