using System.Globalization;

namespace LunaPlayer.Media;

/// <summary>A chapter's times always refer to the original media, even in audiobook mode.</summary>
internal sealed record Chapter(int Index, string? Title, double Start, double End)
{
    internal double Duration => End - Start;
}

internal static class Chapters
{
    /// <summary>mpv supplies starts rather than lengths. Reject unusable markers before finding ends,
    /// so a malformed marker cannot give its preceding chapter a negative or non-finite duration.</summary>
    internal static IReadOnlyList<Chapter> Read(object? value, double? duration)
    {
        if (value is not IEnumerable<object?> values || duration is not > 0 || !double.IsFinite(duration.Value))
            return [];
        var markers = new List<(string? Title, double Start)>();
        foreach (var item in values)
        {
            if (item is not IDictionary<string, object?> fields || !fields.TryGetValue("time", out var raw) || raw is null)
                continue;
            double start;
            try { start = Convert.ToDouble(raw, CultureInfo.InvariantCulture); }
            catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
            {
                // An invalid marker does not make the remaining chapters unusable.
                continue;
            }
            if (!double.IsFinite(start) || start < 0 || start >= duration
                || markers.Count > 0 && start <= markers[^1].Start)
                continue;
            fields.TryGetValue("title", out var title);
            markers.Add((title is string text && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null, start));
        }
        if (markers.Count > 0 && markers[0].Start > 0)
            markers.Insert(0, (null, 0));
        return markers.Select((marker, index) => new Chapter(index, marker.Title, marker.Start,
            index + 1 < markers.Count ? markers[index + 1].Start : duration.Value)).ToArray();
    }

    internal static Chapter? At(IReadOnlyList<Chapter> chapters, double position)
        => chapters.LastOrDefault(chapter => position >= chapter.Start);
}
