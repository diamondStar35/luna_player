using System.Net;
using LunaPlayer.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Scanning;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class ServerOperationsTests
{
    [TestMethod]
    public async Task ScanMonitoringObservesCompletionWithoutRestartingScan()
    {
        var polls = 0;
        using var handler = new Handler((sent, token) => sent.Endpoint == "getScanStatus"
            ? Task.FromResult(Fixture.Json($"\"scanStatus\":{{\"scanning\":{(++polls < 3 ? "true" : "false")},\"count\":{polls}}}"))
            : Fixture.Reply(sent, token));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await client.Scanning.StartAsync(true);
        var counts = new List<long?>();
        await foreach (var status in client.Scanning.WatchAsync(TimeSpan.FromMilliseconds(1))) counts.Add(status.Count);
        CollectionAssert.AreEqual(new long?[] { 1, 2, 3 }, counts);
        Assert.AreEqual(1, handler.Requests.Count(x => x.Endpoint == "startScan"));
        Assert.AreEqual("true", handler.Requests[0].Parameters["fullScan"][0]);
    }

    [TestMethod]
    public async Task CancellingMonitoringDoesNotSendAServerMutation()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Json("\"scanStatus\":{\"scanning\":true}")));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in client.Scanning.WatchAsync(token: cancellation.Token)) cancellation.Cancel();
        });
        Assert.AreEqual(1, handler.Requests.Count);
        Assert.AreEqual("getScanStatus", handler.Requests[0].Endpoint);
    }

    [TestMethod]
    public async Task RadioAndPodcastCommandsUseServerOperations()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await client.Radio.GetAllAsync();
        await client.Radio.CreateAsync("https://radio.example/live", "Station", "https://radio.example/");
        await client.Radio.UpdateAsync("r", "https://radio.example/new", "New");
        await client.Radio.DeleteAsync("r");
        await client.Podcasts.GetChannelsAsync(includeEpisodes: false);
        await client.Podcasts.GetNewestEpisodesAsync(30);
        Assert.AreEqual("episode", (await client.Podcasts.GetEpisodeAsync("episode")).Id);
        await client.Podcasts.SubscribeAsync("https://podcast.example/feed");
        await client.Podcasts.UnsubscribeAsync("channel");
        await client.Podcasts.RefreshAsync();
        await client.Podcasts.DownloadEpisodeAsync("episode");
        await client.Podcasts.DeleteEpisodeAsync("episode");
        Assert.AreEqual("https://radio.example/", handler.Requests.Single(x => x.Endpoint == "createInternetRadioStation").Parameters["homepageUrl"][0]);
        Assert.AreEqual("false", handler.Requests.Single(x => x.Endpoint == "getPodcasts").Parameters["includeEpisodes"][0]);
        Assert.AreEqual("30", handler.Requests.Single(x => x.Endpoint == "getNewestPodcasts").Parameters["count"][0]);
    }

    [TestMethod]
    public async Task UnsupportedPodcastResponseIsNotMistakenForAnEmptyLibrary()
    {
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented)));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var error = await Assert.ThrowsExactlyAsync<RequestException>(() => client.Podcasts.GetChannelsAsync());
        Assert.AreEqual(FailureKind.Unsupported, error.Kind);
        Assert.AreEqual(1, handler.Requests.Count);
    }
}
