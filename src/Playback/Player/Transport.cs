namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    // Pausing, resuming and stopping are state changes like any other, and they are announced like any other.
    // Nothing about them is visible in a property that raises an event of its own, so a listener that has to
    // follow whether the player is running - the play button and the Windows overlay both do - would
    // otherwise have no way of hearing about it.
    internal bool TogglePause()
    {
        if (_resolvingMedia)
        {
            if (_pendingMediaPaused) Play(); else Pause();
            return !_pendingMediaPaused;
        }
        if (_chapterStopHeld)
        {
            ResumeAfterChapterStop();
            return IsPlaying;
        }
        if (IsPaused && IsAtItemEnd)
        {
            ResumeCompletedItem();
            return IsPlaying;
        }
        return NotifyStateChanged(_engine.TogglePause());
    }

    internal void Play()
    {
        if (_resolvingMedia) { _pendingMediaPaused = false; StateChanged?.Invoke(); return; }
        if (!_loaded && CurrentPath is { } identity && _mediaResolver?.Handles(identity) == true) { LoadCurrent(0, false); return; }
        if (_chapterStopHeld)
        {
            ResumeAfterChapterStop();
            return;
        }
        if (IsAtItemEnd)
        {
            ResumeCompletedItem();
            return;
        }
        _engine.Play();
        StateChanged?.Invoke();
    }

    private bool IsAtItemEnd => !_engine.IsLoading && _engine.EndReached
        && SourceElapsed is double elapsed
        && (_playlist.CurrentItem?.End ?? _engine.Duration) is double end && elapsed >= end - 0.001;

    private void ResumeCompletedItem()
    {
        if (IsRepeatFileEnabled || _endBehavior != Configuration.Models.EndBehavior.Advance)
            RestartCurrent();
        else if (!Next(_wrapPlaylist))
            Stop();
    }

    internal void Pause()
    {
        if (_resolvingMedia) { _pendingMediaPaused = true; _engine.Pause(); StateChanged?.Invoke(); return; }
        _engine.Pause();
        StateChanged?.Invoke();
    }

    internal void Seek(double seconds)
    {
        Interlocked.Increment(ref _playbackRevision);
        if ((_playlist.CurrentItem?.Chapter is not null || _engine.IsLoading || _resolvingMedia || _currentInput is not null) && Elapsed is double elapsed)
            SeekAbsolute(elapsed + seconds);
        else
        {
            if (_stopAtChapterEnd && SourceElapsed is double source)
                FollowChapterStop(Math.Max(0, source + seconds));
            _engine.SeekRelative(seconds);
        }
    }

    internal void SeekAbsolute(double seconds)
    {
        Interlocked.Increment(ref _playbackRevision);
        var source = ToSourceTime(seconds);
        RememberDiscSeek(source);
        _requestedSourcePosition = source;
        if (_chapterStopHeld)
        {
            LoadCurrent(source, paused: true);
            return;
        }
        if (CurrentPath is { } identity && _mediaResolver?.Handles(identity) == true
            && (_resolvingMedia || _currentInput?.SeekByReopening == true))
        { BeginMediaLoad(identity, source, IsPaused, fade: false); return; }
        FollowChapterStop(source);
        _engine.SeekAbsolute(EngineTime(source));
    }

    internal bool SeekToEnd()
    {
        if (Duration is not double duration || duration <= 0)
            return false;
        SeekAbsolute(_playlist.CurrentItem?.Chapter is not null ? duration : Math.Max(0, duration - 0.2));
        return true;
    }

    internal void Stop()
    {
        StopEngine();
        StateChanged?.Invoke();
    }
}
