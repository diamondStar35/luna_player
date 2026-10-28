using LunaPlayer.Equalizer;

namespace LunaPlayer.Playback;

internal sealed partial class AudioProcessor
{
    private bool? _monoFilterState;
    private string? _silenceFilterGraph;

    public bool SetNormalization(bool enabled)
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
    {
        if (_monoFilterState == enabled && HasFilter("audiomono") == enabled)
            return true;
        _session.RemoveFilter("@audiomono");
        var applied = !enabled || _session.AddFilter("@audiomono:lavfi=[aformat=channel_layouts=mono]");
        _monoFilterState = applied ? enabled : null;
        return applied;
    }

    public bool SetSilenceRemoval(bool enabled, string graph)
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
        => _session.ReadObject("af") is IEnumerable<object?> filters
            && filters.Any(value => value is IDictionary<string, object?> filter
                && filter.TryGetValue("label", out var name) && string.Equals(name as string, label, StringComparison.Ordinal)
                && (!filter.TryGetValue("enabled", out var enabled) || enabled is not false));

    public bool SetEqualizer(Preset? preset)
    {
        _equalizerPreset = preset;
        return BuildEqualizer();
    }

    /// <summary>Rebuilds the equalizer from its persisted filter string. Updating only the live filter
    /// would be lost whenever mpv rebuilds its audio chain. The filter is removed when the equalizer is off
    /// because a nominally flat band can ring at the Nyquist frequency, and it is prepended so normalization
    /// can limit any gain the equalizer adds.</summary>
    private bool BuildEqualizer()
    {
        _session.RemoveFilter($"@{AudioFilters.EqualizerLabel}");
        _equalizerFilterActive = false;
        if (_equalizerPreset is null)
            return true;

        // The rate the decoded audio runs at, so bands at or beyond its Nyquist are left out. Null before
        // a file is loaded; the graph is then built whole and rebuilt once the real rate is known.
        _equalizerSampleRate = _session.ReadDouble("audio-params/samplerate") ?? 0;
        var graph = AudioFilters.EqualizerGraph(
            Bands.Slots(_equalizerPreset), _equalizerSampleRate > 0 ? _equalizerSampleRate : null);
        if (graph.Length == 0)
            return true;

        var filter = $"@{AudioFilters.EqualizerLabel}:lavfi=[{graph}]";
        _equalizerFilterActive = _session.TryDo(mpv => mpv.Command("af", "pre", filter)) || _session.AddFilter(filter);
        return _equalizerFilterActive;
    }

}
