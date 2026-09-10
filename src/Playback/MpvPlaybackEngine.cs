using System.Globalization;
using LunaPlayer.Equalizer;
using LunaPlayer.Configuration;
using LunaPlayer.Media;
using MpvNet;

namespace LunaPlayer.Playback;

internal sealed class MpvPlaybackEngine : IPlaybackEngine
{
    private readonly MPV _mpv;
    private readonly IDisposable _endRegistration;
    private double _volume = 100;
    private double _pitch;
    private bool _pitchFilterActive;
    private double _pan;
    private bool _panFilterActive;
    private bool _normalizationEnabled;
    private bool _equalizerFilterActive;
    private bool _disposed;

    internal MpvPlaybackEngine(nint windowHandle)
    {
        var options = new Dictionary<string, object?>
        {
            ["vo"] = "gpu",
            ["osc"] = false,
            ["keep_open"] = "no",
            ["input_default_bindings"] = false,
            ["input_vo_keyboard"] = false,
            ["volume_max"] = Math.Ceiling(ToMpvVolume(AudioSettings.MaximumVolume)),
        };
        if (windowHandle != 0)
            options["wid"] = windowHandle.ToString(CultureInfo.InvariantCulture);

        _mpv = new MPV(options: options);
        SetPropertySafely("network-timeout", 10);
        SetPropertySafely("media-controls", "yes");
        SetPropertySafely("input-media-keys", "yes");
        _endRegistration = _mpv.OnEvent(HandleEndFile, MpvEventId.EndFile);
        // First into the chain, and before anything the settings switch on later, so that what the
        // equalizer lifts is still ahead of the limiter normalization puts at the end. Added here
        // rather than when a preset is chosen: it stays for the life of the engine.
        BuildEqualizer(Bands.Slots(null));
    }

    public event Action<PlaybackEndReason>? Ended;

    /// <remarks>
    /// <paramref name="audioFile"/> is set as a property rather than passed as a loadfile option, and set
    /// on every load rather than only when there is one. Both matter. The option list is flattened into one
    /// string with commas and equals signs, which a stream address is made of, so a URL cannot survive it;
    /// and mpv keeps the property until it is told otherwise, so leaving it alone would play the previous
    /// video's sound over the next file.
    /// </remarks>
    public bool Load(string path, double? startPosition = null, bool paused = false, string? audioFile = null)
    {
        Dictionary<string, object?>? options = startPosition.HasValue
            ? new Dictionary<string, object?> { ["start"] = Precision.Normalize(startPosition.Value) }
            : null;
        return TryDo(mpv =>
        {
            mpv.SetProperty("audio-files", audioFile is null ? Array.Empty<string>() : new[] { audioFile });
            mpv.LoadFile(path, "replace", options);
            mpv.SetProperty("pause", paused);
        });
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
        => BuildEqualizer(Bands.Slots(preset));

    /// <summary>Puts the equalizer into the chain, carrying the bands it is to apply.</summary>
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
    /// Prepended rather than appended, so the equalizer stays ahead of the limiter normalization puts in
    /// the chain and a lifted band is still limited. If this build of mpv will not take <c>pre</c> it goes
    /// on the end instead - the wrong side of the limiter, but present and right in every other respect.
    /// </remarks>
    private bool BuildEqualizer(Band[] bands)
    {
        var filter = $"@{AudioFilters.EqualizerLabel}:lavfi=[{AudioFilters.EqualizerGraph(bands)}]";
        RemoveFilter($"@{AudioFilters.EqualizerLabel}");
        _equalizerFilterActive = TryDo(mpv => mpv.Command("af", "pre", filter)) || AddFilter(filter);
        return _equalizerFilterActive;
    }


    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _endRegistration.Dispose();
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
