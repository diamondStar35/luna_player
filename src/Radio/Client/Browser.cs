using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Xml;
using System.Xml.Linq;

namespace LunaPlayer.Radio.Client;

/// <summary>An asynchronous client for Radio Browser's public API. Methods return one requested page;
/// they do not fetch entire catalogs or register playback clicks on the caller's behalf.</summary>
/// <remarks>Endpoint and format behavior follows https://docs.radio-browser.info/ and the server's
/// src/api/mod.rs. Station lists support JSON, XML, CSV, M3U, PLS, XSPF and Turtle; directory and
/// history lists support JSON, XML and CSV. Configuration and streaming servers support JSON and XML;
/// statistics also support HTML. Mirror lists are JSON only. Unsupported formats fail before discovery.
/// A borrowed HttpClient keeps its headers, base address and ownership unchanged. A failed mutation
/// may have reached the server; the client never repeats it automatically.</remarks>
internal sealed partial class Browser : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly Options _options;
    private readonly ProductInfoHeaderValue[] _userAgent;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _disposedToken;
    private Uri? _preferredServer;
    private int _disposed;

    internal Browser(Options? options = null, HttpClient? httpClient = null)
    {
        _options = options ?? new Options();
        if (_options.RequestTimeout <= TimeSpan.Zero || _options.RequestTimeout.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (_options.DiscoveryCacheDuration <= TimeSpan.Zero || _options.MaximumAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (_options.Server is Uri server
            && (!server.IsAbsoluteUri || server.Scheme is not ("http" or "https")
                || server.UserInfo.Length > 0 || server.Query.Length > 0 || server.Fragment.Length > 0))
            throw new ArgumentException(nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.UserAgent);
        using var headers = new HttpRequestMessage();
        headers.Headers.UserAgent.ParseAdd(_options.UserAgent);
        _userAgent = headers.Headers.UserAgent.ToArray();
        _ownsHttp = httpClient is null;
        _http = httpClient ?? new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        }) { Timeout = Timeout.InfiniteTimeSpan };
        _disposedToken = _lifetime.Token;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        _lifetime.Cancel();
        if (_ownsHttp)
            _http.Dispose();
        _lifetime.Dispose();
        // Discovery may still have awaiters unwinding cancellation. Its semaphore never opens a wait
        // handle, and must remain available for those awaiters to release without a disposal race.
    }

    private async Task<T> ReadAsync<T>(string path, Dictionary<string, string>? parameters,
        JsonTypeInfo<T> type, CancellationToken token, bool mutation = false) where T : class
    {
        var content = await SendAsync(path, parameters, token, mutation).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        var result = Deserialize(content, type);
        token.ThrowIfCancellationRequested();
        return result;
    }

    private static T Deserialize<T>(Content content, JsonTypeInfo<T> type) where T : class
    {
        try
        {
            var result = JsonSerializer.Deserialize(content.Text, type) ?? throw new JsonException();
            if (result is ActionResult { Ok: false } rejected)
                throw new RequestException(FailureKind.Rejected, content.Endpoint, serverMessage: rejected.Message);
            return result;
        }
        catch (JsonException exception)
        {
            throw new RequestException(FailureKind.InvalidResponse, content.Endpoint, innerException: exception);
        }
    }

    private async Task<Content> SendAsync(string path, Dictionary<string, string>? parameters,
        CancellationToken token, bool mutation = false)
        => await ExecuteAsync(path, parameters, token, mutation, async (response, endpoint, cancellation) =>
        {
            var body = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
            var content = new Content(body, response.Content.Headers.ContentType?.ToString(), endpoint);
            if (mutation) EnsureActionSuccess(content, path);
            return content;
        }).ConfigureAwait(false);

    private async Task<T> ExecuteAsync<T>(string path, Dictionary<string, string>? parameters,
        CancellationToken token, bool mutation, Func<HttpResponseMessage, Uri, CancellationToken, Task<T>> read)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, _disposedToken);
        var cancellation = lifetime.Token;
        var servers = await GetServersAsync(cancellation).ConfigureAwait(false);
        var discovery = Volatile.Read(ref _servers);
        if (!ReferenceEquals(discovery?.Addresses, servers))
            discovery = null;
        if (Volatile.Read(ref _preferredServer) is Uri preferred && servers.Contains(preferred))
            servers = servers.OrderBy(server => server == preferred ? 0 : 1).ToArray();
        RequestException? failure = null;
        var attempts = mutation ? 1 : Math.Min(_options.MaximumAttempts, servers.Length);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            cancellation.ThrowIfCancellationRequested();
            var server = servers[attempt];
            var endpoint = new Uri(server, path);
            if (!mutation && parameters is { Count: > 0 })
                endpoint = new UriBuilder(endpoint) { Query = Encode(parameters) }.Uri;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            timeout.CancelAfter(_options.RequestTimeout);
            using var request = new HttpRequestMessage(mutation ? HttpMethod.Post : HttpMethod.Get, endpoint);
            foreach (var product in _userAgent)
                request.Headers.UserAgent.Add(product);
            if (mutation)
                request.Content = new FormUrlEncodedContent(parameters ?? []);
            try
            {
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                    var retryAfter = response.Headers.RetryAfter?.Delta;
                    if (!retryAfter.HasValue && response.Headers.RetryAfter?.Date is DateTimeOffset retryDate)
                        retryAfter = TimeSpan.FromSeconds(Math.Max(0, (retryDate - DateTimeOffset.UtcNow).TotalSeconds));
                    failure = new RequestException(FailureKind.Http, endpoint, response.StatusCode,
                        body.Length > 4096 ? body[..4096] : body, retryAfter);
                    // A vote or click changes state even though the API also accepts it through GET.
                    // Retry only reads, and never evade a rate limit by trying another mirror.
                    if (mutation || !Retryable(response.StatusCode))
                        throw failure;
                    continue;
                }
                var result = await read(response, endpoint, timeout.Token).ConfigureAwait(false);
                cancellation.ThrowIfCancellationRequested();
                Volatile.Write(ref _preferredServer, server);
                return result;
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException)
            {
                cancellation.ThrowIfCancellationRequested();
                failure = new RequestException(FailureKind.Transport, endpoint, innerException: exception);
            }
            catch (OperationCanceledException exception) when (!cancellation.IsCancellationRequested)
            {
                failure = new RequestException(FailureKind.Timeout, endpoint, innerException: exception);
            }
        }
        cancellation.ThrowIfCancellationRequested();
        // Exhausted discovery data may be stale. The next request discovers again, while this
        // request remains bounded by MaximumAttempts and never repeats a mutation.
        if (discovery is not null)
            Interlocked.CompareExchange(ref _servers, null, discovery);
        // A later bounded read retry must not promote the mirror that just failed to the front again.
        if (!mutation) Interlocked.CompareExchange(ref _preferredServer, null, servers[0]);
        throw failure!;
    }

    private static bool Retryable(HttpStatusCode status)
        => status == HttpStatusCode.RequestTimeout || (int)status >= 500;

    private static void EnsureActionSuccess(Content content, string path)
    {
        if (path.StartsWith("json/", StringComparison.Ordinal))
        {
            Deserialize(content, Serialization.Default.ActionResult);
            return;
        }
        if (!path.StartsWith("xml/", StringComparison.Ordinal))
            return; // M3U and PLS click responses report failures through HTTP, not a status object.
        try
        {
            using var text = new StringReader(content.Text);
            using var xml = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            var status = XDocument.Load(xml).Root?.Element("status");
            if (!bool.TryParse((string?)status?.Attribute("ok"), out var ok))
                throw new XmlException();
            if (!ok)
                throw new RequestException(FailureKind.Rejected, content.Endpoint,
                    serverMessage: (string?)status?.Attribute("message"));
        }
        catch (XmlException exception)
        {
            throw new RequestException(FailureKind.InvalidResponse, content.Endpoint, innerException: exception);
        }
    }
}
