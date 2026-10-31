using System.Globalization;
using System.Text;

namespace LunaPlayer.Playback;

internal sealed partial class AudioProcessor
{
    private readonly Lock _sofaGate = new();
    private long _sofaConfiguration;
    private long _sofaRejectedConfiguration = -1;
    private SofaRequest _sofaRequested = new(string.Empty);
    private SofaRequest _sofaApplied = new(string.Empty);

    public bool SetSofa(string graph)
    {
        lock (_sofaGate)
        {
            var request = _sofaRequested = new(graph, ++_sofaConfiguration);
            return _session.QueueFilter(AudioFilter.Spatialization, () => ApplySofa(request));
        }
    }

    public bool SetSofaDirection(double rotation, double elevation)
    {
        if (!double.IsFinite(rotation) || !double.IsFinite(elevation)) return false;
        lock (_sofaGate)
        {
            if (_sofaRequested.Graph.Length == 0) return false;
            var request = _sofaRequested = _sofaRequested with { Rotation = rotation, Elevation = elevation };
            return _session.QueueFilter(AudioFilter.Spatialization, () => ApplySofa(request));
        }
    }

    internal void ReplaySofaDirection()
    {
        lock (_sofaGate)
        {
            if (_sofaRequested.Graph.Length == 0 || _sofaRequested.Rotation is null || _sofaRequested.Elevation is null) return;
            var request = _sofaRequested;
            _session.QueueFilter(AudioFilter.Spatialization, () => ApplySofa(request, replay: true), replay: true);
        }
    }

    private bool ApplySofa(SofaRequest request, bool replay = false)
    {
        // A lifecycle replay can replace pending work in the queue, so it must also install
        // an unapplied request. Only a configuration already rejected by mpv is skipped.
        if (replay && request.Configuration == _sofaRejectedConfiguration) return true;
        if (_sofaApplied.Graph != request.Graph || HasFilter(AudioFilters.SofaLabel) != (request.Graph.Length > 0))
        {
            // mpv replaces an existing label in place and retains the old chain on failure.
            // Removing it first would discard that recovery and initialize the dataset twice.
            if (request.Graph.Length == 0)
                _session.RemoveFilter($"@{AudioFilters.SofaLabel}");
            else if (!PrependSofa(request.Graph))
            {
                _sofaRejectedConfiguration = request.Configuration;
                return false;
            }
            _sofaApplied = new(request.Graph, request.Configuration);
        }
        if (request.Graph.Length == 0 || request.Rotation is null || request.Elevation is null)
            return true;
        // Hold the desired direction during decoder transitions. A ready callback retries it;
        // accepting the request does not imply that a live lavfi instance already exists.
        if (_session.ReadLiveObject("seeking") is true ||
            _session.ReadLiveObject("audio-params/samplerate") is null) return true;
        if (_sofaApplied == request && (!replay ||
            _session.ReadBoolean("audio-filter-history-reset") == true)) return true;
        var rotation = request.Rotation.Value.ToString("0.###", CultureInfo.InvariantCulture);
        var elevation = request.Elevation.Value.ToString("0.###", CultureInfo.InvariantCulture);
        if (!_session.TryDoNow(mpv =>
        {
            mpv.Command("af-command", AudioFilters.SofaLabel, "rotation", rotation, "sofalizer");
            mpv.Command("af-command", AudioFilters.SofaLabel, "elevation", elevation, "sofalizer");
        }))
        {
            // A discontinuity can start between the readiness check and the command.
            // Keep the pending request instead of disabling a valid SOFA configuration.
            return _session.ReadLiveObject("seeking") is true ||
                _session.ReadLiveObject("audio-params/samplerate") is null;
        }
        _sofaApplied = request;
        return true;
    }

    private bool PrependSofa(string graph)
    {
        var length = Encoding.UTF8.GetByteCount(graph);
        return _session.TryDoNow(mpv => mpv.Command("af", "pre", $"@{AudioFilters.SofaLabel}:lavfi=graph=%{length}%{graph}"));
    }

    private sealed record SofaRequest(string Graph, long Configuration = 0, double? Rotation = null, double? Elevation = null);
}
