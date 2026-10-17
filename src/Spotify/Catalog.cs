using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Spotify;

internal enum SpotifyCollectionKind { Playlist, Mix, Radio, LikedSongs, TopTracks, RecentlyPlayed, Artist }
internal sealed record SpotifyCollection(string Id, string Name, SpotifyCollectionKind Kind, string Description)
{
    internal string Label => $"{Name} - {Kind switch { SpotifyCollectionKind.LikedSongs => "Liked songs", SpotifyCollectionKind.TopTracks => "Your top tracks", SpotifyCollectionKind.RecentlyPlayed => "Recently played", SpotifyCollectionKind.Mix => "Mix", SpotifyCollectionKind.Radio => "Radio", SpotifyCollectionKind.Artist => "Artist", _ => "Playlist" }}";
}

internal sealed record SpotifyTrack(string Name, string Artists, string Uri, int DurationMs)
{
    internal string Label => $"{Name} - {Artists} - {TimeSpan.FromMilliseconds(DurationMs):m\\:ss}";
    // Different releases can give the same song different Spotify track URIs.
    internal string ArtistSongKey => Normalize(Name) + "\0" + Normalize(Artists);

    private static string Normalize(string value) =>
        string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
}

internal sealed record SpotifySearchItem(SpotifyTrack? Track, SpotifyCollection? Collection)
{
    internal string Label => Track is { } track ? $"{track.Label} - Track" : Collection?.Label ?? string.Empty;
}

internal sealed record SpotifySearchPage(IReadOnlyList<SpotifySearchItem> Items, bool HasMore);
internal enum SpotifySearchFilter { All, Songs, Artists, Playlists }
internal sealed record SpotifyTrackPage(IReadOnlyList<SpotifyTrack> Tracks, string? Next);
internal sealed class SpotifyQuotaException : Exception
{
    internal SpotifyQuotaException(TimeSpan? retryAfter)
        : base(retryAfter is { } wait
            ? $"Spotify's developer-app quota has been reached. Spotify says to try again in {FormatWait(wait)}."
            : "Spotify's developer-app quota has been reached. Spotify did not provide a reset time.") { }

    private static string FormatWait(TimeSpan wait)
    {
        // Round up so the displayed wait never tells users to retry before Retry-After expires.
        var totalMinutes = Math.Max(1L, (long)Math.Ceiling(wait.TotalMinutes));
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        var hourText = $"{hours} {(hours == 1 ? "hour" : "hours")}";
        var minuteText = $"{minutes} {(minutes == 1 ? "minute" : "minutes")}";
        return hours == 0 ? minuteText : minutes == 0 ? hourText : $"{hourText} and {minuteText}";
    }
}
internal sealed class SpotifyRateLimitException : Exception
{
    internal SpotifyRateLimitException(TimeSpan? retryAfter = null)
        : base(retryAfter is { } wait
            ? $"Spotify asked Luna to wait about {Math.Ceiling(wait.TotalSeconds)} seconds before trying again."
            : "Spotify is still limiting requests. Wait a moment and try again.") { }
}

internal sealed class SpotifyCatalog
{
    private static readonly Uri ApiRoot = new("https://api.spotify.com/v1/");
    private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly Func<CancellationToken, Task<string>> _accessToken;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private readonly Dictionary<(string ArtistId, int Offset), (string[] AlbumIds, bool HasMore)> _artistAlbumBatches = [];
    private readonly object _albumBatchLock = new();

    internal SpotifyCatalog(SpotifyAuth auth, SpotifySettings settings)
        : this(token => AccessTokenAsync(auth, settings, token), SharedHttp) { }

    // The injectable transport lets the small client be tested without a Spotify account.
    internal SpotifyCatalog(Func<CancellationToken, Task<string>> accessToken, HttpClient http)
    {
        _accessToken = accessToken;
        _http = http;
    }

    private static Task<string> AccessTokenAsync(SpotifyAuth auth, SpotifySettings settings, CancellationToken token)
    {
        if (settings.ClientId.Length == 0)
            throw new InvalidOperationException(Tr("Spotify credentials have not been configured yet."));
        return auth.GetAccessTokenAsync(settings.ClientId, token);
    }

    internal async Task<IReadOnlyList<SpotifyCollection>> LibraryAsync(CancellationToken token = default)
    {
        var result = new List<SpotifyCollection>
        {
            new("liked", "Liked Songs", SpotifyCollectionKind.LikedSongs, "Tracks saved to your Spotify library."),
        };
        await ForEachPageAsync("me/playlists?limit=50", item =>
        {
            var id = String(item, "id");
            if (string.IsNullOrWhiteSpace(id)) return;
            var name = String(item, "name") ?? "Untitled playlist";
            var kind = name.Contains("mix", StringComparison.OrdinalIgnoreCase) ? SpotifyCollectionKind.Mix
                : name.Contains("radio", StringComparison.OrdinalIgnoreCase) ? SpotifyCollectionKind.Radio
                : SpotifyCollectionKind.Playlist;
            result.Add(new SpotifyCollection(id, name, kind, String(item, "description") ?? string.Empty));
        }, token).ConfigureAwait(false);
        return result;
    }

    internal async Task<SpotifyTrackPage> TracksPageAsync(SpotifyCollection collection, string? cursor,
        CancellationToken token = default)
    {
        if (collection.Kind == SpotifyCollectionKind.Artist)
            return await ArtistPageAsync(collection, cursor, token).ConfigureAwait(false);

        var initial = collection.Kind switch
        {
            SpotifyCollectionKind.LikedSongs => "me/tracks?limit=50",
            SpotifyCollectionKind.TopTracks => "me/top/tracks?limit=50",
            SpotifyCollectionKind.RecentlyPlayed => "me/player/recently-played?limit=50",
            _ => $"playlists/{Uri.EscapeDataString(collection.Id)}/items?limit=50",
        };
        var uri = cursor is null ? new Uri(ApiRoot, initial) : ValidateNext(cursor);
        using (var document = await GetAsync(uri, token).ConfigureAwait(false))
        {
            var result = new List<SpotifyTrack>();
            foreach (var item in Items(document.RootElement))
            {
                var track = collection.Kind switch
                {
                    SpotifyCollectionKind.LikedSongs or SpotifyCollectionKind.RecentlyPlayed => Property(item, "track"),
                    SpotifyCollectionKind.TopTracks => item,
                    _ => Property(item, "item") is { ValueKind: JsonValueKind.Object } value
                        ? value : Property(item, "track"),
                };
                if (Map(track) is { } mapped) result.Add(mapped);
            }
            var next = collection.Kind is SpotifyCollectionKind.TopTracks or SpotifyCollectionKind.RecentlyPlayed
                ? null : String(document.RootElement, "next");
            if (next is not null) ValidateNext(next);
            return new SpotifyTrackPage(result, next);
        }
    }

    private async Task<SpotifyTrackPage> ArtistPageAsync(SpotifyCollection artist, string? cursor,
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
            var query = $"artist:\"{artist.Name.Replace("\"", string.Empty, StringComparison.Ordinal)}\"";
            var path = $"search?q={Uri.EscapeDataString(query)}&type=track&limit=10&offset={searchOffset}";
            using var document = await GetAsync(new Uri(ApiRoot, path), token).ConfigureAwait(false);
            var results = Property(document.RootElement, "tracks");
            return new SpotifyTrackPage(MapArtistTracks(results, artist.Id),
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
                return new SpotifyTrackPage([], null);
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

    private async Task<SpotifyTrackPage> ArtistAlbumPageAsync(string artistId, Uri uri,
        int albumOffset, bool moreAlbums, CancellationToken token)
    {
        using var document = await GetAsync(uri, token).ConfigureAwait(false);
        var tracks = MapArtistTracks(document.RootElement, artistId);
        var nextTrackPage = String(document.RootElement, "next");
        if (nextTrackPage is not null)
        {
            ValidateNext(nextTrackPage);
            return new SpotifyTrackPage(tracks,
                $"tracks:{albumOffset}:{(moreAlbums ? 1 : 0)}:{Uri.EscapeDataString(nextTrackPage)}");
        }
        return new SpotifyTrackPage(tracks, moreAlbums ? $"albums:{albumOffset + 1}" : null);
    }

    private static IReadOnlyList<SpotifyTrack> MapArtistTracks(JsonElement page, string artistId) =>
        Items(page).Where(item => Items(Property(item, "artists"))
                .Any(value => String(value, "id") == artistId))
            .Select(Map).OfType<SpotifyTrack>().ToArray();

    private static Uri ValidateNext(string next)
    {
        if (!Uri.TryCreate(next, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || uri.Host != ApiRoot.Host || !uri.AbsolutePath.StartsWith("/v1/", StringComparison.Ordinal))
            throw new InvalidDataException("Spotify returned an invalid next-page link.");
        return uri;
    }

    internal async Task<SpotifySearchPage> SearchAsync(string query, int offset = 0,
        SpotifySearchFilter filter = SpotifySearchFilter.All, CancellationToken token = default)
    {
        if (offset is < 0 or > 1000) return new SpotifySearchPage([], false);
        var type = filter switch
        {
            SpotifySearchFilter.Songs => "track",
            SpotifySearchFilter.Artists => "artist",
            SpotifySearchFilter.Playlists => "playlist",
            _ => "track,artist,playlist",
        };
        var path = $"search?q={Uri.EscapeDataString(query)}&type={type}&limit=10&offset={offset}";
        using var document = await GetAsync(new Uri(ApiRoot, path), token).ConfigureAwait(false);
        var results = new List<SpotifySearchItem>();
        var tracks = Property(document.RootElement, "tracks");
        var artists = Property(document.RootElement, "artists");
        var playlists = Property(document.RootElement, "playlists");
        foreach (var item in Items(tracks))
            if (Map(item) is { } track) results.Add(new(track, null));
        foreach (var item in Items(artists))
            if (MapCollection(item, SpotifyCollectionKind.Artist) is { } artist)
                results.Add(new(null, artist));
        foreach (var item in Items(playlists))
            if (MapCollection(item, SpotifyCollectionKind.Playlist) is { } playlist)
                results.Add(new(null, playlist));
        return new SpotifySearchPage(results, offset < 1000 &&
            (HasNext(tracks) || HasNext(artists) || HasNext(playlists)));
    }

    private static IEnumerable<JsonElement> Items(JsonElement container)
    {
        var items = container.ValueKind == JsonValueKind.Array ? container : Property(container, "items");
        return items.ValueKind == JsonValueKind.Array ? items.EnumerateArray() : [];
    }

    private static bool HasNext(JsonElement container) =>
        !string.IsNullOrWhiteSpace(String(container, "next"));

    private static SpotifyCollection? MapCollection(JsonElement item, SpotifyCollectionKind kind)
    {
        if (item.ValueKind != JsonValueKind.Object) return null;
        var id = String(item, "id");
        var name = String(item, "name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) return null;
        return new SpotifyCollection(id, name, kind,
            kind == SpotifyCollectionKind.Playlist ? String(item, "description") ?? string.Empty : string.Empty);
    }

    private async Task ForEachPageAsync(string path, Action<JsonElement> visit, CancellationToken token,
        bool followNext = true)
    {
        Uri? page = new(ApiRoot, path);
        while (page is not null)
        {
            using var document = await GetAsync(page, token).ConfigureAwait(false);
            var root = document.RootElement;
            var items = Property(root, "items");
            if (items.ValueKind == JsonValueKind.Array)
                foreach (var item in items.EnumerateArray()) visit(item);
            if (!followNext) break;
            var next = String(root, "next");
            if (string.IsNullOrEmpty(next)) break;
            if (!Uri.TryCreate(next, UriKind.Absolute, out page) || page.Scheme != Uri.UriSchemeHttps
                || page.Host != ApiRoot.Host || !page.AbsolutePath.StartsWith("/v1/", StringComparison.Ordinal))
                throw new InvalidDataException("Spotify returned an invalid next-page link.");
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
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                    await _accessToken(token).ConfigureAwait(false));
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token)
                    .ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    var retryAfter = response.Headers.RetryAfter;
                    var retryWait = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
                    if (IsQuotaExceeded(body)) throw new SpotifyQuotaException(retryWait);
                    var wait = retryWait ?? TimeSpan.FromSeconds(2 << attempt);
                    if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
                    if (wait > TimeSpan.FromSeconds(60)) throw new SpotifyRateLimitException(wait);
                    if (attempt == 2) throw new SpotifyRateLimitException(wait);
                    await Task.Delay(wait, token).ConfigureAwait(false);
                    continue;
                }
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                return await JsonDocument.ParseAsync(stream, cancellationToken: token).ConfigureAwait(false);
            }
            throw new SpotifyRateLimitException();
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

    private static SpotifyTrack? Map(JsonElement track)
    {
        if (track.ValueKind != JsonValueKind.Object || String(track, "type") is { } type && type != "track") return null;
        var uri = String(track, "uri");
        var name = String(track, "name");
        if (string.IsNullOrWhiteSpace(uri) || string.IsNullOrWhiteSpace(name)
            || !uri.StartsWith("spotify:track:", StringComparison.Ordinal)
            || uri.Length != 36 || !uri[14..].All(char.IsLetterOrDigit)
            || Property(track, "is_local").ValueKind == JsonValueKind.True
            || Property(track, "is_playable").ValueKind == JsonValueKind.False) return null;
        var artists = Property(track, "artists");
        var artistNames = artists.ValueKind == JsonValueKind.Array
            ? string.Join(", ", artists.EnumerateArray().Select(artist => String(artist, "name") ?? string.Empty))
            : string.Empty;
        var duration = Property(track, "duration_ms");
        return new SpotifyTrack(name, artistNames, uri,
            duration.ValueKind == JsonValueKind.Number && duration.TryGetInt32(out var milliseconds) ? milliseconds : 0);
    }

    internal static string DescribeError(Exception error) => error switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => Tr("Spotify authorization expired. Sign in again."),
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden } => Tr("Spotify does not allow this content for this developer app or account. Some playlists and recommendations are restricted."),
        HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } => Tr("Spotify rate limit reached. Wait a moment and try again."),
        HttpRequestException { StatusCode: { } status } => TrFormat("Spotify request failed ({code}).", (int)status),
        HttpRequestException => Tr("Could not reach Spotify. Check your internet connection."),
        TaskCanceledException => Tr("Spotify did not respond in time."),
        Login5StatusException => Tr("Spotify refused the playback sign in. Sign in again."),
        InvalidDataException => Tr("Spotify could not load this track."),
        _ => TrFormat("Spotify request failed: {reason}", error.Message),
    };
}
