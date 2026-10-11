using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Commands.Playback;

/// <summary>The commands behind the Sleep timer submenu: setting a timer, cancelling it, and hearing the time
/// left.</summary>
///
/// <remarks>
/// Setting opens the dialog prefilled from the last-used choices and the current armed state. When the dialog
/// comes back with a request, the choices are written to settings (so the window opens on them next time) and
/// the shared <see cref="SleepTimer"/> is armed; when it comes back asking to turn the timer off, the timer is
/// cancelled. End-of-track mode needs something playing to end, so it is refused with a warning when nothing is
/// loaded rather than arming a timer that could never fire.
/// </remarks>
internal sealed class Sleep
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly SleepTimer _timer;
    private readonly PlayerSettings _settings;
    private readonly SettingsStore _settingsStore;

    internal Sleep(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        SleepTimer timer,
        PlayerSettings settings,
        SettingsStore settingsStore)
    {
        _view = view;
        _player = player;
        _timer = timer;
        _settings = settings;
        _settingsStore = settingsStore;
        router.Register(ActionId.OpenSleepTimer, Open);
        router.Register(ActionId.CancelSleepTimer, _timer.Cancel);
        router.Register(ActionId.AnnounceSleepTimerRemaining, _timer.AnnounceRemaining);
    }

    private void Open()
    {
        var saved = _settings.SleepTimer;
        var initial = new SleepTimerRequest(
            saved.Mode, saved.DurationMinutes, saved.Action, saved.Fade, saved.FadeSeconds);
        var result = _view.ChooseSleepTimer(initial, _timer.IsArmed, _timer.IsArmed ? StatusText() : null);
        if (result is not SleepTimerDialogResult chosen)
            return;
        if (chosen.TurnOff)
        {
            _timer.Cancel();
            return;
        }
        var request = chosen.Request;
        if (request.Mode == SleepTimerMode.EndOfTrack && !_player.IsLoaded)
        {
            _view.ShowWarning(
                // Translators: Shown when the sleep timer is set to stop at the end of the current track but
                // nothing is playing.
                Tr("Open a file before setting the timer to stop at the end of the current track."),
                Tr("Sleep timer"));
            return;
        }
        Persist(request);
        _timer.Arm(request);
    }

    private void Persist(SleepTimerRequest request)
    {
        var saved = _settings.SleepTimer;
        saved.Mode = request.Mode;
        saved.Action = request.Action;
        saved.DurationMinutes = request.DurationMinutes;
        saved.Fade = request.Fade;
        saved.FadeSeconds = request.FadeSeconds;
        _settingsStore.SaveExplicit(_settings);
    }

    private string StatusText()
    {
        if (_timer.Mode == SleepTimerMode.EndOfTrack)
            // Translators: Status line in the sleep-timer window when a timer is already set to stop at the end
            // of the current track.
            return Tr("A timer is set to stop at the end of the current track.");
        var formatted = PlaybackTimeFormatter.Format(_timer.Remaining) ?? "00:00:00";
        // Translators: Status line in the sleep-timer window when a timed timer is already running. {time} is a
        // clock reading like 00:12:30.
        return TrFormat("A timer is set. {time} remaining.", formatted);
    }
}
