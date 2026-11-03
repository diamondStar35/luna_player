using System.Reflection;
using System.Runtime.CompilerServices;
using LunaPlayer.Application;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Spotify;
using LunaPlayer.Spotify.Playback;
using MpvNet;

namespace LunaPlayer.Tests.Spotify;

public sealed partial class CrossfadeTests
{

    [TestMethod]
    public Task StaleDecoderErrorTest() => StaleDecoderError();

    static Task StaleDecoderError()
    {
        using var engine = NewEngine(); var errors = 0;
        engine.Ended += reason => { if (reason == EndReason.Error) errors++; };
        void Set(string name, object value) => typeof(PlaybackCoordinator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Field(engine, "_coordinator"), value);
        Set("_awaitingLoadRestart", true); Set("_loadingEntryId", 42L); Set("_loadingSourceEntryId", 1L);
        void Error(long entry) => typeof(PlaybackCoordinator).GetMethod("HandleEndFile", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Field(engine, "_coordinator"),
            [new MpvEvent { EventId = MpvEventId.EndFile, Data = new MpvEndFileEvent { Reason = MpvEndFileReason.Error, PlaylistEntryId = entry } }]);
        Error(41);
        Check(errors == 0, "An outgoing entry's late error was attributed to its replacement.");
        Error(42);
        Check(errors == 1, "A genuine replacement failure was ignored.");
        return Task.CompletedTask;
    }

    [TestMethod]
    public Task StaleControllerErrorTest() => StaleControllerError();

    static async Task StaleControllerError()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options(); options.Crossfade = false;
        var opens = 0;
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => { opens++; return Task.FromResult(Audio()); });
        Configure(player, sessions, options);
        sessions.Play(Track(1), null, default);
        await Until(dispatcher, () => !engine.IsLoading && engine.Duration is > 0);
        var controller = (ApplicationController)RuntimeHelpers.GetUninitializedObject(typeof(ApplicationController));
        void Set(string name, object value) => typeof(ApplicationController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(controller, value);
        Set("_spotify", sessions); Set("_player", player); Set("_dispatcher", dispatcher);
        typeof(ApplicationController).GetMethod("OnPlaybackEnded", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(controller, [EndReason.Error]);
        player.Stop();
        dispatcher.Drain();
        Check(opens == 1 && !player.IsPlaying, "A stale error restarted stopped playback.");
    }

    [TestMethod]
    public Task LateFailureDialogTest() => LateFailureDialog();

    static async Task LateFailureDialog()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options(); options.Crossfade = false;
        var opens = 0; var reported = new List<Exception>();
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) =>
            ++opens == 1 ? Task.FromException<AudioDecryptStream>(new InvalidDataException()) : Task.FromResult(Audio()));
        Configure(player, sessions, options);
        sessions.CommandFailed += reported.Add;
        var track = Track(1);
        sessions.Play(track, null, default);
        await Until(dispatcher, () => reported.Count == 1);
        var generation = sessions.CurrentLoadGeneration;
        typeof(Sessions).GetMethod("LoadManaged", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sessions, [track.Uri, 0.0]);
        await Until(dispatcher, () => !engine.IsLoading && engine.Duration is > 0);
        Check(sessions.CurrentLoadGeneration == generation && player.IsPlaying, "Retry test did not retain the selected track.");
        Check(!sessions.TakeCurrentFailure(reported[0]), "A failure from the previous attempt can still stop successful playback.");
        var current = new InvalidDataException();
        typeof(Sessions).GetMethod("ReportFailure", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sessions, [current]);
        Check(sessions.TakeCurrentFailure(current) && !sessions.TakeCurrentFailure(current), "A genuine failure was hidden or presented twice.");
    }

    [TestMethod]
    public Task FailureOwnershipTest() => FailureOwnership();

    static async Task FailureOwnership()
    {
        using var engine = NewEngine(); using var player = new Player(engine);
        var dispatcher = new Dispatcher(); var options = Options(); options.Crossfade = false;
        using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
        Configure(player, sessions, options);
        sessions.Play(Track(1), null, default);
        await Until(dispatcher, () => !engine.IsLoading && engine.Duration is > 0);
        var report = typeof(Sessions).GetMethod("ReportFailure", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Exception Report()
        {
            var error = new InvalidDataException(); report.Invoke(sessions, [error]); return error;
        }
        void Change(string name, object value) => typeof(Sessions).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(sessions, value);
        var identity = Report();
        Check(!sessions.TakeCurrentFailure(new InvalidDataException()) && sessions.TakeCurrentFailure(identity)
            && !sessions.TakeCurrentFailure(identity), "Exception identity or single consumption was not enforced.");
        var attempt = Report();
        Change("_loadAttempt", (int)Field(sessions, "_loadAttempt")! + 1);
        Check(!sessions.TakeCurrentFailure(attempt), "A previous load attempt still owns the dialog.");
        var generation = Report();
        Change("_loadGeneration", sessions.CurrentLoadGeneration + 1);
        Check(!sessions.TakeCurrentFailure(generation), "A previous selection generation still owns the dialog.");
        var track = Report(); var current = sessions.CurrentTrack!;
        Change("_current", Track(2));
        Check(!sessions.TakeCurrentFailure(track), "A different track still owns the dialog.");
        Change("_current", current);
        var revision = Report(); player.Stop();
        Check(!sessions.TakeCurrentFailure(revision), "A previous native playback revision still owns the dialog.");
    }
}
