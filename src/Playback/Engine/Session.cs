using System.Globalization;
using LunaPlayer.Configuration.Models;
using MpvNet;
namespace LunaPlayer.Playback;

internal sealed partial class MpvSession : IDisposable
{
    private readonly MPV _mpv;
    private readonly Lock _gate;
    private volatile bool _disposed;
    private readonly List<IDisposable> _subscriptions = [];
    internal MpvSession(nint windowHandle, Lock gate, IReadOnlyDictionary<string, object?>? overrides = null)
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

        if (overrides is not null)
            foreach (var (key, value) in overrides) options[key] = value;

        _mpv = new MPV(options: options, optionalOptions: new Dictionary<string, object?>
        {
            // The on-screen controller option exists only when libmpv includes Lua.
            ["osc"] = false,
        });
        _work = new WorkQueue();
        InitializeProperties();
    }
    internal IDisposable ObserveProperty(string name, Action<string, object?> handler)
        => Subscribe(() => _mpv.ObserveProperty(name, (property, value) =>
        {
            if (_disposed) return;
            CacheProperty(property, value);
            handler(property, value);
        }));

    internal IDisposable OnEvent(Action<MpvEvent> handler, MpvEventId id)
        => Subscribe(() => _mpv.OnEvent(value =>
        {
            if (_disposed) return;
            if (id is MpvEventId.FileLoaded or MpvEventId.PlaybackRestart or MpvEventId.EndFile)
                RefreshProperties();
            handler(value);
        }, id));

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
        _work.Dispose();
        // The application disposes its player on the shutdown worker, outside the UI loop.
        _work.Completion.GetAwaiter().GetResult();
        foreach (var subscription in subscriptions) subscription.Dispose();
        _mpv.Dispose();
    }

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

}
