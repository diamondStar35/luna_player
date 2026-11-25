using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Transport;

/// <summary>Owns requests, not application state. Mutations are identified by API semantics, not HTTP verbs.</summary>
internal sealed class Session : IDisposable
{
    private readonly Options _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _disposedToken;
    private readonly Uri _base;
    private bool _formPost;
    private int _disposed;
    internal TimeProvider Clock => _options.TimeProvider;
    internal void EnsureActive() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    internal Session(Options options, HttpClient? http)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options with { Headers = new Dictionary<string, string>(options.Headers) };
        _formPost = options.UseFormPost;
        _base = new Uri(options.Server.AbsoluteUri.TrimEnd('/') + "/");
        _ownsHttp = http is null;
        _http = http ?? new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false, UseCookies = false, AutomaticDecompression = DecompressionMethods.All
        }) { Timeout = Timeout.InfiniteTimeSpan };
        _disposedToken = _lifetime.Token;
    }

    internal Uri Address(string endpoint, Parameters? parameters = null, bool authenticate = true)
        => new(Endpoint(endpoint).AbsoluteUri + "?" + Query(parameters, authenticate).Query);

    internal void SetFormPost(bool supported) => Volatile.Write(ref _formPost, _options.UseFormPost || supported);

    private Uri Endpoint(string endpoint)
    {
        EnsureActive();
        if (endpoint.Length == 0 || !endpoint.All(c => char.IsAsciiLetterOrDigit(c)))
            throw new ArgumentException(nameof(endpoint));
        return new Uri(_base, "rest/" + endpoint + ".view");
    }

    private Parameters Query(Parameters? parameters, bool authenticate)
    {
        var query = new Parameters().With("v", _options.ApiVersion).With("c", _options.ClientName).With("f", "json");
        if (authenticate)
        {
            var credentials = _options.Credentials;
            if (credentials.Kind == AuthenticationKind.ApiKey)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(credentials.ApiKey);
                query.With("apiKey", credentials.ApiKey);
            }
            else
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(credentials.Username);
                query.With("u", credentials.Username);
                if (credentials.Kind == AuthenticationKind.LegacyPassword)
                    query.With("p", "enc:" + Convert.ToHexStringLower(Encoding.UTF8.GetBytes(credentials.Password)));
                else
                {
                    var salt = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
                    query.With("t", Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(credentials.Password + salt))))
                        .With("s", salt);
                }
            }
        }
        if (parameters is not null) query.Include(parameters);
        return query;
    }

    internal async Task<Response> ReadAsync(string endpoint, Parameters? parameters = null,
        CancellationToken token = default, bool mutation = false, bool authenticate = true, byte[]? json = null)
    {
        using var response = await SendAsync(endpoint, parameters, token, mutation, authenticate, json).ConfigureAwait(false);
        try
        {
            await using var stream = await response.Response.Content.ReadAsStreamAsync(response.Token).ConfigureAwait(false);
            var envelope = await JsonSerializer.DeserializeAsync(stream, ProtocolJson.Default.Envelope, response.Token).ConfigureAwait(false);
            return Validate(envelope?.Response, endpoint);
        }
        catch (JsonException) { throw new RequestException(FailureKind.InvalidResponse, endpoint); }
        catch (IOException) { throw new RequestException(FailureKind.Transport, endpoint); }
        catch (HttpRequestException) { throw new RequestException(FailureKind.Transport, endpoint); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && !_disposedToken.IsCancellationRequested)
        { throw new RequestException(FailureKind.Timeout, endpoint); }
    }

    internal async Task<Media.Response> OpenAsync(string endpoint, Parameters? parameters, CancellationToken token,
        long? byteOffset = null)
    {
        if (byteOffset < 0) throw new ArgumentOutOfRangeException(nameof(byteOffset));
        var lease = await SendAsync(endpoint, parameters, token, false, true, byteOffset: byteOffset, media: true).ConfigureAwait(false);
        try
        {
            var content = lease.Response.Content;
            var type = content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
            var body = await content.ReadAsStreamAsync(lease.Token).ConfigureAwait(false);
            var media = new Media.Response(lease, body);
            // XmlReader has no token overload; the response stream applies the request's cancellation to reads.
            var stream = media.Stream;
            if (type is "application/json" or "text/json")
            {
                try
                {
                    var result = await JsonSerializer.DeserializeAsync(stream, ProtocolJson.Default.Envelope, lease.Token).ConfigureAwait(false);
                    Validate(result?.Response, endpoint);
                }
                catch (JsonException) { throw new RequestException(FailureKind.InvalidResponse, endpoint); }
                throw new RequestException(FailureKind.InvalidResponse, endpoint);
            }
            if (type is "application/xml" or "text/xml")
            {
                using var reader = XmlReader.Create(stream, new XmlReaderSettings
                {
                    Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, CloseInput = false
                });
                try
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        lease.Token.ThrowIfCancellationRequested();
                        if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "error") continue;
                        int.TryParse(reader.GetAttribute("code"), out var code);
                        throw ApiError(endpoint, new Models.Error { Code = code, Message = reader.GetAttribute("message") ?? "" });
                    }
                }
                catch (XmlException) { throw new RequestException(FailureKind.InvalidResponse, endpoint); }
                throw new RequestException(FailureKind.InvalidResponse, endpoint);
            }
            if (type.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
                throw new RequestException(FailureKind.InvalidResponse, endpoint);
            if (byteOffset is > 0 && (lease.Response.StatusCode != HttpStatusCode.PartialContent
                || content.Headers.ContentRange?.From != byteOffset))
                throw new RequestException(FailureKind.InvalidResponse, endpoint);
            // Header negotiation is bounded, but an active media stream must not expire after 30 seconds.
            lease.Timeout.CancelAfter(Timeout.InfiniteTimeSpan);
            return media;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && !_disposedToken.IsCancellationRequested)
        { lease.Dispose(); throw new RequestException(FailureKind.Timeout, endpoint); }
        catch (HttpRequestException) { lease.Dispose(); throw new RequestException(FailureKind.Transport, endpoint); }
        catch (IOException) { lease.Dispose(); throw new RequestException(FailureKind.Transport, endpoint); }
        catch { lease.Dispose(); throw; }
    }

    private async Task<Lease> SendAsync(string endpoint, Parameters? parameters, CancellationToken token,
        bool mutation, bool authenticate, byte[]? json = null, long? byteOffset = null, bool media = false)
    {
        EnsureActive();
        token.ThrowIfCancellationRequested();
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(token, _disposedToken);
        timeout.CancelAfter(_options.RequestTimeout);
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    var target = Endpoint(endpoint);
                    var query = Query(parameters, authenticate).Query;
                    var form = json is null && Volatile.Read(ref _formPost)
                        && endpoint is not ("ping" or "getOpenSubsonicExtensions");
                    // Form requests never pass through a URI: a large queue can exceed its length limit.
                    var uri = form ? target : new Uri(target.AbsoluteUri + "?" + query);
                    using var request = new HttpRequestMessage(json is null ? HttpMethod.Get : HttpMethod.Post, uri);
                    if (form)
                    {
                        request.Method = HttpMethod.Post;
                        request.Content = new StringContent(query, Encoding.UTF8, "application/x-www-form-urlencoded");
                    }
                    foreach (var header in _options.Headers) request.Headers.Add(header.Key, header.Value);
                    if (byteOffset.HasValue) request.Headers.Range = new RangeHeaderValue(byteOffset, null);
                    if (json is not null)
                    {
                        request.Content = new ByteArrayContent(json);
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    }
                    var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                    if (media) response = await FollowMediaRedirectsAsync(response, uri, byteOffset, endpoint, timeout.Token).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode) return new Lease(response, timeout);
                    var retry = !mutation && json is null && attempt < _options.MaximumAttempts
                        && ((int)response.StatusCode is 408 or 429 || (int)response.StatusCode >= 500 && response.StatusCode != HttpStatusCode.NotImplemented);
                    var delay = response.Headers.RetryAfter?.Delta
                        ?? (response.Headers.RetryAfter?.Date is { } date ? date - Clock.GetUtcNow() : TimeSpan.FromMilliseconds(100 * attempt));
                    var status = response.StatusCode;
                    response.Dispose();
                    if (!retry) throw new RequestException(status == HttpStatusCode.NotImplemented ? FailureKind.Unsupported
                        : status is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized ? FailureKind.Forbidden : FailureKind.Http, endpoint, status);
                    await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, Clock, timeout.Token).ConfigureAwait(false);
                }
                catch (HttpRequestException) when (!mutation && json is null && attempt < _options.MaximumAttempts)
                { await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), Clock, timeout.Token).ConfigureAwait(false); }
                catch (HttpRequestException) { throw new RequestException(FailureKind.Transport, endpoint); }
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && !_disposedToken.IsCancellationRequested)
        { timeout.Dispose(); throw new RequestException(FailureKind.Timeout, endpoint); }
        catch { timeout.Dispose(); throw; }
    }

    private async Task<HttpResponseMessage> FollowMediaRedirectsAsync(HttpResponseMessage response, Uri source,
        long? byteOffset, string endpoint, CancellationToken token)
    {
        try
        {
            for (var hop = 0; (int)response.StatusCode is 301 or 302 or 303 or 307 or 308; hop++)
            {
                if (hop == 5 || response.Headers.Location is not { } location)
                    throw new RequestException(FailureKind.InvalidResponse, endpoint);
                var target = location.IsAbsoluteUri ? location : new Uri(source, location);
                if (target.Scheme is not ("http" or "https") || target.UserInfo.Length > 0
                    || source.Scheme == "https" && target.Scheme != "https")
                    throw new RequestException(FailureKind.InvalidResponse, endpoint);
                using var request = new HttpRequestMessage(HttpMethod.Get, target);
                // A CDN or Gravatar redirect must not receive reverse-proxy credentials for the music server.
                if (target.GetLeftPart(UriPartial.Authority) == _base.GetLeftPart(UriPartial.Authority))
                    foreach (var header in _options.Headers) request.Headers.Add(header.Key, header.Value);
                if (byteOffset.HasValue) request.Headers.Range = new RangeHeaderValue(byteOffset, null);
                response.Dispose();
                response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                source = target;
            }
            return response;
        }
        catch { response.Dispose(); throw; }
    }

    internal static Response Validate(Response? response, string endpoint)
    {
        if (response?.Status == "ok") return response;
        if (response?.Error is { } error) throw ApiError(endpoint, error);
        throw new RequestException(FailureKind.InvalidResponse, endpoint);
    }
    private static RequestException ApiError(string endpoint, Models.Error error)
        => new(error.Code is 20 or 30 ? FailureKind.Unsupported : error.Code == 50 ? FailureKind.Forbidden : FailureKind.Rejected,
            endpoint, code: error.Code, serverMessage: error.Message, helpUrl: error.HelpUrl);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();
        if (_ownsHttp) _http.Dispose();
        _lifetime.Dispose();
    }

    internal sealed class Lease(HttpResponseMessage response, CancellationTokenSource timeout) : IDisposable
    {
        internal HttpResponseMessage Response { get; } = response;
        internal CancellationTokenSource Timeout { get; } = timeout;
        internal CancellationToken Token { get; } = timeout.Token;
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            try { Timeout.Cancel(); }
            finally { Response.Dispose(); Timeout.Dispose(); }
        }
    }
}
