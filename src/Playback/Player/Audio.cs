using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    internal double SetVolume(double volume) => _engine.SetVolume(volume);

    internal double ChangeVolume(double delta) => SetVolume(Volume + delta);

    internal double SetSpeed(double speed) => _engine.SetSpeed(speed);

    internal double ChangeSpeed(double delta) => SetSpeed(Speed + delta);

    internal double SetPitch(double semitones) => _engine.SetPitch(semitones);

    internal double ChangePitch(double delta) => SetPitch(Pitch + delta);

    internal double SetPan(double pan) => _engine.SetPan(pan);

    internal double ChangePan(double delta) => SetPan(Pan + delta);

    internal bool SetNormalization(bool enabled)
    {
        var previous = _normalizationEnabled;
        _normalizationEnabled = enabled;
        if (_engine.SetNormalization(enabled))
            return true;
        _normalizationEnabled = previous;
        _engine.SetNormalization(previous);
        return false;
    }

    /// <summary>The preset the equalizer is on, or null when it is off.</summary>
    internal Preset? CurrentEqualizer => _equalizer;

    internal bool SetEqualizer(Preset? preset)
    {
        var previous = _equalizer;
        _equalizer = preset;
        if (_engine.SetEqualizer(preset))
            return true;
        _equalizer = previous;
        _engine.SetEqualizer(previous);
        return false;
    }

    internal bool SetMono(bool enabled)
    {
        var previous = _monoEnabled;
        _monoEnabled = enabled;
        if (_engine.SetMono(enabled))
            return true;
        _monoEnabled = previous;
        _engine.SetMono(previous);
        return false;
    }

    internal bool ConfigureSilence(SilenceSettings settings)
    {
        var previous = _silence;
        _silence = settings.Copy();
        if (!_silenceEnabled || _engine.SetSilenceRemoval(true, AudioFilters.SilenceGraph(_silence)))
            return true;
        _silence = previous;
        _engine.SetSilenceRemoval(true, AudioFilters.SilenceGraph(_silence));
        return false;
    }

    // A detached runtime copy: menu commands must never modify the saved preference defaults.
    private AudioSettings _sofa = new();
    internal bool CanToggleSofa => !string.IsNullOrWhiteSpace(_sofa.SofaFile);
    internal bool IsSofaEnabled => _sofa.SofaEnabled;
    internal double SofaRotation => _sofa.SofaRotation;
    internal double SofaElevation => _sofa.SofaElevation;

    internal bool SetSofaEnabled(bool enabled)
    {
        if (enabled && !CanToggleSofa)
            return false;
        var next = _sofa.Copy();
        next.SofaEnabled = enabled;
        return ConfigureSofa(next);
    }

    internal bool ChangeSofaRotation(double delta)
    {
        if (!IsSofaEnabled || !double.IsFinite(delta))
            return false;
        var next = _sofa.Copy();
        // Wrap around the listener rather than stopping after one full turn.
        next.SofaRotation = Precision.Normalize(((_sofa.SofaRotation + delta) % 360 + 360) % 360) % 360;
        return ConfigureSofa(next);
    }

    internal bool ChangeSofaElevation(double delta)
    {
        if (!IsSofaEnabled || !double.IsFinite(delta))
            return false;
        var next = _sofa.Copy();
        next.SofaElevation = Precision.Normalize(Math.Clamp(_sofa.SofaElevation + delta, -90, 90));
        return ConfigureSofa(next);
    }

    internal bool ConfigureSofa(AudioSettings settings)
    {
        if (settings.SofaEnabled && !AudioFilters.IsSofaFile(settings.SofaFile))
            return false;
        var graph = AudioFilters.SofaGraph(settings);
        if (!_engine.SetSofa(graph))
        {
            _engine.SetSofa(_sofaGraph);
            return false;
        }
        _sofaGraph = graph;
        _sofa = settings.Copy();
        StateChanged?.Invoke();
        return true;
    }

    internal bool SetSilenceRemoval(bool enabled)
    {
        var previous = _silenceEnabled;
        _silenceEnabled = enabled;
        if (_engine.SetSilenceRemoval(enabled, AudioFilters.SilenceGraph(_silence)))
        {
            StateChanged?.Invoke();
            return true;
        }
        _silenceEnabled = previous;
        _engine.SetSilenceRemoval(previous, AudioFilters.SilenceGraph(_silence));
        return false;
    }
}
