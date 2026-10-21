namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    internal IReadOnlyList<AudioDevice> GetAudioDevices() => _engine.GetAudioDevices();

    internal string CurrentAudioDevice => _engine.CurrentAudioDevice;

    internal bool SetAudioDevice(string name) => _engine.SetAudioDevice(name);

    internal IReadOnlyList<AudioTrack> GetAudioTracks() => _loaded ? _engine.GetAudioTracks() : [];

    internal bool SetAudioTrack(int id) => _loaded && _engine.SetAudioTrack(id);

    internal IReadOnlyList<SubtitleTrack> GetSubtitleTracks() => _loaded ? _engine.GetSubtitleTracks() : [];

    internal bool SetSubtitleTrack(int id) => _loaded && _engine.SetSubtitleTrack(id);

    internal bool DisableSubtitles() => _loaded && _engine.DisableSubtitles();

    internal bool AddSubtitle(string source, string? title = null, string? language = null)
        => _loaded && _engine.AddSubtitle(source, title, language);
}
