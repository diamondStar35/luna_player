using System.Globalization;
using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed partial class PlaybackActions
{
    private const double DefaultSeekStep = 5;
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly ISpeechOutput _speech;
    private readonly PlaybackSelection _selection;

    internal PlaybackActions(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        SettingsStore settingsStore,
        ISpeechOutput speech,
        PlaybackSelection selection)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _settingsStore = settingsStore;
        _speech = speech;
        _selection = selection;
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
        router.Register(ActionId.VolumeUp, () => ChangeVolume(_settings.Audio.VolumeStep));
        router.Register(ActionId.VolumeDown, () => ChangeVolume(-_settings.Audio.VolumeStep));
        router.Register(ActionId.VolumeMaximize, () => SetVolume(1000));
        router.Register(ActionId.VolumeMinimize, () => SetVolume(5));
        router.Register(ActionId.AnnounceVolume, AnnounceVolume);
        router.Register(ActionId.AnnounceElapsed, () => AnnounceTime(
            // Translators: Spoken before the time played so far, as in "Elapsed time: 1 minute 20 seconds".
            Tr("Elapsed time:"), _player.Elapsed));
        router.Register(ActionId.AnnounceRemaining, () => AnnounceTime(
            // Translators: Spoken before the time still left to play, as in "Remaining time: 1 minute 20 seconds".
            Tr("Remaining time:"), _player.Remaining));
        router.Register(ActionId.AnnounceDuration, () => AnnounceTime(
            // Translators: Spoken before the whole length of the file, as in "Total time: 3 minutes 40 seconds".
            Tr("Total time:"), _player.Duration));
        router.Register(ActionId.AnnouncePercent, AnnouncePercent);
        router.Register(ActionId.AnnounceSpeed, AnnounceSpeed);
        router.Register(ActionId.ToggleVerbosity, ToggleVerbosity);
        router.Register(ActionId.SpeedUp, () => ChangeSpeed(_settings.Audio.SpeedStep));
        router.Register(ActionId.SpeedDown, () => ChangeSpeed(-_settings.Audio.SpeedStep));
        router.Register(ActionId.ResetSpeed, () => SetSpeed(1));
        router.Register(ActionId.ToggleSilenceRemoval, ToggleSilenceRemoval);
        router.Register(ActionId.StartSelection, StartSelection);
        router.Register(ActionId.EndSelection, EndSelection);
        router.Register(ActionId.ClearSelection, ClearSelection);
    }

    private void RegisterGeneratedActions(ActionRouter router)
    {
        foreach (var step in PlaybackActionDefinitions.SeekSteps)
            router.Register(step.Id, () => SetSeekStep(step));
        foreach (var jump in PlaybackActionDefinitions.PercentJumps)
            router.Register(jump.Id, () => JumpToPercent(jump.Percent));
    }

    private void TogglePlayPause()
    {
        if (string.IsNullOrEmpty(_player.CurrentPath))
        {
            _speech.Speak(Tr("No file loaded."), Tr("No file."));
            return;
        }

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
        if (_selection.IsActive(_player.CurrentPath) && _selection.Start is double start)
            _player.SeekAbsolute(start);
        else
            _player.SeekAbsolute(0);
    }

    private void SeekEnd()
    {
        if (_selection.IsActive(_player.CurrentPath) && _selection.End is double end)
        {
            _player.SeekAbsolute(end);
            return;
        }
        if (!_player.SeekToEnd())
            _speech.Speak(
                // Translators: Spoken when the player cannot tell how long the file is, so it has no time to report or move to.
                Tr("Time not available"),
                // Translators: The short wording spoken when a time the user asked for is not known.
                Tr("Unavailable"));
    }

    private void GoToTime()
    {
        if (_player.Duration is not double duration || duration <= 0)
        {
            _view.ShowWarning(Tr("Time is not available for the current file."),
                // Translators: Title of the window that asks the user which point in the file to move to.
                Tr("Go to time"));
            return;
        }
        var position = _view.ChooseTime(duration, Math.Clamp(_player.Elapsed ?? 0, 0, duration));
        if (!position.HasValue)
            return;
        _player.SeekAbsolute(position.Value);
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
            _speech.Speak(Tr("Time not available"), Tr("Unavailable"));
        else
            _speech.Speak($"{label} {formatted}", formatted);
    }

    private void AnnouncePercent()
    {
        if (_player.Duration is not double duration || duration <= 0 || _player.Elapsed is not double elapsed)
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
        if (_player.Duration is not double duration || duration <= 0)
        {
            _speech.Speak(Tr("Time not available"), Tr("Unavailable"));
            return;
        }
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
        foreach (var step in PlaybackActionDefinitions.SeekSteps)
        {
            if (step.Key == _settings.Audio.SeekStepKey && step.Seconds > 0)
                return step.Seconds;
        }
        return DefaultSeekStep;
    }

    private static string FormatSpeed(double speed)
        => speed.ToString("0.###", CultureInfo.InvariantCulture);
}
