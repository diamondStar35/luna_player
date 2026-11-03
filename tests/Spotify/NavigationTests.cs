using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Spotify.Playback;

namespace LunaPlayer.Tests.Spotify;

public sealed partial class CrossfadeTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task AbsoluteNavigationHonorsTheRequestedIndexAfterNativeAdvancement(int target)
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, Options(manual: true));
        var tracks = new[] { Track(1), Track(2), Track(3), Track(4) };
        sessions.Play(tracks[0], tracks, default);
        await AwaitNativeAdvance(engine, dispatcher);

        Assert.IsTrue(sessions.GoToIndex(target));
        Assert.AreEqual(target, sessions.CurrentIndex);
        Assert.AreSame(tracks[target], sessions.CurrentTrack);
        await Until(dispatcher, () => !engine.IsLoading && player.CurrentSource == sessions.CurrentLink);
    }

    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(1, 2)]
    public async Task RelativeNavigationStartsFromTheTrackAlreadyPlaying(int direction, int target)
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, Options(manual: true));
        var tracks = new[] { Track(1), Track(2), Track(3), Track(4) };
        sessions.Play(tracks[0], tracks, default);
        await AwaitNativeAdvance(engine, dispatcher);

        Assert.IsTrue(direction > 0 ? sessions.Next() : sessions.Previous());
        Assert.AreEqual(target, sessions.CurrentIndex);
        Assert.AreSame(tracks[target], sessions.CurrentTrack);
        await Until(dispatcher, () => !engine.IsLoading && player.CurrentSource == sessions.CurrentLink);
    }

    [TestMethod]
    public async Task RemovingCurrentAfterNativeAdvancementRemovesTheTrackAlreadyPlaying()
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, Options());
        var tracks = new[] { Track(1), Track(2), Track(3), Track(4) };
        sessions.Play(tracks[0], tracks, default);
        await AwaitNativeAdvance(engine, dispatcher);

        Assert.IsTrue(sessions.CloseCurrent());
        CollectionAssert.AreEqual(new[] { tracks[0], tracks[2], tracks[3] }, sessions.CurrentQueue.ToArray());
        Assert.AreSame(tracks[2], sessions.CurrentTrack);
        await Until(dispatcher, () => !engine.IsLoading && player.CurrentSource == sessions.CurrentLink);
    }

    [TestMethod]
    public async Task PauseAdoptsTheTrackAlreadyPlayingBeforeChangingItsState()
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, Options());
        var tracks = new[] { Track(1), Track(2), Track(3) };
        sessions.Play(tracks[0], tracks, default);
        await AwaitNativeAdvance(engine, dispatcher);

        sessions.Pause();
        Assert.AreSame(tracks[1], sessions.CurrentTrack);
        Assert.AreEqual(1, sessions.CurrentIndex);
        await Until(dispatcher, () => player.IsPaused);
        Assert.IsFalse(sessions.IsPlaying);
        sessions.Resume();
        await Until(dispatcher, () => player.IsPlaying);
        Assert.AreSame(tracks[1], sessions.CurrentTrack);
    }

    [TestMethod]
    public async Task StopSavesThePositionOfTheTrackAlreadyPlaying()
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        var options = Options();
        options.SaveFilePositions = true;
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, options);
        var positions = new Dictionary<string, double>();
        sessions.ConfigurePlayback(options, _ => null, (link, position) => positions[link] = position);
        var tracks = new[] { Track(1), Track(2), Track(3) };
        sessions.Play(tracks[0], tracks, default);
        await AwaitNativeAdvance(engine, dispatcher);

        sessions.Stop();
        Assert.IsTrue(positions.ContainsKey("https://open.spotify.com/track/0000000000000000000002"));
        Assert.AreEqual(0, positions["https://open.spotify.com/track/0000000000000000000001"]);
        Assert.IsFalse(player.InSpotifySession);
        dispatcher.Drain();
        Assert.IsFalse(sessions.IsActive);
    }

    [TestMethod]
    public async Task ShuffleKeepsTheTrackAlreadyPlayingAtItsNewIndex()
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, Options());
        var tracks = new[] { Track(1), Track(2), Track(3), Track(4) };
        sessions.Play(tracks[0], tracks, default);
        await AwaitNativeAdvance(engine, dispatcher);

        Assert.IsTrue(sessions.ToggleShuffle());
        Assert.AreSame(tracks[1], sessions.CurrentTrack);
        Assert.AreSame(tracks[1], sessions.CurrentQueue[sessions.CurrentIndex]);
        Assert.IsFalse(sessions.ToggleShuffle());
        Assert.AreEqual(1, sessions.CurrentIndex);
        CollectionAssert.AreEqual(tracks, sessions.CurrentQueue.ToArray());
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task RapidAbsoluteNavigationLoadsTheLastSelection(bool manual, bool paused)
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tracks = new[] { Track(1), Track(2), Track(3), Track(4) };
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, async (uri, _, token) =>
        {
            if (uri != tracks[0].Uri) await ready.Task.WaitAsync(token);
            return Audio();
        });
        var errors = new List<Exception>();
        sessions.CommandFailed += errors.Add;
        Configure(player, sessions, Options(manual));
        sessions.Play(tracks[0], tracks, default);
        await Until(dispatcher, () => !engine.IsLoading && player.IsPlaying);
        if (paused) sessions.Pause();
        Assert.IsTrue(sessions.GoToIndex(3));
        Assert.IsTrue(sessions.GoToIndex(1));
        Assert.IsTrue(sessions.GoToIndex(2));
        if (paused) sessions.Pause();
        ready.SetResult();

        await Until(dispatcher, () => !engine.IsLoading && player.CurrentSource == sessions.CurrentLink);
        Assert.AreSame(tracks[2], sessions.CurrentTrack);
        Assert.AreEqual(2, sessions.CurrentIndex);
        Assert.AreEqual(paused, player.IsPaused);
        Assert.HasCount(0, errors);
    }

    [TestMethod]
    [DataRow(false, 120d, 11d)]
    [DataRow(true, 120d, 11d)]
    [DataRow(false, -2d, 0d)]
    [DataRow(true, -2d, 0d)]
    [DataRow(false, double.NaN, 0d)]
    [DataRow(true, double.NaN, 0d)]
    public async Task PreparedAndOrdinaryLoadsUseTheSameResumeBounds(bool prepare, double saved, double expected)
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        var options = Options(manual: true);
        options.Crossfade = prepare;
        options.SaveFilePositions = true;
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, options);
        var tracks = new[] { Track(1), Track(2) };
        sessions.ConfigurePlayback(options, link => link.EndsWith("2", StringComparison.Ordinal) ? saved : 0, (_, _) => { });
        sessions.Play(tracks[0], tracks, default);
        sessions.Pause();
        await Until(dispatcher, () => !engine.IsLoading && engine.Duration is > 0 && (!prepare || HasSuccessor(engine)));
        Assert.IsTrue(sessions.Next());
        sessions.Pause();
        await Until(dispatcher, () => !engine.IsLoading && player.CurrentSource == sessions.CurrentLink && engine.Elapsed is not null);

        Assert.AreSame(tracks[1], sessions.CurrentTrack);
        Assert.AreEqual(expected, engine.Elapsed!.Value, 0.2);
        Assert.IsTrue(player.IsPaused);
    }

    [TestMethod]
    public async Task SleepTimerEndingTheSessionPreventsNavigationIntoTheRestoredPlaylist()
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, Options());
        var tracks = new[] { Track(1), Track(2), Track(3) };
        sessions.Play(tracks[0], tracks, default);
        await AwaitNativeAdvance(engine, dispatcher);
        sessions.BeforeTrackEnd = () => { sessions.Stop(); return true; };

        Assert.IsFalse(sessions.GoToIndex(2));
        Assert.IsFalse(sessions.IsActive);
        Assert.IsFalse(player.InSpotifySession);
        Assert.IsNull(sessions.CurrentTrack);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(4)]
    public async Task InvalidAbsoluteTargetsKeepTheCurrentTrack(int target)
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, Options());
        var tracks = new[] { Track(1), Track(2), Track(3), Track(4) };
        sessions.Play(tracks[0], tracks, default);
        await AwaitNativeAdvance(engine, dispatcher);

        Assert.IsFalse(sessions.GoToIndex(target));
        Assert.AreSame(tracks[1], sessions.CurrentTrack);
        Assert.AreEqual(1, sessions.CurrentIndex);
    }

    private static async Task AwaitNativeAdvance(Engine engine, Dispatcher dispatcher)
    {
        await Until(dispatcher, () => HasSuccessor(engine) && !engine.IsLoading);
        engine.SeekAbsolute(11);
        // Leave the UI callback pending to exercise the interval in which mpv is one entry ahead.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (Field(engine, "_completedAdvance") is null) await Task.Delay(10, timeout.Token);
    }
}
