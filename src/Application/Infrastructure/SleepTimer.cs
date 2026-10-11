using LunaPlayer.Accessibility;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application;

/// <summary>The sleep timer: a countdown, or a wait for the current track to end, after which playback is
/// paused or stopped - optionally fading the volume down over the last seconds first.</summary>
///
/// <remarks>
/// The countdown runs on the idiomatic UI-thread clock (<see cref="IApplicationDispatcher.Repeat"/>), the same
/// pattern the media-controls clock uses, so every volume change and the firing itself happen on the UI thread
/// where the player expects them. The end-of-track mode has nothing to count: its firing is driven by
/// <see cref="OnTrackEnded"/>, which the controller calls when a track ends; a tick runs then only to carry out
/// the fade over the track's final seconds.
///
/// The fade touches only <see cref="MediaPlayer.SetVolume"/>, never the saved <c>audio.volume</c>. The user's
/// volume is captured the moment the fade starts (so a change made during the countdown is respected) and put
/// back on firing, cancelling, or disposal. The controller disposes this before it reads the player volume into
/// settings at shutdown, so a faded-down value is never saved as the user's own.
/// </remarks>
internal sealed class SleepTimer : IDisposable
{
    private readonly IApplicationDispatcher _dispatcher;
    private readonly MediaPlayer _player;
    private readonly ISpeechOutput _speech;

    private bool _armed;
    private SleepTimerMode _mode;
    private SleepTimerAction _action;
    private bool _fade;
    private int _fadeSeconds;
    private DateTime _deadline;
    private IDisposable? _tick;
    private double? _originalVolume;
    private bool _warned;

    internal SleepTimer(IApplicationDispatcher dispatcher, MediaPlayer player, ISpeechOutput speech)
    {
        _dispatcher = dispatcher;
        _player = player;
        _speech = speech;
    }

    internal bool IsArmed => _armed;
    internal SleepTimerMode Mode => _mode;

    /// <summary>Seconds left before a duration timer fires, or null when none is armed or the timer waits on
    /// the end of the track rather than a clock.</summary>
    internal double? Remaining
        => _armed && _mode == SleepTimerMode.Duration
            ? Math.Max(0, (_deadline - DateTime.UtcNow).TotalSeconds)
            : null;

    /// <summary>Starts a timer, replacing any already running, and announces what was set.</summary>
    internal void Arm(SleepTimerRequest request)
    {
        RestoreVolume();
        StopTick();
        _armed = true;
        _mode = request.Mode;
        _action = request.Action;
        _fade = request.Fade;
        _fadeSeconds = Math.Max(1, request.FadeSeconds);
        _warned = false;

        if (_mode == SleepTimerMode.Duration)
        {
            var minutes = Math.Max(1, request.DurationMinutes);
            _deadline = DateTime.UtcNow.AddMinutes(minutes);
            _tick = _dispatcher.Repeat(TimeSpan.FromSeconds(1), OnTick);
            _speech.Speak(
                // Translators: Spoken when a sleep timer is set to fire after a number of minutes. {minutes} is
                // that number.
                TrFormat("Sleep timer set for {minutes} minutes.", minutes),
                TrFormat("Sleep {minutes}m", minutes));
        }
        else
        {
            // The stop comes from the end-of-track hook; a tick is needed only to run the fade.
            if (_fade)
                _tick = _dispatcher.Repeat(TimeSpan.FromSeconds(1), OnTick);
            AnnounceEndOfTrackArmed();
        }
    }

    /// <summary>Stops the timer, restoring the volume if a fade was under way, and says so.</summary>
    internal void Cancel()
    {
        if (!_armed)
        {
            AnnounceNotSet();
            return;
        }
        StopTick();
        RestoreVolume();
        _armed = false;
        _speech.Speak(
            // Translators: Spoken when a running sleep timer is turned off.
            Tr("Sleep timer cancelled."), Tr("Sleep off"));
    }

    /// <summary>Speaks how long is left, or that the timer waits on the end of the track, or that none is set.
    /// </summary>
    internal void AnnounceRemaining()
    {
        if (!_armed)
        {
            AnnounceNotSet();
            return;
        }
        if (_mode == SleepTimerMode.EndOfTrack)
        {
            AnnounceEndOfTrackArmed();
            return;
        }
        var formatted = PlaybackTimeFormatter.Format(Remaining) ?? "00:00:00";
        _speech.Speak(
            // Translators: Spoken when the user asks how long is left on the sleep timer. {time} is a clock
            // reading like 00:12:30.
            TrFormat("Sleep timer: {time} remaining.", formatted), formatted);
    }

    /// <summary>Called by the controller when a track ends. Fires an end-of-track timer and returns true so the
    /// controller does not then advance or repeat; returns false, changing nothing, for any other state.
    /// </summary>
    internal bool OnTrackEnded()
    {
        if (!_armed || _mode != SleepTimerMode.EndOfTrack)
            return false;
        StopTick();
        RestoreVolume();
        // On pause there is nothing to do: mpv's keep-open already holds the finished file paused, and the
        // controller syncs the view. On stop the file is unloaded.
        if (_action == SleepTimerAction.Stop)
            _player.Stop();
        _armed = false;
        AnnounceFired();
        return true;
    }

    /// <summary>Stops the tick and restores the volume without speaking. Used at shutdown, where a fade must be
    /// undone before the player volume is read into settings but no announcement is wanted.</summary>
    public void Dispose()
    {
        StopTick();
        RestoreVolume();
        _armed = false;
    }

    private void OnTick()
    {
        if (!_armed)
            return;
        if (_mode == SleepTimerMode.Duration)
        {
            var remaining = (_deadline - DateTime.UtcNow).TotalSeconds;
            if (remaining <= 0)
            {
                Fire();
                return;
            }
            if (!_warned && remaining <= 60)
            {
                _warned = true;
                _speech.Speak(
                    // Translators: Spoken once, a minute before a sleep timer fires.
                    Tr("Sleep timer: one minute remaining."), Tr("Sleep 1:00"), interrupt: false);
            }
            ApplyFade(remaining);
        }
        else if (_player.Remaining is double trackRemaining)
        {
            ApplyFade(trackRemaining);
        }
    }

    /// <summary>Lowers the volume in proportion to how much of the fade window is left, capturing the user's
    /// volume the first time so it can be put back. Does nothing until the last <see cref="_fadeSeconds"/>.
    /// </summary>
    private void ApplyFade(double remaining)
    {
        if (!_fade || remaining > _fadeSeconds)
            return;
        _originalVolume ??= _player.Volume;
        var factor = Math.Clamp(remaining / _fadeSeconds, 0, 1);
        _player.SetVolume(_originalVolume.Value * factor);
    }

    private void Fire()
    {
        StopTick();
        RestoreVolume();
        if (_action == SleepTimerAction.Stop)
            _player.Stop();
        else if (_player.IsPlaying)
            _player.TogglePause();
        _armed = false;
        AnnounceFired();
    }

    private void RestoreVolume()
    {
        if (_originalVolume is double volume)
        {
            _player.SetVolume(volume);
            _originalVolume = null;
        }
    }

    private void StopTick()
    {
        _tick?.Dispose();
        _tick = null;
    }

    private void AnnounceEndOfTrackArmed()
        => _speech.Speak(
            // Translators: Spoken when the sleep timer is set to fire at the end of the current track.
            Tr("Sleep timer set for the end of the current track."), Tr("Sleep: end of track"));

    private void AnnounceFired()
        => _speech.Speak(
            _action == SleepTimerAction.Stop
                // Translators: Spoken when the sleep timer fires and stops playback.
                ? Tr("Sleep timer: playback stopped.")
                // Translators: Spoken when the sleep timer fires and pauses playback.
                : Tr("Sleep timer: playback paused."),
            _action == SleepTimerAction.Stop ? Tr("Sleep: stopped") : Tr("Sleep: paused"));

    private void AnnounceNotSet()
        => _speech.Speak(
            // Translators: Spoken when a sleep-timer command is used but no timer is currently set.
            Tr("No sleep timer is set."), Tr("No sleep timer"));
}
