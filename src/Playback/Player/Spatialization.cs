using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    // Runtime values are detached from preferences so session controls never persist their edits.
    private AudioSettings _sofa = new();
    private string _sofaGraph = string.Empty;

    internal bool CanToggleSofa => !string.IsNullOrWhiteSpace(_sofa.SofaFile);
    internal bool IsSofaEnabled => _sofa.SofaEnabled;
    internal double SofaRotation => _sofa.SofaRotation;
    internal double SofaElevation => _sofa.SofaElevation;

    internal bool SetSofaEnabled(bool enabled)
    {
        if (enabled && !CanToggleSofa) return false;
        var next = _sofa.Copy();
        next.SofaEnabled = enabled;
        return ConfigureSofa(next);
    }

    internal bool ChangeSofaRotation(double delta)
    {
        if (!IsSofaEnabled || !double.IsFinite(delta)) return false;
        var next = _sofa.Copy();
        // Luna reports clockwise angles: subtracting a step turns left, wrapping after a full turn.
        next.SofaRotation = Precision.Normalize(((_sofa.SofaRotation + delta) % 360 + 360) % 360) % 360;
        return ConfigureSofa(next);
    }

    internal bool ChangeSofaElevation(double delta)
    {
        if (!IsSofaEnabled || !double.IsFinite(delta)) return false;
        var next = _sofa.Copy();
        next.SofaElevation = Precision.Normalize(Math.Clamp(_sofa.SofaElevation + delta,
            -AudioSettings.MaximumSofaElevation, AudioSettings.MaximumSofaElevation));
        return ConfigureSofa(next);
    }

    internal bool ConfigureSofa(AudioSettings settings)
    {
        if (settings.SofaEnabled && !AudioFilters.IsSofaFile(settings.SofaFile)) return false;
        var next = settings.Copy();
        next.SofaElevation = Math.Clamp(next.SofaElevation,
            -AudioSettings.MaximumSofaElevation, AudioSettings.MaximumSofaElevation);
        var graph = AudioFilters.SofaGraph(next);
        if (!_engine.SetSofa(graph))
        {
            _engine.SetSofa(_sofaGraph);
            return false;
        }
        _sofaGraph = graph;
        _sofa = next;
        StateChanged?.Invoke();
        return true;
    }
}
