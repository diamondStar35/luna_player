using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Spotify;
using LunaPlayer.Spotify.Playback;
using MpvNet;

namespace LunaPlayer.Tests.Spotify;

public sealed partial class CrossfadeTests
{
    [TestMethod]
    public async Task SeekReplacesPreparationFromThePreviousGeneration()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        var oldPreparation = new TaskCompletionSource<AudioDecryptStream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var newPreparation = new TaskCompletionSource<AudioDecryptStream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var opens = 0; CancellationToken abandoned = default;
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, token) =>
        {
            var call = Interlocked.Increment(ref opens);
            if (call == 1) return Task.FromResult(Audio());
            if (call == 2) { abandoned = token; return oldPreparation.Task; }
            return newPreparation.Task;
        });
        Configure(player, sessions, Options());
        var tracks = new[] { Track(1), Track(2) };
        sessions.Play(tracks[0], tracks, default);
        await Until(dispatcher, () => Volatile.Read(ref opens) == 2 && !engine.IsLoading && engine.Duration is > 0);
        sessions.Seek(2);
        await Until(dispatcher, () => Volatile.Read(ref opens) == 3);
        Check(abandoned.IsCancellationRequested, "Seeking retained preparation owned by an older selection generation.");
        var stale = Audio();
        oldPreparation.SetResult(stale);
        newPreparation.SetResult(Audio());
        await Until(dispatcher, () => HasSuccessor(engine) && !stale.CanRead);
        Check(sessions.CurrentIndex == 0, "Replacing speculative preparation navigated away from the current track.");
    }


    [TestMethod]
    public Task PlaybackSettingsSurviveLeavingSpotify()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var audio = Options();
        player.ConfigurePlaybackEffects(audio);
        player.BeginSpotifySession();
        Check((bool)Field(engine, "_crossfade")! && (int)Field(engine, "_crossfadeMilliseconds")! == 500,
            "Spotify did not use local crossfade settings.");
        audio.Crossfade = false; player.ConfigurePlaybackEffects(audio);
        Check(!(bool)Field(engine, "_crossfade")!, "Disabling shared crossfade did not disable Spotify.");
        audio.Crossfade = true; audio.CrossfadeMilliseconds = 2222; player.ConfigurePlaybackEffects(audio);
        player.LeaveSession();
        Check((bool)Field(engine, "_crossfade")! && (int)Field(engine, "_crossfadeMilliseconds")! == 2222,
            "Leaving Spotify changed shared settings.");
        var settings = new Settings { Audio = audio };
        var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        var restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings)!;
        Check(restored.Audio.Crossfade && restored.Audio.CrossfadeMilliseconds == 2222, "Shared settings did not round-trip.");
        Check(!json.Split("\"spotify\"")[1].Contains("crossfade", StringComparison.OrdinalIgnoreCase), "Spotify-specific crossfade settings remain.");
        var older = JsonSerializer.Deserialize("{\"audio\":{\"crossfade\":true,\"crossfadeMilliseconds\":2222},\"spotify\":{\"crossfade\":false,\"crossfadeMilliseconds\":9999}}", SettingsJsonContext.Default.Settings)!;
        Check(older.Audio.Crossfade && older.Audio.CrossfadeMilliseconds == 2222, "Obsolete Spotify fields overrode the shared controls.");
        return Task.CompletedTask;
    }

    [TestMethod]
    public Task UnclaimedAudioIsReleasedWithoutAffectingOtherRegistrations()
    {
        using var engine = NewEngine();
        var first = Audio(); var second = Audio();
        var firstUrl = engine.RegisterSpotifyAudio(first);
        var secondUrl = engine.RegisterSpotifyAudio(second);
        Check(first.CanRead && second.CanRead, "New registration disposed another pending stream.");
        engine.ReleaseSpotifyAudio(firstUrl);
        Check(!first.CanRead && second.CanRead, "Release affected another stream.");
        engine.ReleaseSpotifyAudio(secondUrl);
        Check(!second.CanRead, "Unclaimed stream was retained.");
        var abandoned = Audio(); engine.RegisterSpotifyAudio(abandoned);
        engine.Stop();
        Check(!abandoned.CanRead, "Stop retained an unopened Spotify stream.");
        return Task.CompletedTask;
    }

    [TestMethod]
    public Task DecoderReadersHaveIndependentPositionsAndCancellation()
    {
        using var engine = NewEngine();
        var audio = Audio(); var url = engine.RegisterSpotifyAudio(audio);
        IMpvStream Open() => (IMpvStream)typeof(PlaybackCoordinator).GetMethod("OpenSpotifyStream", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Field(engine, "_coordinator"), [url])!;
        using var first = Open();
        using var second = Open();
        var a = new byte[128]; var b = new byte[128];
        first.Read(a, 0, a.Length); second.Read(b, 0, b.Length);
        Check(a.SequenceEqual(b), "Reopened readers do not have independent positions.");
        first.Cancel();
        using var third = Open();
        third.Read(b, 0, b.Length);
        Check(a.SequenceEqual(b), "Prefetch cancellation invalidated a later open.");
        engine.ReleaseSpotifyAudio(url);
        Check(audio.CanRead, "Registration release disposed an active decoder.");
        first.Dispose(); second.Dispose(); third.Dispose();
        Check(!audio.CanRead, "Final decoder close retained retired audio.");
        return Task.CompletedTask;
    }

    [TestMethod]
    [DataRow((int)CrossfadeCurve.EqualPower)]
    [DataRow((int)CrossfadeCurve.Linear)]
    public async Task AutomaticTransitionsReusePreparedAudio(int curve)
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options();
        options.CrossfadeCurve = (CrossfadeCurve)curve;
        var opens = new ConcurrentDictionary<string, int>();
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (uri, _, _) =>
        {
            opens.AddOrUpdate(uri, 1, (_, count) => count + 1);
            return Task.FromResult(Audio());
        });
        Configure(player, sessions, options);
        engine.Ended += reason => { var generation = sessions.CurrentLoadGeneration; dispatcher.Post(() => sessions.HandleEnded(reason, generation)); };
        var tracks = new[] { Track(1), Track(2) };
        sessions.Play(tracks[0], tracks, default);
        await Until(dispatcher, () => HasSuccessor(engine) && engine.Duration is > 0);
        await Until(dispatcher, () => (bool)Field(engine, "_queuedNext")!);
        engine.SeekAbsolute(11);
        await Until(dispatcher, () => sessions.CurrentIndex == 1);
        Check(player.CurrentSource == sessions.CurrentLink, "Player and Spotify session disagree after transition.");
        Check(opens[tracks[1].Uri] == 1, "Queued transition reopened incoming audio.");
        Check(((System.Collections.IDictionary)Field(engine, "_spotifyAudio")!).Count == 1,
            "Automatic advancement retained outgoing registrations.");
        sessions.HandleEnded(EndReason.QueuedAdvance, sessions.CurrentLoadGeneration);
        Check(sessions.CurrentIndex == 1 && opens[tracks[1].Uri] == 1, "Duplicate transition changed the queue.");
    }

    [TestMethod]
    public async Task RapidNavigationKeepsOutgoingAudioUntilReplacementIsReady()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options(manual: true);
        CancellationToken outgoing = default;
        var destinationReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, async (uri, _, token) =>
        {
            if (uri == Track(1).Uri) outgoing = token;
            else await destinationReady.Task.WaitAsync(token);
            return Audio();
        });
        Configure(player, sessions, options);
        var tracks = new[] { Track(1), Track(2), Track(3), Track(4) };
        sessions.Play(tracks[0], tracks, default);
        await Until(dispatcher, () => engine.Duration is > 0 && player.IsPlaying);
        var oldPath = player.CurrentPath;
        sessions.Next();
        sessions.Next();
        Check(!outgoing.IsCancellationRequested, "Navigation cancelled outgoing audio before mpv released it.");
        Check(player.CurrentPath == oldPath && player.IsPlaying, "Manual navigation stopped outgoing audio before preparation.");
        destinationReady.SetResult();
        await Until(dispatcher, () => player.CurrentSource == sessions.CurrentLink && player.CurrentPath != oldPath);
        await Until(dispatcher, () => !engine.IsLoading);
        sessions.Pause();
        sessions.Next();
        await Until(dispatcher, () => player.CurrentSource == sessions.CurrentLink);
        Check(player.IsPaused && !sessions.IsPlaying, "Paused navigation started playback.");
    }

    [TestMethod]
    public async Task PreparedSuccessorOpensWithoutAnotherFetch()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options(manual: true);
        var tracks = new[] { Track(1), Track(2) };
        var fetches = 0; var errors = 0;
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) =>
        {
            Interlocked.Increment(ref fetches);
            return Task.FromResult(Audio());
        });
        sessions.CommandFailed += _ => errors++;
        Configure(player, sessions, options);
        sessions.Play(tracks[0], tracks, default);
        await Until(dispatcher, () => HasSuccessor(engine) && !engine.IsLoading);
        Check(fetches == 2, "Preparation fetched more than one successor.");
        sessions.Next();
        Check(player.CurrentSource == sessions.CurrentLink && sessions.CurrentIndex == 1,
            "Prepared navigation was delayed instead of opening immediately.");
        await Until(dispatcher, () => !engine.IsLoading && engine.Duration is > 0);
        Check(fetches == 2 && errors == 0, "Prepared navigation fetched again or failed to reopen prefetched audio.");
    }

    [TestMethod]
    public async Task ConsecutiveSkipsIgnoreSupersededDecoderFailures()
    {
        foreach (var delay in new[] { 0, 20, 80, 200, 500 })
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options(manual: true);
            options.CrossfadeMilliseconds = 3000;
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
            Configure(player, sessions, options);
            var errors = new ConcurrentQueue<Exception>();
            sessions.CommandFailed += errors.Enqueue;
            engine.Ended += reason => { var generation = sessions.CurrentLoadGeneration; dispatcher.Post(() => sessions.HandleEnded(reason, generation)); };
            var tracks = new[] { Track(1), Track(2), Track(3), Track(4) };
            sessions.Play(tracks[0], tracks, default);
            await Until(dispatcher, () => errors.Count > 0 || HasSuccessor(engine) && !engine.IsLoading);
            Check(errors.IsEmpty, "Initial load before double Next failed at " + delay + "ms: " + string.Join("; ", errors.Select(error => error.GetType().Name + " " + error.StackTrace)));
            sessions.Next();
            await Task.Delay(delay);
            dispatcher.Drain();
            sessions.Next();
            await Until(dispatcher, () => errors.Count > 0 || sessions.CurrentIndex == 2 && player.CurrentSource == sessions.CurrentLink && !engine.IsLoading);
            Check(errors.IsEmpty, "Double Next failed at " + delay + "ms: " + string.Join("; ", errors.Select(error => error.GetType().Name + " " + error.StackTrace)));
            await Task.Delay(150); dispatcher.Drain();
            Check(errors.IsEmpty, "Late decoder error after double Next at " + delay + "ms.");
        }
    }

    [TestMethod]
    public async Task CancellingOneDecoderDoesNotCancelAnother()
    {
        using var engine = NewEngine();
        using var handler = new HeldRange(); using var http = new HttpClient(handler);
        var buffered = await BufferedAudioStream.OpenAsync(http, ["https://audio.test/track"], default);
        var audio = new AudioDecryptStream(buffered, new byte[16]);
        var url = engine.RegisterSpotifyAudio(audio);
        IMpvStream Open() => (IMpvStream)typeof(PlaybackCoordinator).GetMethod("OpenSpotifyStream", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Field(engine, "_coordinator"), [url])!;
        using var first = Open(); using var second = Open();
        first.Seek(131072);
        var reading = Task.Run(() => first.Read(new byte[128], 0, 128));
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        first.Cancel();
        try { await reading.WaitAsync(TimeSpan.FromSeconds(2)); throw new InvalidOperationException("Cancelled read returned successfully."); }
        catch (OperationCanceledException) { }
        Check(second.Read(new byte[128], 0, 128) == 128, "One decoder cancelled the entire backing stream.");
        engine.ReleaseSpotifyAudio(url);
    }

    [TestMethod]
    public async Task PrefetchingAnotherPageDoesNotNavigate()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options();
        var page = new TaskCompletionSource<TrackPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, options);
        var first = Track(1); var second = Track(2);
        sessions.Play(first, [first], default, pageLoader: (_, _) => page.Task, nextPage: "next");
        await Until(dispatcher, () => sessions.CurrentTrack == first && engine.Duration is > 0);
        page.SetResult(new TrackPage([second], null));
        await Until(dispatcher, () => sessions.CurrentQueue.Count == 2);
        Check(sessions.CurrentTrack == first && sessions.CurrentIndex == 0, "Speculative page loading navigated.");
    }

    [TestMethod]
    public async Task CancelledLoadDisposesLateAudio()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options();
        var pending = new TaskCompletionSource<AudioDecryptStream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => { started.TrySetResult(); return pending.Task; });
        Configure(player, sessions, options);
        sessions.Play(Track(1), null, default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        sessions.Stop();
        var audio = Audio(); pending.SetResult(audio);
        await Until(dispatcher, () => !audio.CanRead);
        Check(!player.InSpotifySession, "Stale result reentered Spotify.");
    }

    [TestMethod]
    public async Task RepeatAndStopModesDoNotQueueSuccessors()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options();
        var audio = Options(); audio.EndBehavior = EndBehavior.None;
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, options, audio);
        var tracks = new[] { Track(1), Track(2) };
        sessions.Play(tracks[0], tracks, default);
        await Until(dispatcher, () => engine.Duration is > 0);
        Check(!HasSuccessor(engine), "Stop-at-end prepared an advancing successor.");
        audio.EndBehavior = EndBehavior.Advance; player.SetEndBehavior(EndBehavior.Advance);
        await Until(dispatcher, () => HasSuccessor(engine));
        sessions.ToggleRepeatCurrent();
        await Until(dispatcher, () => !HasSuccessor(engine));
        Check(!HasSuccessor(engine) && !(bool)Field(engine, "_queuedNext")!, "Repeat retained a successor.");
        player.HoldAutomaticAdvance(true);
        sessions.ToggleRepeatCurrent();
        await Task.Delay(100); dispatcher.Drain();
        Check(!(bool)Field(engine, "_queuedNext")!, "Sleep hold allowed an advancing successor.");
        player.HoldAutomaticAdvance(false);
        await Until(dispatcher, () => HasSuccessor(engine));
    }

    [TestMethod]
    public async Task PreparationFailureDoesNotInterruptPlayback()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options();
        var first = Track(1); var second = Track(2);
        var attempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (uri, _, _) =>
        {
            if (uri == first.Uri) return Task.FromResult(Audio());
            attempted.TrySetResult();
            throw new IOException("Simulated speculative failure.");
        });
        var errors = 0; sessions.CommandFailed += _ => errors++;
        Configure(player, sessions, options);
        sessions.Play(first, [first, second], default);
        await Until(dispatcher, () => attempted.Task.IsCompleted);
        await Until(dispatcher, () => engine.Duration is > 0);
        Check(errors == 0 && sessions.CurrentTrack == first && player.IsPlaying, "Speculative failure interrupted current playback.");
    }

    [TestMethod]
    public async Task RefreshingSettingsKeepsUnchangedPreparation()
    {
        using var engine = NewEngine();
        using var player = new Player(engine);
        var dispatcher = new Dispatcher();
        var options = Options();
        var pending = new TaskCompletionSource<AudioDecryptStream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        CancellationToken preparation = default;
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, token) =>
        {
            if (Interlocked.Increment(ref calls) == 1) return Task.FromResult(Audio());
            preparation = token;
            return pending.Task;
        });
        Configure(player, sessions, options);
        var tracks = new[] { Track(1), Track(2) };
        sessions.Play(tracks[0], tracks, default);
        await Until(dispatcher, () => Volatile.Read(ref calls) == 2);
        player.ConfigurePlaybackEffects(options);
        player.ConfigurePlaybackEffects(options);
        Assert.IsFalse(preparation.IsCancellationRequested);
        Assert.AreEqual(2, calls);
        pending.SetResult(Audio());
        await Until(dispatcher, () => HasSuccessor(engine));
        var url = Mpv(engine).GetProperty("playlist/1/filename");
        player.ConfigurePlaybackEffects(options);
        await Until(dispatcher, () => HasSuccessor(engine));
        Assert.AreEqual(url, Mpv(engine).GetProperty("playlist/1/filename"));
        Assert.AreEqual(2, calls);
    }
}
