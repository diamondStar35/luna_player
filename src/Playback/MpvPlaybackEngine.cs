using System.Globalization;
using LunaPlayer.Equalizer;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using MpvNet;
using System.Collections.Concurrent;
using LunaPlayer.Spotify;

namespace LunaPlayer.Playback;

internal sealed class MpvPlaybackEngine : IPlaybackEngine
{
    private readonly MPV _mpv;
    private readonly ConcurrentDictionary<string, AudioDecryptStream> _spotifyAudio = new(StringComparer.Ordinal);
    private readonly IDisposable _endRegistration;
    private readonly IDisposable _sampleRateRegistration;
    private readonly IDisposable _videoParamsRegistration;
    private readonly IDisposable _trackListRegistration;
    private readonly IDisposable _subTextRegistration;
    // Cached from the video-params observer; mpv learns it asynchronously after a load, not on demand.
    private bool _hasVideo;
    private double _volume = 100;
    private double _pitch;
    private bool _pitchFilterActive;
    private double _pan;
    private bool _panFilterActive;
    private bool _normalizationEnabled;
    private Preset? _equalizerPreset;
    private double _equalizerSampleRate;
    private bool _equalizerFilterActive;
    // The last subtitle line handed out, so mpv re-reporting the same cue is not announced twice. Cleared
    // when the cue goes away, so a line that genuinely returns is read again.
    private string? _lastSubText;
    private bool _disposed;

    internal MpvPlaybackEngine(nint windowHandle, bool mediaControls = true)
    {
        var options = new Dictionary<string, object?>
        {
            ["vo"] = "gpu",
            ["osc"] = false,
            ["keep_open"] = "no",
            ["input_default_bindings"] = false,
            ["input_vo_keyboard"] = false,
            // Subtitles start off: nothing is read until the user picks a track. sub-auto keeps mpv from
            // pulling in sidecar .srt files, and sid=no leaves every embedded track unselected on load.
            ["sub_auto"] = "no",
            ["sid"] = "no",
            ["volume_max"] = Math.Ceiling(ToMpvVolume(AudioSettings.MaximumVolume)),
        };
        if (windowHandle != 0)
            options["wid"] = windowHandle.ToString(CultureInfo.InvariantCulture);

        _mpv = new MPV(options: options);
        _mpv.RegisterStreamProtocol("luna-spotify", OpenSpotifyStream);
        SetPropertySafely("network-timeout", 10);
        ApplyMediaControls(mediaControls);
        _endRegistration = _mpv.OnEvent(HandleEndFile, MpvEventId.EndFile);
        // The equalizer is left out of the chain entirely until a preset asks for it, and which bands it
        // then carries depends on the file's sample rate: a band at the Nyquist frequency rings rather
        // than filters, so the graph is rebuilt whenever that rate changes under it. Off is nothing at
        // all, not a row of flat bands.
        _sampleRateRegistration = _mpv.ObserveProperty("audio-params/samplerate", HandleSampleRateChange);
        // Video parameters are not known when the load returns, so the width is watched rather than read.
        _videoParamsRegistration = _mpv.ObserveProperty("video-params/w", HandleVideoParamsChange);
        // Tracks are learned after the load returns as well, so the count is watched to tell when the audio
        // tracks a file offers have changed under the menu that gates on them.
        _trackListRegistration = _mpv.ObserveProperty("track-list/count", HandleTrackListChange);
        // The text of the subtitle currently on screen. Watching it turns each cue into a line to announce as
        // it comes up; mpv leaves it empty between cues and whenever no subtitle track is selected.
        _subTextRegistration = _mpv.ObserveProperty("sub-text", HandleSubTextChange);
    }

    public event Action<PlaybackEndReason>? Ended;

    public event Action? VideoAvailabilityChanged;

    public event Action? AudioTracksChanged;

    public event Action? SubtitleTracksChanged;

    public event Action<string>? SubtitleTextChanged;

    public bool HasVideo => _hasVideo;

    /// <remarks>
    /// None of this blocks the UI thread on mpv. <c>mpv_set_property</c> is synchronous - it waits until the
    /// core has processed the change - and issuing those sets right after a loadfile, while the core is busy
    /// starting the load, is what intermittently stalled the UI. So subtitles-off and the pause state ride
    /// with the loadfile as per-file options (applied atomically with the load, in order, no separate call),
    /// and the audio file is set asynchronously. The audio file cannot be a loadfile option - its value is a
    /// URL, which the flattened option string would break on commas and equals signs - and it is set on
    /// every load because mpv keeps the property, so leaving it would play the previous video's sound over
    /// the next. The async set is submitted before the loadfile, and mpv processes a client's requests in
    /// order, so it is applied before the file loads.
    /// </remarks>
    public bool Load(string path, double? startPosition = null, bool paused = false, string? audioFile = null, bool noVideo = false)
    {
        var options = new Dictionary<string, object?>
        {
            // Track ids do not carry across files, and the user opts subtitles in per file through the menu.
            ["sid"] = "no",
            ["pause"] = paused ? "yes" : "no",
            // An audio-only play - a YouTube video opened "as audio" - can still resolve to a stream that
            // carries a picture. vid=no leaves that picture undecoded so the play is truly sound only: no
            // video to go full screen over, and none of the cost of decoding frames nobody is watching. It
            // rides as a per-file option, so an ordinary load afterwards gets video back on its own.
            ["vid"] = noVideo ? "no" : "auto",
        };
        if (startPosition.HasValue)
            options["start"] = Precision.Normalize(startPosition.Value);
        return TryDo(mpv =>
        {
            mpv.SetPropertyAsync(0, "audio-files", audioFile is null ? Array.Empty<string>() : new[] { audioFile });
            mpv.LoadFileAsync(path, "replace", options);
        });
    }

    internal string RegisterSpotifyAudio(AudioDecryptStream audio)
    {
        var id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        foreach (var pending in _spotifyAudio)
            if (_spotifyAudio.TryRemove(pending.Key, out var unused)) unused.Dispose();
        _spotifyAudio[id] = audio;
        return "luna-spotify://" + id;
    }

    internal void ReleaseSpotifyAudio(string url)
    {
        const string prefix = "luna-spotify://";
        if (url.StartsWith(prefix, StringComparison.Ordinal)
            && _spotifyAudio.TryRemove(url[prefix.Length..], out var pending)) pending.Dispose();
    }

    private IMpvStream OpenSpotifyStream(string uri)
    {
        const string prefix = "luna-spotify://";
        if (!uri.StartsWith(prefix, StringComparison.Ordinal)
            || !_spotifyAudio.TryRemove(uri[prefix.Length..], out var audio))
            throw new InvalidDataException();
        return new SpotifyMpvStream(audio);
    }

    private sealed class SpotifyMpvStream(AudioDecryptStream audio) : IMpvStream
    {
        public long? Size => audio.Length;
        public int Read(byte[] buffer, int offset, int count) => audio.Read(buffer, offset, count);
        public long Seek(long offset) => audio.Seek(offset, SeekOrigin.Begin);
        public void Cancel() => audio.Cancel();
        public void Dispose() => audio.Dispose();
    }

    public void Stop() => TryDo(static mpv => mpv.Command("stop"));

    public bool TogglePause()
    {
        var paused = IsPaused;
        SetPropertySafely("pause", !paused);
        return paused;
    }

    public void Play() => SetPropertySafely("pause", false);

    public void Pause() => SetPropertySafely("pause", true);

    public bool IsPaused => ReadBoolean("pause") ?? false;

    public double? Duration => Precision.Normalize(ReadDouble("duration"));

    public double? Elapsed => Precision.Normalize(ReadDouble("time-pos"));

    public double? Remaining => Precision.Normalize(ReadDouble("time-remaining"));

    public void SeekRelative(double seconds)
        => TryDo(mpv => mpv.Command("seek", Precision.Normalize(seconds), "relative"));

    public void SeekAbsolute(double seconds)
        => TryDo(mpv => mpv.Command("seek", Precision.Normalize(Math.Max(0, seconds)), "absolute"));

    public bool SetLoopStart(double seconds)
    {
        var startSet = TrySetProperty("ab-loop-a", Precision.Normalize(Math.Max(0, seconds)));
        var endCleared = TrySetProperty("ab-loop-b", "no");
        return startSet && endCleared;
    }

    public bool SetLoopEnd(double seconds)
        => TrySetProperty("ab-loop-b", Precision.Normalize(Math.Max(0, seconds)));

    public bool ClearLoop()
    {
        var startCleared = TrySetProperty("ab-loop-a", "no");
        var endCleared = TrySetProperty("ab-loop-b", "no");
        return startCleared && endCleared;
    }

    public double SetVolume(double volume)
    {
        _volume = Precision.Normalize(Math.Clamp(volume, 0, AudioSettings.MaximumVolume));
        // Volume is mpv's continuously adjustable software mixer. Changing a live lavfi gain filter,
        // even through af-command, can make buffered filters such as dynaudnorm audibly pump or drop out.
        ApplyVolume();
        return _volume;
    }

    public double Volume => _volume;

    public double SetSpeed(double speed)
    {
        var value = Precision.Normalize(Math.Clamp(
            speed, AudioSettings.MinimumSpeed, AudioSettings.MaximumSpeed));
        SetPropertySafely("speed", value);
        return value;
    }

    public double Speed => ReadDouble("speed") is double speed
        ? Precision.Normalize(Math.Clamp(
            speed, AudioSettings.MinimumSpeed, AudioSettings.MaximumSpeed))
        : 1;

    public double SetPitch(double semitones)
    {
        var value = Precision.Normalize(Math.Clamp(
            semitones, AudioSettings.MinimumPitch, AudioSettings.MaximumPitch));
        if (value == 0 && !_pitchFilterActive)
        {
            _pitch = 0;
            return _pitch;
        }

        // MPV implements its pitch property by combining resampling with scaletempo2. This time-domain
        // overlap algorithm is much closer to the one behind BASS_FX's tempo stream than Rubber Band,
        // particularly for voices. Keeping an explicit filter in the graph prevents it being inserted
        // and removed as pitch crosses zero. Luna's pitch and playback-speed ranges fit scaletempo2's
        // normal 0.25-to-8 speed range exactly, even at their combined extremes.
        if (!_pitchFilterActive)
            _pitchFilterActive = AddFilter("@audiopitch:scaletempo2");
        if (!_pitchFilterActive)
            return _pitch;

        // Semitones are logarithmic: twelve semitones double the frequency and twelve negative
        // semitones halve it. Recompute the derived multiplier from the normalized semitone state so
        // repeated changes cannot accumulate floating-point drift.
        var scale = Math.Pow(2, value / 12);
        if (TrySetProperty("pitch", scale))
            _pitch = value;
        return _pitch;
    }

    public double Pitch => _pitch;

    /// <remarks>
    /// Rebuilt rather than adjusted through <c>af-command</c>. The command reaches the running filter but
    /// not the string mpv built it from, and mpv builds the lavfi graph out of that string again whenever
    /// the audio chain is reinitialized - a seek or a pause is enough - at which point the balance goes
    /// back to whatever the string still says. Keeping the value nowhere but in the string is the only
    /// arrangement that survives that, and it is what the equalizer does for the same reason.
    /// </remarks>
    public double SetPan(double pan)
    {
        var value = Precision.Normalize(Math.Clamp(pan, -100, 100));
        if (_panFilterActive)
            RemoveFilter("@audiopan");
        _panFilterActive = false;
        if (value == 0)
        {
            _pan = 0;
            return _pan;
        }

        var balance = Precision.Normalize(value / 100).ToString("0.###", CultureInfo.InvariantCulture);
        _panFilterActive = AddFilter(
            $"@audiopan:lavfi=[aformat=channel_layouts=stereo,stereotools=balance_out={balance}]");
        if (_panFilterActive)
            _pan = value;
        return _pan;
    }

    public double Pan => _pan;

    public IReadOnlyList<AudioDevice> GetAudioDevices()
    {
        if (ReadObject("audio-device-list") is not IEnumerable<object?> values)
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

    public string CurrentAudioDevice => ReadString("audio-device") ?? "auto";

    public bool SetAudioDevice(string name)
    {
        var target = string.IsNullOrWhiteSpace(name) ? "auto" : name;
        if (!string.Equals(target, "auto", StringComparison.Ordinal)
            && !GetAudioDevices().Any(device => string.Equals(device.Name, target, StringComparison.Ordinal)))
            target = "auto";
        return TrySetProperty("audio-device", target);
    }

    public IReadOnlyList<AudioTrack> GetAudioTracks()
    {
        if (ReadObject("track-list") is not IEnumerable<object?> values)
            return [];
        var tracks = new List<AudioTrack>();
        foreach (var value in values)
        {
            if (value is not IDictionary<string, object?> track
                || !track.TryGetValue("type", out var rawType)
                || Convert.ToString(rawType, CultureInfo.InvariantCulture) != "audio"
                || !track.TryGetValue("id", out var rawId))
                continue;
            var id = Convert.ToInt32(rawId, CultureInfo.InvariantCulture);
            track.TryGetValue("title", out var rawTitle);
            var title = Convert.ToString(rawTitle, CultureInfo.InvariantCulture);
            track.TryGetValue("lang", out var rawLang);
            var lang = Convert.ToString(rawLang, CultureInfo.InvariantCulture);
            var channels = track.TryGetValue("demux-channel-count", out var rawChannels) && rawChannels is not null
                ? Convert.ToInt32(rawChannels, CultureInfo.InvariantCulture)
                : 0;
            var selected = track.TryGetValue("selected", out var rawSelected)
                && Convert.ToBoolean(rawSelected, CultureInfo.InvariantCulture);
            tracks.Add(new AudioTrack(
                id,
                string.IsNullOrWhiteSpace(title) ? null : title,
                string.IsNullOrWhiteSpace(lang) ? null : lang,
                channels,
                selected));
        }
        return tracks;
    }

    public bool SetAudioTrack(int id) => TrySetProperty("aid", id);

    public IReadOnlyList<SubtitleTrack> GetSubtitleTracks()
    {
        if (ReadObject("track-list") is not IEnumerable<object?> values)
            return [];
        var tracks = new List<SubtitleTrack>();
        foreach (var value in values)
        {
            if (value is not IDictionary<string, object?> track
                || !track.TryGetValue("type", out var rawType)
                || Convert.ToString(rawType, CultureInfo.InvariantCulture) != "sub"
                || !track.TryGetValue("id", out var rawId))
                continue;
            var id = Convert.ToInt32(rawId, CultureInfo.InvariantCulture);
            track.TryGetValue("title", out var rawTitle);
            var title = Convert.ToString(rawTitle, CultureInfo.InvariantCulture);
            track.TryGetValue("lang", out var rawLang);
            var lang = Convert.ToString(rawLang, CultureInfo.InvariantCulture);
            var selected = track.TryGetValue("selected", out var rawSelected)
                && Convert.ToBoolean(rawSelected, CultureInfo.InvariantCulture);
            tracks.Add(new SubtitleTrack(
                id,
                string.IsNullOrWhiteSpace(title) ? null : title,
                string.IsNullOrWhiteSpace(lang) ? null : lang,
                selected));
        }
        return tracks;
    }

    public bool SetSubtitleTrack(int id) => TrySetProperty("sid", id);

    public bool DisableSubtitles() => TrySetProperty("sid", "no");

    // sub-add <url> [<flags> [<title> [<lang>]]] - "select" adds and switches to it. The track-list grows,
    // which the count observer turns into SubtitleTracksChanged, so the menu and reading pick it up with no
    // extra wiring. mpv opens an http(s) url through ffmpeg just as it opens a local path. Only the arguments
    // that were given are passed, so an empty title does not overwrite the name mpv derives from the source.
    public bool AddSubtitle(string source, string? title, string? language)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;
        return TryDo(mpv =>
        {
            if (!string.IsNullOrWhiteSpace(language))
                mpv.Command("sub-add", source, "select", string.IsNullOrWhiteSpace(title) ? source : title, language);
            else if (!string.IsNullOrWhiteSpace(title))
                mpv.Command("sub-add", source, "select", title);
            else
                mpv.Command("sub-add", source, "select");
        });
    }

    public bool SetNormalization(bool enabled)
    {
        RemoveFilter("@audionormalize");
        _normalizationEnabled = enabled;
        if (!enabled)
        {
            ApplyVolume();
            return true;
        }
        if (!AddFilter("@audionormalize:lavfi=[dynaudnorm=f=150:g=15,alimiter=limit=0.95]"))
        {
            RemoveFilter("@audionormalize");
            _normalizationEnabled = false;
            ApplyVolume();
            return false;
        }
        ApplyVolume();
        return true;
    }

    public bool SetMono(bool enabled)
    {
        RemoveFilter("@audiomono");
        return !enabled || AddFilter("@audiomono:lavfi=[aformat=channel_layouts=mono]");
    }

    public bool SetSilenceRemoval(bool enabled, string graph)
    {
        RemoveFilter("@silenceremove");
        return !enabled || (graph.Length > 0 && AddFilter($"@silenceremove:lavfi=[{graph}]"));
    }

    public bool SetEqualizer(Preset? preset)
    {
        _equalizerPreset = preset;
        return BuildEqualizer();
    }

    /// <summary>Puts the equalizer the current preset asks for into the chain, or takes it out when the
    /// preset is Off.</summary>
    ///
    /// <remarks>
    /// Built afresh for every change rather than adjusted in place, and that is the point of it. mpv keeps
    /// two things: the filter string it was given, and the running filters made from that string. Changing
    /// a running filter through <c>af-command</c> leaves the string as it was, so the moment anything makes
    /// mpv build its audio chain again - a seek, a pause, the next file, a change of output format - it
    /// builds the old string and whatever was chosen is gone. That is not something to notice afterwards
    /// and put right; it is a reason to keep the state nowhere but in the string.
    ///
    /// What it costs is the chain rebuilt each time a preset is chosen, which is what switching
    /// normalization or silence removal on already costs, and it happens only when the user asks for it.
    ///
    /// Off is the empty chain, not a row of flat bands: a flat band is transparent everywhere except at
    /// the Nyquist frequency, where the biquad rings whatever its gain, so a file whose Nyquist lands on
    /// a band beeps even with nothing turned up. The Nyquist-aware graph would drop that one band, but
    /// leaving the whole filter out when there is nothing to apply is both cheaper and plainly right.
    ///
    /// Prepended rather than appended, so the equalizer stays ahead of the limiter normalization puts in
    /// the chain and a lifted band is still limited. If this build of mpv will not take <c>pre</c> it goes
    /// on the end instead - the wrong side of the limiter, but present and right in every other respect.
    /// </remarks>
    private bool BuildEqualizer()
    {
        RemoveFilter($"@{AudioFilters.EqualizerLabel}");
        _equalizerFilterActive = false;
        if (_equalizerPreset is null)
            return true;

        // The rate the decoded audio runs at, so bands at or beyond its Nyquist are left out. Null before
        // a file is loaded; the graph is then built whole and rebuilt once the real rate is known.
        _equalizerSampleRate = ReadDouble("audio-params/samplerate") ?? 0;
        var graph = AudioFilters.EqualizerGraph(
            Bands.Slots(_equalizerPreset), _equalizerSampleRate > 0 ? _equalizerSampleRate : null);
        if (graph.Length == 0)
            return true;

        var filter = $"@{AudioFilters.EqualizerLabel}:lavfi=[{graph}]";
        _equalizerFilterActive = TryDo(mpv => mpv.Command("af", "pre", filter)) || AddFilter(filter);
        return _equalizerFilterActive;
    }

    /// <summary>Rebuilds the equalizer when the decoded sample rate changes under it, so a graph that was
    /// safe for one file's Nyquist frequency is made safe for the next. Does nothing while the equalizer
    /// is Off, and ignores the report mpv sends when a file stops and the rate becomes unknown.</summary>
    private void HandleSampleRateChange(string name, object? value)
    {
        if (_disposed || _equalizerPreset is null || value is null)
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

    private void HandleVideoParamsChange(string name, object? value)
    {
        if (_disposed)
            return;
        var hasVideo = ComputeHasVideo();
        if (hasVideo == _hasVideo)
            return;
        _hasVideo = hasVideo;
        VideoAvailabilityChanged?.Invoke();
    }

    private void HandleTrackListChange(string name, object? value)
    {
        if (_disposed)
            return;
        AudioTracksChanged?.Invoke();
        SubtitleTracksChanged?.Invoke();
    }

    private void HandleSubTextChange(string name, object? value)
    {
        if (_disposed)
            return;
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            // The cue has cleared. Forget it so the very same line is read again if it comes back later.
            _lastSubText = null;
            return;
        }
        if (text == _lastSubText)
            return;
        _lastSubText = text;
        SubtitleTextChanged?.Invoke(text);
    }

    // A decoded video track with real dimensions that is neither a still image nor album art.
    private bool ComputeHasVideo()
    {
        if ((ReadDouble("video-params/w") ?? 0) <= 0)
            return false;
        if (ReadBoolean("current-tracks/video/image") == true)
            return false;
        return ReadBoolean("current-tracks/video/albumart") != true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _endRegistration.Dispose();
        _sampleRateRegistration.Dispose();
        _videoParamsRegistration.Dispose();
        _trackListRegistration.Dispose();
        _subTextRegistration.Dispose();
        foreach (var pending in _spotifyAudio)
            if (_spotifyAudio.TryRemove(pending.Key, out var unused)) unused.Dispose();
        _mpv.Dispose();
    }

    private void HandleEndFile(MpvEvent mpvEvent)
    {
        if (_disposed || mpvEvent.Data is not MpvEndFileEvent endFile)
            return;
        var reason = endFile.Reason switch
        {
            MpvEndFileReason.Eof => PlaybackEndReason.EndOfFile,
            MpvEndFileReason.Error => PlaybackEndReason.Error,
            _ => (PlaybackEndReason?)null,
        };
        if (reason.HasValue)
            Ended?.Invoke(reason.Value);
    }

    /// <summary>What mpv reports as media-title. mpv substitutes the file name when the media declares no
    /// title, so deciding whether this is a real title is left to the caller, which knows the path.</summary>
    public string? MediaTitle => ReadString("media-title")?.Trim() is { Length: > 0 } title
        ? LegacyMetadataEncoding.RepairArabicMojibake(title)
        : null;

    // mpv owns both halves of this: keep-open leaves a finished file loaded so it can still be seeked,
    // and loop-file repeats it without the gap a reload would leave. The managed end-of-file handler still
    // runs for the advance case, and as a fallback if either property is unavailable.
    public void SetEndBehavior(EndBehavior behavior)
    {
        SetPropertySafely("keep-open", behavior == EndBehavior.None ? "yes" : "no");
        SetPropertySafely("loop-file", behavior == EndBehavior.Loop ? "inf" : "no");
    }

    public void SetMediaControls(bool enabled) => ApplyMediaControls(enabled);

    // Both halves are set together: media-controls is the OS transport overlay mpv publishes, and
    // input-media-keys is whether it answers the play/pause keys on a keyboard or headset. Disabling the
    // controls while still capturing the keys is the arrangement that surprises people, so the setting
    // moves them as one.
    private void ApplyMediaControls(bool enabled)
    {
        var value = enabled ? "yes" : "no";
        SetPropertySafely("media-controls", value);
        SetPropertySafely("input-media-keys", value);
    }

    private void SetPropertySafely(string name, object value) => TrySetProperty(name, value);

    /// <summary>Converts Luna's linear percentage to mpv's cubic volume scale. For example, Luna's
    /// 2000% is a 20x amplitude gain and maps to approximately 271.44 on mpv's volume property.</summary>
    private static double ToMpvVolume(double volume)
        => volume <= 0 ? 0 : Precision.Normalize(100 * Math.Cbrt(volume / 100));

    private void ApplyVolume() => SetPropertySafely("volume", ToMpvVolume(_volume));

    /// <summary>The failures a call into libmpv can produce: mpv refusing the call, the player having been
    /// shut down under it, and a property whose value does not convert to the type the caller asked for.
    /// None of them is worth bringing the player down over - every caller here has something sensible to do
    /// with "that did not work", and a media file that makes mpv unhappy is an ordinary event.</summary>
    private static bool IsFailure(Exception exception)
        => exception is MpvException or InvalidOperationException
            or FormatException or InvalidCastException or OverflowException;

    /// <summary>Runs something against mpv, reporting whether it got through.</summary>
    private bool TryDo(Action<MPV> action)
    {
        try
        {
            action(_mpv);
            return true;
        }
        catch (Exception exception) when (IsFailure(exception))
        {
            return false;
        }
    }

    /// <summary>Reads a property and converts it, or null when mpv has no value for it, will not answer, or
    /// answers with something the conversion cannot use.</summary>
    private T? ReadValue<T>(string name, Func<object, T> convert) where T : struct
    {
        try
        {
            return _mpv.GetProperty(name) is { } value ? convert(value) : null;
        }
        catch (Exception exception) when (IsFailure(exception))
        {
            return null;
        }
    }

    /// <summary>A property read without converting it, for the ones that answer with a list rather than a
    /// value. Null means mpv had nothing to say, one way or another.</summary>
    private object? ReadObject(string name)
    {
        try
        {
            return _mpv.GetProperty(name);
        }
        catch (Exception exception) when (IsFailure(exception))
        {
            return null;
        }
    }

    private bool TrySetProperty(string name, object value) => TryDo(mpv => mpv.SetProperty(name, value));

    private double? ReadDouble(string name)
        => ReadValue(name, static value => Convert.ToDouble(value, CultureInfo.InvariantCulture));

    private bool? ReadBoolean(string name)
        => ReadValue(name, static value => Convert.ToBoolean(value, CultureInfo.InvariantCulture));

    private string? ReadString(string name)
        => ReadObject(name) is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private bool AddFilter(string filter) => TryDo(mpv => mpv.Command("af", "add", filter));

    private void RemoveFilter(string label) => TryDo(mpv => mpv.Command("af", "remove", label));
}
