using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LunaPlayer.Subsonic.AudioMuse;

/// <summary>AudioMuse has its own authentication and routes; no Subsonic credentials are sent to it.</summary>
internal sealed partial class Client : IDisposable
{
    private readonly Options _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _disposedToken;
    private int _disposed;
    internal Client(Options options, HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Server);
        if (!options.Server.IsAbsoluteUri || options.Server.Scheme is not ("http" or "https")
            || options.Server.UserInfo.Length > 0 || options.Server.Query.Length > 0 || options.Server.Fragment.Length > 0)
            throw new ArgumentException(nameof(options));
        if (options.ResultCount is < 1 or > 500 || !ValidTimeout(options.SearchTimeout) || !ValidTimeout(options.GenerationTimeout))
            throw new ArgumentOutOfRangeException(nameof(options));
        _options = options;
        _ownsHttp = httpClient is null;
        _http = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        { Timeout = Timeout.InfiniteTimeSpan };
        _disposedToken = _lifetime.Token;
    }
    private static bool ValidTimeout(TimeSpan timeout) => timeout > TimeSpan.Zero && timeout.TotalMilliseconds <= uint.MaxValue - 1;
    internal Task<Result> SearchAsync(string query, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        return ReadAsync("api/clap/search", JsonSerializer.SerializeToUtf8Bytes(
            new TextRequest(query, _options.ResultCount, _options.MusicServer), AudioMuseJson.Default.TextRequest), false, token);
    }
    internal Task<Result> GeneratePlaylistAsync(string prompt, CancellationToken token = default)
        => ReadAsync("chat/api/chatPlaylist", PlaylistBody(prompt), true, token);
    internal Task<Result> AlchemyAsync(IReadOnlyList<Seed> seeds, CancellationToken token = default)
        => ReadAsync("api/alchemy", AlchemyBody(seeds), false, token);
    internal Task<Result> SimilarAsync(string id, SeedKind kind = SeedKind.Song, CancellationToken token = default)
        => AlchemyAsync([new Seed(id, kind)], token);
    private byte[] PlaylistBody(string prompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        return JsonSerializer.SerializeToUtf8Bytes(new PlaylistRequest(prompt, _options.ResultCount, _options.MusicServer), AudioMuseJson.Default.PlaylistRequest);
    }
    private byte[] AlchemyBody(IReadOnlyList<Seed> seeds)
    {
        if (seeds.Count == 0 || !seeds.Any(x => x.Operation == Operation.Add)) throw new ArgumentException(nameof(seeds));
        foreach (var seed in seeds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(seed.Id);
            if (!Enum.IsDefined(seed.Kind) || !Enum.IsDefined(seed.Operation)) throw new ArgumentException(nameof(seeds));
        }
        return JsonSerializer.SerializeToUtf8Bytes(new AlchemyRequest(seeds.Select(x => new AlchemyItem(x.Id,
            x.Kind == SeedKind.Song ? "song" : "artist", x.Operation == Operation.Add ? "ADD" : "SUBTRACT")).ToArray(),
            _options.ResultCount, _options.MusicServer), AudioMuseJson.Default.AlchemyRequest);
    }
    private async Task<Result> ReadAsync(string endpoint, byte[] body, bool generation, CancellationToken token)
    {
        using var timeout = Deadline(token, generation);
        try
        {
            using var response = await SendAsync(endpoint, body, timeout.Token).ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            var reply = await JsonSerializer.DeserializeAsync(stream, AudioMuseJson.Default.Reply, timeout.Token).ConfigureAwait(false);
            return Decode(reply, endpoint);
        }
        catch (JsonException) { throw new RequestException(FailureKind.InvalidResponse, endpoint); }
        catch (HttpRequestException) { throw new RequestException(FailureKind.Transport, endpoint); }
        catch (IOException) { throw new RequestException(FailureKind.Transport, endpoint); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && !_disposedToken.IsCancellationRequested)
        { throw new RequestException(FailureKind.Timeout, endpoint); }
    }
    private static Result Decode(Reply? reply, string endpoint)
    {
        if (reply is null) throw new RequestException(FailureKind.InvalidResponse, endpoint);
        if (reply.Error is not null || reply.ErrorMessage is not null)
            throw new RequestException(FailureKind.Rejected, endpoint, serverMessage: reply.ErrorMessage ?? reply.Error);
        if (reply.Response.ValueKind == JsonValueKind.Object)
        {
            var result = reply.Response.Deserialize(AudioMuseJson.Default.Generation) ?? throw new RequestException(FailureKind.InvalidResponse, endpoint);
            return new(result.QueryResults, result.Response, result.PlaylistId);
        }
        if (reply.Results is null && reply.Response.ValueKind != JsonValueKind.String)
            throw new RequestException(FailureKind.InvalidResponse, endpoint);
        return new(reply.Results ?? [], reply.Response.ValueKind == JsonValueKind.String ? reply.Response.GetString() ?? "" : reply.Message ?? "");
    }
    private CancellationTokenSource Deadline(CancellationToken token, bool generation)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(token, _disposedToken);
        timeout.CancelAfter(generation ? _options.GenerationTimeout : _options.SearchTimeout);
        return timeout;
    }
    private async Task<HttpResponseMessage> SendAsync(string endpoint, byte[] body, CancellationToken token)
    {
        var server = new Uri(_options.Server.AbsoluteUri.TrimEnd('/') + "/");
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(server, endpoint));
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        if (_options.Token.Length > 0) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        if (response.IsSuccessStatusCode) return response;
        var status = response.StatusCode;
        response.Dispose();
        throw new RequestException(status is HttpStatusCode.NotFound or HttpStatusCode.NotImplemented ? FailureKind.Unsupported
            : status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ? FailureKind.Forbidden : FailureKind.Http, endpoint, status);
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();
        if (_ownsHttp) _http.Dispose();
        _lifetime.Dispose();
    }
}
