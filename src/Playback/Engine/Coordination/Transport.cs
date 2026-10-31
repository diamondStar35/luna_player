using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    public bool TogglePause()
    {
        lock (_loadSync)
            return TogglePauseCore();
    }

    private bool TogglePauseCore()
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
        lock (_loadSync)
            PlayCore();
    }

    private void PlayCore()
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
        if (IsPaused)
            ResumePlayback();
    }

    private void ResumePlayback()
    {
        lock (_loadSync)
            ResumePlaybackCore();
    }

    private void ResumePlaybackCore()
    {
        _loadingPaused = false;
        var interval = _fadePauseResume ? _fadeIntervalMilliseconds / 1000.0 : 0;
        _session.TryDo(mpv => mpv.FadePlay(interval), mpv =>
        {
            mpv.FadeVolume(1, 0);
            mpv.SetProperty("pause", false);
        });
    }

    public void Pause()
    {
        lock (_loadSync)
            PauseCore();
    }

    private void PauseCore()
    {
        if (IsPaused)
            return;
        _loadingPaused = true;
        CancelEndFade(restoreVolume: false);
        var interval = _fadePauseResume ? _fadeIntervalMilliseconds / 1000.0 : 0;
        _session.TryDo(mpv => mpv.FadePause(interval), mpv =>
        {
            mpv.SetProperty("pause", true);
            mpv.FadeVolume(1, 0);
        });
    }

    private void HandlePauseChange(string _, object? value)
    {
        lock (_loadSync)
        {
            if (!_awaitingLoadRestart && _manualRequest is null && value is bool paused && _loadingPaused == paused)
                _loadingPaused = null;
        }
    }

    public bool EndReached => _session.ReadBoolean("eof-reached") == true;

    public bool IsLoading => _awaitingLoadRestart || _manualRequest is not null;

    public bool IsPaused => _loadingPaused ?? _session.ReadBoolean("audio-fade/paused") ?? _session.ReadBoolean("pause") ?? false;

    public bool SelectionRangeCompleted
        => _selectionRangeActive
            && !_selectionRangeRepeats
            && (_selectionRangeCompleted || _session.ReadBoolean("eof-reached") == true);

    public double? Duration => Precision.Normalize(_session.ReadDouble("duration"));

    public double? Elapsed => Precision.Normalize(_session.ReadDouble("time-pos"));

    public double? Remaining => Precision.Normalize(_session.ReadDouble("time-remaining"));

    public void SeekRelative(double seconds)
    {
        var delta = Precision.Normalize(seconds);
        if (_endBehaviorRestorePending
            && Elapsed is double position
            && BeginSelectionSeek(position + delta, restoreEndBehavior: true, play: !IsPaused))
            return;
        Seek(delta, absolute: false);
    }

    public void SeekAbsolute(double seconds)
    {
        var target = Precision.Normalize(Math.Max(0, seconds));
        if (_endBehaviorRestorePending
            && BeginSelectionSeek(target, restoreEndBehavior: true, play: !IsPaused))
            return;
        Seek(target, absolute: true);
    }

    private void Seek(double target, bool absolute)
    {
        if (SeekPendingManualLoad(target, absolute))
            return;
        CancelEndFade(restoreVolume: false);
        void PlainSeek(MpvNet.MPV mpv)
        {
            mpv.FadeVolume(1, 0);
            mpv.Command("seek", target, absolute ? "absolute" : "relative");
        }
        var interval = _seekFadeIntervalMilliseconds / 1000.0;
        if (_fadeSeeking && _loaded)
        {
            _session.TryDo(mpv => mpv.FadeSeek(target, interval, absolute), PlainSeek);
            return;
        }
        _session.TryDo(PlainSeek);
    }
}
