namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    // Pausing, resuming and stopping are state changes like any other, and they are announced like any other.
    // Nothing about them is visible in a property that raises an event of its own, so a listener that has to
    // follow whether the player is running - the play button and the Windows overlay both do - would
    // otherwise have no way of hearing about it.
    internal bool TogglePause() => NotifyStateChanged(_engine.TogglePause());

    internal void Play()
    {
        _engine.Play();
        StateChanged?.Invoke();
    }

    internal void Pause()
    {
        _engine.Pause();
        StateChanged?.Invoke();
    }

    internal void Seek(double seconds) => _engine.SeekRelative(seconds);

    internal void SeekAbsolute(double seconds) => _engine.SeekAbsolute(seconds);

    internal bool SeekToEnd()
    {
        if (Duration is not double duration || duration <= 0)
            return false;
        SeekAbsolute(Math.Max(0, duration - 0.2));
        return true;
    }

    internal void Stop()
    {
        StopEngine();
        StateChanged?.Invoke();
    }
}
