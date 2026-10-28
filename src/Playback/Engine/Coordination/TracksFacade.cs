using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
namespace LunaPlayer.Playback;
internal sealed partial class PlaybackCoordinator
{
    public IReadOnlyList<AudioDevice> GetAudioDevices() => _tracks.GetAudioDevices();
    public string CurrentAudioDevice => _tracks.CurrentAudioDevice;
    public bool SetAudioDevice(string name) => _tracks.SetAudioDevice(name);
    public IReadOnlyList<AudioTrack> GetAudioTracks() => _tracks.GetAudioTracks();
    public bool SetAudioTrack(int id) => _tracks.SetAudioTrack(id);
    public IReadOnlyList<SubtitleTrack> GetSubtitleTracks() => _tracks.GetSubtitleTracks();
    public bool SetSubtitleTrack(int id) => _tracks.SetSubtitleTrack(id);
    public bool DisableSubtitles() => _tracks.DisableSubtitles();
    public bool AddSubtitle(string source, string? title, string? language) => _tracks.AddSubtitle(source, title, language);
}
