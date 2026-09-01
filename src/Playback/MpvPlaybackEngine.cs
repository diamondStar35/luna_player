using System.Globalization;
using LunaPlayer.Configuration;
using MpvNet;

namespace LunaPlayer.Playback;

internal sealed class MpvPlaybackEngine : IPlaybackEngine
{
    private readonly MPV _mpv;
    private readonly IDisposable _endRegistration;
    private double _volume = 100;
    private bool _normalizationEnabled;
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
            ["volume_max"] = 1000,
        };
        if (windowHandle != 0)
            options["wid"] = windowHandle.ToString(CultureInfo.InvariantCulture);

        _mpv = new MPV(options: options);
        SetPropertySafely("network-timeout", 10);
        SetPropertySafely("media-controls", "yes");
        SetPropertySafely("input-media-keys", "yes");
        _endRegistration = _mpv.OnEvent(HandleEndFile, MpvEventId.EndFile);
    }

    public event Action<PlaybackEndReason>? Ended;

    public bool Load(string path, double? startPosition = null, bool paused = false)
    {
        Dictionary<string, object?>? options = startPosition.HasValue
            ? new Dictionary<string, object?> { ["start"] = startPosition.Value }
            : null;
        return TryDo(mpv =>
        {
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

    public double? Duration => ReadDouble("duration");

    public double? Elapsed => ReadDouble("time-pos");

    public double? Remaining => ReadDouble("time-remaining");

    public void SeekRelative(double seconds)
        => TryDo(mpv => mpv.Command("seek", seconds, "relative"));

    public void SeekAbsolute(double seconds)
        => TryDo(mpv => mpv.Command("seek", Math.Max(0, seconds), "absolute"));

    public bool SetLoopStart(double seconds)
    {
        var startSet = TrySetProperty("ab-loop-a", Math.Max(0, seconds));
        var endCleared = TrySetProperty("ab-loop-b", "no");
        return startSet && endCleared;
    }

    public bool SetLoopEnd(double seconds)
        => TrySetProperty("ab-loop-b", Math.Max(0, seconds));

    public bool ClearLoop()
    {
        var startCleared = TrySetProperty("ab-loop-a", "no");
        var endCleared = TrySetProperty("ab-loop-b", "no");
        return startCleared && endCleared;
    }

    public double SetVolume(double volume)
    {
        _volume = Math.Clamp(volume, 0, 1000);
        if (!_normalizationEnabled || _volume <= 100)
        {
            SetPreamp(1);
            SetPropertySafely("volume", _volume);
        }
        else
        {
            SetPreamp(Math.Min(10, _volume / 100));
            SetPropertySafely("volume", 100);
        }
        return _volume;
    }

    public double Volume => _volume;

    public double SetSpeed(double speed)
    {
        var value = Math.Clamp(speed, 0.5, 6);
        SetPropertySafely("speed", value);
        return value;
    }

    public double Speed => ReadDouble("speed") is double speed ? Math.Clamp(speed, 0.5, 6) : 1;

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
        RemoveFilter("@audiopreamp");
        RemoveFilter("@audionormalize");
        _normalizationEnabled = enabled;
        if (!enabled)
        {
            SetPropertySafely("volume", _volume);
            return true;
        }
        if (!AddFilter("@audiopreamp:lavfi=[volume=1.0]")
            || !AddFilter("@audionormalize:lavfi=[dynaudnorm=f=150:g=15,alimiter=limit=0.95]"))
        {
            RemoveFilter("@audiopreamp");
            RemoveFilter("@audionormalize");
            _normalizationEnabled = false;
            SetPropertySafely("volume", _volume);
            return false;
        }
        SetVolume(_volume);
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
    public string? MediaTitle => ReadString("media-title")?.Trim() is { Length: > 0 } title ? title : null;

    // mpv owns both halves of this: keep-open leaves a finished file loaded so it can still be seeked,
    // and loop-file repeats it without the gap a reload would leave. The managed end-of-file handler still
    // runs for the advance case, and as a fallback if either property is unavailable.
    public void SetEndBehavior(EndBehavior behavior)
    {
        SetPropertySafely("keep-open", behavior == EndBehavior.None ? "yes" : "no");
        SetPropertySafely("loop-file", behavior == EndBehavior.Loop ? "inf" : "no");
    }

    private void SetPropertySafely(string name, object value) => TrySetProperty(name, value);

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

    private void SetPreamp(double gain)
    {
        if (!_normalizationEnabled) return;
        RemoveFilter("@audiopreamp");
        AddFilter($"@audiopreamp:lavfi=[volume={gain.ToString("0.000", CultureInfo.InvariantCulture)}]");
    }
}
