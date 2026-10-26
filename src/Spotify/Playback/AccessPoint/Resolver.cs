using System.Text.Json;

namespace LunaPlayer.Spotify.Playback.AccessPoint;

/// <summary>Finds Spotify access points in the order returned by its resolver.</summary>
internal sealed class Resolver(HttpClient http)
{
    private const string ResolveUrl =
        "https://apresolve.spotify.com/?type=accesspoint&type=dealer&type=spclient";

    internal async Task<(string Host, int Port)> ResolveAsync(string kind,
        CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(ResolveUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var data = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!data.RootElement.TryGetProperty(kind, out var hosts)
            || hosts.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException();
        foreach (var value in hosts.EnumerateArray())
        {
            if (TryParseAddress(value.GetString(), out var address)) return address;
        }
        throw new InvalidDataException();
    }

    internal static bool TryParseAddress(string? value, out (string Host, int Port) address)
    {
        address = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var separator = value.LastIndexOf(':');
        if (separator <= 0 || separator == value.Length - 1
            || !int.TryParse(value.AsSpan(separator + 1), out var port)
            || port is < 1 or > 65535) return false;
        address = (value[..separator], port);
        return true;
    }
}
