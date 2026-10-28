using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    public bool SetLoopStart(double seconds)
    {
        var startSet = _session.TrySetProperty("ab-loop-a", Precision.Normalize(Math.Max(0, seconds)));
        var endCleared = _session.TrySetProperty("ab-loop-b", "no");
        return startSet && endCleared;
    }

    public bool SetLoopEnd(double seconds)
        => _session.TrySetProperty("ab-loop-b", Precision.Normalize(Math.Max(0, seconds)));

    public bool ClearLoop()
    {
        var startCleared = _session.TrySetProperty("ab-loop-a", "no");
        var endCleared = _session.TrySetProperty("ab-loop-b", "no");
        return startCleared && endCleared;
    }

    public bool SetSelectionRange(double start, double end, bool repeat)
    {
        CancelEndFade();
        start = Precision.Normalize(Math.Max(0, start));
        end = Precision.Normalize(Math.Max(start, end));
        if (end <= start)
            return false;
        // A move between slots replaces the previous range as one operation. Clear its boundary first so
        // neither an old one-shot end nor an old A-B loop can affect the slot being entered.
        if (_selectionRangeActive && !ClearSelectionRange(restoreEndBehavior: false))
        {
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
            return false;
        }
        CancelSelectionSeek();
        _endBehaviorRestorePending = false;
        _selectionRangeActive = true;
        _selectionRangeRepeats = repeat;
        _selectionRangeCompleted = false;
        _selectionRangeEnd = end;
        _selectionRangeEntryId = _session.PositiveEntryId();
        var endSet = _session.TrySetProperty("end", EffectiveEnd(repeat ? null : end));
        var fileLoopCleared = _session.TrySetProperty("loop-file", "no");
        // A one-shot selection reaches an artificial EOF. keep-open makes mpv hold the exact boundary so
        // Play can later clear it and continue from that same timestamp.
        var heldOpen = _session.TrySetProperty("keep-open", "yes");
        if (repeat)
        {
            var startSet = _session.TrySetProperty("ab-loop-a", start);
            var loopEndSet = _session.TrySetProperty("ab-loop-b", end);
            var succeeded = endSet && fileLoopCleared && heldOpen && startSet && loopEndSet;
            if (!succeeded)
                ClearSelectionRange();
            return succeeded;
        }
        var loopCleared = ClearLoop();
        var oneShotSucceeded = endSet && fileLoopCleared && heldOpen && loopCleared;
        if (!oneShotSucceeded)
            ClearSelectionRange();
        return oneShotSucceeded;
    }

    public bool PlaySelectionRange(double start, double end, bool repeat)
    {
        var wasPaused = IsPaused;
        Pause();
        if (!SetSelectionRange(start, end, repeat)
            || !BeginSelectionSeek(start, restoreEndBehavior: false, play: true))
        {
            ClearSelectionRange();
            if (!wasPaused)
                Play();
            return false;
        }
        return true;
    }

    public bool ExitSelectionRange(double end, bool play)
    {
        if (!ClearSelectionRange(restoreEndBehavior: false))
        {
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
            return false;
        }
        if (BeginSelectionSeek(end, restoreEndBehavior: true, play))
            return true;
        _endBehaviorRestorePending = false;
        ApplyEndBehavior();
        return false;
    }

    public bool ClearSelectionRange(bool restoreEndBehavior = true)
    {
        if (!_selectionRangeActive)
            return true;
        var releasedOneShot = !_selectionRangeRepeats;
        if (releasedOneShot)
        {
            // END_FILE may already be queued when the boundary is removed. Keep enough identity to label
            // that event as the released selection's end instead of the real end of the current file.
            _releasedSelectionEndPending = true;
            _releasedSelectionEnd = _selectionRangeEnd;
            _releasedSelectionEntryId = _selectionRangeEntryId;
            _releasedSelectionWasPlaying = !IsPaused;
            _releasedSelectionWasCompleted = SelectionRangeCompleted;
            _releasedSelectionPassed = false;
        }
        CancelSelectionSeek();
        _selectionRangeActive = false;
        _selectionRangeRepeats = false;
        _selectionRangeCompleted = false;
        _selectionRangeEnd = 0;
        _selectionRangeEntryId = null;
        var endCleared = _session.TrySetProperty("end", EffectiveEnd());
        var loopCleared = ClearLoop();
        if (restoreEndBehavior)
        {
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
        }
        else if (releasedOneShot)
            _endBehaviorRestorePending = true;
        else
        {
            // A repeating range has no artificial EOF latch: removing its A-B loop is sufficient, so the
            // configured file-end behaviour can be restored immediately without a recovery seek.
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
        }
        return endCleared && loopCleared;
    }

}
