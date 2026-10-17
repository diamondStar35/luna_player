using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Configuration;

namespace LunaPlayer.Spotify;

internal sealed class SpotifyAuth : IDisposable
{
    internal const string RedirectUri = "http://127.0.0.1:43891/callback/";
    private const string Scopes = "playlist-read-private playlist-read-collaborative user-library-read user-top-read user-read-recently-played";
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private TokenData? _token;

    internal event Action? SignedOut;

    internal SpotifyAuth() => _token = Load();

    internal bool IsSignedIn(string clientId) => _token is { RefreshToken.Length: > 0 } token && token.ClientId == clientId;

    internal async Task<string> GetAccessTokenAsync(string clientId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var token = _token;
            if (token is null || token.ClientId != clientId || token.RefreshToken.Length == 0)
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
                _token = null;
                try { if (File.Exists(Paths.SpotifyTokenFile)) File.Delete(Paths.SpotifyTokenFile); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
                throw new InvalidOperationException(Tr("The Spotify session expired. Sign in again."));
            }
            var refreshed = await ReadTokenAsync(response, clientId, token.RefreshToken, cancellationToken).ConfigureAwait(false);
            Save(refreshed);
            return refreshed.AccessToken;
        }
        finally { _gate.Release(); }
    }

    internal async Task SignInAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (clientId.Length != 32 || !clientId.All(Uri.IsHexDigit))
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
            + WebUtility.HtmlEncode(Tr("You can return to Luna Player now.")) + "</body></html>";
        var body = Encoding.UTF8.GetBytes(responsePage);
        var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headers, timeout.Token).ConfigureAwait(false);
        await stream.WriteAsync(body, timeout.Token).ConfigureAwait(false);
        var values = ParseQuery(query?.Query);
        if (query?.AbsolutePath != "/callback/" || !values.TryGetValue("state", out var receivedState) || receivedState != state)
            throw new InvalidOperationException(Tr("Spotify sign in returned an invalid callback. Please try again."));
        if (values.TryGetValue("error", out var error))
            throw new InvalidOperationException(TrFormat("Spotify sign in was not completed: {reason}.", error));
        if (!values.TryGetValue("code", out var code) || code.Length == 0)
            throw new InvalidOperationException(Tr("Spotify did not return an authorization code."));
        using var tokenResponse = await _http.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = RedirectUri,
                ["client_id"] = clientId, ["code_verifier"] = verifier,
            }), timeout.Token).ConfigureAwait(false);
        var token = await ReadTokenAsync(tokenResponse, clientId, null, timeout.Token).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { Save(token); }
        finally { _gate.Release(); }
    }

    internal void SignOut()
    {
        _gate.Wait();
        try
        {
            _token = null;
            if (File.Exists(Paths.SpotifyTokenFile)) File.Delete(Paths.SpotifyTokenFile);
        }
        finally
        {
            _gate.Release();
            // Stop playback and clear its separate credentials even if deleting this token fails.
            SignedOut?.Invoke();
        }
    }

    private static async Task<TokenData> ReadTokenAsync(HttpResponseMessage response, string clientId,
        string? oldRefreshToken, CancellationToken cancellationToken)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(TrFormat("Spotify sign in failed ({code}). Check the Client ID and redirect URI.", (int)response.StatusCode));
        using var json = JsonDocument.Parse(bytes);
        var root = json.RootElement;
        var access = root.GetProperty("access_token").GetString() ?? string.Empty;
        var refresh = root.TryGetProperty("refresh_token", out var field) ? field.GetString() : oldRefreshToken;
        if (access.Length == 0 || string.IsNullOrWhiteSpace(refresh))
            throw new InvalidOperationException(Tr("Spotify did not provide the tokens needed to stay signed in."));
        var seconds = root.GetProperty("expires_in").GetInt32();
        return new TokenData(clientId, access, refresh, DateTimeOffset.UtcNow.AddSeconds(seconds));
    }

    private void Save(TokenData token)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(token, SpotifyTokenJsonContext.Default.TokenData);
        var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        Paths.EnsureDirectoryFor(Paths.SpotifyTokenFile);
        var temporary = Paths.TemporaryFor(Paths.SpotifyTokenFile);
        File.WriteAllBytes(temporary, encrypted);
        File.Move(temporary, Paths.SpotifyTokenFile, overwrite: true);
        _token = token;
    }

    private static TokenData? Load()
    {
        try
        {
            if (!File.Exists(Paths.SpotifyTokenFile)) return null;
            var plain = ProtectedData.Unprotect(File.ReadAllBytes(Paths.SpotifyTokenFile), null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize(plain, SpotifyTokenJsonContext.Default.TokenData);
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
    public void Dispose() { _http.Dispose(); _gate.Dispose(); }
}

internal sealed record TokenData(string ClientId, string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

[JsonSerializable(typeof(TokenData))]
internal partial class SpotifyTokenJsonContext : JsonSerializerContext;
