using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class NativeSofaTests
{
    [TestMethod]
    public async Task SofaRendersStereoAndSurvivesSeekAndReload()
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        using var session = new MpvSession(0, new Lock(), new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
        var audio = new AudioProcessor(session);
        var settings = new AudioSettings
        {
            SofaEnabled = true, SofaFile = SofaFixture.Write(workspace, "HRTF's [test], semi;.sofa"),
            SofaGain = -2.5, SofaRotation = 30, SofaElevation = 10,
        };
        var graph = AudioFilters.SofaGraph(settings);
        Assert.IsTrue(audio.SetNormalization(true));
        Assert.IsTrue(audio.SetSofa(graph));
        Assert.IsTrue(session.TrySetProperty("pause", true));
        var media = NativeHost.Wave(workspace);
        Assert.IsTrue(session.TryDo(mpv => mpv.Command("loadfile", media)));
        await NativeHost.Until(() => session.ReadDouble("audio-out-params/channel-count") == 2);
        CollectionAssert.AreEqual(new[] { AudioFilters.SofaLabel, "audionormalize" }, FilterLabels(session));
        Assert.IsTrue(audio.SetSofa(graph));
        CollectionAssert.AreEqual(new[] { AudioFilters.SofaLabel, "audionormalize" }, FilterLabels(session));
        Assert.IsTrue(session.TryDo(mpv => mpv.Command("seek", "2", "absolute+exact")));
        Assert.IsTrue(session.TrySetProperty("pause", false));
        await NativeHost.Until(() => session.ReadDouble("time-pos") is > 2);
        Assert.IsTrue(session.TrySetProperty("pause", true));
        Assert.IsTrue(session.TryDo(mpv => mpv.Command("loadfile", media)));
        await NativeHost.Until(() => session.ReadDouble("time-pos") is < 0.5
            && session.ReadDouble("audio-out-params/channel-count") == 2);
        CollectionAssert.AreEqual(new[] { AudioFilters.SofaLabel, "audionormalize" }, FilterLabels(session));
    }

    [TestMethod]
    public async Task RejectedReplacementRetainsTheFilterAndDisablingRemovesIt()
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        using var session = new MpvSession(0, new Lock(), new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
        var audio = new AudioProcessor(session);
        var settings = new AudioSettings { SofaEnabled = true, SofaFile = SofaFixture.Write(workspace) };
        Assert.IsTrue(audio.SetSofa(AudioFilters.SofaGraph(settings)));
        Assert.IsTrue(session.TrySetProperty("pause", true));
        Assert.IsTrue(session.TryDo(mpv => mpv.Command("loadfile", NativeHost.Wave(workspace))));
        await NativeHost.Until(() => session.ReadDouble("audio-out-params/channel-count") == 2);
        settings.SofaFile = workspace.File("invalid.sofa", "not SOFA data");
        Assert.IsFalse(audio.SetSofa(AudioFilters.SofaGraph(settings)));
        CollectionAssert.AreEqual(new[] { AudioFilters.SofaLabel }, FilterLabels(session));
        Assert.IsTrue(audio.SetSofa(string.Empty));
        Assert.IsEmpty(FilterLabels(session));
        Assert.IsTrue(session.TrySetProperty("pause", false));
        await NativeHost.Until(() => session.ReadDouble("time-pos") is > 0.1);
    }

    private static string[] FilterLabels(MpvSession session)
        => (session.ReadObject("af") as IEnumerable<object?> ?? [])
            .OfType<IDictionary<string, object?>>()
            .Select(filter => filter.TryGetValue("label", out var label) ? label as string ?? string.Empty : string.Empty).ToArray();
}
