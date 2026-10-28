using LunaPlayer.Media;
namespace LunaPlayer.Playback;
internal sealed class MetadataDiscovery(MpvSession session, Action<string, object?> changed)
{
    private readonly MpvSession _session = session;
    private IDisposable? _metadataRegistration;
    private IDisposable? _mediaTitleRegistration;
    internal string? Title => _session.ReadString("media-title")?.Trim() is { Length: > 0 } title
        ? LegacyMetadataEncoding.RepairArabicMojibake(title) : null;
    internal Metadata Read()
    {
        _metadataRegistration ??= _session.ObserveProperty("metadata", changed);
        _mediaTitleRegistration ??= _session.ObserveProperty("media-title", changed);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (_session.ReadObject("metadata") is IDictionary<string, object?> tags)
            foreach (var (key, value) in tags)
                if (value is string text && !string.IsNullOrWhiteSpace(text))
                    fields[key] = LegacyMetadataEncoding.RepairArabicMojibake(text.Trim());
        // mpv resolves ICY titles, overrides and fallbacks through media-title. Keep that decision
        // native; the tag snapshot provides artist, album and any additional broadcast fields.
        var title = Title ?? string.Empty;
        // A filename fallback is not title metadata. Let each caller use its file or station name
        // when mpv has no actual title, comparing mpv's own filename rather than guessing from URLs.
        if (string.Equals(title, _session.ReadString("filename"), StringComparison.Ordinal)) title = string.Empty;
        return new Metadata(new MediaTags(title,
            fields.GetValueOrDefault("artist", string.Empty),
            fields.GetValueOrDefault("album", string.Empty)), fields);
    }
}
