using System.Globalization;
using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

internal static class AudioFilters
{
    internal static string SilenceGraph(SilenceSettings settings)
    {
        var culture = CultureInfo.InvariantCulture;
        var detection = settings.Detection == SilenceDetection.Rms ? "rms" : "peak";
        return "silenceremove=" + string.Join(':',
            $"start_periods={settings.StartPeriods}",
            $"start_duration={settings.StartDuration.ToString("0.###", culture)}",
            $"start_threshold={settings.Threshold.ToString("0.###", culture)}dB",
            $"stop_periods={settings.StopPeriods}",
            $"stop_duration={settings.StopDuration.ToString("0.###", culture)}",
            $"stop_threshold={settings.Threshold.ToString("0.###", culture)}dB",
            $"stop_silence={settings.StopSilence.ToString("0.###", culture)}",
            $"window={settings.Window.ToString("0.###", culture)}",
            $"detection={detection}");
    }
}
