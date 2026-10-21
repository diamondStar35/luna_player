namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    /// <summary>Turns the system media controls - mpv's transport overlay and media keys - on or off.
    /// Raises <see cref="StateChanged"/> so the Windows overlay the controller owns is reconciled from the
    /// same setting.</summary>
    internal void SetMediaControls(bool enabled)
    {
        _engine.SetMediaControls(enabled);
        StateChanged?.Invoke();
    }

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
