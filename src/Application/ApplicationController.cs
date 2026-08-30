using LunaPlayer.Actions;
using LunaPlayer.Application.ActionHandlers;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application;

internal sealed class ApplicationController : IDisposable
{
    private const string ApplicationName = "Luna Player";

    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly ActionRouter _router;
    private readonly FileActions _fileActions;
    private readonly PlaybackSelection _selection;
    private readonly SystemMediaControls _mediaControls = new();
    private bool _shutDown;

    internal ApplicationController(
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        SettingsStore settingsStore,
        IApplicationDispatcher dispatcher,
        ActionRouter router,
        FileActions fileActions,
        PlaybackSelection selection)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _settingsStore = settingsStore;
        _dispatcher = dispatcher;
        _router = router;
        _fileActions = fileActions;
        _selection = selection;
        _view.ActionRequested += HandleAction;
        _view.CloseRequested += Shutdown;
        _player.CurrentChanged += OnCurrentChanged;
        _player.StateChanged += SyncViewState;
        _player.Ended += OnPlaybackEnded;
        // Button presses arrive on a Windows Runtime thread, so they are posted like any other outside
        // request rather than run where they land.
        _mediaControls.ButtonPressed += action => _dispatcher.Post(() => HandleAction(action));
        _player.SetVolume(settings.Audio.Volume);
        _player.SetSpeed(settings.Audio.Speed);
        _player.TrackPositions(settings.Audio.SaveFilePositions);
        _player.SetEndBehavior(settings.Audio.EndBehavior);
        _player.ConfigureSilence(settings.Silence);
        _player.SetNormalization(settings.Audio.NormalizeAudio);
        _player.SetMono(settings.Audio.MonoAudio);
        _player.SetSilenceRemoval(settings.Silence.Enabled);
        settings.Audio.NormalizeAudio = _player.IsNormalizationEnabled;
        settings.Audio.MonoAudio = _player.IsMonoEnabled;
        settings.Silence.Enabled = _player.IsSilenceRemovalEnabled;
        if (!string.IsNullOrWhiteSpace(settings.Audio.Device))
            _player.SetAudioDevice(settings.Audio.Device);
        _view.SetMediaLoaded(false);
        _view.SetPlaying(false);
        _view.SetEditState(false, false);
        _view.SetBookmarkState(false);
        _view.SetMarkState(false, false);
        _view.SetMarkedActionsEnabled(false);
        _view.SetSilenceRemovalChecked(_player.IsSilenceRemovalEnabled);
        SyncMediaControls();
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
        _settings.Audio.Volume = _player.Volume;
        _settings.Audio.Speed = _player.Speed;
        _player.SavePosition();
        if (_settings.General.RememberLastPosition && _player.CurrentPath is string path && File.Exists(path))
        {
            _settings.Playback.LastFile = path;
            _settings.Playback.LastPosition = Math.Max(0, _player.Elapsed ?? 0);
        }
        _settingsStore.SaveSession(_settings);
        _player.Stop();
    }

    public void Dispose()
    {
        _view.ActionRequested -= HandleAction;
        _view.CloseRequested -= Shutdown;
        _player.CurrentChanged -= OnCurrentChanged;
        _player.StateChanged -= SyncViewState;
        _player.Ended -= OnPlaybackEnded;
        Shutdown();
        _mediaControls.Dispose();
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

    private void SyncViewState()
    {
        if (_shutDown) return;
        var loaded = _player.CurrentPath is not null;
        var local = _player.CurrentPath is string path && File.Exists(path);
        _view.SetMediaLoaded(loaded);
        _view.SetPlaying(loaded && !_player.IsPaused);
        _view.SetEditState(local, loaded);
        _view.SetBookmarkState(local);
        _view.SetMarkState(_player.IsCurrentMarked, _player.AreAllMarked);
        _view.SetMarkedActionsEnabled(loaded && _player.MarkedCount > 0);
        _view.SetSilenceRemovalChecked(_player.IsSilenceRemovalEnabled);
        SyncMediaControls();
    }

    /// <summary>Publishes the current state to the Windows media overlay. Called whenever playback state
    /// changes, and on a timer so the overlay's scrubber keeps up while a file plays.</summary>
    internal void SyncMediaControls()
    {
        if (!_mediaControls.IsAvailable) return;
        var path = _player.CurrentPath;
        var hasMedia = !string.IsNullOrEmpty(path);
        var index = _player.CurrentIndex;
        _mediaControls.Update(new MediaControlsState(
            HasMedia: hasMedia,
            IsPlaying: hasMedia && !_player.IsPaused,
            Title: _player.CurrentDisplayName ?? string.Empty,
            Artist: ApplicationName,
            Duration: hasMedia ? _player.Duration : null,
            Position: hasMedia ? _player.Elapsed : null,
            CanGoNext: hasMedia && index >= 0 && index < _player.Count - 1,
            CanGoPrevious: hasMedia && index > 0));
    }

    private void OnPlaybackEnded(PlaybackEndReason reason)
        => _dispatcher.Post(() => HandlePlaybackEnded(reason));

    private void HandlePlaybackEnded(PlaybackEndReason reason)
    {
        if (_shutDown || reason is not (PlaybackEndReason.EndOfFile or PlaybackEndReason.Error))
            return;
        if (_player.CurrentPath is null)
            return;
        if (_player.IsRepeatFileEnabled)
        {
            _player.RestartCurrent();
            return;
        }
        switch (_settings.Audio.EndBehavior)
        {
            case EndBehavior.Advance:
                if (!_player.Next(_settings.Audio.WrapPlaylist))
                    _player.Stop();
                break;
            case EndBehavior.Loop:
                _player.RestartCurrent();
                break;
            // EndBehavior.None keeps the finished file loaded; mpv's keep-open holds it at the end.
        }
    }
}
