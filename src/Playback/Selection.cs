using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

internal sealed class PlaybackSelection
{
    internal string? Path { get; private set; }
    internal double? Start { get; private set; }
    internal double? End { get; private set; }

    internal void SetStart(string path, double seconds)
    {
        Path = path;
        Start = Precision.Normalize(seconds);
        End = null;
    }

    internal void SetEnd(double seconds) => End = Precision.Normalize(seconds);

    internal bool IsActive(string? path, bool requireEnd = true)
        => !string.IsNullOrEmpty(path)
            && string.Equals(Path, path, StringComparison.Ordinal)
            && Start.HasValue
            && (!requireEnd || End.HasValue)
            && (!End.HasValue || End > Start);

    internal void Reset()
    {
        Path = null;
        Start = null;
        End = null;
    }
}
