using System.Text;

namespace LunaPlayer.Playback;

internal sealed partial class MpvSession
{
    internal bool AddFilter(string definition) => TryDoNow(mpv =>
    {
        var filters = (ReadLiveObject("af") as IEnumerable<object?> ?? [])
            .Cast<IDictionary<string, object?>>().ToList();
        var spatialization = filters.FindIndex(filter => Label(filter) == AudioFilters.SofaLabel);
        if (spatialization < 0 || definition.StartsWith($"@{AudioFilters.SofaLabel}:", StringComparison.Ordinal)
            || filters.Any(filter => Label(filter) is { Length: > 0 } label
                && definition.StartsWith($"@{label}:", StringComparison.Ordinal)))
        {
            mpv.Command("af", "add", definition);
            return;
        }
        // Spatialize after buffered processing, so its queued audio does not retain old directions.
        // Preserve the existing definitions: mpv can reuse their configured filter instances.
        var definitions = filters.Select(Describe).ToList();
        definitions.Insert(spatialization, definition);
        mpv.Command("af", "set", string.Join(',', definitions));
    });

    private static string? Label(IDictionary<string, object?> filter)
        => filter.TryGetValue("label", out var label) ? label as string : null;

    private static string Describe(IDictionary<string, object?> filter)
    {
        var label = Label(filter);
        var disabled = filter.TryGetValue("enabled", out var enabled) && enabled is false;
        var definition = (string.IsNullOrEmpty(label) ? string.Empty : $"@{label}:")
            + (disabled ? "!" : string.Empty) + (string)filter["name"]!;
        if (filter.TryGetValue("params", out var value) && value is IDictionary<string, object?> parameters
            && parameters.Count > 0)
            definition += "=" + string.Join(':', parameters.Select(parameter =>
                $"{Quote(parameter.Key)}={Quote((string)parameter.Value!)}"));
        return definition;
    }

    // mpv's byte-length quoting preserves filter graphs and paths containing separators or Unicode.
    private static string Quote(string value) => $"%{Encoding.UTF8.GetByteCount(value)}%{value}";
}
