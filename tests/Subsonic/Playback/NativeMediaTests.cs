using LunaPlayer.Playback;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Tests.Infrastructure;
using MpvNet;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, DoNotParallelize, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class NativeMediaTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task QueuedLoadsKeepTheirOwnHeadersAndLocalLoadsClearThem(bool crossfade)
    {
        using var workspace = new Workspace(); using var engine = NativeHost.Create();
        engine.ConfigurePlaybackEffects(new AudioSettings { GaplessPlayback = GaplessPlaybackMode.AlwaysWhenPossible,
            Crossfade = crossfade, CrossfadeMilliseconds = 100 });
        var first = NativeHost.Wave(workspace); var second = NativeHost.Wave(workspace, "second.wav");
        Assert.IsTrue(engine.Load(first, paused: true, itemId: "first", headers: new Dictionary<string, string> { ["X-First"] = "a,b=c %25", ["X-Trailing"] = @"slash\" }));
        await NativeHost.Until(() => !engine.IsLoading && engine.Duration is > 0);
        var coordinator = NativeFields.Read(typeof(Engine), engine, "_coordinator")!;
        var session = NativeFields.Read(typeof(PlaybackCoordinator), coordinator, "_session")!;
        var mpv = (MPV)NativeFields.Read(typeof(MpvSession), session, "_mpv")!;
        CollectionAssert.AreEqual(new[] { "X-First: a,b=c %25\r\nX-Trailing: slash\\" }, Headers(mpv), "Actual: " + string.Join("|", Headers(mpv)) + "; type " + mpv.GetProperty("http-header-fields")?.GetType().Name);
        Assert.IsTrue(engine.QueueNext(second, itemId: "second", headers: new Dictionary<string, string> { ["X-Second"] = "private" }));
        await NativeHost.Until(() => Convert.ToInt64(mpv.GetProperty("playlist/count")) > 1);
        CollectionAssert.AreEqual(new[] { "X-First: a,b=c %25\r\nX-Trailing: slash\\" }, Headers(mpv), "Actual: " + string.Join("|", Headers(mpv)) + "; type " + mpv.GetProperty("http-header-fields")?.GetType().Name);
        engine.SeekAbsolute(9.8); engine.Play();
        QueuedAdvance? advance = null;
        await NativeHost.Until(() => (advance = engine.TakeQueuedAdvance()) is not null);
        Assert.AreEqual("second", advance!.Value.ItemId);
        CollectionAssert.AreEqual(new[] { "X-Second: private" }, Headers(mpv));
        engine.Load(first, paused: true);
        await NativeHost.Until(() => !engine.IsLoading && engine.IsPaused && Headers(mpv).Length == 0);
    }
    private static string[] Headers(MPV mpv) => mpv.GetProperty("http-header-fields") is IEnumerable<object?> values
        ? values.Select(x => x?.ToString() ?? "").ToArray() : [];
}
