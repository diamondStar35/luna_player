using MpvNet;

namespace LunaPlayer.Playback;

internal sealed partial class Engine
{
    private void HandleEndFile(MpvEvent mpvEvent)
    {
        if (_disposed || mpvEvent.Data is not MpvEndFileEvent endFile)
            return;
        var releasedSelectionEnd = false;
        var continueReleasedSelection = false;
        var releasedSelectionEndPosition = 0d;
        if (endFile.Reason == MpvEndFileReason.Eof
            && _releasedSelectionEndPending
            && (!_releasedSelectionEntryId.HasValue
                || endFile.PlaylistEntryId <= 0
                || _releasedSelectionEntryId.Value == endFile.PlaylistEntryId))
        {
            // A boundary that was already firing when the free slot was entered arrives after the boundary
            // has been removed. Match it to that boundary's timestamp; merely being before the file's real
            // end is not enough, because a newer selection in the same file may now be ending instead.
            releasedSelectionEnd = !_releasedSelectionPassed
                && (Elapsed is not double elapsed
                    || Math.Abs(elapsed - _releasedSelectionEnd) <= 0.05);
            continueReleasedSelection = releasedSelectionEnd
                && _releasedSelectionWasPlaying
                && !_selectionRangeActive;
            releasedSelectionEndPosition = _releasedSelectionEnd;
            _releasedSelectionEndPending = false;
            _releasedSelectionEnd = 0;
            _releasedSelectionEntryId = null;
            _releasedSelectionWasPlaying = false;
            _releasedSelectionWasCompleted = false;
            _releasedSelectionPassed = false;
        }
        if (releasedSelectionEnd
            || endFile.Reason == MpvEndFileReason.Eof
                && _selectionRangeActive
                && !_selectionRangeRepeats)
        {
            if (!releasedSelectionEnd)
                _selectionRangeCompleted = true;
            // Selection completion has its own channel. It must never be mistaken for the real end of a
            // file, even when the UI enters the free slot while mpv is already queuing this event.
            SelectionEnded?.Invoke(releasedSelectionEnd
                ? releasedSelectionEndPosition
                : _selectionRangeEnd);
            if (continueReleasedSelection)
                BeginSelectionSeek(releasedSelectionEndPosition, restoreEndBehavior: true, play: true);
            return;
        }
        var reason = endFile.Reason switch
        {
            MpvEndFileReason.Eof => EndReason.EndOfFile,
            MpvEndFileReason.Error => EndReason.Error,
            _ => (EndReason?)null,
        };
        if (reason.HasValue)
        {
            if (reason == EndReason.EndOfFile && _queuedNext
                && _queuedSourceEntryId.HasValue
                && (endFile.PlaylistEntryId <= 0 || _queuedSourceEntryId.Value == endFile.PlaylistEntryId)
                && (ReadInt64("playlist-count") ?? 0) > 1)
            {
                _queuedNext = false;
                _queuedSourceEntryId = null;
                _nextRequest = null;
                // A gapless transition must retain continuous volume. File-transition fading remains
                // available for manual navigation and for the ordinary end path when no successor queued.
                _fadeInOnRestart = false;
                CancelEndFade();
                _gaplessAdvancePending = true;
            }
            else
            {
                _gaplessAdvancePending = false;
                Ended?.Invoke(reason.Value);
            }
        }
    }
}
