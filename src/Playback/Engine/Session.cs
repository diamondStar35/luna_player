using System.Globalization;
using LunaPlayer.Configuration.Models;
using MpvNet;
using LunaPlayer.Media;
namespace LunaPlayer.Playback;

internal sealed class MpvSession : IDisposable
{
    private readonly MPV _mpv;
    private readonly Lock _gate;
    private bool _disposed;
    private readonly List<IDisposable> _subscriptions = [];
    internal MPV Native => _mpv;
    internal MpvSession(nint windowHandle, Lock gate)
    {
        _gate = gate;
        var options = new Dictionary<string, object?>
        {
            ["vo"] = "gpu",
            ["keep_open"] = "no",
            ["input_default_bindings"] = false,
            ["input_vo_keyboard"] = false,
            // Luna owns the Windows media session and routes its buttons through the active source.
            // A second session from mpv duplicates the overlay and bypasses that command routing.
            ["media_controls"] = false,
            ["input_media_keys"] = false,
            // Subtitles start off: nothing is read until the user picks a track. sub-auto keeps mpv from
            // pulling in sidecar .srt files, and sid=no leaves every embedded track unselected on load.
            ["sub_auto"] = "no",
            ["sid"] = "no",
            ["volume_max"] = Math.Ceiling(AudioProcessor.ToMpvVolume(AudioSettings.MaximumVolume)),
        };
        if (windowHandle != 0)
            options["wid"] = windowHandle.ToString(CultureInfo.InvariantCulture);

        _mpv = new MPV(options: options, optionalOptions: new Dictionary<string, object?>
        {
            // The on-screen controller option exists only when libmpv includes Lua.
            ["osc"] = false,
        });
    }
    internal IDisposable ObserveProperty(string name, Action<string, object?> handler)
        => Subscribe(() => _mpv.ObserveProperty(name, handler));

    internal IDisposable OnEvent(Action<MpvEvent> handler, MpvEventId id)
        => Subscribe(() => _mpv.OnEvent(handler, id));

    private IDisposable Subscribe(Func<IDisposable> create)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var registration = create();
            _subscriptions.Add(registration);
            return registration;
        }
    }

    public void Dispose()
    {
        IDisposable[] subscriptions;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            subscriptions = _subscriptions.ToArray();
            _subscriptions.Clear();
        }
        // Native teardown may wait for callbacks that need the coordinator's gate.
        // Release the gate before disposing registrations or joining the native event thread.
        foreach (var subscription in subscriptions) subscription.Dispose();
        _mpv.Dispose();
    }

    internal void SetPropertySafely(string name, object value) => TrySetProperty(name, value);


    internal void SetFade(double level) => TryDo(mpv => mpv.FadeVolume(level, 0));

    /// <summary>The failures a call into libmpv can produce: mpv refusing the call, the player having been
    /// shut down under it, and a property whose value does not convert to the type the caller asked for.
    /// None of them is worth bringing the player down over - every caller here has something sensible to do
    /// with "that did not work", and a media file that makes mpv unhappy is an ordinary event.</summary>
    internal static bool IsFailure(Exception exception)
        => exception is MpvException or InvalidOperationException
            or FormatException or InvalidCastException or OverflowException;

    internal static bool TryConvertDouble(object? value, out double result)
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
    internal bool TryDo(Action<MPV> action)
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
    internal T? ReadValue<T>(string name, Func<object, T> convert) where T : struct
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
    internal object? ReadObject(string name)
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

    internal bool TrySetProperty(string name, object value) => TryDo(mpv => mpv.SetProperty(name, value));

    internal double? ReadDouble(string name)
        => ReadValue(name, static value => Convert.ToDouble(value, CultureInfo.InvariantCulture));

    internal long? ReadInt64(string name)
        => ReadValue(name, static value => Convert.ToInt64(value, CultureInfo.InvariantCulture));

    internal long? PositiveEntryId()
    {
        // MPV exposes entry IDs through playlist records, not a playlist-entry-id property.
        // Read one snapshot: the current entry can already name the successor while the outgoing
        // entry is still playing, so a separate index lookup can identify the wrong file.
        if (ReadObject("playlist") is not IEnumerable<object?> entries)
            return null;
        foreach (var value in entries)
        {
            if (value is not IDictionary<string, object?> entry
                || !entry.TryGetValue("playing", out var playing) || playing is not true
                || !entry.TryGetValue("id", out var raw))
                continue;
            try
            {
                var id = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
                return id > 0 ? id : null;
            }
            catch (Exception exception) when (IsFailure(exception))
            {
                return null;
            }
        }
        return null;
    }

    internal bool? ReadBoolean(string name)
        => ReadValue(name, static value => Convert.ToBoolean(value, CultureInfo.InvariantCulture));

    internal string? ReadString(string name)
        => ReadObject(name) is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    internal bool AddFilter(string filter) => TryDo(mpv => mpv.Command("af", "add", filter));

    internal void RemoveFilter(string label) => TryDo(mpv => mpv.Command("af", "remove", label));
}
