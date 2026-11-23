using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, DoNotParallelize, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class NativeStreamingTests
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task ReturningToAnOriginalSongKeepsSeekingAndRepeatedQueueLoadsReuseItsStream(bool crossfade, bool gapless)
    {
        using var workspace = new Workspace(); var wave = File.ReadAllBytes(NativeHost.Wave(workspace));
        await using var server = new MediaServer(new Dictionary<string, byte[]> { ["first.wav"] = wave, ["second.wav"] = wave });
        using var engine = NativeHost.Create(); using var player = new Player(engine); var pump = new Pump();
        player.ConfigurePlaybackEffects(new AudioSettings { Crossfade = crossfade, CrossfadeManualNavigation = true,
            CrossfadeMilliseconds = 100, GaplessPlayback = gapless ? GaplessPlaybackMode.CompatibleFiles : GaplessPlaybackMode.Disabled });
        var account = Guid.NewGuid(); var first = new Reference(account, "first.wav").Address; var second = new Reference(account, "second.wav").Address;
        player.SetMediaResolver(new Resolver(server), pump.Post);
        player.InsertMedia([(first, MediaTags.None), (second, MediaTags.None)], true);
        await Ready("first.wav");
        Assert.IsTrue(player.Next(false)); await Ready("second.wav");
        Assert.IsTrue(player.Previous(false)); await Ready("first.wav");
        Assert.IsTrue(player.Next(false)); await Ready("second.wav");
        var queue = new PlayQueue { Entries = [new() { Id = "first.wav" }, new() { Id = "second.wav" }], CurrentIndex = 1, Position = 6250 };
        for (var iteration = 0; iteration < 2; iteration++)
        {
            var requests = server.Requests("second.wav");
            using var completion = new NativeHost.SeekCompletion(engine);
            player.ApplyServerQueue(account, queue, play: true);
            Assert.IsFalse(player.IsResolvingMedia);
            await completion.Wait();
            await NativeHost.Until(() => player.IsPlaying && Math.Abs(player.SourceElapsed!.Value - 6.25) < 0.5);
            Assert.AreEqual(requests, server.Requests("second.wav"), "Restoring an already-open original song must retain its buffered input.");
            using var rewind = new NativeHost.SeekCompletion(engine);
            player.SeekAbsolute(1.25); await rewind.Wait();
            await NativeHost.Until(() => Math.Abs(player.SourceElapsed!.Value - 1.25) < 0.5);
            Assert.AreEqual(requests, server.Requests("second.wav"), "Rewinding cached audio must not reopen the HTTP stream.");
        }

        Task Ready(string name) => NativeHost.Until(() =>
        {
            pump.Drain();
            return player.IsLoaded && !engine.IsLoading && player.IsPlaying
                && NativeHost.Handle(engine).GetProperty("path")?.ToString() == server.Url(name) && engine.Elapsed > 0.02;
        });
    }

    [TestMethod]
    public async Task AudioOnlyCrossfadeDoesNotOpenASecondMetadataStream()
    {
        using var workspace = new Workspace();
        var wave = File.ReadAllBytes(NativeHost.Wave(workspace));
        await using var server = new MediaServer(new Dictionary<string, byte[]> { ["first.wav"] = wave, ["second.wav"] = wave });
        using var engine = NativeHost.Create();
        engine.ConfigurePlaybackEffects(new AudioSettings { Crossfade = true, CrossfadeManualNavigation = true, CrossfadeMilliseconds = 100 });
        Assert.IsTrue(engine.Load(server.Url("first.wav"), noVideo: true));
        await NativeHost.Until(() => !engine.IsLoading && !engine.IsPaused && engine.Elapsed > 0.02);
        Assert.IsTrue(engine.Load(server.Url("second.wav"), noVideo: true, fadeTransition: true));
        await NativeHost.Until(() => !engine.IsLoading && engine.Duration > 0
            && NativeHost.Handle(engine).GetProperty("path")?.ToString() == server.Url("second.wav"));
        Assert.AreEqual(1, server.Requests("second.wav"), "An explicitly audio-only transition needs only the playback stream.");
    }

    private sealed class Resolver(MediaServer server) : IMediaResolver
    {
        public bool Handles(string address) => Reference.IsAddress(address);
        public Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
            => Task.FromResult(new MediaInput(server.Url(Reference.Parse(address)!.Id), MediaTags.None, 10));
    }
}
