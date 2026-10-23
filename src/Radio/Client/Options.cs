using System.Net;
using LunaPlayer.Configuration;

namespace LunaPlayer.Radio.Client;

/// <summary>Transport settings. No discovery or HTTP work is performed by construction.</summary>
internal sealed record Options
{
    public Uri? Server { get; init; }
    public string UserAgent { get; init; } = $"{AppInfo.Identifier}/{AppInfo.Version}";
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan DiscoveryCacheDuration { get; init; } = TimeSpan.FromMinutes(30);
    public int MaximumAttempts { get; init; } = 3;
}

internal enum ResponseFormat { Json, Xml, Csv, M3u, Pls, Xspf, Turtle, Html }
internal enum StationField { Uuid, Name, Codec, Country, CountryCode, State, Language, Tag }
internal enum StationRanking { MostClicked, RecentlyClicked, RecentlyChanged, HighestVoted, Broken }
internal enum FailureKind { Discovery, Transport, Timeout, Http, InvalidResponse, Rejected }

/// <summary>An unparsed response, including the exact endpoint that supplied it.</summary>
internal sealed record Content(string Text, string? ContentType, Uri Endpoint);

/// <summary>Structured failure details for the caller to present in its own language.
/// ServerMessage is untrusted service data, not application interface text.</summary>
internal sealed class RequestException(
    FailureKind kind, Uri? endpoint = null, HttpStatusCode? statusCode = null,
    string? serverMessage = null, TimeSpan? retryAfter = null, Exception? innerException = null)
    : Exception(serverMessage ?? kind.ToString(), innerException)
{
    internal FailureKind Kind { get; } = kind;
    internal Uri? Endpoint { get; } = endpoint;
    internal HttpStatusCode? StatusCode { get; } = statusCode;
    internal string? ServerMessage { get; } = serverMessage;
    internal TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>Null leaves the API's default intact; zero and false are sent explicitly.</summary>
internal record PageOptions
{
    public uint? Offset { get; init; }
    public uint? Limit { get; init; }
}

internal record StationPageOptions : PageOptions
{
    public bool? HideBroken { get; init; }
}

/// <summary>Ordering for station and directory lists; accepted field names depend on the endpoint.</summary>
internal record ListOptions : StationPageOptions
{
    public string? Order { get; init; }
    public bool? Reverse { get; init; }
}

internal sealed record StreamingServerOptions : PageOptions
{
    public string? Order { get; init; }
    public bool? Reverse { get; init; }
}

/// <summary>All filters accepted by stations/search. Exact-match switches are independent of their terms.</summary>
internal sealed record SearchOptions : ListOptions
{
    public string? Name { get; init; }
    public bool? NameExact { get; init; }
    public string? Country { get; init; }
    public bool? CountryExact { get; init; }
    public string? CountryCode { get; init; }
    public string? State { get; init; }
    public bool? StateExact { get; init; }
    public string? Language { get; init; }
    public bool? LanguageExact { get; init; }
    public string? Tag { get; init; }
    public bool? TagExact { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public string? Codec { get; init; }
    public uint? BitrateMin { get; init; }
    public uint? BitrateMax { get; init; }
    public bool? HasGeoInfo { get; init; }
    public bool? HasExtendedInfo { get; init; }
    public bool? IsHttps { get; init; }
}

internal record ChangeOptions
{
    public Guid? After { get; init; }
    public uint? Limit { get; init; }
}

internal sealed record CheckOptions : ChangeOptions
{
    public uint? Seconds { get; init; }
}

internal sealed record ClickOptions
{
    public Guid? After { get; init; }
    public uint? Seconds { get; init; }
}

/// <summary>Data submitted only by AddStationAsync. CountryCode is ISO 3166-1 alpha-2.</summary>
internal sealed record StationSubmission
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public string? Homepage { get; init; }
    public string? Favicon { get; init; }
    public string? CountryCode { get; init; }
    public string? State { get; init; }
    public string? Language { get; init; }
    public string? LanguageCodes { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}
