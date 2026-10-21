namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    internal void TrackPositions(bool enabled) => _trackPositions = enabled;

    internal void SavePosition() => SavePosition(CurrentPath, Elapsed);

    internal void SavePosition(string? path, double? elapsed)
    {
        if (!_trackPositions || _positions is null || path is null || !File.Exists(path)
            || elapsed is not double position)
            return;
        _positions.Set(path, position);
    }
}
