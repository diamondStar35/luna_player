using LunaPlayer.Actions;
using LunaPlayer.Accessibility;
using LunaPlayer.Application.Commands;
using LunaPlayer.Application.Commands.Files;
using LunaPlayer.Application.Presentation;
using LunaPlayer.Configuration;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application;

/// <summary>Wires the view and the player together: routes commands, keeps the presenters in step with
/// playback, and owns startup and shutdown. The view state, window title and Windows media overlay are each
/// their own presenter; what is left here is coordination and the end-of-playback decision.</summary>
internal sealed class ApplicationController : IDisposable
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly ActionRouter _router;
    private readonly Files _fileActions;
    private readonly PlaybackSelection _selection;
    private readonly YouTube.Playback.Sessions _sessions;
    private readonly SleepTimer _sleepTimer;
    private readonly MediaTagsCache _tags = new();
    private readonly ViewStatePresenter _viewState;
    private readonly WindowTitlePresenter _title;
    private readonly MediaOverlayPresenter _overlay;
    private bool _shutDown;

    internal ApplicationController(
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        SettingsStore settingsStore,
        IApplicationDispatcher dispatcher,
        ActionRouter router,
        Files fileActions,
        PlaybackSelection selection,
        YouTube.Playback.Sessions sessions,
        SleepTimer sleepTimer,
        ISpeechOutput speech)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _settingsStore = settingsStore;
        _dispatcher = dispatcher;
        _router = router;
        _fileActions = fileActions;
        _selection = selection;
        _sessions = sessions;
        _sleepTimer = sleepTimer;
        _viewState = new ViewStatePresenter(view, player, speech);
        _title = new WindowTitlePresenter(view, player, settings, _tags);
        _overlay = new MediaOverlayPresenter(player, settings, dispatcher, _tags, HandleAction);
        _view.ActionRequested += HandleAction;
        _view.CloseRequested += Shutdown;
        _view.EscapePressed += _sessions.HandleEscape;
        _player.CurrentChanged += OnCurrentChanged;
        _player.StateChanged += SyncViewState;
        _player.Ended += OnPlaybackEnded;
        _player.VideoAvailabilityChanged += OnVideoAvailabilityChanged;
        _player.AudioTracksChanged += OnAudioTracksChanged;
        _player.SetVolume(settings.Audio.Volume);
        settings.Audio.Pitch = _player.SetPitch(settings.Audio.Pitch);
        _player.SetSpeed(settings.Audio.Speed);
        _player.TrackPositions(settings.Audio.SaveFilePositions);
        _player.SetEndBehavior(settings.Audio.EndBehavior);
        _player.ConfigureSilence(settings.Silence);
        _player.SetNormalization(settings.Audio.NormalizeAudio);
        _player.SetMono(settings.Audio.MonoAudio);
        _player.SetPan(settings.Audio.Pan);
        _player.SetSilenceRemoval(settings.Silence.Enabled);
        settings.Audio.NormalizeAudio = _player.IsNormalizationEnabled;
        settings.Audio.MonoAudio = _player.IsMonoEnabled;
        settings.Silence.Enabled = _player.IsSilenceRemovalEnabled;
        if (!string.IsNullOrWhiteSpace(settings.Audio.Device))
            _player.SetAudioDevice(settings.Audio.Device);
        _viewState.PushInitial();
        _overlay.ApplySetting();
        _overlay.Sync();
        _title.Update();
        _overlay.UpdateClock();
    }

    internal void OpenPaths(IEnumerable<string> paths)
    {
        if (!_shutDown)
            _fileActions.OpenPaths(paths);
    }

    internal void Shutdown()
    {
        if (_shutDown)
            return;
        _shutDown = true;
        // Nothing after this wants the overlay published again, and the clock would keep firing until the
        // controller is disposed.
        _overlay.StopClock();
        // Before the volume is read into settings: a timer fading out has lowered the player's volume, and
        // disposing it puts the user's own volume back so that is what gets saved. Quiet, unlike Cancel: the
        // screen reader is moving to the next application and an announcement here would talk over it.
        _sleepTimer.Dispose();
        _settings.Audio.Volume = _player.Volume;
        _settings.Audio.Speed = _player.Speed;
        _settings.Audio.Pitch = _player.Pitch;
        _settings.Audio.Pan = _player.Pan;
        _player.SavePosition();
        if (_settings.General.RememberLastPosition && _player.CurrentPath is string path && File.Exists(path))
        {
            _settings.Playback.LastFile = path;
            _settings.Playback.LastPosition = Math.Max(0, _player.Elapsed ?? 0);
        }
        _settingsStore.SaveSession(_settings);
        // Tear the player down here, while the frame still exists, rather than leaving it for the host's
        // disposal after the window is gone. mpv renders into the frame's window handle; if that handle is
        // destroyed first, terminating mpv afterwards can block on its dead video output and hang the whole
        // exit - which is what left the process alive and blocked a quick relaunch. Closing it first, like
        // the reference player does before destroying its window, lets mpv detach cleanly.
        _player.Dispose();
    }

    public void Dispose()
    {
        _view.ActionRequested -= HandleAction;
        _view.CloseRequested -= Shutdown;
        _player.CurrentChanged -= OnCurrentChanged;
        _player.StateChanged -= SyncViewState;
        _player.Ended -= OnPlaybackEnded;
        _player.VideoAvailabilityChanged -= OnVideoAvailabilityChanged;
        _player.AudioTracksChanged -= OnAudioTracksChanged;
        _view.EscapePressed -= _sessions.HandleEscape;
        Shutdown();
        _overlay.Dispose();
    }

    private void HandleAction(ActionId action)
    {
        if (_shutDown)
            return;
        if (!_router.Execute(action))
            throw new InvalidOperationException($"No handler is registered for {action}.");
    }

    private void OnCurrentChanged()
    {
        if (_shutDown)
            return;
        _selection.Reset();
    }

    /// <summary>Brings every presenter back in line with the player after a state change.</summary>
    private void SyncViewState()
    {
        if (_shutDown) return;
        _viewState.Sync();
        _overlay.ApplySetting();
        _overlay.Sync();
        _title.Update();
        _overlay.UpdateClock();
    }

    private void OnVideoAvailabilityChanged()
        => _dispatcher.Post(SyncViewState);

    private void OnAudioTracksChanged()
        => _dispatcher.Post(SyncViewState);

    private void OnPlaybackEnded(PlaybackEndReason reason)
        => _dispatcher.Post(() => HandlePlaybackEnded(reason));

    private void HandlePlaybackEnded(PlaybackEndReason reason)
    {
        if (_shutDown || reason is not (PlaybackEndReason.EndOfFile or PlaybackEndReason.Error))
            return;
        if (_player.CurrentPath is null)
            return;
        // An end-of-track sleep timer fires here, ahead of the repeat and advance logic, and takes over the
        // end of the track: SyncViewState reflects the pause it may have left, and returning stops the player
        // advancing or looping past the point the user meant it to stop.
        if (_sleepTimer.OnTrackEnded())
        {
            SyncViewState();
            return;
        }
        if (_player.IsRepeatFileEnabled)
        {
            _player.RestartCurrent();
            return;
        }
        switch (_settings.Audio.EndBehavior)
        {
            case EndBehavior.Advance:
                // A video from YouTube goes to the next one in the list the user was shown, not the next
                // playlist entry - and its address may still be being fetched, which is what Pending says.
                // Stopping on that would end the session a moment before its successor arrived.
                switch (_sessions.TryNext())
                {
                    case YouTube.Playback.NextOutcome.Advanced or YouTube.Playback.NextOutcome.Pending:
                        return;
                    case YouTube.Playback.NextOutcome.Exhausted:
                        _player.Stop();
                        return;
                }
                if (!_player.Next(_settings.Audio.WrapPlaylist))
                    _player.Stop();
                break;
            case EndBehavior.Loop:
                _player.RestartCurrent();
                break;
            case EndBehavior.None:
                // mpv's keep-open holds the finished file at its end, pausing itself to do it. That is a
                // change of state nothing told us about, so the play button and the overlay's clock are
                // brought up to date here rather than waiting for the user's next command.
                SyncViewState();
                break;
        }
    }
}
