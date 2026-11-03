namespace LunaPlayer.Spotify.Playback;

internal sealed partial class Sessions
{
    internal bool TogglePause()
    {
        lock (_navigationLock)
        {
            if (SynchronizeNavigation() is null) return false;
            var playing = !_playRequested;
            if (playing) ResumeCurrent(); else PauseCurrent();
            return playing;
        }
    }

    internal void Pause()
    {
        lock (_navigationLock)
        {
            if (SynchronizeNavigation() is null) return;
            PauseCurrent();
        }
    }

    private void PauseCurrent()
    {
        _playRequested = false;
        PauseLoaded();
        SetPlaying(false);
        PrepareSuccessor();
    }

    internal void Resume()
    {
        lock (_navigationLock)
        {
            if (SynchronizeNavigation() is null) return;
            ResumeCurrent();
        }
    }

    private void ResumeCurrent()
    {
        if (_ended && _current is { } track)
        {
            SetCurrent(track, playing: false);
            LoadTrack(track, 0);
            return;
        }
        _playRequested = true;
        PlayLoaded();
        SetPlaying(true);
        PrepareSuccessor();
    }

    internal void Seek(double seconds)
    {
        lock (_navigationLock)
        {
            if (SynchronizeNavigation() is null) return;
            SeekCurrent(seconds);
        }
    }

    internal void SeekRelative(double delta)
    {
        lock (_navigationLock)
        {
            if (SynchronizeNavigation() is not null && Elapsed is double elapsed)
                SeekCurrent(elapsed + delta);
        }
    }

    private void SeekCurrent(double seconds)
    {
        if (Duration is not double duration || duration <= 0 || !double.IsFinite(seconds)) return;
        var position = Math.Clamp(seconds, 0, duration);
        if (position < duration) _ended = false;
        if (_clock is { } clock)
            _clock = clock with { Elapsed = position, Duration = duration, ObservedAt = DateTimeOffset.UtcNow };
        if (IsLoadedCurrent)
        {
            // Ignore completion callbacks and page results belonging to the position before this seek.
            Interlocked.Increment(ref _loadGeneration);
            CancelPageRead();
            PrepareSuccessor();
            _player.SeekAbsolute(position);
        }
        else _pendingSeek = position;
    }
}
