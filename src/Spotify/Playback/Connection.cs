using LunaPlayer.Spotify.Playback.AccessPoint;

namespace LunaPlayer.Spotify.Playback;

/// <summary>Owns the authenticated access point used to unlock Spotify audio.</summary>
internal sealed class Connection : IDisposable
{
    private const string ClientId = "65b708073fc0480ea92a077233ca87bd";
    private readonly HttpClient _http;
    private readonly Resolver _resolver;
    private readonly Login.Credentials _credentials;
    private readonly string _deviceId;
    private readonly SemaphoreSlim _reconnectGate = new(1, 1);
    private readonly object _stateLock = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly CancellationToken _stopToken;
    private Requests _requests;
    private readonly Files _files;
    private readonly AudioKeys _audioKeys;
    private readonly Dictionary<(string Uri, int Bitrate), (Task<(TrackId KeyTrack, byte[] FileId, int Format)> Task, DateTimeOffset Created)> _metadata = [];
    private volatile bool _disposed;

    internal static async Task<Connection> ConnectAsync(
        Login.Credentials credentials, CancellationToken cancellationToken)
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        Codec? connection = null;
        try
        {
            var resolver = new Resolver(http);
            var (host, port) = await resolver.ResolveAsync("accesspoint", cancellationToken)
                .ConfigureAwait(false);
            connection = await Handshake.ConnectAsync(host, port, cancellationToken)
                .ConfigureAwait(false);
            var deviceId = Guid.NewGuid().ToString();
            var reusable = await Login.LoginAsync(connection, credentials, deviceId,
                cancellationToken).ConfigureAwait(false);
            var requests = new Requests(connection);
            connection = null;
            var tokens = new HttpTokens(http, ClientId, deviceId);
            var files = new Files(http, resolver, tokens, reusable);
            return new Connection(requests, files, http, resolver, reusable, deviceId);
        }
        catch
        {
            connection?.Dispose();
            http.Dispose();
            throw;
        }
    }

    private Connection(Requests requests, Files files, HttpClient http,
        Resolver resolver, Login.Credentials credentials, string deviceId)
    {
        _requests = requests;
        _files = files;
        _http = http;
        _resolver = resolver;
        _credentials = credentials;
        _deviceId = deviceId;
        _stopToken = _stop.Token;
        _audioKeys = new AudioKeys(GetAudioKeyAsync, _stopToken);
    }

    internal void SaveCredentials()
    {
        CredentialStore.Save(_credentials);
        CredentialStore.DeleteLegacy();
    }

    internal async Task<AudioDecryptStream> GetAudioAsync(string uri, int bitrate,
        CancellationToken cancellationToken, bool speculative = false)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Connection));
        cancellationToken.ThrowIfCancellationRequested();
        var (track, fileId, format) = await GetMetadataAsync(uri, bitrate, cancellationToken)
            .ConfigureAwait(false);
        var key = await _audioKeys.GetAsync(track, fileId, cancellationToken, speculative).ConfigureAwait(false);
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

    private async Task<(TrackId KeyTrack, byte[] FileId, int Format)> GetMetadataAsync(string uri, int bitrate, CancellationToken token)
    {
        Task<(TrackId KeyTrack, byte[] FileId, int Format)> task;
        var id = (uri, bitrate);
        lock (_stateLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            foreach (var item in _metadata.ToArray())
                if (item.Value.Task.IsCompleted && (item.Value.Task.Status != TaskStatus.RanToCompletion
                    || DateTimeOffset.UtcNow - item.Value.Created > TimeSpan.FromMinutes(1))) _metadata.Remove(item.Key);
            if (!_metadata.TryGetValue(id, out var cached))
            {
                // Share only metadata, never decoder streams or their cancellation lifetimes.
                task = _files.GetTrackFileAsync(uri, bitrate, _stopToken);
                _ = task.ContinueWith(failed => { _ = failed.Exception; }, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                if (_metadata.Count >= 64)
                {
                    var oldest = _metadata.OrderBy(item => item.Value.Created).First();
                    _metadata.Remove(oldest.Key);
                }
                _metadata[id] = (task, DateTimeOffset.UtcNow);
            }
            else task = cached.Task;
        }
        return await task.WaitAsync(token).ConfigureAwait(false);
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
                    Codec? connection = null;
                    try
                    {
                        connection = await Handshake.ConnectAsync(host, port, cancellationToken)
                            .ConfigureAwait(false);
                        await Login.LoginAsync(connection, _credentials, _deviceId,
                            cancellationToken).ConfigureAwait(false);
                        lock (_stateLock)
                        {
                            ObjectDisposedException.ThrowIf(_disposed, this);
                            _requests = new Requests(connection);
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
        Requests requests;
        lock (_stateLock)
        {
            if (_disposed) return;
            _disposed = true;
            _metadata.Clear();
            requests = _requests;
        }
        _stop.Cancel();
        _audioKeys.Dispose();
        requests.Dispose();
        _http.Dispose();
        _stop.Dispose();
        // An in-flight reconnect must still release its managed semaphore after cancellation.
    }
}
