using System.Globalization;
using LunaPlayer.Equalizer;
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

    /// <summary>The label the equalizer's filter carries in mpv's chain.</summary>
    internal const string EqualizerLabel = "audioeq";

    /// <summary>The name of one slot inside that filter, as the graph and <c>af-command</c> both spell
    /// it.</summary>
    /// <remarks>
    /// The filter a slot is decides its name, because in a lavfi graph the two are the same word. A shelf
    /// is <c>bass</c> or <c>treble</c>; everything between them is <c>equalizer</c>, which is a peak.
    /// </remarks>
    internal static string EqualizerSlotName(int slot)
    {
        if (slot == Bands.LowShelfSlot)
            return "bass@eqlow";
        if (slot == Bands.HighShelfSlot)
            return "treble@eqhigh";
        return $"equalizer@eqband{slot - Bands.FirstPeakingSlot}";
    }

    /// <summary>The equalizer as one lavfi graph: every slot in turn.</summary>
    ///
    /// <remarks>
    /// A row of single-band filters rather than one multi-band filter, because every parameter of every
    /// one of them can be changed while it runs. Each is given a name here, which is what lets a single
    /// band be retuned through <c>af-command</c> instead of the whole graph being torn down and rebuilt -
    /// and rebuilding it is what a user would hear, as a break in the sound every time they changed a
    /// preset.
    ///
    /// Every one of these is linear and time-invariant, so their order is free; they are written low to
    /// high because that is the order somebody reading the graph will expect.
    /// </remarks>
    internal static string EqualizerGraph(IReadOnlyList<Band> slots)
    {
        var stages = new List<string>(slots.Count);
        for (var slot = 0; slot < slots.Count; slot++)
        {
            var band = slots[slot];
            stages.Add(string.Join(':',
                $"{EqualizerSlotName(slot)}=f={Bands.Format(band.Frequency)}",
                "t=q",
                $"w={Bands.Format(band.Q)}",
                $"g={Bands.Format(band.Gain)}"));
        }
        return string.Join(',', stages);
    }
}
