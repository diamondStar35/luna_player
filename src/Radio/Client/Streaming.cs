using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace LunaPlayer.Radio.Client;

internal sealed partial class Browser
{
    /// <summary>Delivers stations as the response array arrives. A read retry may deliver a station
    /// again; consumers should identify rows by UUID rather than treating arrival order as an ID.</summary>
    internal Task<int> StreamStationsAsync(SearchOptions options,
        Func<Station, CancellationToken, ValueTask> receive, CancellationToken token = default)
        => StreamArrayAsync("stations/search", SearchParameters(options), Serialization.Default.Station, receive, token);

    internal Task<int> StreamCountriesAsync(ListOptions options,
        Func<Country, CancellationToken, ValueTask> receive, CancellationToken token = default, string? filter = null)
        => StreamArrayAsync(DirectoryRoute("countries", filter), Parameters(options), Serialization.Default.Country, receive, token);

    internal Task<int> StreamLanguagesAsync(ListOptions options,
        Func<Language, CancellationToken, ValueTask> receive, CancellationToken token = default, string? filter = null)
        => StreamArrayAsync(DirectoryRoute("languages", filter), Parameters(options), Serialization.Default.Language, receive, token);

    internal Task<int> StreamTagsAsync(ListOptions options,
        Func<DirectoryEntry, CancellationToken, ValueTask> receive, CancellationToken token = default, string? filter = null)
        => StreamArrayAsync(DirectoryRoute("tags", filter), Parameters(options), Serialization.Default.DirectoryEntry, receive, token);

    internal Task<int> StreamCodecsAsync(ListOptions options,
        Func<DirectoryEntry, CancellationToken, ValueTask> receive, CancellationToken token = default, string? filter = null)
        => StreamArrayAsync(DirectoryRoute("codecs", filter), Parameters(options), Serialization.Default.DirectoryEntry, receive, token);

    internal Task<int> StreamStatesAsync(string country, ListOptions options,
        Func<State, CancellationToken, ValueTask> receive, CancellationToken token = default, string? filter = null)
        => StreamArrayAsync(StatesRoute(country, filter), Parameters(options), Serialization.Default.State, receive, token);

    private Task<int> StreamArrayAsync<T>(string route, Dictionary<string, string> parameters, JsonTypeInfo<T> type,
        Func<T, CancellationToken, ValueTask> receive, CancellationToken token) where T : class
    {
        ArgumentNullException.ThrowIfNull(receive);
        return ExecuteAsync("json/" + route, parameters, token, mutation: false, async (response, endpoint, cancellation) =>
        {
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
                var count = 0;
                await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable(stream, type, cancellation)
                    .ConfigureAwait(false))
                {
                    if (item is null) throw new JsonException();
                    await receive(item, cancellation).ConfigureAwait(false);
                    count++;
                }
                return count;
            }
            catch (JsonException error)
            {
                throw new RequestException(FailureKind.InvalidResponse, endpoint, innerException: error);
            }
        });
    }
}
