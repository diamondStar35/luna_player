using LunaPlayer.Configuration.Models;
using MpvNet;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    private Boundaries _boundaries = Boundaries.None;

    private void SetLoadBoundaries(Boundaries boundaries)
    {
        _boundaries = boundaries;
        _selectionRangeActive = boundaries.Selection.HasValue;
        _selectionRangeRepeats = boundaries.RepeatSelection;
        _selectionRangeCompleted = false;
        _selectionRangeEnd = boundaries.Selection?.End ?? 0;
        _selectionRangeEntryId = null;
    }

    private bool UpdateOpeningBoundaries(Boundaries boundaries)
    {
        lock (_loadSync)
        {
            _boundaries = boundaries;
            if (!IsLoading) return false;
            // A manual replacement can still be waiting for its outgoing fade or metadata probe.
            // Update the request too, so the destination never starts with obsolete per-file options.
            if (_manualRequest is { } request) _manualRequest = request with { Boundaries = boundaries };
            return true;
        }
    }

    private Dictionary<string, object?> BoundaryOptions(Boundaries boundaries, PlaybackRange? range,
        double? chapterStop, bool restoring = false)
    {
        var selection = boundaries.Selection;
        var end = range?.End;
        if (chapterStop is { } stop) end = Math.Min(end ?? stop, stop);
        if (selection is { } selected && !boundaries.RepeatSelection)
            end = Math.Min(end ?? selected.End, selected.End);
        var repeat = selection.HasValue && boundaries.RepeatSelection;
        return new()
        {
            ["end"] = end is { } last ? last : "none",
            ["ab-loop-a"] = (repeat ? selection?.Start : boundaries.LoopStart) is { } start ? start : "no",
            ["ab-loop-b"] = (repeat ? selection?.End : boundaries.LoopEnd) is { } loopEnd ? loopEnd : "no",
            ["keep-open"] = selection.HasValue || restoring || chapterStop.HasValue || _holdAutomaticAdvance
                || _endBehavior == EndBehavior.None ? "yes" : "no",
            ["loop-file"] = selection is null && !restoring && !_holdAutomaticAdvance && range is null
                && chapterStop is null && _endBehavior == EndBehavior.Loop ? "inf" : "no",
        };
    }

    private void ApplyOpeningBoundaries(MPV mpv, long generation)
    {
        Dictionary<string, object?> options;
        lock (_loadSync)
        {
            if (_disposed || generation != _loadGeneration) return;
            // Unbounded loads already received their options with loadfile. Reapplying them here
            // can interfere with a seek issued as soon as the new input becomes ready.
            if (_boundaries.IsEmpty && !_endBehaviorRestorePending) return;
            options = BoundaryOptions(_boundaries, _playbackRange, _chapterStopEnd, _endBehaviorRestorePending);
        }
        // This runs on the native worker before the incoming audio is unpaused. The GUI never waits
        // for these properties, and controls changed during opening supersede the initial load options.
        foreach (var option in options) mpv.SetProperty(option.Key, option.Value);
    }
}
