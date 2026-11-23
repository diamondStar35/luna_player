using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Servers;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Subsonic.Media;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Downloads;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Tests.Subsonic.Servers;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class ConnectionsTests
{
    [TestMethod]
    public async Task LastServerSurvivesReloadAndRapidSelectionsKeepTheLatestChoice()
    {
        using var directory = new TemporaryDirectory(); var path = Path.Combine(directory.Path, "servers.json");
        using var connections = new Connections(new Store(path));
        var first = await connections.SaveAsync(new Profile { Name = "First", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        var second = await connections.SaveAsync(first with { Id = Guid.Empty, Name = "Second" });
        await Task.WhenAll(Enumerable.Range(0, 12).Select(index => connections.SelectAsync(index % 2 == 0 ? first.Id : second.Id)));
        using var restored = new Connections(new Store(path)); await restored.LoadAsync();
        Assert.AreEqual(second.Id, restored.LastServer);
        await restored.SaveAsync(restored.Profile(first.Id) with { Name = "Renamed" });
        using var edited = new Connections(new Store(path)); await edited.LoadAsync();
        Assert.AreEqual(second.Id, edited.LastServer);
        await edited.RemoveAsync(second.Id);
        using var removed = new Connections(new Store(path)); await removed.LoadAsync();
        Assert.IsNull(removed.LastServer); Assert.AreEqual(first.Id, removed.Profiles.Single().Id);
    }

    [TestMethod]
    public async Task InspectionIsSharedAndCancellingOneWaitDoesNotCancelOtherCallers()
    {
        using var directory = new TemporaryDirectory();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new Handler(async (sent, token) =>
        {
            if (sent.Endpoint == "getUser") { started.TrySetResult(); await release.Task.WaitAsync(token); }
            return await Fixture.Reply(sent, token);
        });
        using var http = new HttpClient(handler);
        using var connections = new Connections(new Store(Path.Combine(directory.Path, "servers.json")), profile => new Client(profile.ConnectionOptions(), http));
        var profile = await connections.SaveAsync(new Profile { Name = "Music", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        using var cancellation = new CancellationTokenSource();
        var cancelled = connections.InspectAsync(profile.Id, token: cancellation.Token);
        var other = connections.InspectAsync(profile.Id);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)); cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await cancelled);
        release.TrySetResult(); var information = await other;
        Assert.AreEqual("user", information.Username); Assert.AreEqual(1, information.Folders.Length);
        var requests = handler.Requests.Count;
        Assert.AreSame(information, await connections.InspectAsync(profile.Id)); Assert.AreEqual(requests, handler.Requests.Count);
        await connections.InspectAsync(profile.Id, refresh: true);
        Assert.AreEqual(2, handler.Requests.Count(sent => sent.Endpoint == "getUser"));
        Assert.AreEqual(2, handler.Requests.Count(sent => sent.Endpoint == "getMusicFolders"));
        await connections.SaveAsync(profile with { Name = "Edited" });
        await connections.InspectAsync(profile.Id);
        Assert.AreEqual(3, handler.Requests.Count(sent => sent.Endpoint == "getUser"));
    }

    [TestMethod]
    public async Task ResolvingABrowsedSongDoesNotFetchItsMetadataAgain()
    {
        using var directory = new TemporaryDirectory(); using var handler = new Handler(Fixture.Reply); using var http = new HttpClient(handler);
        using var connections = new Connections(new Store(Path.Combine(directory.Path, "servers.json")), profile => new Client(profile.ConnectionOptions(), http));
        var profile = await connections.SaveAsync(new Profile { Name = "Music", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        connections.Metadata.Observe(profile.Id, new Entry { Id = "s", Title = "Browsed title", Artist = "Artist", Duration = 120 });
        var input = await connections.ResolveAsync(new Reference(profile.Id, "s").Address, null, CancellationToken.None);
        Assert.IsEmpty(handler.Requests, "Building an original stream URL from known metadata needs no network round trip.");
        Assert.AreEqual("Browsed title", input.Tags.Title); Assert.AreEqual(120d, input.Duration);
        await connections.SaveAsync(profile with { Name = "Edited" });
        var refreshed = await connections.ResolveAsync(new Reference(profile.Id, "s").Address, null, CancellationToken.None);
        Assert.AreEqual("Song", refreshed.Tags.Title);
        Assert.AreEqual(1, handler.Requests.Count(sent => sent.Endpoint == "getSong"));
    }
    [TestMethod]
    public async Task SavedOptionsAndProtectedSecretsSurviveConcurrentEdits()
    {
        using var directory = new TemporaryDirectory();
        using var connections = new Connections(new Store(Path.Combine(directory.Path, "servers.json")));
        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => connections.SaveAsync(new Profile { Name = "Server " + i, Server = Fixture.Options.Server,
            Credentials = Fixture.Options.Credentials, SynchronizeQueue = true, ReportListening = true, Downloads = new() { Folder = directory.Path, AlbumFolders = true } })));
        Assert.AreEqual(8, connections.Profiles.Length);
        Assert.IsTrue(connections.Profiles.All(x => x.SynchronizeQueue && x.ReportListening && x.Downloads.AlbumFolders));
        Assert.IsFalse(File.ReadAllText(Path.Combine(directory.Path, "servers.json")).Contains("\"password\": \"secret\"", StringComparison.Ordinal));
    }
    [TestMethod]
    public async Task OlderProfilesReceiveDownloadAndQueueDefaults()
    {
        using var directory = new TemporaryDirectory(); var path = Path.Combine(directory.Path, "servers.json");
        var store = new Store(path);
        await store.SaveAsync(new Profile { Name = "Legacy", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        var saved = json["servers"]![0]!["profile"]!.AsObject(); saved.Remove("downloads"); saved.Remove("synchronizeQueue");
        saved.Remove("hiddenSections"); saved.Remove("refreshMetadataOnStartup");
        await File.WriteAllTextAsync(path, json.ToJsonString());
        var profile = (await store.LoadAsync()).Single();
        Assert.AreEqual("", profile.Downloads.Folder); Assert.IsFalse(profile.SynchronizeQueue);
        Assert.IsEmpty(profile.HiddenSections); Assert.IsTrue(profile.RefreshMetadataOnStartup);
    }
    [TestMethod]
    public async Task PlaybackResolutionUsesTranscodeOffsetsAndCapturedHeaders()
    {
        using var directory = new TemporaryDirectory(); using var handler = new Handler((sent, token) => sent.Endpoint == "getOpenSubsonicExtensions"
            ? Task.FromResult(Fixture.Json("\"openSubsonicExtensions\":[{\"name\":\"transcodeOffset\",\"versions\":[1]}]")) : Fixture.Reply(sent, token)); using var http = new HttpClient(handler);
        using var connections = new Connections(new Store(Path.Combine(directory.Path, "servers.json")), profile => new Client(profile.ConnectionOptions(), http));
        var profile = await connections.SaveAsync(new Profile { Name = "Music", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials,
            Streaming = new() { Mode = StreamingMode.Transcode, Format = "opus", MaximumBitRate = 128 }, Headers = new Dictionary<string, string> { ["X-Proxy"] = "a,b" } });
        var input = await connections.ResolveAsync(new Reference(profile.Id, "s").Address, 23, CancellationToken.None);
        Assert.AreEqual(23d, input.Offset); Assert.IsTrue(input.SeekByReopening); Assert.AreEqual("a,b", input.Headers!["X-Proxy"]);
        Assert.IsTrue(input.Address.Contains("timeOffset=23", StringComparison.Ordinal)); Assert.IsTrue(input.Address.Contains("format=opus", StringComparison.Ordinal));
    }
    [TestMethod]
    public async Task OriginalDownloadFinishesAfterRemovingItsServerAndNeverOverwritesFiles()
    {
        using var directory = new TemporaryDirectory(); using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw("original", "audio/flac"))); using var http = new HttpClient(handler);
        using var connections = new Connections(new Store(Path.Combine(directory.Path, "servers.json")), profile => new Client(profile.ConnectionOptions(), http));
        var profile = await connections.SaveAsync(new Profile { Name = "Music", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials,
            Downloads = new() { Folder = directory.Path }, Streaming = new() { Mode = StreamingMode.Transcode, Format = "opus" } });
        var job = DownloadJob.Create(profile, connections.Client(profile.Id), new Entry { Id = "s", Title = "Song", Suffix = "flac" });
        File.WriteAllText(Path.Combine(directory.Path, "Song.flac"), "keep");
        await connections.RemoveAsync(profile.Id);
        var result = await Task.Run(() => job.Run(_ => { }, CancellationToken.None));
        Assert.IsTrue(result.Success); Assert.AreEqual("original", File.ReadAllText(result.FilePath));
        Assert.AreEqual("keep", File.ReadAllText(Path.Combine(directory.Path, "Song.flac")));
        Assert.AreEqual("raw", handler.Requests.Single().Parameters["format"][0]);
        Assert.ThrowsExactly<KeyNotFoundException>(() => connections.Client(profile.Id));
    }
    [TestMethod]
    public async Task DifferentDownloadProvidersShareQueueSchedulingAndResults()
    {
        using var queue = new DownloadQueue(new Settings());
        var one = queue.Add(new Job("Music", (_, _) => Outcome.Saved("music.flac")));
        var two = queue.Add(new Job("Video", (_, _) => Outcome.Saved("video.mkv")));
        Assert.AreEqual(QueueItemStatus.InQueue, queue.Snapshot(one)!.Value.Status);
        queue.Start(); await Pump.Wait(() => queue.Snapshot(one)!.Value.Status == QueueItemStatus.Completed && queue.Snapshot(two)!.Value.Status == QueueItemStatus.Completed);
        Assert.AreEqual("music.flac", queue.Snapshot(one)!.Value.FilePath); Assert.AreEqual("video.mkv", queue.Snapshot(two)!.Value.FilePath);
    }
}
