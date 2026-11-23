using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic;

internal enum Support { Unknown, Supported, Unsupported }
internal sealed record Capabilities(Response Server, Extension[] Extensions, bool ExtensionsKnown)
{
    internal Support Extension(string name, int version = 1) => Extensions.Any(x => x.Name == name && x.Versions.Contains(version))
        ? Support.Supported : ExtensionsKnown ? Support.Unsupported : Support.Unknown;
}

/// <summary>One immutable connection. Feature clients share its lifetime, never another server's credentials.</summary>
internal sealed class Client : IDisposable
{
    private readonly Session _session;
    private readonly SemaphoreSlim _discovery = new(1, 1);
    private Capabilities? _capabilities;
    internal Browsing.Library Library { get; }
    internal Playlists.Playlists Playlists { get; }
    internal Listening.Listening Listening { get; }
    internal Radio.Radio Radio { get; }
    internal Podcasts.Podcasts Podcasts { get; }
    internal Scanning.Scanning Scanning { get; }
    internal Media.Media Media { get; }
    internal Media.Downloads Downloads { get; }
    internal Browsing.Selections Selections { get; }
    internal Client(Options options, HttpClient? httpClient = null)
    {
        _session = new Session(options, httpClient);
        Library = new(_session, RequireAsync);
        Playlists = new(_session);
        Listening = new(_session, RequireAsync);
        Radio = new(_session);
        Podcasts = new(_session, RequireAsync);
        Scanning = new(_session);
        Media = new(_session, RequireAsync);
        Downloads = new(Media);
        Selections = new(this);
    }
    internal Task<Response> PingAsync(CancellationToken token = default) => _session.ReadAsync("ping", token: token);
    internal async Task<Models.License> GetLicenseAsync(CancellationToken token = default)
        => Feature.Required((await _session.ReadAsync("getLicense", token: token).ConfigureAwait(false)).License, "getLicense");
    internal async Task<User> GetUserAsync(string username, CancellationToken token = default)
        => Feature.Required((await _session.ReadAsync("getUser", new Parameters().With("username", Feature.Id(username)), token).ConfigureAwait(false)).User, "getUser");
    internal async Task<TokenInfo> GetTokenInfoAsync(CancellationToken token = default)
        => Feature.Required((await _session.ReadAsync("tokenInfo", token: token).ConfigureAwait(false)).TokenInfo, "tokenInfo");

    internal async Task<Capabilities> DiscoverAsync(bool refresh = false, CancellationToken token = default)
    {
        _session.EnsureActive();
        await _discovery.WaitAsync(token).ConfigureAwait(false);
        try
        {
            _session.EnsureActive();
            if (!refresh && _capabilities is { } cached) return cached;
            var server = await PingAsync(token).ConfigureAwait(false);
            try
            {
                var extensions = await _session.ReadAsync("getOpenSubsonicExtensions", token: token, authenticate: false).ConfigureAwait(false);
                _capabilities = new(server, extensions.OpenSubsonicExtensions, true);
                _session.SetFormPost(_capabilities.Extension("formPost") == Support.Supported);
                return _capabilities;
            }
            catch (RequestException error) when (error.Kind == FailureKind.Unsupported
                || error.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Unauthorized)
            { _session.SetFormPost(false); return _capabilities = new(server, [], false); }
        }
        finally { _discovery.Release(); }
    }
    private async Task RequireAsync(string name, int version, CancellationToken token)
    {
        var capabilities = await DiscoverAsync(token: token).ConfigureAwait(false);
        if (capabilities.Extension(name, version) == Support.Unsupported)
            throw new RequestException(FailureKind.Unsupported, name);
    }
    public void Dispose() => _session.Dispose();
}

internal abstract class Feature(Session session)
{
    private protected Session Session { get; } = session;
    internal static string Id(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); return value; }
    internal static T Required<T>(T? value, string endpoint) where T : class
        => value ?? throw new RequestException(FailureKind.InvalidResponse, endpoint);
    private protected async Task Change(string endpoint, Parameters? query, CancellationToken token)
        => await Session.ReadAsync(endpoint, query, token, mutation: true).ConfigureAwait(false);
}
