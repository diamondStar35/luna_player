using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Spotify;

internal enum CollectionKind { Playlist, Mix, Radio, LikedSongs, TopTracks, RecentlyPlayed, Artist }
internal sealed record Collection(string Id, string Name, CollectionKind Kind, string Description)
{
    // Translators: Row in the Spotify browser. {name} is the collection name and {kind} is its translated type.
    internal string Label => TrFormat("{name} - {kind}", DisplayName, KindLabel);

    internal string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name
        // Translators: Name of the saved favorite tracks collection in the Spotify browser.
        : Kind == CollectionKind.LikedSongs ? Tr("Liked songs")
        // Translators: Fallback name in the Spotify browser when a playlist has no name.
        : Tr("Untitled playlist");

    internal string DisplayDescription => Description.Length > 0 ? Description
        // Translators: Description of the saved favorite tracks collection in the Spotify browser.
        : Kind == CollectionKind.LikedSongs ? Tr("Tracks saved to your Spotify library.") : string.Empty;

    private string KindLabel => Kind switch
    {
        // Translators: Collection type in the Spotify library, containing the user's saved favorite tracks.
        CollectionKind.LikedSongs => Tr("Liked songs"),
        // Translators: Collection type on Spotify Home, containing the user's most played tracks.
        CollectionKind.TopTracks => Tr("Your top tracks"),
        // Translators: Collection type on Spotify Home, containing listening history.
        CollectionKind.RecentlyPlayed => Tr("Recently played"),
        // Translators: Type of a personalized song collection in the Spotify browser.
        CollectionKind.Mix => Tr("Mix"),
        // Translators: Type of a collection of related tracks in the Spotify browser.
        CollectionKind.Radio => Tr("Radio"),
        // Translators: Type of a musician or group result in Spotify search.
        CollectionKind.Artist => Tr("Artist"),
        // Translators: Type of a song collection in the Spotify browser.
        _ => Tr("Playlist"),
    };
}

internal sealed record Track(string Name, string Artists, string Uri, int DurationMs)
{
    // Translators: Track row in Spotify lists. {name} is the song, {artists} lists its performers and {duration} is its playing time.
    internal string Label => TrFormat("{name} - {artists} - {duration}", Name, Artists,
        TimeSpan.FromMilliseconds(Math.Max(0, DurationMs)).ToString(
            DurationMs >= 3_600_000 ? @"h\:mm\:ss" : @"m\:ss"));
    // Different releases can give the same song different Spotify track URIs.
    internal string ArtistSongKey => Normalize(Name) + "\0" + Normalize(Artists);

    private static string Normalize(string value) =>
        string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
}

internal sealed record SearchItem(Track? Track, Collection? Collection)
{
    internal string Label => Track is { } track
        // Translators: A song result in Spotify search. {track} contains the name, artists and duration; Track identifies the result type.
        ? TrFormat("{track} - Track", track.Label) : Collection?.Label ?? string.Empty;
}

internal sealed record SearchPage(IReadOnlyList<SearchItem> Items, bool HasMore);
internal enum SearchFilter { All, Songs, Artists, Playlists }
internal sealed record TrackPage(IReadOnlyList<Track> Tracks, string? Next);
internal sealed class QuotaException : Exception
{
    internal QuotaException(TimeSpan? retryAfter) => RetryAfter = retryAfter;

    private TimeSpan? RetryAfter { get; }

    internal string Describe() => RetryAfter is { } wait
            // Translators: Error when a Spotify developer app has exhausted its quota. {wait} is the localized waiting time.
            ? TrFormat("Spotify's developer-app quota has been reached. Spotify says to try again in {wait}.", FormatWait(wait))
            // Translators: Error when Spotify has exhausted a developer app's quota without saying when it resets.
            : Tr("Spotify's developer-app quota has been reached. Spotify did not provide a reset time.");

    private static string FormatWait(TimeSpan wait)
    {
        // Round up so the displayed wait never tells users to retry before Retry-After expires.
        var totalMinutes = (int)Math.Clamp(Math.Ceiling(wait.TotalMinutes), 1, int.MaxValue);
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        // Translators: Waiting time in a Spotify quota error. {count} is the number of hours.
        var hourText = TrPluralFormat("{count} hour", "{count} hours", hours, hours);
        // Translators: Waiting time in a Spotify quota error. {count} is the number of minutes.
        var minuteText = TrPluralFormat("{count} minute", "{count} minutes", minutes, minutes);
        return hours == 0 ? minuteText : minutes == 0 ? hourText
            // Translators: Waiting time in a Spotify quota error. {hours} and {minutes} already contain their localized number and unit.
            : TrFormat("{hours} and {minutes}", hourText, minuteText);
    }
}
internal sealed class RateLimitException : Exception
{
    internal RateLimitException(TimeSpan? retryAfter = null) => RetryAfter = retryAfter;

    private TimeSpan? RetryAfter { get; }

    internal string Describe() => RetryAfter is { } wait
            // Translators: Error when Spotify asks the app to delay requests. {seconds} is the waiting time in seconds.
            ? TrPluralFormat("Spotify asked Luna to wait about {seconds} second before trying again.",
                "Spotify asked Luna to wait about {seconds} seconds before trying again.",
                (int)Math.Clamp(Math.Ceiling(wait.TotalSeconds), 1, int.MaxValue),
                (int)Math.Clamp(Math.Ceiling(wait.TotalSeconds), 1, int.MaxValue))
            // Translators: Error when Spotify continues limiting requests without giving a waiting time.
            : Tr("Spotify is still limiting requests. Wait a moment and try again.");
}

internal sealed class Catalog
{
    private static readonly Uri ApiRoot = new("https://api.spotify.com/v1/");
    private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly Func<CancellationToken, Task<string>> _accessToken;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private readonly Dictionary<(string ArtistId, int Offset), (string[] AlbumIds, bool HasMore)> _artistAlbumBatches = [];
    private readonly object _albumBatchLock = new();

    internal Catalog(Func<Auth> auth, SpotifySettings settings)
        : this(token => AccessTokenAsync(auth(), settings, token), SharedHttp) { }

    // The injectable transport lets the small client be tested without a Spotify account.
    internal Catalog(Func<CancellationToken, Task<string>> accessToken, HttpClient http)
    {
        _accessToken = accessToken;
        _http = http;
    }

    private static Task<string> AccessTokenAsync(Auth auth, SpotifySettings settings, CancellationToken token)
    {
        if (settings.ClientId.Length == 0)
            throw new AuthorizationException(AuthorizationFailure.MissingConfiguration);
        return auth.GetAccessTokenAsync(settings.ClientId, token);
    }

    internal async Task<IReadOnlyList<Collection>> LibraryAsync(CancellationToken token = default)
    {
        var result = new List<Collection>
        {
            new("liked", string.Empty, CollectionKind.LikedSongs, string.Empty),
        };
        await ForEachPageAsync("me/playlists?limit=50", item =>
        {
            var id = String(item, "id");
            if (string.IsNullOrWhiteSpace(id)) return;
            var name = String(item, "name") ?? string.Empty;
            var kind = name.Contains("mix", StringComparison.OrdinalIgnoreCase) ? CollectionKind.Mix
                : name.Contains("radio", StringComparison.OrdinalIgnoreCase) ? CollectionKind.Radio
                : CollectionKind.Playlist;
            result.Add(new Collection(id, name, kind, String(item, "description") ?? string.Empty));
        }, token).ConfigureAwait(false);
        return result;
    }

    internal async Task<TrackPage> TracksPageAsync(Collection collection, string? cursor,
        CancellationToken token = default)
    {
        if (collection.Kind == CollectionKind.Artist)
            return await ArtistPageAsync(collection, cursor, token).ConfigureAwait(false);

        var initial = collection.Kind switch
        {
            CollectionKind.LikedSongs => "me/tracks?limit=50",
            CollectionKind.TopTracks => "me/top/tracks?limit=50",
            CollectionKind.RecentlyPlayed => "me/player/recently-played?limit=50",
            _ => $"playlists/{Uri.EscapeDataString(collection.Id)}/items?limit=50",
        };
        var uri = cursor is null ? new Uri(ApiRoot, initial) : ValidateNext(cursor);
        using (var document = await GetAsync(uri, token).ConfigureAwait(false))
        {
            var result = new List<Track>();
            foreach (var item in Items(document.RootElement))
            {
                var track = collection.Kind switch
                {
                    CollectionKind.LikedSongs or CollectionKind.RecentlyPlayed => Property(item, "track"),
                    CollectionKind.TopTracks => item,
                    _ => Property(item, "item") is { ValueKind: JsonValueKind.Object } value
                        ? value : Property(item, "track"),
                };
                if (Map(track) is { } mapped) result.Add(mapped);
            }
            var next = collection.Kind is CollectionKind.TopTracks or CollectionKind.RecentlyPlayed
                ? null : String(document.RootElement, "next");
            if (next is not null) ValidateNext(next);
            return new TrackPage(result, next);
        }
    }

    private async Task<TrackPage> ArtistPageAsync(Collection artist, string? cursor,
        CancellationToken token)
    {
        // Spotify's Top Tracks endpoint is unavailable to development apps. Continue
        // through search results before walking releases: each search page supplies up
        // to ten tracks with one request, whereas a release needs an album lookup too.
        if (cursor is null || cursor.StartsWith("search:", StringComparison.Ordinal))
        {
            var searchOffset = 0;
            if (cursor is not null && (!int.TryParse(cursor[7..], out searchOffset)
                || searchOffset < 0 || searchOffset > 1000))
                throw new InvalidDataException("Invalid artist search cursor.");
            var name = artist.Name.Replace("\"", string.Empty, StringComparison.Ordinal);
            var query = $"artist:\"{name}\"";
            var path = $"search?q={Uri.EscapeDataString(query)}&type=track&limit=10&offset={searchOffset}";
            using var document = await GetAsync(new Uri(ApiRoot, path), token).ConfigureAwait(false);
            var results = Property(document.RootElement, "tracks");
            return new TrackPage(MapArtistTracks(results, artist.Id),
                searchOffset < 1000 && HasNext(results) ? $"search:{searchOffset + 10}" : "albums:0");
        }

        if (cursor.StartsWith("albums:", StringComparison.Ordinal)
            && int.TryParse(cursor[7..], out var albumOffset) && albumOffset >= 0)
        {
            var batchOffset = albumOffset / 10 * 10;
            (string[] AlbumIds, bool HasMore) batch;
            lock (_albumBatchLock)
                _artistAlbumBatches.TryGetValue((artist.Id, batchOffset), out batch);
            if (batch.AlbumIds is null)
            {
                var path = $"artists/{Uri.EscapeDataString(artist.Id)}/albums?limit=10&offset={batchOffset}";
                using var albums = await GetAsync(new Uri(ApiRoot, path), token).ConfigureAwait(false);
                batch = (Items(albums.RootElement).Select(item => String(item, "id"))
                    .Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).ToArray(),
                    HasNext(albums.RootElement));
                lock (_albumBatchLock) _artistAlbumBatches[(artist.Id, batchOffset)] = batch;
            }
            var index = albumOffset - batchOffset;
            if (index >= batch.AlbumIds.Length)
                return new TrackPage([], null);
            var albumId = batch.AlbumIds[index];
            var moreAlbums = index + 1 < batch.AlbumIds.Length || batch.HasMore;
            var trackPath = $"albums/{Uri.EscapeDataString(albumId)}/tracks?limit=50";
            return await ArtistAlbumPageAsync(artist.Id, new Uri(ApiRoot, trackPath),
                albumOffset, moreAlbums, token).ConfigureAwait(false);
        }

        var parts = cursor.Split(':', 4);
        if (parts.Length == 4 && parts[0] == "tracks"
            && int.TryParse(parts[1], out var offset) && offset >= 0
            && parts[2] is "0" or "1")
            return await ArtistAlbumPageAsync(artist.Id, ValidateNext(Uri.UnescapeDataString(parts[3])),
                offset, parts[2] == "1", token).ConfigureAwait(false);
        throw new InvalidDataException("Invalid artist page cursor.");
    }

    private async Task<TrackPage> ArtistAlbumPageAsync(string artistId, Uri uri,
        int albumOffset, bool moreAlbums, CancellationToken token)
    {
        using var document = await GetAsync(uri, token).ConfigureAwait(false);
        var tracks = MapArtistTracks(document.RootElement, artistId);
        var nextTrackPage = String(document.RootElement, "next");
        if (nextTrackPage is not null)
        {
            ValidateNext(nextTrackPage);
            return new TrackPage(tracks,
                $"tracks:{albumOffset}:{(moreAlbums ? 1 : 0)}:{Uri.EscapeDataString(nextTrackPage)}");
        }
        return new TrackPage(tracks, moreAlbums ? $"albums:{albumOffset + 1}" : null);
    }

    private static IReadOnlyList<Track> MapArtistTracks(JsonElement page, string artistId) =>
        Items(page).Where(item => Items(Property(item, "artists"))
                .Any(value => String(value, "id") == artistId))
            .Select(Map).OfType<Track>().ToArray();

    private static Uri ValidateNext(string next)
    {
        if (!Uri.TryCreate(next, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || uri.Host != ApiRoot.Host || uri.Port != ApiRoot.Port || uri.UserInfo.Length != 0
            || !uri.AbsolutePath.StartsWith("/v1/", StringComparison.Ordinal))
            throw new InvalidDataException("Spotify returned an invalid next-page link.");
        return uri;
    }

    internal async Task<SearchPage> SearchAsync(string query, int offset = 0,
        SearchFilter filter = SearchFilter.All, CancellationToken token = default)
    {
        if (offset is < 0 or > 1000) return new SearchPage([], false);
        var type = filter switch
        {
            SearchFilter.Songs => "track",
            SearchFilter.Artists => "artist",
            SearchFilter.Playlists => "playlist",
            _ => "track,artist,playlist",
        };
        var path = $"search?q={Uri.EscapeDataString(query)}&type={type}&limit=10&offset={offset}";
        using var document = await GetAsync(new Uri(ApiRoot, path), token).ConfigureAwait(false);
        var results = new List<SearchItem>();
        var tracks = Property(document.RootElement, "tracks");
        var artists = Property(document.RootElement, "artists");
        var playlists = Property(document.RootElement, "playlists");
        foreach (var item in Items(tracks))
            if (Map(item) is { } track) results.Add(new(track, null));
        foreach (var item in Items(artists))
            if (MapCollection(item, CollectionKind.Artist) is { } artist)
                results.Add(new(null, artist));
        foreach (var item in Items(playlists))
            if (MapCollection(item, CollectionKind.Playlist) is { } playlist)
                results.Add(new(null, playlist));
        return new SearchPage(results, offset < 1000 &&
            (HasNext(tracks) || HasNext(artists) || HasNext(playlists)));
    }

    private static IEnumerable<JsonElement> Items(JsonElement container)
    {
        var items = container.ValueKind == JsonValueKind.Array ? container : Property(container, "items");
        return items.ValueKind == JsonValueKind.Array ? items.EnumerateArray() : [];
    }

    private static bool HasNext(JsonElement container) =>
        !string.IsNullOrWhiteSpace(String(container, "next"));

    private static Collection? MapCollection(JsonElement item, CollectionKind kind)
    {
        if (item.ValueKind != JsonValueKind.Object) return null;
        var id = String(item, "id");
        var name = String(item, "name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) return null;
        return new Collection(id, name, kind,
            kind == CollectionKind.Playlist ? String(item, "description") ?? string.Empty : string.Empty);
    }

    private async Task ForEachPageAsync(string path, Action<JsonElement> visit, CancellationToken token,
        bool followNext = true)
    {
        Uri? page = new(ApiRoot, path);
        var visited = new HashSet<Uri>();
        while (page is not null)
        {
            if (!visited.Add(page)) throw new InvalidDataException("Spotify repeated a playlist page.");
            using var document = await GetAsync(page, token).ConfigureAwait(false);
            var root = document.RootElement;
            var items = Property(root, "items");
            if (items.ValueKind == JsonValueKind.Array)
                foreach (var item in items.EnumerateArray()) visit(item);
            if (!followNext) break;
            var next = String(root, "next");
            if (string.IsNullOrEmpty(next)) break;
            page = ValidateNext(next);
        }
    }

    private async Task<JsonDocument> GetAsync(Uri uri, CancellationToken token)
    {
        // Serialize catalog calls so an empty artist page cannot trigger a burst of
        // concurrent retries while another view is fetching Spotify metadata.
        await _requestGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                // ResponseHeadersRead ends HttpClient's timeout once headers arrive.
                // Keep a deadline for token lookup and the JSON/error body as well.
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(20));
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                    await _accessToken(timeout.Token).ConfigureAwait(false));
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                    .ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                    var retryAfter = response.Headers.RetryAfter;
                    var retryWait = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
                    if (IsQuotaExceeded(body)) throw new QuotaException(retryWait);
                    var wait = retryWait ?? TimeSpan.FromSeconds(2 << attempt);
                    if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
                    if (wait > TimeSpan.FromSeconds(60)) throw new RateLimitException(wait);
                    if (attempt == 2) throw new RateLimitException(wait);
                    await Task.Delay(wait, token).ConfigureAwait(false);
                    continue;
                }
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                return await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token).ConfigureAwait(false);
            }
            throw new RateLimitException();
        }
        finally { _requestGate.Release(); }
    }

    private static bool IsQuotaExceeded(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return String(Property(document.RootElement, "error"), "reason") == "QUOTA_EXCEEDED";
        }
        catch (JsonException) { return false; }
    }

    private static JsonElement Property(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var property) ? property : default;

    private static string? String(JsonElement value, string name)
    {
        var property = Property(value, name);
        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static Track? Map(JsonElement track)
    {
        if (track.ValueKind != JsonValueKind.Object || String(track, "type") is { } type && type != "track") return null;
        var uri = String(track, "uri");
        var name = String(track, "name");
        if (string.IsNullOrWhiteSpace(uri) || string.IsNullOrWhiteSpace(name)
            || !TrackId.TryParse(uri, out _)
            || Property(track, "is_local").ValueKind == JsonValueKind.True
            || Property(track, "is_playable").ValueKind == JsonValueKind.False) return null;
        var artists = Property(track, "artists");
        var artistNames = artists.ValueKind == JsonValueKind.Array
            ? string.Join(", ", artists.EnumerateArray().Select(artist => String(artist, "name") ?? string.Empty))
            : string.Empty;
        var duration = Property(track, "duration_ms");
        return new Track(name, artistNames, uri,
            duration.ValueKind == JsonValueKind.Number && duration.TryGetInt32(out var milliseconds) ? Math.Max(0, milliseconds) : 0);
    }

    internal static string DescribeError(Exception error) => error switch
    {
        AuthorizationException authorization => authorization.Describe(),
        QuotaException quota => quota.Describe(),
        RateLimitException rateLimit => rateLimit.Describe(),
        // Translators: Error shown when loading Spotify browsing results or preparing playback fails.
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => Tr("Spotify authorization expired. Sign in again."),
        // Translators: Error shown when loading Spotify browsing results or preparing playback fails.
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden } => Tr("Spotify does not allow this content for this developer app or account. Some playlists and recommendations are restricted."),
        // Translators: Error shown when loading Spotify browsing results or preparing playback fails.
        HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } => Tr("Spotify rate limit reached. Wait a moment and try again."),
        // Translators: Error shown when loading Spotify browsing results or preparing playback fails. {code} is the HTTP status code.
        HttpRequestException { StatusCode: { } status } => TrFormat("Spotify request failed ({code}).", (int)status),
        // Translators: Error shown when loading Spotify browsing results or preparing playback fails.
        HttpRequestException => Tr("Could not reach Spotify. Check your internet connection."),
        // Translators: Error shown when loading Spotify browsing results or preparing playback fails.
        OperationCanceledException or TimeoutException => Tr("Spotify did not respond in time."),
        // Translators: Error shown when loading Spotify browsing results or preparing playback fails.
        Login5StatusException => Tr("Spotify refused the playback sign in. Sign in again."),
        // Translators: Error shown when loading Spotify browsing results or preparing playback fails.
        InvalidDataException => Tr("Spotify could not load this track."),
        // Translators: Error when Spotify audio/network data or protected account files cannot be read or saved.
        IOException or UnauthorizedAccessException => Tr("Spotify could not read or save required data. Check your connection and available storage, then try again."),
        // Translators: Error when an operation outlives its disconnected Spotify session.
        ObjectDisposedException => Tr("Spotify was disconnected. Sign in again to continue."),
        // Translators: Error shown when loading Spotify browsing results or preparing playback fails. {reason} is the failure reason returned by the service.
        _ => TrFormat("Spotify request failed: {reason}", error.Message),
    };
}
