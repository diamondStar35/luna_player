using System.Globalization;

namespace LunaPlayer.Playback;

internal sealed partial class MpvSession
{
    private readonly Lock _propertiesGate = new();
    private readonly Dictionary<string, object?> _properties = new(StringComparer.Ordinal);
    // UI getters consume observations, never a native query that could wait behind a filter.
    private static readonly string[] ObservedProperties =
    [
        "af", "audio-device", "audio-device-list", "audio-filter-history-reset", "audio-fade", "audio-fade/active",
        "audio-fade/paused", "audio-fade/transport-active", "audio-params/samplerate", "audio-out-params/channel-count",
        "chapter", "chapter-list", "current-tracks/video/image", "current-tracks/video/albumart",
        "duration", "eof-reached", "filename", "media-title", "metadata", "path", "pause",
        "playlist", "playlist-count", "seeking", "speed", "sub-text", "time-pos",
        "time-remaining", "track-list", "track-list/count", "video-params/w",
    ];

    private void InitializeProperties()
    {
        foreach (var name in ObservedProperties)
        {
            CacheProperty(name, ReadLiveObject(name));
            _subscriptions.Add(_mpv.ObserveProperty(name, CacheProperty));
        }
    }

    private void CacheProperty(string name, object? value)
    {
        lock (_propertiesGate) _properties[name] = value;
    }

    // File lifecycle events can precede property notifications. Refresh on the event thread,
    // before entering the coordinator's lock, so identity checks do not block UI commands.
    private void RefreshProperties()
    {
        foreach (var name in ObservedProperties)
            CacheProperty(name, ReadLiveObject(name));
    }

    internal object? ReadObject(string name)
    {
        lock (_propertiesGate) return _properties.GetValueOrDefault(name);
    }

    /// <summary>Native reads are restricted to initialization, the worker, and event callbacks.</summary>
    internal object? ReadLiveObject(string name)
    {
        try { return _mpv.GetProperty(name); }
        catch (Exception exception) when (IsFailure(exception)) { return null; }
    }

    internal T? ReadValue<T>(string name, Func<object, T> convert) where T : struct
    {
        try { return ReadObject(name) is { } value ? convert(value) : null; }
        catch (Exception exception) when (IsFailure(exception)) { return null; }
    }

    internal double? ReadDouble(string name)
        => ReadValue(name, static value => Convert.ToDouble(value, CultureInfo.InvariantCulture));
    internal long? ReadInt64(string name)
        => ReadValue(name, static value => Convert.ToInt64(value, CultureInfo.InvariantCulture));
    internal bool? ReadBoolean(string name)
        => ReadValue(name, static value => Convert.ToBoolean(value, CultureInfo.InvariantCulture));
    internal string? ReadString(string name)
        => ReadObject(name) is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    internal long? PositiveEntryId()
    {
        // The playing record is authoritative when the playlist index already names a successor.
        if (ReadObject("playlist") is not IEnumerable<object?> entries) return null;
        foreach (var value in entries)
        {
            if (value is not IDictionary<string, object?> entry
                || !entry.TryGetValue("playing", out var playing) || playing is not true
                || !entry.TryGetValue("id", out var raw)) continue;
            try
            {
                var id = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
                return id > 0 ? id : null;
            }
            catch (Exception exception) when (IsFailure(exception)) { return null; }
        }
        return null;
    }
}
