using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Servers;

namespace LunaPlayer.Tests.Subsonic.Servers;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class StoreTests
{
    [TestMethod]
    public async Task ProfilesRoundTripWithProtectedPasswordsKeysHeadersAndAudioMuseToken()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "servers.json");
        var store = new Store(path);
        var saved = await store.SaveAsync(new Profile
        {
            Name = "Home", Server = Fixture.Options.Server,
            Credentials = new() { Username = "user", Password = "unique password αβ", ApiKey = "unique apikey" },
            Headers = new Dictionary<string, string> { ["X-Proxy-Key"] = "unique header secret" },
            Streaming = new() { Mode = StreamingMode.Transcode, Format = "opus", MaximumBitRate = 128 },
            LibraryIds = ["library1", "library2"], ReportListening = true,
            AudioMuse = new() { Server = new Uri("https://ai.example/prefix/"), Token = "unique AI secret", MusicServer = "Home" }
        });
        Assert.AreNotEqual(Guid.Empty, saved.Id);
        var text = await File.ReadAllTextAsync(path);
        foreach (var secret in new[] { "unique password", "unique apikey", "unique header secret", "unique AI secret" })
            Assert.IsFalse(text.Contains(secret, StringComparison.Ordinal));
        var loaded = (await new Store(path).LoadAsync()).Single();
        Assert.AreEqual(saved.Id, loaded.Id);
        Assert.AreEqual(saved.Credentials, loaded.Credentials);
        Assert.AreEqual("unique header secret", loaded.Headers["X-Proxy-Key"]);
        Assert.AreEqual("unique AI secret", loaded.AudioMuse!.Token);
        Assert.AreEqual(StreamingMode.Transcode, loaded.Streaming.Mode);
        Assert.AreEqual(128, loaded.Streaming.MaximumBitRate);
        Assert.IsTrue(loaded.ReportListening);
        CollectionAssert.AreEqual(new[] { "library1", "library2" }, loaded.LibraryIds);
    }

    [TestMethod]
    public async Task UpdateAndRemoveUseStableServerIdentity()
    {
        using var directory = new TemporaryDirectory();
        var store = new Store(Path.Combine(directory.Path, "servers.json"));
        Assert.AreEqual(0, (await store.LoadAsync()).Length);
        var first = await store.SaveAsync(new Profile { Name = "First", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        var second = await store.SaveAsync(first with { Id = Guid.Empty, Name = "Second" });
        Assert.AreNotEqual(new TrackReference(first.Id, "same"), new TrackReference(second.Id, "same"));
        await store.SaveAsync(first with { Name = "Renamed" });
        Assert.AreEqual(2, (await store.LoadAsync()).Length);
        Assert.AreEqual("Renamed", (await store.LoadAsync()).Single(x => x.Id == first.Id).Name);
        Assert.IsTrue(await store.RemoveAsync(first.Id));
        Assert.IsFalse(await store.RemoveAsync(first.Id));
        Assert.AreEqual(second.Id, (await store.LoadAsync()).Single().Id);
    }

    [TestMethod]
    [DataRow("invalid JSON")]
    [DataRow("{\"version\":99,\"servers\":[]}")]
    [DataRow("{\"version\":1,\"servers\":[null]}")]
    public async Task InvalidStoreCannotBeOverwritten(string invalid)
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "servers.json");
        await File.WriteAllTextAsync(path, invalid);
        var store = new Store(path);
        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => store.SaveAsync(new Profile
        { Name = "New", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials }));
        Assert.AreEqual(invalid, await File.ReadAllTextAsync(path));
        Assert.AreEqual(1, Directory.GetFiles(directory.Path).Length);
    }

    [TestMethod]
    public async Task InvalidTranscodeConfigurationIsRejectedWithoutCreatingStore()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "servers.json");
        var store = new Store(path);
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(new Profile
        {
            Name = "New", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials,
            Streaming = new() { Mode = StreamingMode.Transcode }
        }));
        Assert.IsFalse(File.Exists(path));
    }
}
