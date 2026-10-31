using System.Collections.Concurrent;
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
        settings.SofaGain = -5;
        var replacement = AudioFilters.SofaGraph(settings);
        Assert.IsTrue(audio.SetSofa(replacement));
        await NativeHost.Until(() => FilterDefinition(session) == replacement);
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
        var failure = new TaskCompletionSource<AudioFilter>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.FilterFailed += filter => failure.TrySetResult(filter);
        Assert.IsTrue(audio.SetSofa(AudioFilters.SofaGraph(settings)));
        Assert.AreEqual(AudioFilter.Spatialization, await failure.Task.WaitAsync(TimeSpan.FromSeconds(8)));
        await NativeHost.Until(() => FilterLabels(session).Length == 1);
        CollectionAssert.AreEqual(new[] { AudioFilters.SofaLabel }, FilterLabels(session));
        Assert.IsTrue(audio.SetSofa(string.Empty));
        await NativeHost.Until(() => FilterLabels(session).Length == 0);
        Assert.IsTrue(session.TrySetProperty("pause", false));
        await NativeHost.Until(() => session.ReadDouble("time-pos") is > 0.1);
    }

    [TestMethod]
    public async Task RuntimeDirectionsPreserveTheFilterDefinitionAcrossSeeks()
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        using var session = new MpvSession(0, new Lock(), new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
        var audio = new AudioProcessor(session);
        var errors = new ConcurrentQueue<AudioFilter>();
        session.FilterFailed += errors.Enqueue;
        session.OnEvent(_ => audio.ReplaySofaDirection(), MpvNet.MpvEventId.PlaybackRestart);
        session.OnEvent(_ => audio.ReplaySofaDirection(), MpvNet.MpvEventId.AudioReconfig);
        var graph = AudioFilters.SofaGraph(new AudioSettings { SofaEnabled = true, SofaFile = SofaFixture.Write(workspace) });
        Assert.IsTrue(audio.SetSofa(graph));
        session.TrySetProperty("pause", true);
        session.TryDo(mpv => mpv.Command("loadfile", NativeHost.Wave(workspace)));
        await NativeHost.Until(() => session.ReadDouble("audio-out-params/channel-count") == 2);
        Assert.IsTrue(audio.SetSofaDirection(-45, 90));
        await Checkpoint();
        Assert.IsEmpty(errors);
        Assert.AreEqual(graph, FilterDefinition(session));
        session.TryDo(mpv => mpv.Command("seek", "2", "absolute+exact"));
        await NativeHost.Until(() => session.ReadDouble("time-pos") is > 1.8);
        await Checkpoint();
        Assert.IsEmpty(errors);
        Assert.AreEqual(graph, FilterDefinition(session));
        Assert.IsTrue(audio.SetSofaDirection(45, -90));
        await Checkpoint();
        Assert.IsEmpty(errors);
        Assert.AreEqual(graph, FilterDefinition(session));

        async Task Checkpoint()
        {
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            session.TryDo(_ => done.TrySetResult());
            await done.Task.WaitAsync(TimeSpan.FromSeconds(8));
        }
    }

    private static string? FilterDefinition(MpvSession session)
        => (session.ReadObject("af") as IEnumerable<object?> ?? [])
            .OfType<IDictionary<string, object?>>()
            .Where(filter => filter.TryGetValue("label", out var label) && label as string == AudioFilters.SofaLabel)
            .Select(filter => filter.TryGetValue("params", out var parameters)
                && parameters is IDictionary<string, object?> values && values.TryGetValue("graph", out var value) ? value as string : null)
            .SingleOrDefault();

    [TestMethod]
    public async Task DirectionChangesDuringRepeatedSeeksDoNotDisableSpatialization()
    {
        NativeHost.Initialize();
        using var workspace = new Workspace();
        using var session = new MpvSession(0, new Lock(), new Dictionary<string, object?> { ["ao"] = "null", ["vo"] = "null" });
        var audio = new AudioProcessor(session);
        var errors = new ConcurrentQueue<AudioFilter>();
        session.FilterFailed += errors.Enqueue;
        session.OnEvent(_ => audio.ReplaySofaDirection(), MpvNet.MpvEventId.PlaybackRestart);
        audio.SetSofa(AudioFilters.SofaGraph(new() { SofaEnabled = true, SofaFile = SofaFixture.Write(workspace) }));
        session.TrySetProperty("pause", true);
        session.TryDo(mpv => mpv.Command("loadfile", NativeHost.Wave(workspace)));
        await NativeHost.Until(() => session.ReadDouble("audio-out-params/channel-count") == 2);
        for (var i = 1; i <= 5; i++)
        {
            var target = i;
            session.TryDo(mpv => mpv.Command("seek", target, "absolute+exact"));
            Assert.IsTrue(audio.SetSofaDirection(-i * 5, i * 5));
            await NativeHost.Until(() => session.ReadDouble("time-pos") is double position && Math.Abs(position - target) < 0.1);
        }
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.TryDo(_ => completed.SetResult());
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(8));
        Assert.IsEmpty(errors);
        CollectionAssert.Contains(FilterLabels(session), AudioFilters.SofaLabel);
    }

    private static string[] FilterLabels(MpvSession session)
        => (session.ReadObject("af") as IEnumerable<object?> ?? [])
            .OfType<IDictionary<string, object?>>()
            .Select(filter => filter.TryGetValue("label", out var label) ? label as string ?? string.Empty : string.Empty).ToArray();
}
