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

    internal void TrackPositions(bool enabled) => _trackPositions = enabled;

    internal void SavePosition() => SavePosition(CurrentPath, Elapsed);

    internal void SavePosition(string? path, double? elapsed)
    {
        if (!_trackPositions || _positions is null || path is null || !File.Exists(path)
            || elapsed is not double position)
            return;
        _positions.Set(path, position);
    }

    internal IReadOnlyList<AudioDevice> GetAudioDevices() => _engine.GetAudioDevices();
    internal string CurrentAudioDevice => _engine.CurrentAudioDevice;
    internal bool SetAudioDevice(string name) => _engine.SetAudioDevice(name);
    internal IReadOnlyList<AudioTrack> GetAudioTracks() => _loaded ? _engine.GetAudioTracks() : [];
    internal bool SetAudioTrack(int id) => _loaded && _engine.SetAudioTrack(id);
    internal IReadOnlyList<SubtitleTrack> GetSubtitleTracks() => _loaded ? _engine.GetSubtitleTracks() : [];
    internal bool SetSubtitleTrack(int id) => _loaded && _engine.SetSubtitleTrack(id);
    internal bool DisableSubtitles() => _loaded && _engine.DisableSubtitles();
    internal bool AddSubtitle(string source, string? title = null, string? language = null)
        => _loaded && _engine.AddSubtitle(source, title, language);

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
