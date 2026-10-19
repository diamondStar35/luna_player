using System.Globalization;
using LunaPlayer.Equalizer;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using MpvNet;

namespace LunaPlayer.Playback;

internal sealed class MpvPlaybackEngine : IPlaybackEngine
{
    private readonly MPV _mpv;
    private readonly IDisposable _endRegistration;
    private readonly IDisposable _playbackRestartRegistration;
    private readonly IDisposable _positionRegistration;
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
    private EndBehavior _endBehavior;
    private bool _selectionRangeActive;
    private bool _selectionRangeRepeats;
    private bool _selectionRangeCompleted;
    private double _selectionRangeEnd;
    private long? _selectionRangeEntryId;
    private bool _releasedSelectionEndPending;
    private double _releasedSelectionEnd;
    private long? _releasedSelectionEntryId;
    private bool _releasedSelectionWasPlaying;
    private bool _releasedSelectionWasCompleted;
    private bool _releasedSelectionPassed;
    private bool _endBehaviorRestorePending;
    private bool _selectionSeekPending;
    private bool _playAfterSelectionSeek;
    private bool _restoreEndBehaviorAfterSelectionSeek;
    private double _selectionSeekTarget;
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
        SetPropertySafely("network-timeout", 10);
        ApplyMediaControls(mediaControls);
        _endRegistration = _mpv.OnEvent(HandleEndFile, MpvEventId.EndFile);
        _playbackRestartRegistration = _mpv.OnEvent(HandlePlaybackRestart, MpvEventId.PlaybackRestart);
        _positionRegistration = _mpv.ObserveProperty("time-pos", HandlePositionChange);
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

    public event Action<double>? SelectionEnded;

    public event Action? SelectionSeekCompleted;

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
        CancelSelectionSeek();
        _endBehaviorRestorePending = false;
        // A delayed EOF from the replaced entry is still identified below, but it must never resume by
        // seeking the newly loaded entry to the old selection's timestamp.
        _releasedSelectionWasPlaying = false;
        _releasedSelectionWasCompleted = false;
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
            // A one-shot selection sets this dynamically. Never let that boundary leak into the next file.
            ["end"] = "none",
            ["keep-open"] = _endBehavior == EndBehavior.None ? "yes" : "no",
            ["loop-file"] = _endBehavior == EndBehavior.Loop ? "inf" : "no",
        };
        if (startPosition.HasValue)
            options["start"] = Precision.Normalize(startPosition.Value);
        var loaded = TryDo(mpv =>
        {
            mpv.SetPropertyAsync(0, "audio-files", audioFile is null ? Array.Empty<string>() : new[] { audioFile });
            mpv.LoadFileAsync(path, "replace", options);
        });
        _selectionRangeActive = false;
        _selectionRangeRepeats = false;
        _selectionRangeCompleted = false;
        _selectionRangeEnd = 0;
        _selectionRangeEntryId = null;
        return loaded;
    }

    public void Stop()
    {
        CancelSelectionSeek();
        _endBehaviorRestorePending = false;
        _releasedSelectionEndPending = false;
        _releasedSelectionWasPlaying = false;
        _releasedSelectionWasCompleted = false;
        _releasedSelectionPassed = false;
        TryDo(static mpv => mpv.Command("stop"));
    }

    public bool TogglePause()
    {
        if (IsPaused)
        {
            Play();
            return true;
        }
        Pause();
        return false;
    }

    public void Play()
    {
        // Clearing an mpv end boundary does not release its EOF latch. When playback is explicitly resumed
        // from the free slot, seek to the exact current timestamp to re-arm decoding, then play after mpv
        // confirms that zero-distance seek. Entering the free slot itself never changes time or playback.
        if (_selectionSeekPending)
            return;
        if (_endBehaviorRestorePending
            && (IsPaused || _releasedSelectionWasCompleted)
            && (Elapsed ?? (_releasedSelectionEndPending ? _releasedSelectionEnd : null)) is double position
            && BeginSelectionSeek(position, restoreEndBehavior: true, play: true))
            return;
        SetPropertySafely("pause", false);
    }

    public void Pause() => SetPropertySafely("pause", true);

    public bool IsPaused => ReadBoolean("pause") ?? false;

    public bool SelectionRangeCompleted
        => _selectionRangeActive
            && !_selectionRangeRepeats
            && (_selectionRangeCompleted || ReadBoolean("eof-reached") == true);

    public double? Duration => Precision.Normalize(ReadDouble("duration"));

    public double? Elapsed => Precision.Normalize(ReadDouble("time-pos"));

    public double? Remaining => Precision.Normalize(ReadDouble("time-remaining"));

    public void SeekRelative(double seconds)
    {
        var delta = Precision.Normalize(seconds);
        if (_endBehaviorRestorePending
            && Elapsed is double position
            && BeginSelectionSeek(position + delta, restoreEndBehavior: true, play: !IsPaused))
            return;
        TryDo(mpv => mpv.Command("seek", delta, "relative"));
    }

    public void SeekAbsolute(double seconds)
    {
        var target = Precision.Normalize(Math.Max(0, seconds));
        if (_endBehaviorRestorePending
            && BeginSelectionSeek(target, restoreEndBehavior: true, play: !IsPaused))
            return;
        TryDo(mpv => mpv.Command("seek", target, "absolute"));
    }

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

    public bool SetSelectionRange(double start, double end, bool repeat)
    {
        start = Precision.Normalize(Math.Max(0, start));
        end = Precision.Normalize(Math.Max(start, end));
        if (end <= start)
            return false;
        // A move between slots replaces the previous range as one operation. Clear its boundary first so
        // neither an old one-shot end nor an old A-B loop can affect the slot being entered.
        if (_selectionRangeActive && !ClearSelectionRange(restoreEndBehavior: false))
        {
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
            return false;
        }
        CancelSelectionSeek();
        _endBehaviorRestorePending = false;
        _selectionRangeActive = true;
        _selectionRangeRepeats = repeat;
        _selectionRangeCompleted = false;
        _selectionRangeEnd = end;
        var entryId = ReadInt64("playlist-entry-id");
        _selectionRangeEntryId = entryId is > 0 ? entryId : null;
        var endSet = TrySetProperty("end", repeat ? "none" : end);
        var fileLoopCleared = TrySetProperty("loop-file", "no");
        // A one-shot selection reaches an artificial EOF. keep-open makes mpv hold the exact boundary so
        // Play can later clear it and continue from that same timestamp.
        var heldOpen = TrySetProperty("keep-open", "yes");
        if (repeat)
        {
            var startSet = TrySetProperty("ab-loop-a", start);
            var loopEndSet = TrySetProperty("ab-loop-b", end);
            var succeeded = endSet && fileLoopCleared && heldOpen && startSet && loopEndSet;
            if (!succeeded)
                ClearSelectionRange();
            return succeeded;
        }
        var loopCleared = ClearLoop();
        var oneShotSucceeded = endSet && fileLoopCleared && heldOpen && loopCleared;
        if (!oneShotSucceeded)
            ClearSelectionRange();
        return oneShotSucceeded;
    }

    public bool PlaySelectionRange(double start, double end, bool repeat)
    {
        var wasPaused = IsPaused;
        Pause();
        if (!SetSelectionRange(start, end, repeat)
            || !BeginSelectionSeek(start, restoreEndBehavior: false, play: true))
        {
            ClearSelectionRange();
            if (!wasPaused)
                Play();
            return false;
        }
        return true;
    }

    public bool ExitSelectionRange(double end, bool play)
    {
        if (!ClearSelectionRange(restoreEndBehavior: false))
        {
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
            return false;
        }
        if (BeginSelectionSeek(end, restoreEndBehavior: true, play))
            return true;
        _endBehaviorRestorePending = false;
        ApplyEndBehavior();
        return false;
    }

    public bool ClearSelectionRange(bool restoreEndBehavior = true)
    {
        if (!_selectionRangeActive)
            return true;
        var releasedOneShot = !_selectionRangeRepeats;
        if (releasedOneShot)
        {
            // END_FILE may already be queued when the boundary is removed. Keep enough identity to label
            // that event as the released selection's end instead of the real end of the current file.
            _releasedSelectionEndPending = true;
            _releasedSelectionEnd = _selectionRangeEnd;
            _releasedSelectionEntryId = _selectionRangeEntryId;
            _releasedSelectionWasPlaying = !IsPaused;
            _releasedSelectionWasCompleted = SelectionRangeCompleted;
            _releasedSelectionPassed = false;
        }
        CancelSelectionSeek();
        _selectionRangeActive = false;
        _selectionRangeRepeats = false;
        _selectionRangeCompleted = false;
        _selectionRangeEnd = 0;
        _selectionRangeEntryId = null;
        var endCleared = TrySetProperty("end", "none");
        var loopCleared = ClearLoop();
        if (restoreEndBehavior)
        {
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
        }
        else if (releasedOneShot)
            _endBehaviorRestorePending = true;
        else
        {
            // A repeating range has no artificial EOF latch: removing its A-B loop is sufficient, so the
            // configured file-end behaviour can be restored immediately without a recovery seek.
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
        }
        return endCleared && loopCleared;
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
        _playbackRestartRegistration.Dispose();
        _positionRegistration.Dispose();
        _sampleRateRegistration.Dispose();
        _videoParamsRegistration.Dispose();
        _trackListRegistration.Dispose();
        _subTextRegistration.Dispose();
        _mpv.Dispose();
    }

    private void HandleEndFile(MpvEvent mpvEvent)
    {
        if (_disposed || mpvEvent.Data is not MpvEndFileEvent endFile)
            return;
        var releasedSelectionEnd = false;
        var continueReleasedSelection = false;
        var releasedSelectionEndPosition = 0d;
        if (endFile.Reason == MpvEndFileReason.Eof
            && _releasedSelectionEndPending
            && (!_releasedSelectionEntryId.HasValue
                || endFile.PlaylistEntryId <= 0
                || _releasedSelectionEntryId.Value == endFile.PlaylistEntryId))
        {
            // A boundary that was already firing when the free slot was entered arrives after the boundary
            // has been removed. Match it to that boundary's timestamp; merely being before the file's real
            // end is not enough, because a newer selection in the same file may now be ending instead.
            releasedSelectionEnd = !_releasedSelectionPassed
                && (Elapsed is not double elapsed
                    || Math.Abs(elapsed - _releasedSelectionEnd) <= 0.05);
            continueReleasedSelection = releasedSelectionEnd
                && _releasedSelectionWasPlaying
                && !_selectionRangeActive;
            releasedSelectionEndPosition = _releasedSelectionEnd;
            _releasedSelectionEndPending = false;
            _releasedSelectionEnd = 0;
            _releasedSelectionEntryId = null;
            _releasedSelectionWasPlaying = false;
            _releasedSelectionWasCompleted = false;
            _releasedSelectionPassed = false;
        }
        if (releasedSelectionEnd
            || endFile.Reason == MpvEndFileReason.Eof
                && _selectionRangeActive
                && !_selectionRangeRepeats)
        {
            if (!releasedSelectionEnd)
                _selectionRangeCompleted = true;
            // Selection completion has its own channel. It must never be mistaken for the real end of a
            // file, even when the UI enters the free slot while mpv is already queuing this event.
            SelectionEnded?.Invoke(releasedSelectionEnd
                ? releasedSelectionEndPosition
                : _selectionRangeEnd);
            if (continueReleasedSelection)
                BeginSelectionSeek(releasedSelectionEndPosition, restoreEndBehavior: true, play: true);
            return;
        }
        var reason = endFile.Reason switch
        {
            MpvEndFileReason.Eof => PlaybackEndReason.EndOfFile,
            MpvEndFileReason.Error => PlaybackEndReason.Error,
            _ => (PlaybackEndReason?)null,
        };
        if (reason.HasValue)
            Ended?.Invoke(reason.Value);
    }

    private bool BeginSelectionSeek(double seconds, bool restoreEndBehavior, bool play)
    {
        var target = Precision.Normalize(Math.Max(0, seconds));
        _selectionSeekPending = true;
        _restoreEndBehaviorAfterSelectionSeek = restoreEndBehavior;
        _playAfterSelectionSeek = play;
        _selectionSeekTarget = target;
        if (TryDo(mpv => mpv.Command("seek", target, "absolute+exact")))
            return true;
        CancelSelectionSeek();
        return false;
    }

    private void HandlePlaybackRestart(MpvEvent mpvEvent)
    {
        if (_disposed)
            return;
        if (!_selectionSeekPending)
            return;
        // Playback-restart events from an earlier seek may already be waiting in mpv's event queue. Only
        // finish this transition once the seek we issued is no longer running and its target is current.
        if (ReadBoolean("seeking") == true
            || Elapsed is double elapsed && Math.Abs(elapsed - _selectionSeekTarget) > 0.5)
            return;
        var restoreEndBehavior = _restoreEndBehaviorAfterSelectionSeek;
        var play = _playAfterSelectionSeek;
        CancelSelectionSeek();
        if (restoreEndBehavior || _endBehaviorRestorePending)
        {
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
        }
        if (play)
            Play();
        SelectionSeekCompleted?.Invoke();
    }

    private void HandlePositionChange(string name, object? value)
    {
        if (_disposed
            || !_releasedSelectionEndPending
            || _releasedSelectionPassed
            || !TryConvertDouble(value, out var position)
            || position <= _releasedSelectionEnd + 0.001)
            return;
        var entryId = ReadInt64("playlist-entry-id");
        if (_releasedSelectionEntryId.HasValue
            && entryId.HasValue
            && _releasedSelectionEntryId.Value != entryId.Value)
            return;
        // The old boundary was removed before it fired and playback has demonstrably crossed it. End
        // behaviour is safe to restore now; the pending identity remains until the eventual real EOF so
        // a delayed event can still be classified from its timestamp instead of advancing by mistake.
        _releasedSelectionPassed = true;
        _releasedSelectionWasPlaying = false;
        _releasedSelectionWasCompleted = false;
        if (_endBehaviorRestorePending)
        {
            _endBehaviorRestorePending = false;
            ApplyEndBehavior();
        }
    }

    private void CancelSelectionSeek()
    {
        _selectionSeekPending = false;
        _restoreEndBehaviorAfterSelectionSeek = false;
        _playAfterSelectionSeek = false;
        _selectionSeekTarget = 0;
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
        _endBehavior = behavior;
        if (!_selectionRangeActive && !_endBehaviorRestorePending)
            ApplyEndBehavior();
    }

    private void ApplyEndBehavior()
    {
        SetPropertySafely("keep-open", _endBehavior == EndBehavior.None ? "yes" : "no");
        SetPropertySafely("loop-file", _endBehavior == EndBehavior.Loop ? "inf" : "no");
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

    private static bool TryConvertDouble(object? value, out double result)
    {
        try
        {
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return value is not null;
        }
        catch (Exception exception) when (IsFailure(exception))
        {
            result = 0;
            return false;
        }
    }

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

    private long? ReadInt64(string name)
        => ReadValue(name, static value => Convert.ToInt64(value, CultureInfo.InvariantCulture));

    private bool? ReadBoolean(string name)
        => ReadValue(name, static value => Convert.ToBoolean(value, CultureInfo.InvariantCulture));

    private string? ReadString(string name)
        => ReadObject(name) is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private bool AddFilter(string filter) => TryDo(mpv => mpv.Command("af", "add", filter));

    private void RemoveFilter(string label) => TryDo(mpv => mpv.Command("af", "remove", label));
}
