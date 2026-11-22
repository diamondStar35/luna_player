namespace LunaPlayer.Playback;

/// <summary>Playback constraints in the timeline of the media input receiving them.</summary>
internal sealed record Boundaries(double? LoopStart = null, double? LoopEnd = null,
    PlaybackRange? Selection = null, bool RepeatSelection = false)
{
    internal static Boundaries None { get; } = new();
    internal bool IsEmpty => LoopStart is null && LoopEnd is null && Selection is null;

    internal double? ResolveFrom(double? position)
    {
        if (LoopStart is { } loop) position = Math.Min(position ?? loop, loop);
        if (Selection is { } range) position = Math.Min(position ?? range.Start, range.Start);
        return position;
    }

    internal Boundaries RelativeTo(double offset) => this with
    {
        LoopStart = LoopStart - offset,
        LoopEnd = LoopEnd - offset,
        Selection = Selection is { } range ? new(range.Start - offset, range.End - offset) : null,
    };
}
