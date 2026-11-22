using System.Net;

namespace LunaPlayer.Subsonic;

internal enum AuthenticationKind { Token, LegacyPassword, ApiKey }
internal enum StreamingMode { Original, ServerDefault, Transcode }

internal sealed record Credentials
{
    public AuthenticationKind Kind { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string ApiKey { get; set; } = "";
}

internal sealed record Options
{
    public required Uri Server { get; init; }
    public Credentials Credentials { get; init; } = new();
    public string ApiVersion { get; init; } = "1.16.1";
    public string ClientName { get; init; } = "LunaPlayer";
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaximumAttempts { get; init; } = 3;
    public bool UseFormPost { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(Server);
        ArgumentNullException.ThrowIfNull(Credentials);
        ArgumentNullException.ThrowIfNull(Headers);
        ArgumentNullException.ThrowIfNull(TimeProvider);
        if (!Enum.IsDefined(Credentials.Kind)) throw new ArgumentException(nameof(Options));
        if (!Server.IsAbsoluteUri || Server.Scheme is not ("http" or "https")
            || Server.UserInfo.Length > 0 || Server.Query.Length > 0 || Server.Fragment.Length > 0)
            throw new ArgumentException(nameof(Options));
        if (RequestTimeout <= TimeSpan.Zero || RequestTimeout.TotalMilliseconds > uint.MaxValue - 1
            || MaximumAttempts < 1) throw new ArgumentOutOfRangeException(nameof(Options));
        ArgumentException.ThrowIfNullOrWhiteSpace(ClientName);
        if (!Version.TryParse(ApiVersion, out _)) throw new ArgumentException(nameof(Options));
        foreach (var header in Headers)
        {
            using var probe = new HttpRequestMessage();
            probe.Headers.Add(header.Key, header.Value);
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(nameof(Options));
        }
    }
}

internal sealed record StreamOptions
{
    public StreamingMode Mode { get; set; }
    public string? Format { get; set; }
    public int? MaximumBitRate { get; set; }
    public double? TimeOffset { get; set; }
    public bool EstimateContentLength { get; set; }
}

internal enum FailureKind { Transport, Timeout, Http, InvalidResponse, Rejected, Unsupported, Forbidden }
/// <summary>Endpoint excludes authentication; server messages remain untrusted service data.</summary>
internal sealed class RequestException(FailureKind kind, string endpoint, HttpStatusCode? status = null,
    int? code = null, string? serverMessage = null, string? helpUrl = null)
    : Exception(kind.ToString())
{
    internal FailureKind Kind { get; } = kind;
    internal string Endpoint { get; } = endpoint;
    internal HttpStatusCode? StatusCode { get; } = status;
    internal int? Code { get; } = code;
    internal string? ServerMessage { get; } = serverMessage;
    internal string? HelpUrl { get; } = helpUrl;
}
