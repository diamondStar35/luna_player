using System.Diagnostics;
using System.Text;

namespace LunaPlayer.YouTube;

/// <summary>The two programs the optional yt-dlp path needs, and where they live.</summary>
///
/// <remarks>
/// Beside the player rather than in its settings folder, which is where the Python player keeps them and
/// where an installed copy can find them without a search. Neither is shipped: both are fetched on request
/// and the player works without either, so everything here answers "is it there" before it answers
/// anything else.
///
/// Deno is here because yt-dlp needs a JavaScript engine to work out how YouTube has signed a stream this
/// week. Without one it still runs, but the addresses it produces are throttled to a trickle or rejected
/// outright, so the two are fetched and checked as a pair.
/// </remarks>
internal static class Tools
{
    /// <summary>The folder the programs are kept in: the one the player itself runs from.</summary>
    internal static string Directory { get; } = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

    internal static string YtDlpPath { get; } = Path.Combine(Directory, "yt-dlp.exe");

    internal static string DenoPath { get; } = Path.Combine(Directory, "deno.exe");

    internal static bool HasYtDlp => File.Exists(YtDlpPath);

    internal static bool HasDeno => File.Exists(DenoPath);

    /// <summary>Whether both are present. Either one on its own is not enough to resolve reliably.
    /// </summary>
    internal static bool HasAll => HasYtDlp && HasDeno;

    /// <summary>The names of the ones that are not there, for a message that says what is being fetched.
    /// </summary>
    internal static IReadOnlyList<string> Missing
    {
        get
        {
            var missing = new List<string>(2);
            if (!HasYtDlp) missing.Add("yt-dlp");
            if (!HasDeno) missing.Add("Deno");
            return missing;
        }
    }

    /// <summary>What to pass yt-dlp so it uses the Deno beside it, or null when there is none.</summary>
    /// <remarks>
    /// The path is spelled with forward slashes, as the Python player spells it: yt-dlp splits this
    /// argument on the colon after <c>deno</c>, and a Windows drive letter carries a colon of its own.
    /// </remarks>
    internal static string? DenoRuntime
        => HasDeno ? $"deno:{Path.GetFullPath(DenoPath).Replace('\\', '/')}" : null;

    /// <summary>Starts one of the programs with the player's folder on its PATH.</summary>
    ///
    /// <remarks>
    /// The folder is prepended rather than the environment left alone, because yt-dlp looks for its helper
    /// programs on PATH and would otherwise find whatever else on the machine is called ffmpeg. The Python
    /// player does the same, by changing its own process's PATH; this changes only the child's, which
    /// leaves the player itself alone.
    /// </remarks>
    internal static Process Start(string executable, IEnumerable<string> arguments)
    {
        var info = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory,
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        info.Environment["PATH"] = path.Length > 0 ? $"{Directory}{Path.PathSeparator}{path}" : Directory;
        return Process.Start(info) ?? throw new InvalidOperationException($"Could not start {executable}.");
    }
}
