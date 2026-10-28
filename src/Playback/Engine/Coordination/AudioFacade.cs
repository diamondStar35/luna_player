using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
namespace LunaPlayer.Playback;
internal sealed partial class PlaybackCoordinator
{
    public bool SetNormalization(bool enabled) => _audio.SetNormalization(enabled);
    public bool SetMono(bool enabled) => _audio.SetMono(enabled);
    public bool SetSilenceRemoval(bool enabled, string graph) => _audio.SetSilenceRemoval(enabled, graph);
    public bool SetEqualizer(Preset? preset) => _audio.SetEqualizer(preset);
    public double SetVolume(double volume) => _audio.SetVolume(volume);
    public double Volume => _audio.Volume;
    public double SetSpeed(double speed) => _audio.SetSpeed(speed);
    public double Speed => _audio.Speed;
    public double SetPitch(double semitones) => _audio.SetPitch(semitones);
    public double Pitch => _audio.Pitch;
    public double SetPan(double pan) => _audio.SetPan(pan);
    public double Pan => _audio.Pan;
}
