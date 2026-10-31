using LunaPlayer.Configuration;
namespace LunaPlayer.Playback;
internal sealed partial class AudioProcessor(MpvSession session)
{
    private readonly MpvSession _session = session;
    private void ApplyVolume() => _session.TryDo(mpv => mpv.SetProperty("volume", ToMpvVolume(_volume)));
    private double _volume = 100;
    private double _speed = 1;
    private double _pitch;
    private bool _pitchFilterActive;
    private double _pan;
    private bool _panFilterActive;
    private bool _normalizationEnabled;

    /// <summary>Converts Luna's linear percentage to mpv's cubic volume scale. For example, Luna's
    /// 2000% is a 20x amplitude gain and maps to approximately 271.44 on mpv's volume property.</summary>
    internal static double ToMpvVolume(double volume)
        => volume <= 0 ? 0 : Precision.Normalize(100 * Math.Cbrt(volume / 100));

}
