using System.Collections.Concurrent;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class UnavailableTests
{
    [TestMethod]
    public async Task MissingSongsAreSkippedInBothDirectionsUntilExplicitRefresh()
    {
        using var rig = new Rig(); rig.Resolver.Missing = "gone";
        rig.Open(["a", "gone", "b"]); await rig.At("a");
        rig.Player.Next(false); await rig.At("b");
        Assert.AreEqual(1, rig.Resolver.Calls["gone"]);
        rig.Player.Previous(false); await rig.At("a");
        Assert.AreEqual(1, rig.Resolver.Calls["gone"]);
        rig.Player.RetryServerMedia(rig.Server); rig.Resolver.Missing = null;
        rig.Player.Next(false); await rig.At("gone");
        Assert.AreEqual(2, rig.Resolver.Calls["gone"]);
    }

    [TestMethod]
    public async Task AuthenticationFailureIsNotTreatedAsDeletedMedia()
    {
        using var rig = new Rig(); rig.Resolver.Missing = "auth"; rig.Resolver.Authentication = true;
        var failures = 0; rig.Player.MediaResolutionFailed += _ => failures++;
        rig.Open(["a", "auth", "b"]); await rig.At("a"); rig.Player.Next(false);
        await rig.Pump.Until(() => failures > 0);
        Assert.AreEqual("auth", Reference.Parse(rig.Player.CurrentPath)!.Id);
        Assert.IsFalse(rig.Player.IsMediaUnavailable(rig.Address("auth")));
        Assert.IsFalse(rig.Resolver.Calls.ContainsKey("b"));
    }

    [TestMethod]
    public async Task AllMissingSongsStopWithoutAnEndlessWrapLoop()
    {
        using var rig = new Rig(); rig.Resolver.Missing = "*";
        rig.Player.ConfigurePlaybackEffects(new AudioSettings { WrapPlaylist = true });
        var failures = 0; rig.Player.MediaResolutionFailed += _ => failures++;
        rig.Open(["a", "b"]); await rig.Pump.Until(() => failures > 0);
        Assert.AreEqual(2, rig.Resolver.Calls.Values.Sum()); Assert.AreEqual(1, failures);
        Assert.IsFalse(rig.Player.IsResolvingMedia); Assert.IsFalse(rig.Player.IsLoaded);
    }

    [TestMethod]
    public async Task KnownMissingPrefetchIsNotRepeatedWhenPlaybackSettingsChange()
    {
        using var rig = new Rig(); rig.Resolver.Missing = "gone";
        rig.Player.ConfigurePlaybackEffects(new AudioSettings { Crossfade = true });
        rig.Open(["a", "gone", "b"]); await rig.At("a");
        await rig.Pump.Until(() => rig.Player.IsMediaUnavailable(rig.Address("gone")));
        rig.Player.ConfigurePlaybackEffects(new AudioSettings { Crossfade = true });
        rig.Player.ConfigurePlaybackEffects(new AudioSettings { Crossfade = true });
        Assert.AreEqual(1, rig.Resolver.Calls["gone"]);
        rig.Player.Next(false); await rig.At("b");
        Assert.AreEqual(1, rig.Resolver.Calls["gone"]);
    }

    private sealed class Rig : IDisposable
    {
        internal readonly Guid Server = Guid.NewGuid();
        internal readonly Pump Pump = new();
        internal readonly Resolver Resolver = new();
        internal readonly Player Player = new(new FakeEngine());
        internal Rig() => Player.SetMediaResolver(Resolver, Pump.Post);
        internal string Address(string id) => new Reference(Server, id).Address;
        internal void Open(string[] ids) => Player.InsertMedia(ids.Select(id => (Address(id), MediaTags.None)).ToArray(), true);
        internal Task At(string id) => Pump.Until(() => Player.IsLoaded && Reference.Parse(Player.CurrentPath)?.Id == id);
        public void Dispose() => Player.Dispose();
    }
    private sealed class Resolver : IMediaResolver
    {
        internal readonly ConcurrentDictionary<string, int> Calls = [];
        internal string? Missing;
        internal bool Authentication;
        public bool Handles(string address) => Reference.IsAddress(address);
        public Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
        {
            var id = Reference.Parse(address)!.Id; Calls.AddOrUpdate(id, 1, (_, count) => count + 1);
            if (Missing == id || Missing == "*") throw Authentication
                ? new RequestException(FailureKind.Forbidden, "getSong", System.Net.HttpStatusCode.Unauthorized, 40)
                : new RequestException(FailureKind.Rejected, "getSong", code: 70);
            return Task.FromResult(new MediaInput("https://invalid/" + id, new MediaTags(id, "", "")));
        }
    }
}
