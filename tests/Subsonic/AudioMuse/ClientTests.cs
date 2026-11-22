using System.Net;
using System.Text.Json;
using LunaPlayer.Subsonic;
using Ai = LunaPlayer.Subsonic.AudioMuse;

namespace LunaPlayer.Tests.Subsonic.AudioMuse;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class ClientTests
{
    private static Ai.Options Options => new()
    { Server = new Uri("https://ai.example/prefix/"), Token = "separate-token", MusicServer = "Home", ResultCount = 7 };

    [TestMethod]
    public async Task SearchUsesIndependentBearerAndDecodesLegacyNumericTrackIds()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw(
            "{\"results\":[{\"item_id\":12345678901234567890,\"title\":\"Song\",\"author\":\"Artist\"}]}")));
        using var http = new HttpClient(handler);
        using (var client = new Ai.Client(Options, http))
        {
            var result = await client.SearchAsync("موسيقى هادئة");
            Assert.AreEqual("12345678901234567890", result.Tracks[0].Id);
            Assert.AreEqual("Artist", result.Tracks[0].Credit);
        }
        var sent = handler.Requests.Single();
        Assert.AreEqual("/prefix/api/clap/search", sent.Uri.AbsolutePath);
        Assert.AreEqual("", sent.Uri.Query);
        Assert.AreEqual("Bearer separate-token", sent.Headers["Authorization"][0]);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.AreEqual("موسيقى هادئة", body.RootElement.GetProperty("query").GetString());
        Assert.AreEqual(7, body.RootElement.GetProperty("limit").GetInt32());
        Assert.AreEqual("Home", body.RootElement.GetProperty("server").GetString());
        using var alive = await http.GetAsync("https://still-alive.example/");
        Assert.IsTrue(alive.IsSuccessStatusCode);
    }

    [TestMethod]
    public async Task GeneratedPlaylistDecodesNestedResultsAndAlchemyKeepsSeedOperations()
    {
        using var handler = new Handler((sent, _) => Task.FromResult(Fixture.Raw(sent.Endpoint == "chatPlaylist"
            ? "{\"response\":{\"query_results\":[{\"item_id\":\"s\",\"title\":\"Song\"}],\"response\":\"Here\",\"playlist_id\":\"p\"}}"
            : "{\"results\":[{\"item_id\":\"mix\"}]}")));
        using var http = new HttpClient(handler);
        using var client = new Ai.Client(Options, http);
        var result = await client.GeneratePlaylistAsync("Evening music");
        Assert.AreEqual("s", result.Tracks[0].Id);
        Assert.AreEqual("Here", result.Message);
        Assert.AreEqual("p", result.PlaylistId);
        await client.AlchemyAsync([new("s"), new("artist", Ai.SeedKind.Artist, Ai.Operation.Subtract)]);
        using var generation = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.AreEqual("Evening music", generation.RootElement.GetProperty("userInput").GetString());
        Assert.AreEqual(7, generation.RootElement.GetProperty("n").GetInt32());
        using var alchemy = JsonDocument.Parse(handler.Requests[1].Body);
        var seeds = alchemy.RootElement.GetProperty("items");
        Assert.AreEqual("ADD", seeds[0].GetProperty("op").GetString());
        Assert.AreEqual("artist", seeds[1].GetProperty("type").GetString());
        Assert.AreEqual("SUBTRACT", seeds[1].GetProperty("op").GetString());
    }

    [TestMethod]
    public async Task ServerSentProgressSupportsCommentsMultilineDataAndFinalResult()
    {
        const string sse = ": keepalive\n\ndata: {\"type\":\"log\",\n"
            + "data: \"message\":\"Searching\"}\n\ndata: {\"type\":\"done\",\"response\":{\"query_results\":[{\"item_id\":\"s\"}],\"response\":\"Ready\"}}\n\n";
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw(sse, "text/event-stream")));
        using var http = new HttpClient(handler);
        using var client = new Ai.Client(Options, http);
        var events = new List<Ai.ProgressEvent>();
        await foreach (var progress in client.GeneratePlaylistProgressAsync("Evening")) events.Add(progress);
        Assert.AreEqual(2, events.Count);
        Assert.AreEqual("Searching", events[0].Message);
        Assert.AreEqual("s", events[1].Result!.Tracks[0].Id);
        Assert.AreEqual("Ready", events[1].Result!.Message);
        Assert.AreEqual("/prefix/chat/api/chatPlaylistStream", handler.Requests.Single().Uri.AbsolutePath);
    }

    [TestMethod]
    [DataRow("data: {\"type\":\"log\",\"message\":\"Waiting\"}\n\n", "InvalidResponse")]
    [DataRow("data: {\"type\":\"error\",\"error\":\"Failed\"}\n\n", "Rejected")]
    [DataRow("data: broken\n\n", "InvalidResponse")]
    public async Task ProgressRequiresCompletionAndSurfacesServiceErrors(string sse, string kind)
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw(sse, "text/event-stream")));
        using var http = new HttpClient(handler);
        using var client = new Ai.Client(Options, http);
        var error = await Assert.ThrowsExactlyAsync<RequestException>(async () =>
        { await foreach (var _ in client.GeneratePlaylistProgressAsync("Evening")) { } });
        Assert.AreEqual(kind, error.Kind.ToString());
        Assert.AreEqual(1, handler.Requests.Count);
    }

    [TestMethod]
    public async Task MalformedSuccessIsNotAnEmptyResultAndGenerationIsNotRetried()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw("{}")));
        using var http = new HttpClient(handler);
        using var client = new Ai.Client(Options, http);
        var error = await Assert.ThrowsExactlyAsync<RequestException>(() => client.SearchAsync("Evening"));
        Assert.AreEqual(FailureKind.InvalidResponse, error.Kind);
        Assert.AreEqual(1, handler.Requests.Count);
        using var failures = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        using var failureHttp = new HttpClient(failures);
        using var failureClient = new Ai.Client(Options, failureHttp);
        await Assert.ThrowsExactlyAsync<RequestException>(() => failureClient.GeneratePlaylistAsync("Evening"));
        Assert.AreEqual(1, failures.Requests.Count);
    }

    [TestMethod]
    public async Task SimilarityFallsBackForMissingSupportButNotInvalidCredentials()
    {
        using var aiHandler = new Handler((_, _) => Task.FromResult(Fixture.Raw("{\"results\":[{\"item_id\":\"ai\"}]}")));
        using var aiHttp = new HttpClient(aiHandler);
        using var ai = new Ai.Client(Options, aiHttp);
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented)));
        using var http = new HttpClient(handler);
        using var server = new Client(Fixture.Options, http);
        Assert.AreEqual("ai", (await new Ai.Mixes(server, ai).SimilarAsync("s")).Tracks[0].Id);
        using var denied = new Handler((_, _) => Task.FromResult(Fixture.Raw(
            "{\"subsonic-response\":{\"status\":\"failed\",\"error\":{\"code\":40}}}")));
        using var deniedHttp = new HttpClient(denied);
        using var deniedServer = new Client(Fixture.Options, deniedHttp);
        await Assert.ThrowsExactlyAsync<RequestException>(() => new Ai.Mixes(deniedServer, ai).SimilarAsync("s"));
        Assert.AreEqual(1, aiHandler.Requests.Count);
    }

    [TestMethod]
    public async Task SearchAndProgressDistinguishDeadlinesFromCallerCancellation()
    {
        using var handler = new Handler(async (_, token) =>
        { await Task.Delay(Timeout.InfiniteTimeSpan, token); return Fixture.Raw("{}"); });
        using var http = new HttpClient(handler);
        using var client = new Ai.Client(Options with
        { SearchTimeout = TimeSpan.FromMilliseconds(100), GenerationTimeout = TimeSpan.FromMilliseconds(100) }, http);
        var search = await Assert.ThrowsExactlyAsync<RequestException>(() => client.SearchAsync("Evening"));
        Assert.AreEqual(FailureKind.Timeout, search.Kind);
        var progress = await Assert.ThrowsExactlyAsync<RequestException>(async () =>
        { await foreach (var _ in client.GeneratePlaylistProgressAsync("Evening")) { } });
        Assert.AreEqual(FailureKind.Timeout, progress.Kind);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.SearchAsync("Evening", cancellation.Token));
    }
}
