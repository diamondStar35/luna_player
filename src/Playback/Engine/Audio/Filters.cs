namespace LunaPlayer.Playback;

internal sealed partial class AudioProcessor
{
    private bool? _monoFilterState;
    private string? _silenceFilterGraph;

    public bool SetNormalization(bool enabled)
        => _session.QueueFilter(AudioFilter.Normalization, () => ApplyNormalization(enabled));

    private bool ApplyNormalization(bool enabled)
    {
        // Rebuilding an unchanged graph discards decoded lookahead just before a native overlap.
        // Check the persisted filter list too, since a file can supply its own per-file options.
        if (_normalizationEnabled == enabled && HasFilter("audionormalize") == enabled)
            return true;
        _session.RemoveFilter("@audionormalize");
        _normalizationEnabled = enabled;
        if (!enabled)
        {
            ApplyVolume();
            return true;
        }
        if (!_session.AddFilter("@audionormalize:lavfi=[dynaudnorm=f=150:g=15,alimiter=limit=0.95]"))
        {
            _session.RemoveFilter("@audionormalize");
            _normalizationEnabled = false;
            ApplyVolume();
            return false;
        }
        ApplyVolume();
        return true;
    }

    public bool SetMono(bool enabled)
        => _session.QueueFilter(AudioFilter.Mono, () => ApplyMono(enabled));

    private bool ApplyMono(bool enabled)
    {
        if (_monoFilterState == enabled && HasFilter("audiomono") == enabled)
            return true;
        _session.RemoveFilter("@audiomono");
        var applied = !enabled || _session.AddFilter("@audiomono:lavfi=[aformat=channel_layouts=mono]");
        _monoFilterState = applied ? enabled : null;
        return applied;
    }

    public bool SetSilenceRemoval(bool enabled, string graph)
        => _session.QueueFilter(AudioFilter.SilenceRemoval, () => ApplySilenceRemoval(enabled, graph));

    private bool ApplySilenceRemoval(bool enabled, string graph)
    {
        var desired = enabled ? graph : string.Empty;
        if (_silenceFilterGraph == desired && HasFilter("silenceremove") == enabled)
            return true;
        _session.RemoveFilter("@silenceremove");
        var applied = !enabled || (graph.Length > 0 && _session.AddFilter($"@silenceremove:lavfi=[{graph}]"));
        _silenceFilterGraph = applied ? desired : null;
        return applied;
    }

    private bool HasFilter(string label)
        => _session.ReadLiveObject("af") is IEnumerable<object?> filters
            && filters.Any(value => value is IDictionary<string, object?> filter
                && filter.TryGetValue("label", out var name) && string.Equals(name as string, label, StringComparison.Ordinal)
                && (!filter.TryGetValue("enabled", out var enabled) || enabled is not false));

}
