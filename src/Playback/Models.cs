using System.Globalization;

namespace LunaPlayer.Playback;

internal enum EndReason
{
    EndOfFile,
    Error,
    GaplessAdvance,
}

internal readonly record struct AudioDevice(string Name, string Description);

/// <param name="Id">mpv's track ID, written to <c>aid</c> when selecting this track.</param>
/// <param name="Title">The title stored in the media, or null when it has none.</param>
/// <param name="Language">The raw language code, such as "eng", or null when it has none.</param>
/// <param name="Channels">The channel count, or 0 when mpv has not reported it.</param>
/// <param name="Selected">Whether this track is currently playing.</param>
internal readonly record struct AudioTrack(int Id, string? Title, string? Language, int Channels, bool Selected);

/// <param name="Id">mpv's track ID, written to <c>sid</c> when selecting this track.</param>
/// <param name="Title">The title stored in the media, or null when it has none.</param>
/// <param name="Language">The raw language code, such as "eng", or null when it has none.</param>
/// <param name="Selected">Whether this subtitle track is currently selected.</param>
internal readonly record struct SubtitleTrack(int Id, string? Title, string? Language, bool Selected);

internal static class PlaybackTimeFormatter
{
    internal static string? Format(double? seconds)
    {
        if (seconds is not double value || double.IsNaN(value) || double.IsInfinity(value))
            return null;
        var total = (long)Math.Round(Math.Max(0, value));
        var hours = total / 3600;
        var minutes = total % 3600 / 60;
        var remainingSeconds = total % 60;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{hours:00}:{minutes:00}:{remainingSeconds:00}");
    }
}
