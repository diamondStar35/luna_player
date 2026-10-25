using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LunaPlayer.Spotify;

/// <summary>Authorizes the playback connection through a browser PKCE flow.</summary>
internal static class PlaybackOAuth
{
    private const string ClientId = "65b708073fc0480ea92a077233ca87bd";
    private const string Redirect = "http://127.0.0.1:5588/login";
    private const string Scopes = "streaming user-read-playback-state user-modify-playback-state user-read-currently-playing";

    internal static async Task<AccessPointLogin.Credentials> SignInAsync(string responseText, CancellationToken cancellationToken)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 5588);
        listener.Start();
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(48));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(24));
        var parameters = new Dictionary<string, string>
        {
            ["response_type"] = "code", ["client_id"] = ClientId, ["redirect_uri"] = Redirect,
            ["scope"] = Scopes, ["state"] = state, ["code_challenge_method"] = "S256",
            ["code_challenge"] = challenge,
        };
        var authorize = "https://accounts.spotify.com/authorize?" + Form(parameters);
        Process.Start(new ProcessStartInfo(authorize) { UseShellExecute = true });
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        using var browser = await listener.AcceptTcpClientAsync(timeout.Token).ConfigureAwait(false);
        await using var stream = browser.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var request = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
        var route = request?.Split(' ', 3).ElementAtOrDefault(1);
        var callback = route is null ? null : new Uri(new Uri(Redirect), route);
        var body = Encoding.UTF8.GetBytes("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><body>"
            + WebUtility.HtmlEncode(responseText) + "</body></html>");
        var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headers, timeout.Token).ConfigureAwait(false);
        await stream.WriteAsync(body, timeout.Token).ConfigureAwait(false);
        var query = ParseQuery(callback?.Query);
        if (callback?.AbsolutePath != "/login" || !query.TryGetValue("state", out var received)
            || received != state || !query.TryGetValue("code", out var code))
            throw new AuthorizationException(AuthorizationFailure.PlaybackIncomplete);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var response = await http.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code", ["code"] = code,
                ["redirect_uri"] = Redirect, ["client_id"] = ClientId,
                ["code_verifier"] = verifier,
            }), timeout.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(timeout.Token)
            .ConfigureAwait(false));
        var token = json.RootElement.GetProperty("access_token").GetString();
        if (string.IsNullOrEmpty(token)) throw new InvalidDataException();
        return AccessPointLogin.Credentials.FromToken(token);
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=')
        .Replace('+', '-').Replace('/', '_');

    private static string Form(IReadOnlyDictionary<string, string> parameters)
        => string.Join("&", parameters.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in (query ?? string.Empty).TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            values[Uri.UnescapeDataString(pieces[0])] = pieces.Length == 2
                ? Uri.UnescapeDataString(pieces[1].Replace('+', ' ')) : string.Empty;
        }
        return values;
    }
}
