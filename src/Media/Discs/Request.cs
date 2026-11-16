using LunaPlayer.Configuration;

namespace LunaPlayer.Media.Discs;

internal sealed record Request(DiscFormat? Format, string Location)
{
    internal const string Argument = "--dvd";
    internal const string VideoArgument = "--dvd-video";
    internal const string AudioArgument = "--dvd-audio";

    /// <summary>Recognizes explicit disc launches before ordinary folder scanning can consume them.
    /// A malformed disc request retains its format and an empty location for the normal disc error flow.</summary>
    internal static Request? FromArguments(IReadOnlyList<string> arguments)
    {
        for (var index = 0; index < arguments.Count; index++)
        {
            var value = arguments[index];
            var audio = value.Equals(AudioArgument, StringComparison.OrdinalIgnoreCase);
            var automatic = value.Equals(Argument, StringComparison.OrdinalIgnoreCase);
            if (!automatic && !audio && !value.Equals(VideoArgument, StringComparison.OrdinalIgnoreCase)) continue;
            DiscFormat? format = automatic ? null : audio ? DiscFormat.Audio : DiscFormat.Video;
            if (index != 0 || arguments.Count != 2) return new(format, string.Empty);
            var location = arguments[1].Trim().Trim('"');
            if (location.Length == 0 || location.StartsWith("--", StringComparison.Ordinal))
                return new(format, string.Empty);
            // A bare drive letter must mean its root, not the process's remembered directory on that drive.
            if (location.Length == 2 && char.IsAsciiLetter(location[0]) && location[1] == ':') location += "\\";
            return new(format, Paths.TryAbsolute(location, out var absolute) ? absolute : string.Empty);
        }
        return null;
    }

    // A final dot keeps quoted drive roots from ending in a backslash that escapes the closing quote.
    internal static string Command(string executable, string argument)
        => $"\"{executable}\" {argument} \"%1\\.\"";
}
