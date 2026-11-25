using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.UI.Subsonic;

internal static class LyricsTimeline
{
    internal static int At(StructuredLyrics lyrics, double position)
    {
        if (!lyrics.Synced || !double.IsFinite(position)) return -1;
        // The protocol defines positive offsets as displaying a line sooner.
        var timestamp = position * 1000 + (lyrics.Offset ?? 0);
        var result = -1;
        for (var index = 0; index < lyrics.Lines.Length; index++)
            if (lyrics.Lines[index].Start is { } start && start <= timestamp) result = index;
        return result;
    }
}
