using System.Text;

namespace LunaPlayer.Playback;

internal sealed partial class AudioProcessor
{
    private string _sofaFilterGraph = string.Empty;

    public bool SetSofa(string graph)
    {
        if (_sofaFilterGraph == graph && HasFilter(AudioFilters.SofaLabel) == (graph.Length > 0)) return true;
        if (graph.Length > 0 && !SofaFilterValidator.IsValid(graph)) return false;
        _session.RemoveFilter($"@{AudioFilters.SofaLabel}");
        if (graph.Length > 0 && !PrependSofa(graph))
        {
            _session.RemoveFilter($"@{AudioFilters.SofaLabel}");
            if (_sofaFilterGraph.Length > 0 && !PrependSofa(_sofaFilterGraph))
                _sofaFilterGraph = string.Empty;
            return false;
        }
        _sofaFilterGraph = graph;
        return true;
    }

    private bool PrependSofa(string graph)
    {
        var length = Encoding.UTF8.GetByteCount(graph);
        return _session.TryDo(mpv => mpv.Command("af", "pre", $"@{AudioFilters.SofaLabel}:lavfi=graph=%{length}%{graph}"));
    }
}
