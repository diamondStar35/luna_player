using LunaPlayer.Equalizer;

namespace LunaPlayer.Playback;

internal sealed partial class AudioProcessor
{
    private Preset? _equalizerPreset;
    private string _equalizerGraph = string.Empty;
    private bool _equalizerLoading;

    public bool SetEqualizer(Preset? preset)
    {
        _equalizerPreset = preset is null ? null : preset with { Bands = preset.Bands.ToArray() };
        return _session.QueueFilter(AudioFilter.Equalizer, BuildEqualizer);
    }

    internal void HandleSampleRateChange(string _, object? value)
    {
        if (_equalizerPreset is not null && value is not null)
            _session.QueueFilter(AudioFilter.Equalizer, BuildEqualizer);
    }

    // Called by the native worker while a replacement is silent. Its old rate must not
    // determine which bands are installed for the next decoder.
    internal void ClearEqualizerForLoad()
    {
        _equalizerLoading = true;
        if (HasFilter(AudioFilters.EqualizerLabel))
            _session.RemoveFilter($"@{AudioFilters.EqualizerLabel}");
        _equalizerGraph = string.Empty;
    }

    internal bool BuildEqualizer()
    {
        if (_equalizerLoading) return true;
        var preset = _equalizerPreset;
        var rate = MpvSession.TryConvertDouble(_session.ReadLiveObject("audio-params/samplerate"), out var value) ? value : 0;
        var graph = preset is null ? string.Empty : AudioFilters.EqualizerGraph(Bands.Slots(preset), rate);
        var installed = HasFilter(AudioFilters.EqualizerLabel);
        if (_equalizerGraph == graph && installed == (graph.Length > 0))
            return true;
        if (graph.Length == 0)
        {
            if (installed) _session.RemoveFilter($"@{AudioFilters.EqualizerLabel}");
            _equalizerGraph = string.Empty;
            return true;
        }
        // Replace the labelled filter in place; removing it first would rebuild the rest
        // of the chain twice. The native call runs outside the UI and playback locks.
        if (!_session.TryDoNow(mpv => mpv.Command("af", "pre", $"@{AudioFilters.EqualizerLabel}:lavfi=[{graph}]")))
            return false;
        _equalizerGraph = graph;
        return true;
    }

    internal bool CompleteEqualizerLoad()
    {
        _equalizerLoading = false;
        return BuildEqualizer();
    }
}
