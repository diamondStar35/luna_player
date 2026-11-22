using System.Net;
using System.Security.Cryptography;
using System.Text;
using LunaPlayer.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Transport;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class RequestsTests
{
    [TestMethod]
    public async Task TokenAuthIsFreshUtf8AndBorrowedClientRemainsUnchanged()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://untouched.example/") };
        http.DefaultRequestHeaders.Add("X-Borrowed", "keep");
        var timeout = http.Timeout;
        using (var client = new Client(Fixture.Options with { Credentials = new() { Username = "مستخدم &+", Password = "密碼" } }, http))
        {
            Assert.AreEqual(0, handler.Requests.Count);
            await client.PingAsync(); await client.PingAsync();
            Assert.AreNotEqual(handler.Requests[0].Parameters["s"][0], handler.Requests[1].Parameters["s"][0]);
            foreach (var sent in handler.Requests)
            {
                Assert.AreEqual("/prefix/rest/ping.view", sent.Uri.AbsolutePath);
                var query = sent.Parameters;
                Assert.AreEqual("مستخدم &+", query["u"][0]);
                Assert.AreEqual(Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes("密碼" + query["s"][0]))), query["t"][0]);
                Assert.IsFalse(sent.Uri.AbsoluteUri.Contains("密碼", StringComparison.Ordinal));
            }
        }
        Assert.AreEqual("https://untouched.example/", http.BaseAddress.AbsoluteUri);
        Assert.AreEqual(timeout, http.Timeout);
        Assert.AreEqual("keep", http.DefaultRequestHeaders.GetValues("X-Borrowed").Single());
        using var reply = await http.GetAsync("still-usable");
    }
    [TestMethod]
    public async Task ApiKeyAndLegacyAuthenticationUseOnlyTheirOwnMechanism()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using (var client = new Client(Fixture.Options with { Credentials = new() { Kind = AuthenticationKind.ApiKey, ApiKey = "key &+=" } }, http))
            await client.PingAsync();
        var query = handler.Requests[0].Parameters;
        Assert.AreEqual("key &+=", query["apiKey"][0]);
        foreach (var key in new[] { "u", "t", "s", "p" }) Assert.IsFalse(query.ContainsKey(key));
        using (var client = new Client(Fixture.Options with { Credentials = new() { Kind = AuthenticationKind.LegacyPassword, Username = "user", Password = "é" } }, http))
            await client.PingAsync();
        Assert.AreEqual("enc:c3a9", handler.Requests[1].Parameters["p"][0]);
    }
    [TestMethod]
    public async Task FormPostPreservesRepeatedAndEmptyParameters()
    {
        using var handler = new Handler(Fixture.Reply); using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options with { UseFormPost = true }, http);
        await client.Playlists.UpdateAsync("p", comment: "", isPublic: false, add: ["a", "a"], removeIndexes: [0, 3]);
        var sent = handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, sent.Method);
        Assert.AreEqual("", sent.Uri.Query);
        CollectionAssert.AreEqual(new[] { "a", "a" }, sent.Parameters["songIdToAdd"]);
        CollectionAssert.AreEqual(new[] { "0", "3" }, sent.Parameters["songIndexToRemove"]);
        Assert.AreEqual("", sent.Parameters["comment"][0]);
        Assert.AreEqual("false", sent.Parameters["public"][0]);
    }
    [TestMethod]
    public async Task SafeReadsRetryButMutationsNeverRepeat()
    {
        var calls = 0;
        using var handler = new Handler((_, _) =>
        {
            calls++;
            var response = Fixture.Json(status: calls < 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
            response.Headers.RetryAfter = new(TimeSpan.Zero);
            return Task.FromResult(response);
        });
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        await client.PingAsync();
        Assert.AreEqual(3, calls);
        calls = 0;
        var error = await Assert.ThrowsAsync<RequestException>(() => client.Playlists.DeleteAsync("p"));
        Assert.AreEqual(1, calls);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, error.StatusCode);
    }
    [TestMethod]
    public async Task ApiErrorsInsideHttpSuccessRemainStructured()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw(
            "{\"subsonic-response\":{\"status\":\"failed\",\"error\":{\"code\":40,\"message\":\"Wrong password\"}}}")));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var error = await Assert.ThrowsAsync<RequestException>(() => client.PingAsync());
        Assert.AreEqual(FailureKind.Rejected, error.Kind); Assert.AreEqual(40, error.Code);
        Assert.AreEqual("Wrong password", error.ServerMessage);
        Assert.IsFalse(error.ToString().Contains("secret", StringComparison.Ordinal));
        Assert.AreEqual(1, handler.Requests.Count);
    }
    [TestMethod]
    public async Task MissingPayloadAndInvalidJsonAreRejected()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Json()));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        Assert.AreEqual(FailureKind.InvalidResponse, (await Assert.ThrowsAsync<RequestException>(() => client.Library.GetSongAsync("x"))).Kind);
        using var broken = new Handler((_, _) => Task.FromResult(Fixture.Raw("<html>Sign in</html>")));
        using var brokenHttp = new HttpClient(broken); using var brokenClient = new Client(Fixture.Options, brokenHttp);
        await Assert.ThrowsAsync<RequestException>(() => brokenClient.PingAsync());
    }
    [TestMethod]
    public async Task LegacySingletonAndLargeNumericIdsDeserializeWithoutReflection()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Json(
            "\"artists\":{\"index\":{\"name\":\"A\",\"artist\":{\"id\":18446744073709551616,\"name\":\"One\"}}}")));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var artists = await client.Library.GetArtistsAsync();
        Assert.AreEqual("18446744073709551616", artists.Indexes.Single().Artists.Single().Id);
    }
    [TestMethod]
    public async Task CancelBeforeRequestDoesNotContactServer()
    {
        using var handler = new Handler(Fixture.Reply); using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.PingAsync(cancellation.Token));
        Assert.AreEqual(0, handler.Requests.Count);
    }
    [TestMethod]
    public async Task CapabilitiesAreCachedAndAbsenceBlocksOnlyAdvertisedExtensions()
    {
        using var handler = new Handler((sent, _) => Task.FromResult(sent.Endpoint == "getOpenSubsonicExtensions"
            ? Fixture.Json("\"openSubsonicExtensions\":[]") : Fixture.Json()));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var capabilities = await client.DiscoverAsync(); await client.DiscoverAsync();
        Assert.AreEqual(2, handler.Requests.Count);
        Assert.AreEqual(Support.Unsupported, capabilities.Extension("transcoding"));
        var failure = await Assert.ThrowsAsync<RequestException>(() => client.Media.OpenStreamAsync("s", new StreamOptions { TimeOffset = 10 }));
        Assert.AreEqual(FailureKind.Unsupported, failure.Kind);
        Assert.AreEqual(2, handler.Requests.Count);
    }
    [TestMethod]
    public async Task OlderServersWithoutDiscoveryHaveUnknownCapabilities()
    {
        using var handler = new Handler((sent, _) => Task.FromResult(sent.Endpoint == "getOpenSubsonicExtensions"
            ? new HttpResponseMessage(HttpStatusCode.NotFound) : Fixture.Json()));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        Assert.AreEqual(Support.Unknown, (await client.DiscoverAsync()).Extension("songLyrics"));
    }
}
