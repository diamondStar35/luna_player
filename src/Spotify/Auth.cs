using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Configuration;

namespace LunaPlayer.Spotify;

internal sealed class Auth : IDisposable
{
    internal const string RedirectUri = "http://127.0.0.1:43891/callback/";
    private const string Scopes = "playlist-read-private playlist-read-collaborative user-library-read user-top-read user-read-recently-played";
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _stateLock = new();
    private TokenData? _token;
    private int _version;
    private bool _disposed;

    internal event Action? SignedOut;

    internal Auth() => _token = Load();

    internal bool IsSignedIn(string clientId) => _token is { RefreshToken.Length: > 0 } token && token.ClientId == clientId;

    internal async Task<string> GetAccessTokenAsync(string clientId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            TokenData? token;
            lock (_stateLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                token = _token;
            }
            if (token is null || token.ClientId != clientId || token.RefreshToken.Length == 0)
                // Translators: Error when Spotify browsing is requested without a connected account.
                throw new InvalidOperationException(Tr("Sign in to Spotify first."));
            if (token.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1) && token.AccessToken.Length > 0)
                return token.AccessToken;
            using var response = await _http.PostAsync("https://accounts.spotify.com/api/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token", ["refresh_token"] = token.RefreshToken,
                    ["client_id"] = clientId,
                }), cancellationToken).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                lock (_stateLock)
                {
                    if (ReferenceEquals(_token, token))
                    {
                        _token = null;
                        _version++;
                        try { if (File.Exists(Paths.SpotifyTokenFile)) File.Delete(Paths.SpotifyTokenFile); }
                        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                        {
                            // The in-memory token is already invalidated. A failed deletion must not hide
                            // the session-expired message, which tells the user how to sign in again.
                        }
                    }
                }
                // Translators: Error when Spotify rejects the saved browsing refresh token and requires a new sign-in.
                throw new InvalidOperationException(Tr("The Spotify session expired. Sign in again."));
            }
            var refreshed = await ReadTokenAsync(response, clientId, token.RefreshToken, cancellationToken).ConfigureAwait(false);
            lock (_stateLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                // Sign-out must not wait on this network operation or let its late result
                // restore credentials after the user disconnected the account.
                if (!ReferenceEquals(_token, token)) throw new OperationCanceledException(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                Save(refreshed);
            }
            return refreshed.AccessToken;
        }
        finally { _gate.Release(); }
    }

    internal async Task SignInAsync(string clientId, CancellationToken cancellationToken = default)
    {
        int version;
        lock (_stateLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            version = _version;
        }
        if (clientId.Length != 32 || !clientId.All(Uri.IsHexDigit))
            // Translators: Error before browser authorization when the Client ID field in Spotify Preferences is invalid. Keep Client ID unchanged.
            throw new InvalidOperationException(Tr("Enter a valid Spotify Client ID in Preferences first."));
        using var listener = new TcpListener(IPAddress.Loopback, 43891);
        listener.Start();
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(48));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(24));
        var url = "https://accounts.spotify.com/authorize?" + Form(new Dictionary<string, string>
        {
            ["response_type"] = "code", ["client_id"] = clientId, ["redirect_uri"] = RedirectUri,
            ["scope"] = Scopes, ["state"] = state, ["code_challenge_method"] = "S256",
            ["code_challenge"] = challenge,
        });
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        using var browser = await listener.AcceptTcpClientAsync(timeout.Token).ConfigureAwait(false);
        using var stream = browser.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var request = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
        var route = request?.Split(' ', 3).ElementAtOrDefault(1);
        var query = route is null ? null : new Uri(new Uri(RedirectUri), route);
        var responsePage = "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><body>"
            // Translators: Confirmation displayed in the browser after returning from Spotify authorization.
            + WebUtility.HtmlEncode(Tr("You can return to Luna Player now.")) + "</body></html>";
        var body = Encoding.UTF8.GetBytes(responsePage);
        var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headers, timeout.Token).ConfigureAwait(false);
        await stream.WriteAsync(body, timeout.Token).ConfigureAwait(false);
        var values = ParseQuery(query?.Query);
        if (query?.AbsolutePath != "/callback/" || !values.TryGetValue("state", out var receivedState) || receivedState != state)
            // Translators: Error when the Spotify authorization callback has an unexpected address or does not match the sign-in request.
            throw new InvalidOperationException(Tr("Spotify sign in returned an invalid callback. Please try again."));
        if (values.TryGetValue("error", out var error))
            // Translators: Error when browser authorization was denied or canceled. {reason} is the error code returned by Spotify.
            throw new InvalidOperationException(TrFormat("Spotify sign in was not completed: {reason}.", error));
        if (!values.TryGetValue("code", out var code) || code.Length == 0)
            // Translators: Error when Spotify returns from browser authorization without the code needed to sign in.
            throw new InvalidOperationException(Tr("Spotify did not return an authorization code."));
        using var tokenResponse = await _http.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = RedirectUri,
                ["client_id"] = clientId, ["code_verifier"] = verifier,
            }), timeout.Token).ConfigureAwait(false);
        var token = await ReadTokenAsync(tokenResponse, clientId, null, timeout.Token).ConfigureAwait(false);
        await _gate.WaitAsync(timeout.Token).ConfigureAwait(false);
        try
        {
            lock (_stateLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                timeout.Token.ThrowIfCancellationRequested();
                if (version != _version) throw new OperationCanceledException(cancellationToken);
                Save(token);
            }
        }
        finally { _gate.Release(); }
    }

    internal void SignOut()
    {
        try
        {
            lock (_stateLock)
            {
                _token = null;
                _version++;
                if (File.Exists(Paths.SpotifyTokenFile)) File.Delete(Paths.SpotifyTokenFile);
            }
        }
        finally
        {
            // Stop playback and clear its separate credentials even if deleting this token fails.
            SignedOut?.Invoke();
        }
    }

    private static async Task<TokenData> ReadTokenAsync(HttpResponseMessage response, string clientId,
        string? oldRefreshToken, CancellationToken cancellationToken)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            // Translators: Error after a failed token request. {code} is the HTTP status; Client ID and redirect URI name fields in Spotify app settings.
            throw new InvalidOperationException(TrFormat("Spotify sign in failed ({code}). Check the Client ID and redirect URI.", (int)response.StatusCode));
        using var json = JsonDocument.Parse(bytes);
        var root = json.RootElement;
        var access = root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("access_token", out var accessField) && accessField.ValueKind == JsonValueKind.String
                ? accessField.GetString() : null;
        var refresh = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("refresh_token", out var field)
            ? field.ValueKind == JsonValueKind.String ? field.GetString() : null : oldRefreshToken;
        if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh)
            || !root.TryGetProperty("expires_in", out var expires) || expires.ValueKind != JsonValueKind.Number
            || !expires.TryGetInt32(out var seconds) || seconds <= 0)
            // Translators: Error when Spotify's authorization response is missing usable access/refresh tokens or their expiry.
            throw new InvalidOperationException(Tr("Spotify did not provide the tokens needed to stay signed in."));
        return new TokenData(clientId, access, refresh, DateTimeOffset.UtcNow.AddSeconds(seconds));
    }

    private void Save(TokenData token)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(token, TokenJsonContext.Default.TokenData);
        try
        {
            var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            Paths.EnsureDirectoryFor(Paths.SpotifyTokenFile);
            var temporary = Paths.TemporaryFor(Paths.SpotifyTokenFile);
            File.WriteAllBytes(temporary, encrypted);
            File.Move(temporary, Paths.SpotifyTokenFile, overwrite: true);
            _token = token;
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    private static TokenData? Load()
    {
        try
        {
            if (!File.Exists(Paths.SpotifyTokenFile)) return null;
            var plain = ProtectedData.Unprotect(File.ReadAllBytes(Paths.SpotifyTokenFile), null, DataProtectionScope.CurrentUser);
            try
            {
                var token = JsonSerializer.Deserialize(plain, TokenJsonContext.Default.TokenData);
                return token is { ClientId: not null, AccessToken: not null, RefreshToken.Length: > 0 }
                    ? token : null;
            }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        { return null; }
    }

    private static string Form(IReadOnlyDictionary<string, string> values) =>
        string.Join("&", values.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in (query ?? string.Empty).TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = part.Split('=', 2);
            result[Uri.UnescapeDataString(split[0])] = split.Length == 2 ? Uri.UnescapeDataString(split[1].Replace('+', ' ')) : string.Empty;
        }
        return result;
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public void Dispose()
    {
        lock (_stateLock)
        {
            if (_disposed) return;
            _disposed = true;
            _version++;
            _token = null;
        }
        _http.Dispose();
        // Refresh and sign-in continuations must still release their managed gate.
    }
}

internal sealed record TokenData(string ClientId, string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

[JsonSerializable(typeof(TokenData))]
internal partial class TokenJsonContext : JsonSerializerContext;
