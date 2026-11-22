using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class InsertionTests
{
    [TestMethod]
    public async Task PlayingServerSongsAppendsToTheEndAndReusesExistingEntries()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine); var pump = new Pump();
        player.SetMediaResolver(new Resolver(), pump.Post);
        player.OpenFiles(["local-a", "local-b"]);
        var local = player.Items.Select(item => item.Id).ToArray();
        var server = Guid.NewGuid(); var first = new Reference(server, "first").Address; var second = new Reference(server, "second").Address;
        Assert.AreEqual(1, player.InsertMedia([(first, MediaTags.None), (first, MediaTags.None)], true));
        await pump.Until(() => player.IsLoaded && player.CurrentPath == first);
        var identity = player.CurrentItemId;
        CollectionAssert.AreEqual(new[] { "local-a", "local-b", first }, player.Items.Select(item => item.Path).ToArray());
        Assert.AreEqual(1, player.InsertMedia([(first, MediaTags.None), (second, MediaTags.None)], true));
        await pump.Until(() => player.IsLoaded && player.CurrentPath == first);
        Assert.AreEqual(identity, player.CurrentItemId);
        Assert.AreEqual(0, player.InsertMedia([(first, MediaTags.None)], true));
        await pump.Until(() => player.IsLoaded);
        CollectionAssert.AreEqual(new[] { "local-a", "local-b", first, second }, player.Items.Select(item => item.Path).ToArray());
        CollectionAssert.AreEqual(local, player.Items.Take(2).Select(item => item.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "local-a", "local-b", first, second }, player.SnapshotSaved().Select(item => item.Location).ToArray());
    }

    [TestMethod]
    public void AppendingExistingServerSongsDoesNotChangeTheCurrentTrack()
    {
        using var player = new Player(new FakeEngine()); player.OpenFiles(["local", "tail"]);
        var current = player.CurrentItemId; var path = new Reference(Guid.NewGuid(), "song").Address;
        Assert.AreEqual(1, player.InsertMedia([(path, MediaTags.None)], false));
        Assert.AreEqual(0, player.InsertMedia([(path, MediaTags.None)], false));
        Assert.AreEqual(current, player.CurrentItemId); Assert.AreEqual("local", player.CurrentPath); Assert.AreEqual(3, player.Count);
    }

    private sealed class Resolver : IMediaResolver
    {
        public bool Handles(string address) => Reference.IsAddress(address);
        public Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
            => Task.FromResult(new MediaInput("https://invalid/" + Reference.Parse(address)!.Id, MediaTags.None));
    }
}
