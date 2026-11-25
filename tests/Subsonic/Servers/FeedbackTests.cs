using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Servers;

namespace LunaPlayer.Tests.Subsonic.Servers;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class FeedbackTests
{
    [TestMethod]
    public async Task PartialRatingFailureKeepsSuccessfulItemsAndPublishesOneBatch()
    {
        using var directory = new TemporaryDirectory();
        using var handler = new Handler((sent, _) => Task.FromResult(sent.Endpoint == "setRating" && sent.Parameters["id"][0] == "bad"
            ? Fixture.Raw("{\"subsonic-response\":{\"status\":\"failed\",\"version\":\"1.16.1\",\"error\":{\"code\":50}}}") : Fixture.Json("")));
        using var http = new HttpClient(handler);
        using var connections = new Connections(new Store(Path.Combine(directory.Path, "profiles.json")), profile => new Client(profile.ConnectionOptions(), http));
        var profile = await connections.SaveAsync(new Profile { Name = "Fixture", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        connections.Metadata.Observe(profile.Id, new Entry[] { new() { Id = "good", UserRating = 1 }, new() { Id = "bad", UserRating = 2 } });
        var batches = 0; connections.Metadata.Changed += (_, _) => batches++;
        var result = await connections.RateAsync(profile.Id, ["good", "bad", "good"], 4, CancellationToken.None);
        Assert.AreEqual(1, result.Changed); Assert.AreEqual(1, result.Failures.Length);
        Assert.AreEqual("bad", result.Failures[0].Id); Assert.AreEqual(1, batches);
        Assert.AreEqual(4, connections.Metadata.Get(profile.Id, "good")!.UserRating);
        Assert.AreEqual(2, connections.Metadata.Get(profile.Id, "bad")!.UserRating);
        Assert.AreEqual(2, handler.Requests.Count);
    }

    [TestMethod]
    public async Task RemovingARatingAndFavoriteRetainsOtherMetadata()
    {
        using var directory = new TemporaryDirectory(); using var handler = new Handler((_, _) => Task.FromResult(Fixture.Json("")));
        using var http = new HttpClient(handler);
        using var connections = new Connections(new Store(Path.Combine(directory.Path, "profiles.json")), profile => new Client(profile.ConnectionOptions(), http));
        var profile = await connections.SaveAsync(new Profile { Name = "Fixture", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        connections.Metadata.Observe(profile.Id, new Entry { Id = "s", Title = "Title", Artist = "Artist", UserRating = 5, Starred = "2026-01-01" });
        await connections.RateAsync(profile.Id, ["s"], 0, CancellationToken.None);
        await connections.StarAsync(profile.Id, ["s"], false, CancellationToken.None);
        var song = connections.Metadata.Get(profile.Id, "s")!;
        Assert.AreEqual(0, song.UserRating); Assert.IsNull(song.Starred);
        Assert.AreEqual("Title", song.Title); Assert.AreEqual("Artist", song.Artist);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => connections.RateAsync(profile.Id, ["s"], 6, CancellationToken.None));
        Assert.AreEqual(2, handler.Requests.Count);
    }

    [TestMethod]
    public void MetadataIsBoundedAndServerIdentitiesDoNotOverlap()
    {
        var metadata = new Metadata(); var first = Guid.NewGuid(); var second = Guid.NewGuid(); var batches = 0;
        metadata.Changed += (_, _) => batches++;
        metadata.Observe(first, new Entry { Id = "same", Title = "First" });
        metadata.Observe(second, new Entry { Id = "same", Title = "Second" });
        Assert.AreEqual("First", metadata.Get(first, "same")!.Title);
        Assert.AreEqual("Second", metadata.Get(second, "same")!.Title);
        metadata.Observe(first, Enumerable.Range(0, 5000).Select(index => new Entry { Id = index.ToString() }));
        Assert.IsNull(metadata.Get(first, "0")); Assert.IsNotNull(metadata.Get(first, "4999"));
        Assert.AreEqual(3, batches);
    }
}
