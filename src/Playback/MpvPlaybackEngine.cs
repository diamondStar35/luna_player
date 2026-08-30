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
        try
        {
            Dictionary<string, object?>? options = startPosition.HasValue
                ? new Dictionary<string, object?> { ["start"] = startPosition.Value }
                : null;
            _mpv.LoadFile(path, "replace", options);
            _mpv.SetProperty("pause", paused);
            return true;
        }
        catch (MpvException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public void Stop() => TryCommand(static mpv => mpv.Command("stop"));

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
        => TryCommand(mpv => mpv.Command("seek", seconds, "relative"));

    public void SeekAbsolute(double seconds)
        => TryCommand(mpv => mpv.Command("seek", Math.Max(0, seconds), "absolute"));

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
        try
        {
            if (_mpv.GetProperty("audio-device-list") is not IEnumerable<object?> values)
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
        catch (MpvException)
        {
            return [];
        }
        catch (InvalidOperationException)
        {
            return [];
        }
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

    // mpv owns both halves of this: keep-open leaves a finished file loaded so it can still be seeked,
    // and loop-file repeats it without the gap a reload would leave. The managed end-of-file handler still
    // runs for the advance case, and as a fallback if either property is unavailable.
    /// <summary>What mpv reports as media-title. mpv substitutes the file name when the media declares no
    /// title, so deciding whether this is a real title is left to the caller, which knows the path.</summary>
    public string? MediaTitle => ReadString("media-title")?.Trim() is { Length: > 0 } title ? title : null;

    public void SetEndBehavior(EndBehavior behavior)
    {
        SetPropertySafely("keep-open", behavior == EndBehavior.None ? "yes" : "no");
        SetPropertySafely("loop-file", behavior == EndBehavior.Loop ? "inf" : "no");
    }

    private void SetPropertySafely(string name, object value) => TrySetProperty(name, value);

    private bool TrySetProperty(string name, object value)
    {
        try
        {
            _mpv.SetProperty(name, value);
            return true;
        }
        catch (MpvException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private double? ReadDouble(string name)
    {
        try
        {
            var value = _mpv.GetProperty(name);
            return value is null ? null : Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch (MpvException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private bool? ReadBoolean(string name)
    {
        try
        {
            var value = _mpv.GetProperty(name);
            return value is null ? null : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }
        catch (MpvException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private string? ReadString(string name)
    {
        try
        {
            return Convert.ToString(_mpv.GetProperty(name), CultureInfo.InvariantCulture);
        }
        catch (MpvException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private void TryCommand(Action<MPV> command)
    {
        try
        {
            command(_mpv);
        }
        catch (MpvException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private bool AddFilter(string filter)
    {
        try
        {
            _mpv.Command("af", "add", filter);
            return true;
        }
        catch (Exception exception) when (exception is MpvException or InvalidOperationException)
        {
            return false;
        }
    }

    private void RemoveFilter(string label) => TryCommand(mpv => mpv.Command("af", "remove", label));

    private void SetPreamp(double gain)
    {
        if (!_normalizationEnabled) return;
        RemoveFilter("@audiopreamp");
        AddFilter($"@audiopreamp:lavfi=[volume={gain.ToString("0.000", CultureInfo.InvariantCulture)}]");
    }
}
