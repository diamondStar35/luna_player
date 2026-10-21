namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    internal bool IsShuffleEnabled => _playlist.IsShuffleEnabled;
    internal bool IsRepeatFileEnabled => _playlist.IsRepeatFileEnabled;
    internal bool IsSilenceRemovalEnabled => _silenceEnabled;
    internal bool IsNormalizationEnabled => _normalizationEnabled;
    internal bool IsMonoEnabled => _monoEnabled;

    internal bool ToggleShuffle()
    {
        var enabled = _playlist.ToggleShuffle();
        PrepareGaplessNext();
        return NotifyStateChanged(enabled);
    }

    internal bool ToggleRepeatFile()
    {
        var enabled = _playlist.ToggleRepeatFile();
        _engine.SetRepeatFile(enabled);
        PrepareGaplessNext();
        return NotifyStateChanged(enabled);
    }

    internal bool? ToggleCurrentMarked()
    {
        var value = _playlist.ToggleCurrentMarked();
        if (value.HasValue)
            StateChanged?.Invoke();
        return value;
    }

    internal bool ToggleAllMarked() => NotifyStateChanged(_playlist.ToggleAllMarked());

    internal bool ClearMarked()
    {
        var value = _playlist.ClearMarked();
        if (value)
            StateChanged?.Invoke();
        return value;
    }

    internal int MarkedCount => _playlist.MarkedCount;
    internal bool IsCurrentMarked => _playlist.IsCurrentMarked;
    internal bool AreAllMarked => _playlist.AreAllMarked;
    internal IReadOnlyList<string> MarkedFiles => _playlist.MarkedFiles;
}
