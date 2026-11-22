using System.Collections.Concurrent;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Playlist;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class RemoteMediaTests
{
    [TestMethod]
    public void ReferencesPreserveOpaqueIdsAndRejectMalformedAddresses()
    {
        var server = Guid.NewGuid();
        foreach (var id in new[] { "a/A?b#c%", "مرحبا", "a", "A", "song/1" })
        {
            var reference = new Reference(server, id);
            Assert.AreEqual(reference, Reference.Parse(reference.Address));
        }
        Assert.AreNotEqual(LunaPlayer.Configuration.Paths.Key(new Reference(server, "a").Address), LunaPlayer.Configuration.Paths.Key(new Reference(server, "A").Address));
        foreach (var invalid in new[] { "luna-subsonic://bad/song/00", "luna-subsonic://" + server.ToString("N") + "/song/ff", "https://example.invalid/song", "luna-subsonic://" + server.ToString("N") + "/bad/61" })
            Assert.IsNull(Reference.Parse(invalid));
    }
    [TestMethod]
    public void InsertionAndReconciliationPreserveLocalPlaybackAndFolderReferences()
    {
        var server = Guid.NewGuid(); var other = Guid.NewGuid();
        var a = new Reference(server, "a").Address; var b = new Reference(server, "b").Address;
        var foreign = new Reference(other, "foreign").Address;
        var state = new PlaylistState(); state.OpenFiles(["local", a, "tail", foreign]);
        var folder = new SavedEntry(Guid.NewGuid(), EntryKind.Folder, @"C:\Music");
        state.ReplaceSaved([folder, SavedEntry.FromPath(a), SavedEntry.FromPath("tail"), SavedEntry.FromPath(foreign)]);
        var current = state.CurrentItem!.Id;
        state.ReconcileServer(server, [b, b, a], 4);
        Assert.AreEqual(current, state.CurrentItem!.Id);
        Assert.AreEqual("local", state.CurrentPath);
        Assert.AreEqual(folder, state.SavedSnapshot![0]);
        CollectionAssert.AreEqual(new[] { b, b, a }, state.Items.Where(x => x.ServerMedia?.ServerId == server).Select(x => x.Path).ToArray());
        Assert.AreEqual(1, state.Items.Count(x => x.Path == foreign));
        Assert.AreEqual(3, state.Items.Where(x => x.ServerMedia?.ServerId == server).Select(x => x.Id).Distinct().Count());
        state.ToggleShuffle(); var before = state.Items.Select(x => x.Id).ToArray();
        state.Insert(["new"], false, 4);
        CollectionAssert.AreEqual(before, state.Items.Where(x => x.Path != "new").Select(x => x.Id).ToArray());
    }
    [TestMethod]
    public void PlayingAnInsertedCollectionKeepsItsOrderWhenShuffleIsEnabled()
    {
        var state = new PlaylistState(); state.OpenFiles(["local", "tail", "last"]); state.ToggleShuffle();
        var old = state.Items.Select(x => x.Id).ToArray(); var current = state.CurrentItem!.Id;
        state.Insert(["one", "two", "two"], true, 0);
        var currentIndex = state.Items.ToList().FindIndex(x => x.Id == current);
        CollectionAssert.AreEqual(new[] { "one", "two", "two" }, state.Items.Skip(currentIndex + 1).Take(3).Select(x => x.Path).ToArray());
        CollectionAssert.AreEqual(old, state.Items.Where(x => x.Path is not ("one" or "two")).Select(x => x.Id).ToArray());
        Assert.AreEqual(current, state.CurrentItem.Id);
    }
    [TestMethod]
    public void ServerReferencesRoundTripWithoutCredentialsAndM3uRefusesThemAtomically()
    {
        using var directory = new TemporaryDirectory();
        var reference = new Reference(Guid.NewGuid(), "song");
        var document = new Document { Name = "Mixed", Entries = [SavedEntry.FromPath("local.mp3"), SavedEntry.FromPath(reference.Address)] };
        var path = Path.Combine(directory.Path, "list.json"); DocumentStore.Write(document, path);
        var restored = DocumentStore.Read(path);
        Assert.AreEqual(2, restored.Version); Assert.AreEqual(reference, restored.Entries[1].ServerMedia);
        Assert.IsFalse(File.ReadAllText(path).Contains("password", StringComparison.OrdinalIgnoreCase));
        var m3u = Path.Combine(directory.Path, "list.m3u8"); File.WriteAllText(m3u, "keep");
        Assert.ThrowsExactly<InvalidDataException>(() => DocumentWriter.Export(document, m3u));
        Assert.AreEqual("keep", File.ReadAllText(m3u));
        document.Entries.RemoveAt(1); DocumentStore.Write(document, path); Assert.AreEqual(1, DocumentStore.Read(path).Version);
    }
    [TestMethod]
    public async Task PausingAndSeekingWhileResolvingUsesTheLatestRequest()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var dispatcher = new Pump(); var resolver = new Resolver(); player.SetMediaResolver(resolver, dispatcher.Post);
        var address = new Reference(Guid.NewGuid(), "song").Address;
        player.InsertMedia([(address, new MediaTags("Song", "Artist", "Album"))], true);
        await Pump.Wait(() => resolver.Calls.Count == 1);
        player.Pause(); player.SeekAbsolute(12);
        await Pump.Wait(() => resolver.Calls.Count == 2);
        var calls = resolver.Calls.ToArray();
        calls[0].Result.SetResult(new MediaInput("https://invalid/old", MediaTags.None));
        calls[1].Result.SetResult(new MediaInput("https://invalid/new", new MediaTags("Song", "Artist", "Album"), 100, Offset: 12, SeekByReopening: true));
        await dispatcher.Until(() => player.IsLoaded);
        CollectionAssert.AreEqual(new[] { "https://invalid/new" }, engine.Loads);
        Assert.IsTrue(player.IsPaused); Assert.AreEqual(12d, player.Elapsed); Assert.AreEqual(88d, player.Remaining);
        player.SeekAbsolute(20); await Pump.Wait(() => resolver.Calls.Count == 3);
        Assert.AreEqual(20d, resolver.Calls.Last().Position);
        resolver.Calls.Last().Result.SetResult(new MediaInput("https://invalid/seek", MediaTags.None, 100, Offset: 20));
        await dispatcher.Until(() => engine.Loads.Count == 2); Assert.IsTrue(player.IsPaused);
    }
    [TestMethod]
    public async Task SwitchingToLocalOrStoppingDiscardsLateServerResults()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var dispatcher = new Pump(); var resolver = new Resolver(); player.SetMediaResolver(resolver, dispatcher.Post);
        player.InsertMedia([(new Reference(Guid.NewGuid(), "s").Address, MediaTags.None)], true);
        await Pump.Wait(() => resolver.Calls.Count == 1);
        player.OpenFile("local"); resolver.Calls.Last().Result.SetResult(new MediaInput("https://invalid/stale", MediaTags.None));
        await Pump.Wait(() => dispatcher.HasPosts); dispatcher.Drain(); CollectionAssert.AreEqual(new[] { "local" }, engine.Loads);
        player.InsertMedia([(new Reference(Guid.NewGuid(), "s").Address, MediaTags.None)], true);
        await Pump.Wait(() => resolver.Calls.Count == 2); player.Stop();
        resolver.Calls.Last().Result.SetResult(new MediaInput("https://invalid/stopped", MediaTags.None));
        await Pump.Wait(() => dispatcher.HasPosts); dispatcher.Drain(); Assert.AreEqual(1, engine.Loads.Count); Assert.IsFalse(player.IsLoaded);
    }
    [TestMethod]
    public async Task PausingAResolvingReplacementPausesOutgoingAudioAndIgnoresItsLateEof()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var dispatcher = new Pump(); var resolver = new Resolver(); player.SetMediaResolver(resolver, dispatcher.Post);
        player.OpenFile("local"); var ends = 0; player.Ended += _ => ends++;
        player.InsertMedia([(new Reference(Guid.NewGuid(), "song").Address, MediaTags.None)], true);
        await Pump.Wait(() => resolver.Calls.Count == 1);
        player.TogglePause(); Assert.IsTrue(engine.IsPaused); Assert.IsTrue(player.IsPaused);
        engine.End(EndReason.EndOfFile); Assert.AreEqual(0, ends);
        resolver.Calls.Last().Result.SetResult(new MediaInput("https://invalid/replacement", MediaTags.None, 100));
        await dispatcher.Until(() => player.IsLoaded); Assert.IsTrue(player.IsPaused);
    }
    [TestMethod]
    public async Task RemovingOneServerPreservesOtherEntriesAndTheirPlayback()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        player.OpenFiles(["one", "two"]); var server = Guid.NewGuid(); var other = Guid.NewGuid();
        player.InsertMedia([(new Reference(server, "a").Address, MediaTags.None), (new Reference(other, "b").Address, MediaTags.None)], false);
        player.RemoveServerMedia(server);
        Assert.AreEqual("one", player.CurrentPath); Assert.IsTrue(player.IsLoaded); Assert.AreEqual(3, player.Count); Assert.AreEqual(1, engine.Loads.Count);
        Assert.IsFalse(player.SnapshotSaved().Any(x => x.ServerMedia?.ServerId == server));
        await Task.CompletedTask;
    }
    [TestMethod]
    public void RemoteQueueDoesNotInterruptLocalPlaybackOrRemoveOtherServers()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        player.OpenFile("local"); var server = Guid.NewGuid(); var other = new Reference(Guid.NewGuid(), "other").Address;
        player.InsertMedia([(other, MediaTags.None)], false);
        player.ApplyServerQueue(server, new PlayQueue { Entries = [new() { Id = "a", Title = "A" }, new() { Id = "a", Title = "A" }], CurrentIndex = 1, Position = 9000 });
        Assert.AreEqual("local", player.CurrentPath); Assert.AreEqual(1, engine.Loads.Count);
        Assert.AreEqual(4, player.Count); Assert.AreEqual(2, player.Items.Count(x => x.ServerMedia?.ServerId == server));
        Assert.IsTrue(player.Items.Any(x => x.Path == other));
    }
    private sealed class Resolver : IMediaResolver
    {
        internal readonly record struct Call(double? Position, TaskCompletionSource<MediaInput> Result);
        internal ConcurrentQueue<Call> Calls { get; } = new();
        public bool Handles(string address) => Reference.IsAddress(address);
        public Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
        { var result = new TaskCompletionSource<MediaInput>(TaskCreationOptions.RunContinuationsAsynchronously); Calls.Enqueue(new(position, result)); return result.Task; }
    }
}
