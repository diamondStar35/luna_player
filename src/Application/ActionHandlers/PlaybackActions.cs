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
        router.Register(ActionId.SeekBackwardX8, () => SeekByMultiplier(-8));
        router.Register(ActionId.SeekForwardX8, () => SeekByMultiplier(8));
        router.Register(ActionId.SeekStart, SeekStart);
        router.Register(ActionId.SeekEnd, SeekEnd);
        router.Register(ActionId.GoToTime, GoToTime);
        router.Register(ActionId.VolumeUp, () => ChangeVolume(_settings.Audio.VolumeStep));
        router.Register(ActionId.VolumeDown, () => ChangeVolume(-_settings.Audio.VolumeStep));
        router.Register(ActionId.VolumeMaximize, () => SetVolume(1000));
        router.Register(ActionId.VolumeMinimize, () => SetVolume(5));
        router.Register(ActionId.AnnounceVolume, AnnounceVolume);
        router.Register(ActionId.AnnounceElapsed, () => AnnounceTime("Elapsed time:", _player.Elapsed));
        router.Register(ActionId.AnnounceRemaining, () => AnnounceTime("Remaining time:", _player.Remaining));
        router.Register(ActionId.AnnounceDuration, () => AnnounceTime("Total time:", _player.Duration));
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
            _speech.Speak("No file loaded.", "No file.");
            return;
        }

        if (_player.Duration is null)
        {
            if (_player.Reload() && _settings.General.Verbosity == SpeechVerbosity.Beginner)
                _speech.SpeakText("Play");
            return;
        }

        var isPlaying = _player.TogglePause();
        _view.SetPlaying(isPlaying);
        if (_settings.General.Verbosity == SpeechVerbosity.Beginner)
            _speech.SpeakText(isPlaying ? "Play" : "Pause");
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
            _speech.Speak("Time not available", "Unavailable");
    }

    private void GoToTime()
    {
        if (_player.Duration is not double duration || duration <= 0)
        {
            _view.ShowWarning("Time is not available for the current file.", "Go to time");
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
        _speech.Speak($"Volume {whole} percent", $"{whole}%");
    }

    private void AnnounceVolume()
    {
        var whole = (int)_player.Volume;
        _speech.Speak($"Volume is {whole}%", $"{whole}%");
    }

    private void AnnounceTime(string label, double? seconds)
    {
        var formatted = PlaybackTimeFormatter.Format(seconds);
        if (formatted is null)
            _speech.Speak("Time not available", "Unavailable");
        else
            _speech.Speak($"{label} {formatted}", formatted);
    }

    private void AnnouncePercent()
    {
        if (_player.Duration is not double duration || duration <= 0 || _player.Elapsed is not double elapsed)
        {
            _speech.Speak("Percentage not available", "Unavailable");
            return;
        }
        var percent = Math.Clamp((int)(elapsed / duration * 100), 0, 100);
        _speech.Speak($"{percent} percent", $"{percent}%");
    }

    private void AnnounceSpeed() => _speech.SpeakText($"{FormatSpeed(_player.Speed)}x");

    private void ChangeSpeed(double delta) => SetSpeed(_player.Speed + delta);

    private void SetSpeed(double value)
    {
        var speed = _player.SetSpeed(value);
        _settings.Audio.Speed = speed;
        _speech.SpeakText($"{FormatSpeed(speed)}x");
    }

    private void ToggleVerbosity()
    {
        if (_settings.General.Verbosity == SpeechVerbosity.Advanced)
        {
            _settings.General.Verbosity = SpeechVerbosity.Beginner;
            _speech.SpeakText("Beginner mode");
        }
        else
        {
            _settings.General.Verbosity = SpeechVerbosity.Advanced;
            _speech.SpeakText("Advanced mode");
        }
        _settingsStore.SaveExplicit(_settings);
    }

    private void ToggleSilenceRemoval()
    {
        var enabled = !_player.IsSilenceRemovalEnabled;
        if (!_player.SetSilenceRemoval(enabled))
        {
            _speech.Speak("Could not change silence removal filter.", "Filter failed.");
            return;
        }
        _settings.Silence.Enabled = enabled;
        _settingsStore.SaveExplicit(_settings);
        _view.SetSilenceRemovalChecked(enabled);
        _speech.Speak(enabled ? "Silence removal on" : "Silence removal off",
            enabled ? "Silence removal on" : "Silence removal off");
    }

    private void JumpToPercent(int percent)
    {
        if (_player.Duration is not double duration || duration <= 0)
        {
            _speech.Speak("Time not available", "Unavailable");
            return;
        }
        if (percent >= 100)
            _player.SeekToEnd();
        else
            _player.SeekAbsolute(duration * percent / 100.0);
        _speech.Speak($"{percent} percent", $"{percent}%");
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
