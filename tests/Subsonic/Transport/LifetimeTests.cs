using System.Net;
using System.Text.Json;
using LunaPlayer.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Transport;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class LifetimeTests
{
    [TestMethod]
    public async Task RequestTimeoutIsDistinctFromCallerCancellation()
    {
        using var handler = new Handler(async (_, token) =>
        { await Task.Delay(Timeout.InfiniteTimeSpan, token); return Fixture.Json(); });
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options with { RequestTimeout = TimeSpan.FromMilliseconds(25) }, http);
        var error = await Assert.ThrowsExactlyAsync<RequestException>(() => client.PingAsync());
        Assert.AreEqual(FailureKind.Timeout, error.Kind);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.PingAsync(cancellation.Token));
    }

    [TestMethod]
    public async Task DisposingClientCancelsPendingRequestsAndRejectsNewOnes()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new Handler(async (_, token) =>
        { started.SetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, token); return Fixture.Json(); });
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var pending = client.PingAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        client.Dispose();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pending.WaitAsync(TimeSpan.FromSeconds(5)));
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => client.PingAsync());
    }

    [TestMethod]
    public async Task DiscoveryCoalescesConcurrentRequestsAndRefreshIsExplicit()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.DiscoverAsync()));
        Assert.IsTrue(results.All(x => ReferenceEquals(results[0], x)));
        Assert.AreEqual(2, handler.Requests.Count);
        var discovery = handler.Requests.Single(x => x.Endpoint == "getOpenSubsonicExtensions");
        Assert.IsFalse(discovery.Parameters.ContainsKey("u"));
        Assert.IsFalse(discovery.Parameters.ContainsKey("t"));
        await client.DiscoverAsync(true);
        Assert.AreEqual(4, handler.Requests.Count);
    }

    [TestMethod]
    public async Task ExtendedMetadataDoesNotCollideWithLegacyArtistAndNullCollectionsAreEmpty()
    {
        const string payload = "\"song\":{\"id\":\"s\",\"artist\":\"Legacy\",\"artists\":[{\"id\":12,\"name\":\"Modern\"}],"
            + "\"displayArtist\":\"Modern feat. Guest\",\"albumArtists\":[{\"id\":\"albumArtist\",\"name\":\"Album artist\"}],"
            + "\"genres\":null,\"coverArt\":null,\"contributors\":[{\"role\":\"composer\",\"artist\":{\"id\":\"composer\",\"name\":\"Composer\"}}],"
            + "\"works\":[{\"name\":\"Symphony\"}],\"movements\":[{\"number\":2}],\"replayGain\":{\"trackGain\":-3.5}}";
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Json(payload)));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var song = await client.Library.GetSongAsync("s");
        Assert.AreEqual("Legacy", song.Artist);
        Assert.AreEqual("12", song.Artists[0].Id);
        Assert.AreEqual("Modern feat. Guest", song.DisplayArtist);
        Assert.AreEqual("albumArtist", song.AlbumArtists[0].Id);
        Assert.AreEqual(0, song.Genres.Length);
        Assert.AreEqual("", song.CoverArt);
        Assert.AreEqual("composer", song.Contributors[0].Artist!.Id);
        Assert.AreEqual("Symphony", song.Works[0].Name);
        Assert.AreEqual(2, song.Movements[0].Number);
        Assert.AreEqual(-3.5, song.ReplayGain!.TrackGain);
    }

    [TestMethod]
    public async Task MediaRedirectDropsServerHeadersAndDoesNotRegenerateAuthentication()
    {
        using var handler = new Handler((sent, _) =>
        {
            if (sent.Uri.Host != "music.example") return Task.FromResult(Fixture.Raw("image", "image/png"));
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("https://images.example/avatar");
            return Task.FromResult(redirect);
        });
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options with { Headers = new Dictionary<string, string> { ["X-Proxy-Token"] = "private" } }, http);
        await using var image = await client.Media.OpenAvatarAsync("user");
        Assert.AreEqual(2, handler.Requests.Count);
        Assert.AreEqual("private", handler.Requests[0].Headers["X-Proxy-Token"][0]);
        Assert.IsFalse(handler.Requests[1].Headers.ContainsKey("X-Proxy-Token"));
        Assert.AreEqual("", handler.Requests[1].Uri.Query);
    }

    [TestMethod]
    public async Task RedirectLoopsAndHttpsDowngradesAreRejected()
    {
        using var handler = new Handler((_, _) =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("https://music.example/loop");
            return Task.FromResult(redirect);
        });
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var error = await Assert.ThrowsExactlyAsync<RequestException>(() => client.Media.OpenAvatarAsync("user"));
        Assert.AreEqual(FailureKind.InvalidResponse, error.Kind);
        Assert.AreEqual(6, handler.Requests.Count);
        using var insecure = new Handler((_, _) =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("http://other.example/image");
            return Task.FromResult(redirect);
        });
        using var insecureHttp = new HttpClient(insecure);
        using var insecureClient = new Client(Fixture.Options, insecureHttp);
        await Assert.ThrowsExactlyAsync<RequestException>(() => insecureClient.Media.OpenAvatarAsync("user"));
        Assert.AreEqual(1, insecure.Requests.Count);
    }

    [TestMethod]
    public async Task ValidationRejectsInvalidArgumentsBeforeSendingRequests()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await Assert.ThrowsAsync<ArgumentException>(() => client.Library.GetSongAsync(" "));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Radio.CreateAsync("file:///private", "Station"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Library.SearchAsync("q", new() { SongOffset = -1 }));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Library.GetAlbumsAsync(new() { Order = LunaPlayer.Subsonic.Browsing.AlbumOrder.ByGenre }));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Listening.SetRatingAsync("s", 6));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Listening.SaveQueueAsync(["a"], currentIndex: 1, byIndex: true));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Media.GetStreamAddressAsync("s", new() { TimeOffset = double.NaN }));
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public async Task UserLibrariesAcceptBothLegacyNumericAndOpaqueIds()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Json(
            "\"user\":{\"username\":\"user\",\"folder\":[12,\"opaque & id\"],\"downloadRole\":false}")));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var user = await client.GetUserAsync("user");
        CollectionAssert.AreEqual(new[] { "12", "opaque & id" }, user.Folders);
        Assert.IsFalse(user.DownloadRole);
    }

    [TestMethod]
    public async Task CachedDiscoveryAndStreamResolutionRejectDisposedConnections()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await client.DiscoverAsync();
        client.Dispose();
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => client.DiscoverAsync());
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => client.Media.GetStreamAddressAsync("s"));
        Assert.AreEqual(2, handler.Requests.Count);
    }
}
