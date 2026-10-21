using LunaPlayer.Equalizer;

namespace LunaPlayer.Playback;

internal sealed partial class Engine
{
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

    /// <summary>Rebuilds the equalizer from its persisted filter string. Updating only the live filter
    /// would be lost whenever mpv rebuilds its audio chain. The filter is removed when the equalizer is off
    /// because a nominally flat band can ring at the Nyquist frequency, and it is prepended so normalization
    /// can limit any gain the equalizer adds.</summary>
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

}
