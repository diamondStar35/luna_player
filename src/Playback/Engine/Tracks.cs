using System.Globalization;

namespace LunaPlayer.Playback;

internal sealed class TrackDiscovery(MpvSession session)
{
    private readonly MpvSession _session = session;
    public IReadOnlyList<AudioDevice> GetAudioDevices()
    {
        if (_session.ReadObject("audio-device-list") is not IEnumerable<object?> values)
            return [];
        var devices = new List<AudioDevice>();
        foreach (var value in values)
        {
            if (value is not IDictionary<string, object?> device || !device.TryGetValue("name", out var rawName))
                continue;
            var name = Convert.ToString(rawName, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(name))
                continue;
            device.TryGetValue("description", out var rawDescription);
            var description = Convert.ToString(rawDescription, CultureInfo.InvariantCulture);
            devices.Add(new AudioDevice(name, string.IsNullOrWhiteSpace(description) ? name : description));
        }
        return devices;
    }

    public string CurrentAudioDevice => _session.ReadString("audio-device") ?? "auto";

    public bool SetAudioDevice(string name)
    {
        var target = string.IsNullOrWhiteSpace(name) ? "auto" : name;
        if (!string.Equals(target, "auto", StringComparison.Ordinal)
            && !GetAudioDevices().Any(device => string.Equals(device.Name, target, StringComparison.Ordinal)))
            target = "auto";
        return _session.TrySetProperty("audio-device", target);
    }

    public IReadOnlyList<AudioTrack> GetAudioTracks()
    {
        if (_session.ReadObject("track-list") is not IEnumerable<object?> values)
            return [];
        var tracks = new List<AudioTrack>();
        foreach (var value in values)
        {
            if (ReadTrack(value, "audio") is not TrackData track)
                continue;
            var channels = track.Values.TryGetValue("demux-channel-count", out var rawChannels)
                && rawChannels is not null
                ? Convert.ToInt32(rawChannels, CultureInfo.InvariantCulture)
                : 0;
            tracks.Add(new AudioTrack(track.Id, track.Title, track.Language, channels, track.Selected));
        }
        return tracks;
    }

    public bool SetAudioTrack(int id) => _session.TrySetProperty("aid", id);

    public IReadOnlyList<SubtitleTrack> GetSubtitleTracks()
    {
        if (_session.ReadObject("track-list") is not IEnumerable<object?> values)
            return [];
        var tracks = new List<SubtitleTrack>();
        foreach (var value in values)
        {
            if (ReadTrack(value, "sub") is not TrackData track)
                continue;
            tracks.Add(new SubtitleTrack(track.Id, track.Title, track.Language, track.Selected));
        }
        return tracks;
    }

    public bool SetSubtitleTrack(int id) => _session.TrySetProperty("sid", id);

    public bool DisableSubtitles() => _session.TrySetProperty("sid", "no");

    private static TrackData? ReadTrack(object? value, string expectedType)
    {
        if (value is not IDictionary<string, object?> track
            || !track.TryGetValue("type", out var rawType)
            || Convert.ToString(rawType, CultureInfo.InvariantCulture) != expectedType
            || !track.TryGetValue("id", out var rawId))
            return null;
        track.TryGetValue("title", out var rawTitle);
        track.TryGetValue("lang", out var rawLanguage);
        var title = Convert.ToString(rawTitle, CultureInfo.InvariantCulture);
        var language = Convert.ToString(rawLanguage, CultureInfo.InvariantCulture);
        var selected = track.TryGetValue("selected", out var rawSelected)
            && Convert.ToBoolean(rawSelected, CultureInfo.InvariantCulture);
        return new TrackData(
            track,
            Convert.ToInt32(rawId, CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(title) ? null : title,
            string.IsNullOrWhiteSpace(language) ? null : language,
            selected);
    }

    private readonly record struct TrackData(
        IDictionary<string, object?> Values, int Id, string? Title, string? Language, bool Selected);

    // sub-add <url> [<flags> [<title> [<lang>]]] - "select" adds and switches to it. The track-list grows,
    // which the count observer turns into SubtitleTracksChanged, so the menu and reading pick it up with no
    // extra wiring. mpv opens an http(s) url through ffmpeg just as it opens a local path. Only the arguments
    // that were given are passed, so an empty title does not overwrite the name mpv derives from the source.
    public bool AddSubtitle(string source, string? title, string? language)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;
        return _session.TryDo(mpv =>
        {
            if (!string.IsNullOrWhiteSpace(language))
                mpv.Command("sub-add", source, "select", string.IsNullOrWhiteSpace(title) ? source : title, language);
            else if (!string.IsNullOrWhiteSpace(title))
                mpv.Command("sub-add", source, "select", title);
            else
                mpv.Command("sub-add", source, "select");
        });
    }
}
