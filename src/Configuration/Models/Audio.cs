using System.Text.Json.Serialization;

namespace LunaPlayer.Configuration.Models;

internal enum EndBehavior { Advance, Loop, None }

internal enum GaplessPlaybackMode { Disabled, CompatibleFiles, AlwaysWhenPossible }

internal sealed class AudioSettings
{
    internal const double MaximumVolume = 2000;
    internal const double MinimumSpeed = 0.5;
    internal const double MaximumSpeed = 4;
    internal const double MinimumPitch = -12;
    internal const double MaximumPitch = 12;
    internal const double MinimumPitchStep = 0.001;
    internal const double MaximumPitchStep = 12;
    internal const int MaximumFadeIntervalMilliseconds = 10000;

    public double Volume { get; set; } = 100;
    public double Speed { get; set; } = 1;
    public double Pitch { get; set; }
    public double PitchStep { get; set; } = 0.1;
    public string Device { get; set; } = string.Empty;
    public int VolumeStep { get; set; } = 5;
    public double Pan { get; set; }
    public int PanStep { get; set; } = 5;
    public double SpeedStep { get; set; } = 0.1;
    public string SeekStepKey { get; set; } = "2";
    public double CustomSeekStep { get; set; } = 5;
    [JsonConverter(typeof(JsonStringEnumConverter<EndBehavior>))]
    public EndBehavior EndBehavior { get; set; } = EndBehavior.Advance;
    public bool WrapPlaylist { get; set; }
    public bool SaveFilePositions { get; set; }
    public bool NormalizeAudio { get; set; } = true;
    public bool MonoAudio { get; set; }
    public int FadeIntervalMilliseconds { get; set; }
    public bool FadeSeeking { get; set; }
    public int SeekFadeIntervalMilliseconds { get; set; } = 100;
    public bool FadeFileTransitions { get; set; }
    public bool FadePauseResume { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter<GaplessPlaybackMode>))]
    public GaplessPlaybackMode GaplessPlayback { get; set; }
    internal AudioSettings Copy() => (AudioSettings)MemberwiseClone();
    internal void Apply(AudioSettings source)
    {
        Volume = source.Volume;
        Speed = source.Speed;
        Pitch = source.Pitch;
        PitchStep = source.PitchStep;
        Device = source.Device;
        VolumeStep = source.VolumeStep;
        Pan = source.Pan;
        PanStep = source.PanStep;
        SpeedStep = source.SpeedStep;
        SeekStepKey = source.SeekStepKey;
        CustomSeekStep = source.CustomSeekStep;
        EndBehavior = source.EndBehavior;
        WrapPlaylist = source.WrapPlaylist;
        SaveFilePositions = source.SaveFilePositions;
        NormalizeAudio = source.NormalizeAudio;
        MonoAudio = source.MonoAudio;
        FadeIntervalMilliseconds = source.FadeIntervalMilliseconds;
        FadeSeeking = source.FadeSeeking;
        SeekFadeIntervalMilliseconds = source.SeekFadeIntervalMilliseconds;
        FadeFileTransitions = source.FadeFileTransitions;
        FadePauseResume = source.FadePauseResume;
        GaplessPlayback = source.GaplessPlayback;
    }
}

