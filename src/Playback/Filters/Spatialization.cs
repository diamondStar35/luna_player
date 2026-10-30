using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Playback;

internal static partial class AudioFilters
{
    internal const string SofaLabel = "audiosofa";

    internal static bool IsSofaFile(string path)
        => string.Equals(Path.GetExtension(path), ".sofa", StringComparison.OrdinalIgnoreCase) && File.Exists(path);

    internal static string SofaGraph(AudioSettings settings)
    {
        if (!settings.SofaEnabled) return string.Empty;
        var path = Path.GetFullPath(settings.SofaFile).Replace('\\', '/');
        var option = EscapeToken(path, "\\':");
        var graphPath = EscapeToken(option, "\\'[],;");
        // SOFA azimuth increases to the left; Luna's controls report clockwise angles.
        var rotation = -settings.SofaRotation;
        var elevation = Math.Clamp(settings.SofaElevation,
            -AudioSettings.MaximumSofaElevation, AudioSettings.MaximumSofaElevation);
        return FormattableString.Invariant(
            $"sofalizer=sofa={graphPath}:gain={settings.SofaGain}:rotation={rotation}:elevation={elevation}:type=freq");
    }

    private static string EscapeToken(string value, string special)
        => string.Concat(value.Select(character => special.Contains(character) || char.IsWhiteSpace(character)
            ? $"\\{character}" : character.ToString()));
}
