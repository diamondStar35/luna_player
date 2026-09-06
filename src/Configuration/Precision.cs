namespace LunaPlayer.Configuration;

/// <summary>Normalizes user state at the boundary where it is retained or passed to the playback engine.
/// Intermediate media, timing, and audio calculations deliberately keep their full precision.</summary>
internal static class Precision
{
    internal const int DecimalPlaces = 3;

    internal static double Normalize(double value)
    {
        if (!double.IsFinite(value))
            return value;
        var normalized = Math.Round(value, DecimalPlaces, MidpointRounding.ToEven);
        return normalized == 0 ? 0 : normalized;
    }

    internal static double? Normalize(double? value)
        => value.HasValue ? Normalize(value.Value) : null;
}
