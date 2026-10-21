namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    internal bool SetLoopStart(double seconds) => _engine.SetLoopStart(seconds);

    internal bool SetLoopEnd(double seconds) => _engine.SetLoopEnd(seconds);

    internal bool ClearLoop() => _engine.ClearLoop();

    internal bool SetSelectionRange(double start, double end, bool repeat)
    {
        _engine.ClearQueuedNext();
        return _engine.SetSelectionRange(start, end, repeat);
    }

    internal bool PlaySelectionRange(double start, double end, bool repeat)
    {
        _engine.ClearQueuedNext();
        return _engine.PlaySelectionRange(start, end, repeat);
    }
    internal bool ExitSelectionRange(double end, bool play)
        => _engine.ExitSelectionRange(end, play);

    internal bool ClearSelectionRange(bool restoreEndBehavior = true)
    {
        var cleared = _engine.ClearSelectionRange(restoreEndBehavior);
        PrepareGaplessNext();
        return cleared;
    }
}
