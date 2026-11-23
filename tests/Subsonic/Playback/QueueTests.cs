using System.Collections.Concurrent;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class QueueTests
{
    [TestMethod]
    public async Task LoadingAServerQueueStartsPlaybackAtTheSavedPosition()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var dispatcher = new Pump(); var resolver = new Resolver(); player.SetMediaResolver(resolver, dispatcher.Post);
        var server = Guid.NewGuid();
        player.ApplyServerQueue(server, new PlayQueue
        {
            Entries = [new() { Id = "second", Title = "Second" }], CurrentIndex = 0, Position = 9000
        }, play: true);
        Assert.AreEqual(new Reference(server, "second").Address, player.CurrentPath);
        Assert.IsTrue(player.IsResolvingMedia, "Loading the queue must start its saved song and position while stopped.");
        Assert.AreEqual(9d, player.Elapsed);
        Assert.IsFalse(player.IsPaused);
        await Pump.Wait(() => resolver.Calls.Count == 1);
        Assert.AreEqual(9d, resolver.Calls.Single().Position);
        resolver.Calls.Single().Result.SetResult(new MediaInput("https://invalid/second", MediaTags.None, 60));
        await dispatcher.Until(() => player.IsLoaded);
        Assert.AreEqual(9d, player.Elapsed);
        Assert.IsTrue(player.IsPlaying);
        Assert.AreEqual(9d, player.Elapsed);
        Assert.AreEqual(1, engine.Loads.Count);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task StoppedQueueRestorationSelectsTheSavedSongAndMapsItsStreamOffset(bool previouslyLoaded, bool byIndex)
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var dispatcher = new Pump(); var resolver = new Resolver(); player.SetMediaResolver(resolver, dispatcher.Post);
        if (previouslyLoaded) { player.OpenFile("local"); player.Stop(); }
        var server = Guid.NewGuid();
        player.ApplyServerQueue(server, new PlayQueue
        {
            Entries = [new() { Id = "first" }, new() { Id = "second" }, new() { Id = "second" }],
            Current = "second", CurrentIndex = byIndex ? 2 : null, Position = 9000
        }, play: true);
        var songs = player.Items.Where(item => item.ServerMedia?.ServerId == server).ToArray();
        Assert.AreEqual(songs[byIndex ? 2 : 1].Id, player.CurrentItemId);
        Assert.AreEqual(previouslyLoaded ? 4 : 3, player.Count);
        await Pump.Wait(() => resolver.Calls.Count == 1);
        Assert.AreEqual(9d, resolver.Calls.Single().Position);
        resolver.Calls.Single().Result.SetResult(new MediaInput("https://invalid/offset", MediaTags.None, 60,
            Offset: 9, SeekByReopening: true));
        await dispatcher.Until(() => player.IsLoaded);
        Assert.AreEqual(0d, engine.Position);
        Assert.AreEqual(9d, player.Elapsed);
        Assert.AreEqual(51d, player.Remaining);
        Assert.IsTrue(player.IsPlaying);
        Assert.AreEqual(9d, player.Elapsed);
        Assert.AreEqual(1, resolver.Calls.Count);
    }

    [TestMethod]
    public async Task PausingWhileTheSavedQueueIsOpeningKeepsItsPosition()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var dispatcher = new Pump(); var resolver = new Resolver(); player.SetMediaResolver(resolver, dispatcher.Post);
        player.ApplyServerQueue(Guid.NewGuid(), new PlayQueue
        { Entries = [new() { Id = "song" }], Current = "song", Position = 9000 }, play: true);
        player.TogglePause();
        Assert.IsTrue(player.IsPaused);
        await Pump.Wait(() => resolver.Calls.Count == 1);
        resolver.Calls.Single().Result.SetResult(new MediaInput("https://invalid/song", MediaTags.None, 60));
        await dispatcher.Until(() => player.IsLoaded);
        Assert.IsTrue(player.IsPaused);
        Assert.IsFalse(player.IsPlaying);
        Assert.AreEqual(9d, player.Elapsed);
        Assert.AreEqual(1, engine.Loads.Count);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task ExplicitLoadingPlaysAnOpenSongWhileBackgroundSynchronizationPreservesPause(bool paused, bool play)
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var dispatcher = new Pump(); var resolver = new Resolver(); player.SetMediaResolver(resolver, dispatcher.Post);
        var server = Guid.NewGuid();
        player.InsertMedia([(new Reference(server, "song").Address, MediaTags.None)], true);
        await Pump.Wait(() => resolver.Calls.Count == 1);
        resolver.Calls.Single().Result.SetResult(new MediaInput("https://invalid/song", MediaTags.None, 60));
        await dispatcher.Until(() => player.IsLoaded);
        if (paused) player.Pause();
        player.ApplyServerQueue(server, new PlayQueue
        { Entries = [new() { Id = "song" }], Current = "song", Position = 9000 }, play: play);
        if (play)
        {
            await Pump.Wait(() => resolver.Calls.Count == 2);
            resolver.Calls.Last().Result.SetResult(new MediaInput("https://invalid/song", MediaTags.None, 60));
            await dispatcher.Until(() => player.IsLoaded);
        }
        Assert.AreEqual(9d, player.Elapsed);
        Assert.AreEqual(paused && !play, player.IsPaused);
        Assert.AreEqual(play ? 2 : 1, engine.Loads.Count);
        CollectionAssert.AreEqual(play ? Array.Empty<double>() : new[] { 9d }, engine.RequestedSeeks);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ExplicitQueueLoadingStartsTheSavedSongAndRetainsOtherMedia(bool session)
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var dispatcher = new Pump(); var resolver = new Resolver(); player.SetMediaResolver(resolver, dispatcher.Post);
        player.OpenFile("local");
        var other = new Reference(Guid.NewGuid(), "other").Address;
        player.InsertMedia([(other, MediaTags.None)], false);
        if (session) player.PlaySessionStream("https://invalid/video", "Video", "YouTube", null, audioOnly: true);
        var server = Guid.NewGuid();
        player.ApplyServerQueue(server, new PlayQueue
        { Entries = [new() { Id = "first" }, new() { Id = "second" }], CurrentIndex = 1, Position = 9000 }, play: true);
        Assert.IsFalse(player.InSession);
        Assert.AreEqual(new Reference(server, "second").Address, player.CurrentPath);
        Assert.AreEqual(4, player.Count);
        Assert.IsTrue(player.Items.Any(item => item.Path == "local"));
        Assert.IsTrue(player.Items.Any(item => item.Path == other));
        await Pump.Wait(() => resolver.Calls.Count == 1);
        resolver.Calls.Single().Result.SetResult(new MediaInput("https://invalid/second", MediaTags.None, 60));
        await dispatcher.Until(() => player.IsLoaded);
        Assert.IsTrue(player.IsPlaying);
        Assert.AreEqual(9d, player.Elapsed);
    }

    [TestMethod]
    public void BackgroundQueueSynchronizationDoesNotLoadAStoppedPlayer()
    {
        using var engine = new FakeEngine(); using var player = new Player(engine);
        var dispatcher = new Pump(); var resolver = new Resolver(); player.SetMediaResolver(resolver, dispatcher.Post);
        player.ApplyServerQueue(Guid.NewGuid(), new PlayQueue
        { Entries = [new() { Id = "song" }], Current = "song", Position = 9000 });
        Assert.AreEqual(1, player.Count);
        Assert.IsFalse(player.IsLoaded);
        Assert.IsFalse(player.IsResolvingMedia);
        Assert.IsEmpty(engine.Loads);
        Assert.IsEmpty(resolver.Calls);
    }

    private sealed class Resolver : IMediaResolver
    {
        internal readonly record struct Call(double? Position, TaskCompletionSource<MediaInput> Result);
        internal ConcurrentQueue<Call> Calls { get; } = new();
        public bool Handles(string address) => Reference.IsAddress(address);
        public Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
        {
            var result = new TaskCompletionSource<MediaInput>(TaskCreationOptions.RunContinuationsAsynchronously);
            Calls.Enqueue(new(position, result)); return result.Task;
        }
    }
}
