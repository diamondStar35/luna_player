using System.Net;
using System.Text;
using LunaPlayer.Subsonic;
namespace LunaPlayer.Tests.Subsonic;

internal sealed record Sent(Uri Uri, HttpMethod Method, string Body, Dictionary<string, string[]> Headers)
{
    internal string Endpoint => Uri.AbsolutePath.Split('/')[^1].Replace(".view", "");
    internal Dictionary<string, string[]> Parameters
    {
        get
        {
            var query = Method == HttpMethod.Post && Body.StartsWith("v=", StringComparison.Ordinal) ? Body : Uri.Query.TrimStart('?');
            return query.Split('&', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('=', 2))
                .GroupBy(x => Uri.UnescapeDataString(x[0])).ToDictionary(g => g.Key,
                    g => g.Select(x => Uri.UnescapeDataString(x.Length == 2 ? x[1].Replace("+", " ") : "")).ToArray());
        }
    }
}
internal sealed class Handler(Func<Sent, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    internal List<Sent> Requests { get; } = [];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var sent = new Sent(request.RequestUri!, request.Method,
            request.Content is null ? "" : await request.Content.ReadAsStringAsync(token),
            request.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
        lock (Requests) Requests.Add(sent);
        return await respond(sent, token);
    }
}
internal static class Fixture
{
    internal static Options Options => new()
    { Server = new Uri("https://music.example/prefix/"), Credentials = new() { Username = "user", Password = "secret" } };
    internal static HttpResponseMessage Json(string payload = "", HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent("{\"subsonic-response\":{\"status\":\"ok\",\"version\":\"1.16.1\""
            + (payload.Length == 0 ? "" : "," + payload) + "}}", Encoding.UTF8, "application/json") };
    internal static HttpResponseMessage Raw(string value, string type = "application/json", HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(value, Encoding.UTF8, type) };
    internal static Task<HttpResponseMessage> Reply(Sent sent, CancellationToken _)
        => Task.FromResult(sent.Endpoint == "getOpenSubsonicExtensions"
            ? Json("\"openSubsonicExtensions\":[{\"name\":\"transcodeOffset\",\"versions\":[1]},{\"name\":\"transcoding\",\"versions\":[1]},{\"name\":\"songLyrics\",\"versions\":[1,2]},{\"name\":\"playbackReport\",\"versions\":[1]},{\"name\":\"indexBasedQueue\",\"versions\":[1]},{\"name\":\"sonicSimilarity\",\"versions\":[1]},{\"name\":\"topSongsByArtistId\",\"versions\":[1]},{\"name\":\"getPodcastEpisode\",\"versions\":[1]}]")
            : Json(Payload(sent.Endpoint)));
    private static string Payload(string endpoint) => endpoint switch
    {
        "getLicense" => "\"license\":{\"valid\":true}",
        "getUser" => "\"user\":{\"username\":\"user\",\"downloadRole\":true}",
        "tokenInfo" => "\"tokenInfo\":{}",
        "getMusicFolders" => "\"musicFolders\":{\"musicFolder\":[{\"id\":0,\"name\":\"Music\"}]}",
        "getArtists" => "\"artists\":{\"index\":[{\"name\":\"A\",\"artist\":[{\"id\":\"artist\",\"name\":\"Artist\"}]}]}",
        "getIndexes" => "\"indexes\":{\"index\":[]}",
        "getMusicDirectory" => "\"directory\":{\"child\":[]}",
        "getArtist" => "\"artist\":{\"id\":\"artist\",\"album\":[{\"id\":\"album\"}]}",
        "getAlbum" => "\"album\":{\"id\":\"album\",\"song\":[{\"id\":\"s\",\"title\":\"Song\",\"duration\":42.5}]}",
        "getSong" => "\"song\":{\"id\":\"s\",\"title\":\"Song\"}",
        "getGenres" => "\"genres\":{\"genre\":[]}",
        "getAlbumList" => "\"albumList\":{\"album\":[]}",
        "getAlbumList2" => "\"albumList2\":{\"album\":[]}",
        "search2" => "\"searchResult2\":{}",
        "search3" => "\"searchResult3\":{}",
        "getStarred" => "\"starred\":{}",
        "getStarred2" => "\"starred2\":{}",
        "getRandomSongs" => "\"randomSongs\":{\"song\":[]}",
        "getSongsByGenre" => "\"songsByGenre\":{\"song\":[]}",
        "getArtistInfo" => "\"artistInfo\":{}",
        "getArtistInfo2" => "\"artistInfo2\":{}",
        "getAlbumInfo" => "\"albumInfo\":{}",
        "getAlbumInfo2" => "\"albumInfo2\":{}",
        "getTopSongs" => "\"topSongs\":{\"song\":[]}",
        "getSimilarSongs" => "\"similarSongs\":{\"song\":[]}",
        "getSimilarSongs2" => "\"similarSongs2\":{\"song\":[]}",
        "getSonicSimilarTracks" or "findSonicPath" => "\"sonicMatch\":[{\"entry\":{\"id\":\"s\"},\"similarity\":0.8}]",
        "getPlaylists" => "\"playlists\":{\"playlist\":[]}",
        "getPlaylist" or "createPlaylist" => "\"playlist\":{\"id\":\"p\",\"entry\":[{\"id\":\"s\"},{\"id\":\"s\"}]}",
        "getNowPlaying" => "\"nowPlaying\":{\"entry\":[]}",
        "getBookmarks" => "\"bookmarks\":{\"bookmark\":[]}",
        "getPlayQueue" => "\"playQueue\":{}",
        "getPlayQueueByIndex" => "\"playQueueByIndex\":{}",
        "getLyrics" => "\"lyrics\":{\"value\":\"Lyrics\"}",
        "getLyricsBySongId" => "\"lyricsList\":{\"structuredLyrics\":[{\"synced\":true,\"line\":[{\"start\":0,\"value\":\"Line\"}]}]}",
        "getInternetRadioStations" => "\"internetRadioStations\":{\"internetRadioStation\":[]}",
        "getPodcasts" => "\"podcasts\":{\"channel\":[]}",
        "getNewestPodcasts" => "\"newestPodcasts\":{\"episode\":[]}",
        "getPodcastEpisode" => "\"podcastEpisode\":{\"id\":\"episode\"}",
        "getScanStatus" or "startScan" => "\"scanStatus\":{\"scanning\":false,\"count\":100}",
        "getTranscodeDecision" => "\"transcodeDecision\":{\"canDirectPlay\":false,\"canTranscode\":true,\"transcodeParams\":\"opaque%2B%2F%3D%26%25token\",\"sourceStream\":{\"protocol\":\"http\",\"container\":\"flac\",\"codec\":\"flac\",\"audioSamplerate\":96000},\"transcodeStream\":{\"protocol\":\"http\",\"container\":\"ogg\",\"codec\":\"opus\",\"audioBitrate\":128000}}",
        _ => ""
    };
}
internal sealed class InlineProgress<T>(Action<T> action) : IProgress<T>
{ public void Report(T value) => action(value); }

internal sealed class TemporaryDirectory : IDisposable
{
    internal string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "luna-subsonic-" + Guid.NewGuid().ToString("N"));
    internal TemporaryDirectory() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path, true);
}
