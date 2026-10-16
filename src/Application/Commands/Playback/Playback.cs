using System.Globalization;
using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Commands.Playback;

internal sealed partial class Playback
{
    private const double DefaultSeekStep = 5;
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly Settings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly ISpeechOutput _speech;
    private readonly PlaybackSelection _selection;
    private readonly MediaGuard _guard;
    private readonly LunaPlayer.Spotify.SpotifyPlaybackHost _spotify;

    internal Playback(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        Settings settings,
        SettingsStore settingsStore,
        ISpeechOutput speech,
        PlaybackSelection selection,
        LunaPlayer.Spotify.SpotifyPlaybackHost spotify)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _settingsStore = settingsStore;
        _speech = speech;
        _selection = selection;
        _guard = new MediaGuard(player, speech);
        _spotify = spotify;
        RegisterCoreActions(router);
        RegisterGeneratedActions(router);
    }

    private void RegisterCoreActions(ActionRouter router)
    {
        router.Register(ActionId.PlayPause, TogglePlayPause);
        router.Register(ActionId.SeekBackward, () => SeekByMultiplier(-1));
        router.Register(ActionId.SeekForward, () => SeekByMultiplier(1));
        router.Register(ActionId.SeekBackwardX2, () => SeekByMultiplier(-2));
        router.Register(ActionId.SeekForwardX2, () => SeekByMultiplier(2));
        router.Register(ActionId.SeekBackwardX4, () => SeekByMultiplier(-4));
        router.Register(ActionId.SeekForwardX4, () => SeekByMultiplier(4));
        router.Register(ActionId.SeekStart, SeekStart);
        router.Register(ActionId.SeekEnd, SeekEnd);
        router.Register(ActionId.GoToTime, GoToTime);
        router.Register(ActionId.ToggleFullScreen, ToggleFullScreen);
        router.Register(ActionId.VolumeUp, () => ChangeVolume(_settings.Audio.VolumeStep));
        router.Register(ActionId.VolumeDown, () => ChangeVolume(-_settings.Audio.VolumeStep));
        router.Register(ActionId.VolumeMaximize, () => SetVolume(AudioSettings.MaximumVolume));
        router.Register(ActionId.VolumeMinimize, () => SetVolume(5));
        router.Register(ActionId.AnnounceVolume, AnnounceVolume);
        router.Register(ActionId.AnnounceElapsed, () => AnnounceTime(
            // Translators: Spoken before the time played so far, as in "Elapsed time: 1 minute 20 seconds".
            Tr("Elapsed time:"), _spotify.IsActive ? _spotify.Elapsed : _player.Elapsed));
        router.Register(ActionId.AnnounceRemaining, () => AnnounceTime(
            // Translators: Spoken before the time still left to play, as in "Remaining time: 1 minute 20 seconds".
            Tr("Remaining time:"), _spotify.IsActive ? _spotify.Remaining : _player.Remaining));
        router.Register(ActionId.AnnounceDuration, () => AnnounceTime(
            // Translators: Spoken before the whole length of the file, as in "Total time: 3 minutes 40 seconds".
            Tr("Total time:"), _spotify.IsActive ? _spotify.Duration : _player.Duration));
        router.Register(ActionId.AnnouncePercent, AnnouncePercent);
        router.Register(ActionId.AnnounceSpeed, AnnounceSpeed);
        router.Register(ActionId.ToggleVerbosity, ToggleVerbosity);
        router.Register(ActionId.SpeedUp, () => ChangeSpeed(_settings.Audio.SpeedStep));
        router.Register(ActionId.SpeedDown, () => ChangeSpeed(-_settings.Audio.SpeedStep));
        router.Register(ActionId.ResetSpeed, () => SetSpeed(1));
        router.Register(ActionId.PitchUp, () => ChangePitch(_settings.Audio.PitchStep));
        router.Register(ActionId.PitchDown, () => ChangePitch(-_settings.Audio.PitchStep));
        router.Register(ActionId.ResetPitch, () => SetPitch(0));
        router.Register(ActionId.AnnouncePitch, AnnouncePitch);
        router.Register(ActionId.PanLeft, () => ChangePan(-_settings.Audio.PanStep));
        router.Register(ActionId.PanRight, () => ChangePan(_settings.Audio.PanStep));
        router.Register(ActionId.AnnouncePan, AnnouncePan);
        router.Register(ActionId.ToggleSilenceRemoval, ToggleSilenceRemoval);
        router.Register(ActionId.StartSelection, StartSelection);
        router.Register(ActionId.EndSelection, EndSelection);
        router.Register(ActionId.ClearSelection, ClearSelection);
    }

    private void RegisterGeneratedActions(ActionRouter router)
    {
        foreach (var step in PlaybackActions.SeekSteps)
            router.Register(step.Id, () => SetSeekStep(step));
        foreach (var jump in PlaybackActions.PercentJumps)
            router.Register(jump.Id, () => JumpToPercent(jump.Percent));
    }

    private void TogglePlayPause()
    {
        if (_spotify.IsActive)
        {
            var playing = _spotify.TogglePause();
            _view.SetPlaying(playing);
            if (_settings.General.Verbosity == SpeechVerbosity.Beginner)
                _speech.SpeakText(playing ? Tr("Play") : Tr("Pause"));
            return;
        }
        if (!_guard.RequireFile(out _))
            return;

        if (_player.Duration is null)
        {
            if (_player.Reload() && _settings.General.Verbosity == SpeechVerbosity.Beginner)
                // Translators: Spoken when playing starts or carries on again.
                _speech.SpeakText(Tr("Play"));
            return;
        }

        var isPlaying = _player.TogglePause();
        _view.SetPlaying(isPlaying);
        if (_settings.General.Verbosity == SpeechVerbosity.Beginner)
            _speech.SpeakText(isPlaying
                ? Tr("Play")
                // Translators: Spoken when playing is held where it is.
                : Tr("Pause"));
    }

    private void SeekByMultiplier(double multiplier)
    {
        var delta = GetSeekStepSeconds() * multiplier;
        if (_spotify.IsActive) { _spotify.SeekRelative(delta); return; }
        if (_selection.IsActive(_player.CurrentPath)
            && _player.Elapsed is double elapsed
            && _selection.Start is double start
            && _selection.End is double end)
        {
            _player.SeekAbsolute(Math.Clamp(elapsed + delta, start, end));
            return;
        }
        _player.Seek(delta);
    }

    private void SeekStart()
    {
        if (_spotify.IsActive) { _spotify.Seek(0); return; }
        if (_selection.IsActive(_player.CurrentPath) && _selection.Start is double start)
            _player.SeekAbsolute(start);
        else
            _player.SeekAbsolute(0);
    }

    private void SeekEnd()
    {
        if (_spotify.IsActive)
        {
            if (_spotify.Duration is double d) _spotify.Seek(Math.Max(0, d - 0.2));
            return;
        }
        if (_selection.IsActive(_player.CurrentPath) && _selection.End is double end)
        {
            _player.SeekAbsolute(end);
            return;
        }
        if (!_player.SeekToEnd())
            _guard.ReportTimeUnavailable();
    }

    private void GoToTime()
    {
        if (_spotify.IsActive)
        {
            if (_spotify.Duration is not double spotifyDuration || spotifyDuration <= 0)
            {
                _view.Dialogs.ShowWarning(Tr("Time is not available for the current file."), Tr("Go to time"));
                return;
            }
            var chosen = _view.Dialogs.ChooseTime(spotifyDuration, Math.Clamp(_spotify.Elapsed ?? 0, 0, spotifyDuration));
            if (chosen.HasValue) _spotify.Seek(chosen.Value);
            return;
        }
        if (_player.Duration is not double duration || duration <= 0)
        {
            _view.Dialogs.ShowWarning(Tr("Time is not available for the current file."),
                // Translators: Title of the window that asks the user which point in the file to move to.
                Tr("Go to time"));
            return;
        }
        var position = _view.Dialogs.ChooseTime(duration, Math.Clamp(_player.Elapsed ?? 0, 0, duration));
        if (!position.HasValue)
            return;
        _player.SeekAbsolute(position.Value);
    }

    private void ToggleFullScreen()
    {
        // The F11 accelerator fires even while the menu item is disabled, so gate on HasVideo.
        if (!_player.HasVideo)
            return;
        var enable = !_view.IsFullScreen;
        _view.SetFullScreen(enable);
        _speech.Speak(
            // Translators: Spoken when full-screen mode is turned on. The beginner form names the F11 key so the
            // user knows how to leave it again.
            enable ? Tr("Full-screen mode on. Press F11 again to turn it off.") : Tr("Full-screen mode off."),
            // Translators: The short advanced-verbosity wording spoken when full-screen mode is turned on or off.
            enable ? Tr("Full-screen on") : Tr("Full-screen off"));
    }

    private void ChangeVolume(double delta) => ApplyVolume(_player.ChangeVolume(delta));

    private void SetVolume(double value) => ApplyVolume(_player.SetVolume(value));

    private void ApplyVolume(double volume)
    {
        // Spotify audio runs through the same mpv volume control as local files.
        AnnounceVolumeValue(volume);
    }

    private void AnnounceVolumeValue(double volume)
    {
        _settings.Audio.Volume = volume;
        var whole = (int)volume;
        // Translators: Spoken after the loudness has been changed. {volume} is the new loudness as a number out of a hundred.
        _speech.Speak(TrFormat("Volume {volume} percent", whole), $"{whole}%");
    }

    private void AnnounceVolume()
    {
        var whole = (int)_player.Volume;
        // Translators: Spoken when the user asks how loud the sound is. {percent} is the loudness as a number out of a hundred.
        _speech.Speak(TrFormat("Volume is {percent}%", whole), $"{whole}%");
    }

    private void AnnounceTime(string label, double? seconds)
    {
        var formatted = PlaybackTimeFormatter.Format(seconds);
        if (formatted is null)
            _guard.ReportTimeUnavailable();
        else
            _speech.Speak($"{label} {formatted}", formatted);
    }

    private void AnnouncePercent()
    {
        var dur = _spotify.IsActive ? _spotify.Duration : _player.Duration;
        var pos = _spotify.IsActive ? _spotify.Elapsed : _player.Elapsed;
        if (dur is not double duration || duration <= 0 || pos is not double elapsed)
        {
            // Translators: Spoken when the player cannot work out how far through the file it is.
            _speech.Speak(Tr("Percentage not available"), Tr("Unavailable"));
            return;
        }
        var percent = Math.Clamp((int)(elapsed / duration * 100), 0, 100);
        // Translators: Spoken when the user asks how far through the file playing has got. {percent} is that share out of a hundred.
        _speech.Speak(TrFormat("{percent} percent", percent), $"{percent}%");
    }

    // Translators: Spoken when the user asks how fast the file is playing. {speed} is the speed, where 1 is the normal
    // speed; "x" is short for "times" and is usually left as it is.
    private void AnnounceSpeed() => _speech.SpeakText(TrFormat("{speed}x", FormatSpeed(_player.Speed)));

    private void ChangeSpeed(double delta) => SetSpeed(_player.Speed + delta);

    private void SetSpeed(double value)
    {
        var speed = _player.SetSpeed(value);
        _settings.Audio.Speed = speed;
        _speech.SpeakText(TrFormat("{speed}x", FormatSpeed(speed)));
    }

    private void ChangePitch(double delta)
    {
        if (!_guard.RequireFile(out _))
            return;
        SetPitch(_player.Pitch + delta);
    }

    private void SetPitch(double value)
    {
        if (!_guard.RequireFile(out _))
            return;
        AnnouncePitchValue(_player.SetPitch(value));
    }

    private void AnnouncePitch()
    {
        if (!_guard.RequireFile(out _))
            return;
        AnnouncePitchValue(_player.Pitch);
    }

    private void AnnouncePitchValue(double pitch)
    {
        _settings.Audio.Pitch = pitch;
        var value = pitch.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture);
        // Translators: Short announcement of pitch in advanced mode. {pitch} is a signed number such as
        // +1, -2, or +0.25, or zero when pitch is normal.
        var advanced = TrFormat("{pitch} semitones", value);
        if (pitch == 0)
        {
            // Translators: Spoken when audio is playing at its original pitch.
            _speech.Speak(Tr("Pitch normal"), advanced);
            return;
        }
        // Translators: Spoken after audio pitch changes or when it is requested. {pitch} is a signed
        // number of semitones, such as +1, -2, or +0.25.
        _speech.Speak(TrFormat("Pitch {pitch} semitones", value), advanced);
    }

    private void ChangePan(double delta)
    {
        if (!_guard.RequireFile(out _))
            return;
        AnnouncePanValue(_player.ChangePan(delta));
    }

    private void AnnouncePan()
    {
        if (!_guard.RequireFile(out _))
            return;
        AnnouncePanValue(_player.Pan);
    }

    private void AnnouncePanValue(double pan)
    {
        _settings.Audio.Pan = pan;
        var value = pan.ToString("0.###", CultureInfo.InvariantCulture);
        // Translators: Spoken after audio panning changes or when it is requested. Negative values are
        // toward the left, positive values are toward the right, and zero is centered.
        _speech.Speak(TrFormat("Pan {pan}%", value), $"{value}%");
    }

    private void ToggleVerbosity()
    {
        if (_settings.General.Verbosity == SpeechVerbosity.Advanced)
        {
            _settings.General.Verbosity = SpeechVerbosity.Beginner;
            // Translators: Spoken when the user switches to whole, clearly worded messages. It should read the same as
            // the "Beginner" entry in the Verbosity list on the General settings page.
            _speech.SpeakText(Tr("Beginner mode"));
        }
        else
        {
            _settings.General.Verbosity = SpeechVerbosity.Advanced;
            // Translators: Spoken when the user switches to short messages. It should read the same as the "Advanced"
            // entry in the Verbosity list on the General settings page.
            _speech.SpeakText(Tr("Advanced mode"));
        }
        _settingsStore.SaveExplicit(_settings);
    }

    private void ToggleSilenceRemoval()
    {
        var enabled = !_player.IsSilenceRemovalEnabled;
        if (!_player.SetSilenceRemoval(enabled))
        {
            _speech.Speak(
                // Translators: Spoken when trimming the silent parts out could not be switched on or off.
                Tr("Could not change silence removal filter."),
                // Translators: The short wording spoken when trimming the silent parts out could not be switched on or off.
                Tr("Filter failed."));
            return;
        }
        _settings.Silence.Enabled = enabled;
        _settingsStore.SaveExplicit(_settings);
        _view.SetSilenceRemovalChecked(enabled);
        _speech.Speak(enabled
                // Translators: Spoken once the player has started trimming the silent parts out.
                ? Tr("Silence removal on")
                // Translators: Spoken once the player has stopped trimming the silent parts out.
                : Tr("Silence removal off"),
            enabled ? Tr("Silence removal on") : Tr("Silence removal off"));
    }

    private void JumpToPercent(int percent)
    {
        if (_spotify.IsActive)
        {
            if (_spotify.Duration is double spotifyDuration && spotifyDuration > 0)
                _spotify.Seek(percent >= 100 ? Math.Max(0, spotifyDuration - 0.2) : spotifyDuration * percent / 100.0);
            _speech.Speak(TrFormat("{percent} percent", percent), $"{percent}%");
            return;
        }
        if (!_guard.RequireDuration(out var duration))
            return;
        if (percent >= 100)
            _player.SeekToEnd();
        else
            _player.SeekAbsolute(duration * percent / 100.0);
        _speech.Speak(TrFormat("{percent} percent", percent), $"{percent}%");
    }

    private void SetSeekStep(SeekStepAction step)
    {
        _settings.Audio.SeekStepKey = step.Key;
        _settingsStore.SaveExplicit(_settings);
        _speech.SpeakText(step.Label);
    }

    private double GetSeekStepSeconds()
    {
        if (_settings.Audio.SeekStepKey == "-")
            return _settings.Audio.CustomSeekStep > 0 ? _settings.Audio.CustomSeekStep : DefaultSeekStep;
        foreach (var step in PlaybackActions.SeekSteps)
        {
            if (step.Key == _settings.Audio.SeekStepKey && step.Seconds > 0)
                return step.Seconds;
        }
        return DefaultSeekStep;
    }

    private static string FormatSpeed(double speed)
        => speed.ToString("0.###", CultureInfo.InvariantCulture);
}
