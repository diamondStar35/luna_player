using System.Globalization;

namespace LunaPlayer.Playback;

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
