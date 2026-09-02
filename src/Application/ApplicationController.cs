using LunaPlayer.Actions;
using LunaPlayer.Application.ActionHandlers;
using LunaPlayer.Configuration;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application;

internal sealed class ApplicationController : IDisposable
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly ActionRouter _router;
    private readonly FileActions _fileActions;
    private readonly PlaybackSelection _selection;
    private readonly SystemMediaControls _mediaControls = new();
    /// <summary>The clock keeping the overlay's scrubber moving, or null while nothing needs one. See
    /// <see cref="UpdateMediaControlsClock"/>.</summary>
    private IDisposable? _mediaControlsClock;
    /// <summary>The file the tags in <see cref="_tags"/> were read from, or null when nothing is open. See
    /// <see cref="TagsFor"/>.</summary>
    private string? _tagsPath;
    private MediaTags _tags = MediaTags.None;
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
        UpdateMediaControlsClock();
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
        StopMediaControlsClock();
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
        StopMediaControlsClock();
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
        _view.SetPlaying(_player.IsPlaying);
        _view.SetEditState(local, loaded);
        _view.SetBookmarkState(local);
        _view.SetMarkState(_player.IsCurrentMarked, _player.AreAllMarked);
        _view.SetMarkedActionsEnabled(loaded && _player.MarkedCount > 0);
        _view.SetSilenceRemovalChecked(_player.IsSilenceRemovalEnabled);
        SyncMediaControls();
        UpdateMediaControlsClock();
    }

    /// <summary>Publishes the current state to the Windows media overlay. Called whenever playback state
    /// changes, and on a clock while a file plays so the overlay's scrubber keeps up.</summary>
    internal void SyncMediaControls()
    {
        if (!_mediaControls.IsAvailable) return;
        var path = _player.CurrentPath;
        var hasMedia = !string.IsNullOrEmpty(path);
        var index = _player.CurrentIndex;
        var tags = TagsFor(path);
        _mediaControls.Update(new MediaControlsState(
            HasMedia: hasMedia,
            IsPlaying: _player.IsPlaying,
            // What the file calls itself, when it says; the name on disk is only a stand-in for that.
            Title: tags.Title.Length > 0 ? tags.Title : _player.CurrentDisplayName ?? string.Empty,
            Artist: tags.Artist,
            Album: tags.Album,
            Duration: hasMedia ? _player.Duration : null,
            Position: hasMedia ? _player.Elapsed : null,
            CanGoNext: hasMedia && index >= 0 && index < _player.Count - 1,
            CanGoPrevious: hasMedia && index > 0));
    }

    /// <summary>What the file says about itself, read once per file rather than once per tick.</summary>
    ///
    /// <remarks>
    /// <see cref="SyncMediaControls"/> runs on a clock while a file is open, and reading a file from disk
    /// every second to learn something that cannot have changed would be a waste; the path the tags came
    /// from is kept so the read happens only when it moves on.
    ///
    /// A stream has no header on disk to read, so it is not asked for one - the overlay simply shows the
    /// name and no artist, which is honest about what is known.
    /// </remarks>
    private MediaTags TagsFor(string? path)
    {
        if (string.IsNullOrEmpty(path) || LinkValidator.IsHttpUrl(path))
        {
            _tagsPath = null;
            _tags = MediaTags.None;
        }
        else if (!string.Equals(path, _tagsPath, StringComparison.Ordinal))
        {
            _tagsPath = path;
            _tags = MediaHeader.ReadTags(path);
        }
        return _tags;
    }

    /// <summary>Starts or stops the clock that keeps the overlay's scrubber moving, so it runs only while
    /// there is something for it to follow.</summary>
    ///
    /// <remarks>
    /// The position playing has reached is the one thing the overlay needs that nothing raises an event for:
    /// mpv does not announce the passage of time, and it does not announce a seek either, so between one
    /// discrete change and the next there is nothing to publish from and a clock has to cover the gap.
    ///
    /// The test is whether a file is open, not whether it is playing. A paused file is not going anywhere on
    /// its own, but it can still be seeked, and a seek is exactly the kind of move that reaches the overlay
    /// only because something is watching. With nothing open there is no position to report at all - before
    /// the first file, and after the playlist runs out, which unloads mpv while leaving the entry in the list.
    ///
    /// Every state change comes through <see cref="SyncViewState"/>, which publishes first and then calls
    /// this, so the overlay always has the final position before the clock stops.
    /// </remarks>
    private void UpdateMediaControlsClock()
    {
        var wanted = !_shutDown && _mediaControls.IsAvailable && _player.IsLoaded;
        if (wanted == (_mediaControlsClock is not null))
            return;
        if (wanted)
            _mediaControlsClock = _dispatcher.Repeat(TimeSpan.FromSeconds(1), SyncMediaControls);
        else
            StopMediaControlsClock();
    }

    private void StopMediaControlsClock()
    {
        _mediaControlsClock?.Dispose();
        _mediaControlsClock = null;
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
            case EndBehavior.None:
                // mpv's keep-open holds the finished file at its end, pausing itself to do it. That is a
                // change of state nothing told us about, so the play button and the overlay's clock are
                // brought up to date here rather than waiting for the user's next command.
                SyncViewState();
                break;
        }
    }
}
