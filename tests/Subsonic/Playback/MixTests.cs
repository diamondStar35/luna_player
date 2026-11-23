using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Playlist;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class MixTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task APlayingSeedKeepsItsPositionAndRelatedSongsAreQueuedNext(bool shuffle)
    {
        using var engine = new FakeEngine(); using var player = new Player(engine); var pump = new Pump();
        player.SetMediaResolver(new Resolver(), pump.Post);
        var server = Guid.NewGuid();
        var seed = new Reference(server, "seed").Address;
        var first = new Reference(server, "first").Address;
        var second = new Reference(server, "second").Address;
        player.OpenFiles(["local", "tail"]);
        player.InsertMedia([(seed, MediaTags.None), (second, MediaTags.None)], true);
        await pump.Until(() => player.IsLoaded);
        player.SeekAbsolute(4);
        if (shuffle) player.ToggleShuffle();
        var current = player.CurrentItemId; var loads = engine.Loads.Count;
        var retained = player.Items.Select(item => item.Id).ToArray();
        player.PlayMediaMix([(seed, MediaTags.None), (first, MediaTags.None), (second, MediaTags.None), (first, MediaTags.None)]);
        Assert.AreEqual(current, player.CurrentItemId);
        Assert.AreEqual(4d, player.Elapsed);
        Assert.AreEqual(loads, engine.Loads.Count);
        Assert.AreEqual(5, player.Count);
        Assert.IsFalse(player.InSession);
        Assert.IsTrue(retained.All(id => player.Items.Any(item => item.Id == id)));
        Assert.IsTrue(player.Next(false));
        await pump.Until(() => player.IsLoaded);
        Assert.AreEqual(first, player.CurrentPath);
        Assert.IsTrue(player.Next(false));
        await pump.Until(() => player.IsLoaded);
        Assert.AreEqual(second, player.CurrentPath);
        Assert.AreEqual(1, player.SnapshotSaved().Count(entry => entry.Location == first));
    }

    [TestMethod]
    public void QueueingAMixRetainsFolderReferencesAndExistingSourceIdentities()
    {
        var state = new PlaylistState(); var server = Guid.NewGuid();
        var first = new Reference(server, "first").Address; var second = new Reference(server, "second").Address;
        state.OpenFiles(["local", first, second, "tail"]);
        var original = state.Items.ToArray();
        var folder = new SavedEntry(Guid.NewGuid(), EntryKind.Folder, @"C:\Music");
        state.ReplaceSaved([folder, SavedEntry.FromPath(first) with { Id = original[1].SourceId },
            SavedEntry.FromPath(second) with { Id = original[2].SourceId }, SavedEntry.FromPath("tail")]);
        state.QueueMix([first, new Reference(server, "new").Address, second], 0);
        Assert.AreEqual(folder, state.SavedSnapshot![0]);
        Assert.IsTrue(original.All(item => state.Items.Any(retained => retained.Id == item.Id)));
        Assert.AreEqual(5, state.Items.Count);
    }

    private sealed class Resolver : IMediaResolver
    {
        public bool Handles(string address) => Reference.IsAddress(address);
        public Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
            => Task.FromResult(new MediaInput("https://invalid/" + Reference.Parse(address)!.Id, MediaTags.None));
    }
}
