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
    private readonly Player _player;
    private readonly Settings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly ISpeechOutput _speech;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly PlaybackSelection _selection;
    private readonly SelectionSlots _selections;
    private readonly MediaGuard _guard;
    private readonly LunaPlayer.Spotify.Playback _spotify;

    internal Playback(
        ActionRouter router,
        IMainView view,
        Player player,
        Settings settings,
        SettingsStore settingsStore,
        ISpeechOutput speech,
        PlaybackSelection selection,
        SelectionSlots selections,
        IApplicationDispatcher dispatcher,
        LunaPlayer.Spotify.Playback spotify)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _settingsStore = settingsStore;
        _speech = speech;
        _dispatcher = dispatcher;
        _selection = selection;
        _selections = selections;
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
        router.Register(ActionId.StartSelection, StartAbSelection);
        router.Register(ActionId.EndSelection, EndSelection);
        router.Register(ActionId.ClearSelection, ClearSelection);
        router.Register(ActionId.SetSelectionStartMarker, SetSelectionStartMarker);
        router.Register(ActionId.SetSelectionStartTime, SetSelectionStartTime);
        router.Register(ActionId.SetSelectionEndMarker, SetSelectionEndMarker);
        router.Register(ActionId.SetSelectionEndTime, SetSelectionEndTime);
        router.Register(ActionId.PreviousSelection, PreviousSelection);
        router.Register(ActionId.NextSelection, NextSelection);
        router.Register(ActionId.ClearSelectionPoint, ClearSelectionPoint);
        router.Register(ActionId.ClearAllSelections, ClearAllSelections);
        router.Register(ActionId.SaveSelection, SaveSelection);
        router.Register(ActionId.SaveSelections, SaveSelections);
        router.Register(ActionId.ToggleRepeatSelections, ToggleRepeatSelections);
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
                _speech.SpeakText(playing
                    // Translators: Playback action announcement or button that starts the selected Spotify track.
                    ? Tr("Play")
                    // Translators: Spoken announcement after pausing Spotify playback.
                    : Tr("Pause"));
            return;
        }
        if (!_guard.RequireFile(out _))
            return;

        // A completed one-shot selection stays bounded to its slot. The user leaves that boundary by
        // moving forward to the always-present free slot, not by toggling playback.
        if (_selections.OneShotActive
            && _selections.Current is SelectionRange range
            && IsAtSelectionEnd(range))
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
                // Translators: Spoken when playing starts or carries on again.
                ? Tr("Play")
                // Translators: Spoken when playing is held where it is.
                : Tr("Pause"));
    }

    private void SeekByMultiplier(double multiplier)
    {
        var delta = GetSeekStepSeconds() * multiplier;
        if (_spotify.IsActive) { _spotify.SeekRelative(delta); return; }
        if (_selections.PlaybackActive
            && _player.Elapsed is double elapsed
            && _selections.Start is double selectionStart
            && _selections.End is double selectionEnd)
        {
            _player.SeekAbsolute(Math.Clamp(elapsed + delta, selectionStart, selectionEnd));
            return;
        }
        if (_selection.IsActive(_player.CurrentPath)
            && _player.Elapsed is double loopElapsed
            && _selection.Start is double start
            && _selection.End is double end)
        {
            _player.SeekAbsolute(Math.Clamp(loopElapsed + delta, start, end));
            return;
        }
        _player.Seek(delta);
    }

    private void SeekStart()
    {
        if (_spotify.IsActive) { _spotify.Seek(0); return; }
        if (_selections.PlaybackActive && _selections.Start is double selectionStart)
            _player.SeekAbsolute(selectionStart);
        else if (_selection.IsActive(_player.CurrentPath) && _selection.Start is double start)
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
        if (_selections.PlaybackActive && _selections.End is double selectionEnd)
        {
            _player.SeekAbsolute(selectionEnd);
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
                _view.Dialogs.ShowWarning(
                    // Translators: Warning when the Spotify track duration is not yet available for seeking.
                    Tr("Time is not available for the current file."),
                    // Translators: Title of the seek dialog warning during Spotify playback.
                    Tr("Go to time"));
                return;
            }
            var chosen = _view.Dialogs.ChooseTime(spotifyDuration, Math.Clamp(_spotify.Elapsed ?? 0, 0, spotifyDuration));
            if (chosen.HasValue) _spotify.Seek(chosen.Value);
            return;
        }
        if (_player.Duration is not double duration || duration <= 0)
        {
            _view.Dialogs.ShowWarning(
                // Translators: Shown when the player cannot determine the current file's duration.
                Tr("Time is not available for the current file."),
                // Translators: Title of the window that asks the user which point in the file to move to.
                Tr("Go to time"));
            return;
        }
        var position = _view.Dialogs.ChooseTime(duration, Math.Clamp(_player.Elapsed ?? 0, 0, duration));
        if (!position.HasValue)
            return;
        _player.SeekAbsolute(_selections.Bound(position.Value));
    }

    private void ToggleFullScreen()
    {
        // The F11 accelerator fires even while the menu item is disabled, so gate on HasVideo.
        if (!_player.HasVideo)
            return;
        var enable = !_view.IsFullScreen;
        _view.SetFullScreen(enable);
        string beginner;
        string advanced;
        if (enable)
        {
            // Translators: Spoken when full-screen mode is turned on. The beginner form names the F11 key so the user knows how to leave it again.
            beginner = Tr("Full-screen mode on. Press F11 again to turn it off.");
            // Translators: The short advanced-verbosity wording spoken when full-screen mode is turned on.
            advanced = Tr("Full-screen on");
        }
        else
        {
            // Translators: Spoken when full-screen mode is turned off.
            beginner = Tr("Full-screen mode off.");
            // Translators: The short advanced-verbosity wording spoken when full-screen mode is turned off.
            advanced = Tr("Full-screen off");
        }
        _speech.Speak(beginner, advanced);
    }

    private void ChangeVolume(double delta) => AnnounceVolumeValue(_player.ChangeVolume(delta));

    private void SetVolume(double value) => AnnounceVolumeValue(_player.SetVolume(value));

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
        if ((_spotify.IsActive ? _spotify.Duration : _player.Duration) is not double duration || duration <= 0
            || (_spotify.IsActive ? _spotify.Elapsed : _player.Elapsed) is not double elapsed)
        {
            _speech.Speak(
                // Translators: Spoken when the player cannot work out how far through the file it is.
                Tr("Percentage not available"),
                // Translators: Short announcement when playback progress cannot be calculated.
                Tr("Unavailable"));
            return;
        }
        var percent = Math.Clamp((int)(elapsed / duration * 100), 0, 100);
        _speech.Speak(
            // Translators: Spoken playback position. {percent} is a number from 0 to 100.
            TrFormat("{percent} percent", percent),
            // Translators: Short spoken playback position. {percent} is the percentage; retain a percent sign.
            TrFormat("{percent}%", percent));
    }

    // Translators: Spoken when the user asks how fast the file is playing. {speed} is the speed, where 1 is the normal
    // speed; "x" is short for "times" and is usually left as it is.
    private void AnnounceSpeed() => _speech.SpeakText(TrFormat("{speed}x", FormatSpeed(_player.Speed)));

    private void ChangeSpeed(double delta) => SetSpeed(_player.Speed + delta);

    private void SetSpeed(double value)
    {
        var speed = _player.SetSpeed(value);
        _settings.Audio.Speed = speed;
        // Translators: Spoken after playback speed changes. {speed} is the multiplier, where 1 is normal; "x" means "times".
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
        string beginner;
        string advanced;
        if (enabled)
        {
            // Translators: Spoken once the player has started trimming silent parts out.
            beginner = Tr("Silence removal on");
            // Translators: Short announcement once the player has started trimming silent parts out.
            advanced = Tr("Silence removal on");
        }
        else
        {
            // Translators: Spoken once the player has stopped trimming silent parts out.
            beginner = Tr("Silence removal off");
            // Translators: Short announcement once the player has stopped trimming silent parts out.
            advanced = Tr("Silence removal off");
        }
        _speech.Speak(beginner, advanced);
    }

    private void JumpToPercent(int percent)
    {
        if (_spotify.IsActive)
        {
            if (_spotify.Duration is double spotifyDuration && spotifyDuration > 0)
                _spotify.Seek(percent >= 100 ? Math.Max(0, spotifyDuration - 0.2) : spotifyDuration * percent / 100.0);
            _speech.Speak(
                // Translators: Spoken playback position. {percent} is a number from 0 to 100.
                TrFormat("{percent} percent", percent),
                // Translators: Short spoken playback position. {percent} is the percentage; retain a percent sign.
                TrFormat("{percent}%", percent));
            return;
        }
        if (!_guard.RequireDuration(out var duration))
            return;
        if (_selections.PlaybackActive)
            _player.SeekAbsolute(_selections.Bound(duration * percent / 100.0));
        else if (percent >= 100)
            _player.SeekToEnd();
        else
            _player.SeekAbsolute(duration * percent / 100.0);
        _speech.Speak(
            // Translators: Spoken playback position. {percent} is a number from 0 to 100.
            TrFormat("{percent} percent", percent),
            // Translators: Short spoken playback position. {percent} is the percentage; retain a percent sign.
            TrFormat("{percent}%", percent));
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
