using System.Text.Json.Nodes;
using LunaPlayer.Subsonic.Servers;

namespace LunaPlayer.Tests.Subsonic.Servers;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class TimingTests
{
    [TestMethod]
    public async Task TimingOptionsSurviveStorageAndConfigureNewConnections()
    {
        using var directory = new TemporaryDirectory(); var path = Path.Combine(directory.Path, "servers.json");
        var timing = new TimingSettings { QueuePollSeconds = 7, QueueSaveSeconds = 11, PlaybackReportSeconds = 3,
            ScanPollMilliseconds = 750, SynchronizationRetrySeconds = 20, ConnectionCacheSeconds = 90,
            StreamCacheSeconds = 120, LibraryCacheSeconds = 600, BrowserRefreshSeconds = 0, SearchDelayMilliseconds = 0,
            RequestTimeoutSeconds = 45, AudioMuseSearchSeconds = 75, AudioMuseGenerationSeconds = 420 };
        await new Store(path).SaveAsync(new Profile { Name = "Music", Server = Fixture.Options.Server,
            Credentials = Fixture.Options.Credentials, Timing = timing });
        var profile = (await new Store(path).LoadAsync()).Single();
        Assert.AreEqual(timing, profile.Timing);
        Assert.AreEqual(TimeSpan.FromSeconds(45), profile.ConnectionOptions().RequestTimeout);
        Assert.AreEqual(TimeSpan.FromMinutes(10), profile.ConnectionOptions().LibraryCacheDuration);
    }

    [TestMethod]
    public async Task ProfilesWithoutTimingOptionsKeepTheExistingDefaults()
    {
        using var directory = new TemporaryDirectory(); var path = Path.Combine(directory.Path, "servers.json");
        await new Store(path).SaveAsync(new Profile { Name = "Old account", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        var document = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        document["servers"]![0]!["profile"]!.AsObject().Remove("timing");
        await File.WriteAllTextAsync(path, document.ToJsonString());
        var timing = (await new Store(path).LoadAsync()).Single().Timing;
        Assert.AreEqual(5, timing.QueuePollSeconds); Assert.AreEqual(5, timing.QueueSaveSeconds);
        Assert.AreEqual(1, timing.PlaybackReportSeconds); Assert.AreEqual(1500, timing.ScanPollMilliseconds);
        Assert.AreEqual(30, timing.SynchronizationRetrySeconds); Assert.AreEqual(300, timing.ConnectionCacheSeconds);
        Assert.AreEqual(300, timing.StreamCacheSeconds); Assert.AreEqual(1800, timing.LibraryCacheSeconds);
        Assert.AreEqual(1800, timing.BrowserRefreshSeconds); Assert.AreEqual(300, timing.SearchDelayMilliseconds);
        Assert.AreEqual(30, timing.RequestTimeoutSeconds); Assert.AreEqual(60, timing.AudioMuseSearchSeconds);
        Assert.AreEqual(300, timing.AudioMuseGenerationSeconds);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task InvalidTimingsCannotReplaceSavedAccounts(int invalid)
    {
        using var directory = new TemporaryDirectory(); var path = Path.Combine(directory.Path, "servers.json");
        var store = new Store(path);
        var saved = await store.SaveAsync(new Profile { Name = "Music", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        var before = await File.ReadAllTextAsync(path);
        var timing = invalid switch
        {
            0 => new TimingSettings { QueuePollSeconds = 0 },
            1 => new TimingSettings { StreamCacheSeconds = -1 },
            2 => new TimingSettings { SearchDelayMilliseconds = 60001 },
            _ => new TimingSettings { RequestTimeoutSeconds = 86401 }
        };
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => store.SaveAsync(saved with { Timing = timing }));
        Assert.AreEqual(before, await File.ReadAllTextAsync(path));
    }
}
