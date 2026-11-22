namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    // Keep song coordinates here. Each new transcoded response can have a different time origin.
    private Boundaries _mediaBoundaries = Boundaries.None;
    private bool IsResolvedMedia => CurrentPath is { } path && _mediaResolver?.Handles(path) == true;
    private double EngineTime(double source) => Math.Max(0, source - (_currentInput?.Offset ?? 0));

    private void ClearMediaBoundaries() => _mediaBoundaries = Boundaries.None;

    private bool ReopenMediaAfterBoundaryChange(double? position, bool paused)
    {
        // Seek-by-reopening inputs cannot rely on a native recovery seek after releasing a
        // demux/decoder boundary. Request a fresh response using the same song coordinates.
        if (_resolvingMedia || _engine.IsLoading || _currentInput?.SeekByReopening != true) return false;
        return BeginMediaLoad(CurrentPath!, position, paused, fade: false);
    }

    private void HoldMediaSuccessor()
    {
        _mediaNext?.Cancel();
        _engine.ClearQueuedNext();
    }

    private bool SetMediaLoopStart(double source)
    {
        if (!double.IsFinite(source)) return false;
        if (_mediaBoundaries.Selection.HasValue) _engine.ClearSelectionRange(restoreEndBehavior: false);
        _mediaBoundaries = new(LoopStart: source);
        HoldMediaSuccessor();
        if (_resolvingMedia || source < (_currentInput?.Offset ?? 0))
            return BeginMediaLoad(CurrentPath!, SourceElapsed, IsPaused, fade: false);
        return _engine.SetLoopStart(EngineTime(source));
    }

    private bool SetMediaLoopEnd(double source)
    {
        if (!double.IsFinite(source) || _mediaBoundaries.LoopStart is not { } start || source <= start) return false;
        _mediaBoundaries = _mediaBoundaries with { LoopEnd = source };
        return _resolvingMedia || _engine.SetLoopEnd(EngineTime(source));
    }

    private bool SetMediaSelection(double start, double end, bool repeat, bool play)
    {
        start = ToSourceTime(start); end = ToSourceTime(end);
        if (!double.IsFinite(start) || !double.IsFinite(end) || end <= start) return false;
        var previous = _mediaBoundaries;
        _mediaBoundaries = new(Selection: new(start, end), RepeatSelection: repeat);
        HoldMediaSuccessor();
        if (_resolvingMedia || start < (_currentInput?.Offset ?? 0)
            || play && (_engine.IsLoading || _currentInput?.SeekByReopening == true))
            return BeginMediaLoad(CurrentPath!, play ? start : SourceElapsed, play ? false : IsPaused, fade: false);
        var accepted = play ? _engine.PlaySelectionRange(EngineTime(start), EngineTime(end), repeat)
            : _engine.SetSelectionRange(EngineTime(start), EngineTime(end), repeat);
        if (!accepted) _mediaBoundaries = previous;
        return accepted;
    }
}
