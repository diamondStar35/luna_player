using LunaPlayer.Media;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    public IReadOnlyList<Chapter> GetChapters()
    {
        lock (_loadSync)
            return _disposed || !_loaded || _awaitingLoadRestart ? [] : Chapters.Read(_session.ReadObject("chapter-list"), _session.ReadDouble("duration"));
    }

    private void HandleChapterChange(string _, object? __)
    {
        if (!_disposed) ChaptersChanged?.Invoke();
    }

    /// <summary>keep-open pauses at EOF without sending END_FILE. Observe that held boundary too, while
    /// rejecting notifications from an outgoing entry and reporting each completion only once.</summary>
    private void HandleHeldEnd(string _, object? value)
    {
        lock (_loadSync)
        {
            if (value is false)
            {
                _heldEndNotified = false;
                return;
            }
            if (value is not true || _disposed || _awaitingLoadRestart || _pendingAdvance is not null
                || _activeEntryId != _session.PositiveEntryId() || _session.ReadBoolean("eof-reached") != true)
                return;
            if (_chapterStopEnd is double end && (!_selectionRangeActive || _selectionRangeEnd >= end))
            {
                if (!_chapterStopCompleted)
                {
                    _chapterStopCompleted = true;
                    Ended?.Invoke(EndReason.ChapterStop);
                }
            }
            else if (_selectionRangeActive && !_selectionRangeRepeats)
            {
                if (!_selectionRangeCompleted)
                {
                    _selectionRangeCompleted = true;
                    SelectionEnded?.Invoke(_selectionRangeEnd);
                }
            }
            else if (!_heldEndNotified && (_holdAutomaticAdvance
                || _endBehavior == Configuration.Models.EndBehavior.None))
            {
                _heldEndNotified = true;
                Ended?.Invoke(EndReason.EndOfFile);
            }
        }
    }

    /// <summary>Selections, chapters and the one-time stop share mpv's end option. Always restore the
    /// outer item boundary when a narrower selection is removed.</summary>
    private object EffectiveEnd(double? selectionEnd = null)
    {
        var end = _playbackRange?.End;
        if (_chapterStopEnd is double stop) end = Math.Min(end ?? stop, stop);
        if (selectionEnd is double selection) end = Math.Min(end ?? selection, selection);
        return end is double seconds ? seconds : "none";
    }

    public void SetChapterStop(double? end)
    {
        lock (_loadSync)
        {
            if (_chapterStopEnd == end)
                return;
            _chapterStopEnd = end;
            _chapterStopCompleted = false;
            if (end.HasValue)
            {
                ClearQueuedNext();
                CancelEndFade();
            }
            _session.SetPropertySafely("end", EffectiveEnd(_selectionRangeActive && !_selectionRangeRepeats
                ? _selectionRangeEnd : null));
            if (!_selectionRangeActive && !_endBehaviorRestorePending)
                ApplyEndBehavior();
        }
    }
}
