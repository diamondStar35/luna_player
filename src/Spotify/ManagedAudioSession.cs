namespace LunaPlayer.Spotify;

/// <summary>Owns the authenticated access point used to unlock Spotify audio.</summary>
internal sealed class ManagedAudioSession : IDisposable
{
    private const string ClientId = "65b708073fc0480ea92a077233ca87bd";
    private readonly HttpClient _http;
    private readonly AccessPointResolver _resolver;
    private readonly AccessPointLogin.Credentials _credentials;
    private readonly string _deviceId;
    private readonly SemaphoreSlim _reconnectGate = new(1, 1);
    private readonly object _stateLock = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly CancellationToken _stopToken;
    private AccessPointRequests _requests;
    private readonly PlaybackFiles _files;
    private volatile bool _disposed;

    internal static async Task<ManagedAudioSession> ConnectAsync(
        AccessPointLogin.Credentials credentials, CancellationToken cancellationToken)
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        AccessPointCodec? connection = null;
        try
        {
            var resolver = new AccessPointResolver(http);
            var (host, port) = await resolver.ResolveAsync("accesspoint", cancellationToken)
                .ConfigureAwait(false);
            connection = await AccessPointHandshake.ConnectAsync(host, port, cancellationToken)
                .ConfigureAwait(false);
            var deviceId = Guid.NewGuid().ToString();
            var reusable = await AccessPointLogin.LoginAsync(connection, credentials, deviceId,
                cancellationToken).ConfigureAwait(false);
            var requests = new AccessPointRequests(connection);
            connection = null;
            var tokens = new PlaybackHttpTokens(http, ClientId, deviceId);
            var files = new PlaybackFiles(http, resolver, tokens, reusable);
            return new ManagedAudioSession(requests, files, http, resolver, reusable, deviceId);
        }
        catch
        {
            connection?.Dispose();
            http.Dispose();
            throw;
        }
    }

    private ManagedAudioSession(AccessPointRequests requests, PlaybackFiles files, HttpClient http,
        AccessPointResolver resolver, AccessPointLogin.Credentials credentials, string deviceId)
    {
        _requests = requests;
        _files = files;
        _http = http;
        _resolver = resolver;
        _credentials = credentials;
        _deviceId = deviceId;
        _stopToken = _stop.Token;
    }

    internal void SaveCredentials()
    {
        PlaybackCredentialStore.Save(_credentials);
        PlaybackCredentialStore.DeleteLegacy();
    }

    internal async Task<AudioDecryptStream> GetAudioAsync(string uri, int bitrate,
        CancellationToken cancellationToken)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ManagedAudioSession));
        var (track, fileId, format) = await _files.GetTrackFileAsync(uri, bitrate, cancellationToken)
            .ConfigureAwait(false);
        var key = await GetAudioKeyAsync(track, fileId, cancellationToken).ConfigureAwait(false);
        try
        {
            var encrypted = await _files.OpenAudioAsync(fileId, cancellationToken)
                .ConfigureAwait(false);
            // Only Vorbis formats 0/1/2 carry Spotify's custom Ogg packet. MP3
            // fallbacks start at byte zero; skipping 167 bytes truncates their audio header.
            try { return new AudioDecryptStream(encrypted, key, format is 0 or 1 or 2 ? 167 : 0); }
            catch { encrypted.Dispose(); throw; }
        }
        finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(key); }
    }

    private async Task<byte[]> GetAudioKeyAsync(TrackId track, byte[] fileId,
        CancellationToken cancellationToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopToken);
        cancellationToken = lifetime.Token;
        for (var attempt = 0; ; attempt++)
        {
            var requests = _requests;
            try
            {
                return await requests.GetAudioKeyAsync(track, fileId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception error) when (attempt == 0 && !_disposed && !cancellationToken.IsCancellationRequested
                && error is IOException or System.Net.Sockets.SocketException or ObjectDisposedException)
            {
                await _reconnectGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (!ReferenceEquals(_requests, requests)) continue;
                    var (host, port) = await _resolver.ResolveAsync("accesspoint", cancellationToken)
                        .ConfigureAwait(false);
                    AccessPointCodec? connection = null;
                    try
                    {
                        connection = await AccessPointHandshake.ConnectAsync(host, port, cancellationToken)
                            .ConfigureAwait(false);
                        await AccessPointLogin.LoginAsync(connection, _credentials, _deviceId,
                            cancellationToken).ConfigureAwait(false);
                        lock (_stateLock)
                        {
                            ObjectDisposedException.ThrowIf(_disposed, this);
                            _requests = new AccessPointRequests(connection);
                            connection = null;
                        }
                        requests.Dispose();
                    }
                    finally { connection?.Dispose(); }
                }
                finally { _reconnectGate.Release(); }
            }
        }
    }

    public void Dispose()
    {
        AccessPointRequests requests;
        lock (_stateLock)
        {
            if (_disposed) return;
            _disposed = true;
            requests = _requests;
        }
        _stop.Cancel();
        requests.Dispose();
        _http.Dispose();
        _stop.Dispose();
        // An in-flight reconnect must still release its managed semaphore after cancellation.
    }
}
