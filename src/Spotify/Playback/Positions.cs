namespace LunaPlayer.Spotify.Playback;

internal sealed partial class Sessions
{
    private double ResumePosition(Track track, double? requested = null)
    {
        var seconds = requested ?? SavedPosition(track);
        // Resume before EOF so opening a remembered, completed track can still start playback.
        return Math.Clamp(double.IsFinite(seconds) ? seconds : 0, 0,
            track.DurationMs > 0 ? Math.Max(0, track.DurationMs / 1000.0 - 1) : double.MaxValue);
    }

    private double SavedPosition(Track track)
    {
        if (_audioSettings?.SaveFilePositions != true || _readPosition is null || !IsTrack(track.Uri)) return 0;
        double seconds;
        lock (_positionLock)
            seconds = _readPosition("https://open.spotify.com/track/" + track.Uri[14..]) ?? 0;
        return double.IsFinite(seconds) ? Math.Max(0, seconds) : 0;
    }

    private void SavePosition(double? overrideSeconds = null)
    {
        if (!_currentLoadSent) return;
        if (_ended && overrideSeconds is null) return;
        var elapsed = overrideSeconds ?? Elapsed;
        if (_audioSettings?.SaveFilePositions != true || _writePosition is null
            || CurrentLink is not { } link || elapsed is null) return;
        lock (_positionLock) _writePosition(link, elapsed.Value);
    }
}
