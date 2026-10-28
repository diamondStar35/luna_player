using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
namespace LunaPlayer.Playback;
internal sealed partial class AudioProcessor(MpvSession session)
{
    private readonly MpvSession _session = session;
    private void ApplyVolume() => _session.SetPropertySafely("volume", ToMpvVolume(_volume));
    private double _volume = 100;
    private double _pitch;
    private bool _pitchFilterActive;
    private double _pan;
    private bool _panFilterActive;
    private bool _normalizationEnabled;
    private Preset? _equalizerPreset;
    private double _equalizerSampleRate;
    private bool _equalizerFilterActive;

    /// <summary>Converts Luna's linear percentage to mpv's cubic volume scale. For example, Luna's
    /// 2000% is a 20x amplitude gain and maps to approximately 271.44 on mpv's volume property.</summary>
    internal static double ToMpvVolume(double volume)
        => volume <= 0 ? 0 : Precision.Normalize(100 * Math.Cbrt(volume / 100));

    /// <summary>Rebuilds the equalizer when the decoded sample rate changes, keeping every band below the
    /// new Nyquist frequency. Reports sent while the equalizer is off or the file is stopping are ignored.</summary>
    internal void HandleSampleRateChange(string _, object? value)
    {
        if (_equalizerPreset is null || value is null)
            return;
        double rate;
        try
        {
            rate = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return;
        }
        if (rate > 0 && rate != _equalizerSampleRate)
            BuildEqualizer();
    }

}
