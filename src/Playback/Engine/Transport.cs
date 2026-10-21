using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

internal sealed partial class Engine
{
    public bool TogglePause()
    {
        if (IsPaused)
        {
            Play();
            return true;
        }
        Pause();
        return false;
    }

    public void Play()
    {
        // Clearing an mpv end boundary does not release its EOF latch. When playback is explicitly resumed
        // from the free slot, seek to the exact current timestamp to re-arm decoding, then play after mpv
        // confirms that zero-distance seek. Entering the free slot itself never changes time or playback.
        if (_selectionSeekPending)
            return;
        if (_endBehaviorRestorePending
            && (IsPaused || _releasedSelectionWasCompleted)
            && (Elapsed ?? (_releasedSelectionEndPending ? _releasedSelectionEnd : null)) is double position
            && BeginSelectionSeek(position, restoreEndBehavior: true, play: true))
            return;
        _pauseRequested = false;
        if (_fadePauseResume && _fadeIntervalMilliseconds > 0)
        {
            _fader.Set(0);
            _resumeFadePending = true;
            _resumeFadePosition = Elapsed;
            if (!TrySetProperty("pause", false))
            {
                _resumeFadePending = false;
                _resumeFadePosition = null;
                _pauseRequested = null;
                _fader.Set(1);
            }
        }
        else
        {
            _resumeFadePending = false;
            _resumeFadePosition = null;
            _fader.Set(1);
            SetPropertySafely("pause", false);
            _pauseRequested = null;
        }
    }

    public void Pause()
    {
        CancelEndFade();
        _resumeFadePending = false;
        _resumeFadePosition = null;
        if (!_fadePauseResume || _fadeIntervalMilliseconds <= 0 || IsPaused)
        {
            SetPropertySafely("pause", true);
            _fader.Set(1);
            _pauseRequested = null;
            return;
        }
        _pauseRequested = true;
        _fader.FadeTo(0, _fadeIntervalMilliseconds, () =>
        {
            if (_disposed || _pauseRequested != true)
                return;
            SetPropertySafely("pause", true);
            _fader.Set(1);
            _pauseRequested = null;
        });
    }

    public bool IsPaused => _pauseRequested ?? ReadBoolean("pause") ?? false;

    public bool SelectionRangeCompleted
        => _selectionRangeActive
            && !_selectionRangeRepeats
            && (_selectionRangeCompleted || ReadBoolean("eof-reached") == true);

    public double? Duration => Precision.Normalize(ReadDouble("duration"));

    public double? Elapsed => Precision.Normalize(ReadDouble("time-pos"));

    public double? Remaining => Precision.Normalize(ReadDouble("time-remaining"));

    public void SeekRelative(double seconds)
    {
        var delta = Precision.Normalize(seconds);
        FadeAroundSeek(() => SeekRelativeCore(delta));
    }

    public void SeekAbsolute(double seconds)
    {
        var target = Precision.Normalize(Math.Max(0, seconds));
        FadeAroundSeek(() => SeekAbsoluteCore(target));
    }

    private bool SeekRelativeCore(double delta)
    {
        if (_endBehaviorRestorePending
            && Elapsed is double position
            && BeginSelectionSeek(position + delta, restoreEndBehavior: true, play: !IsPaused))
            return true;
        return TryDo(mpv => mpv.Command("seek", delta, "relative"));
    }

    private bool SeekAbsoluteCore(double target)
    {
        if (_endBehaviorRestorePending
            && BeginSelectionSeek(target, restoreEndBehavior: true, play: !IsPaused))
            return true;
        return TryDo(mpv => mpv.Command("seek", target, "absolute"));
    }

    private void FadeAroundSeek(Func<bool> seek)
    {
        CancelEndFade();
        if (!_fadeSeeking || !_loaded)
        {
            seek();
            return;
        }

        // Seeking must remain responsive when a key is pressed repeatedly. Silence the current sample,
        // move immediately, then fade the new position in; another seek simply replaces this fade.
        _seekFadeActive = true;
        _fader.Set(0);
        if (!seek())
        {
            _seekFadeActive = false;
            _fader.Set(1);
            return;
        }
        _fader.FadeTo(1, _seekFadeIntervalMilliseconds, () => _seekFadeActive = false);
    }

}
