using LunaPlayer.Media;

namespace LunaPlayer.Configuration;

/// <summary>Every path the player builds, and the one place two paths are decided to be the same path.
/// </summary>
///
/// <remarks>
/// Two jobs live here. The first is where the player keeps its own files, under the roaming application data
/// folder so they follow a user between machines.
///
/// The second is <see cref="Key"/>. The marked file set, the playlist, the saved positions and the bookmarks
/// all have to agree on when two paths name the same file, and each of them having its own answer is how they
/// stop agreeing: one would resolve a relative path and another would not, one would fold case and another
/// would compare a URL as though it were a file name. They now ask this.
/// </remarks>
internal static class Paths
{
    /// <summary>The folder holding the settings, bookmarks and saved positions.</summary>
    internal static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Luna Player");

    /// <summary>The settings file.</summary>
    internal static string SettingsFile { get; } = Path.Combine(RootDirectory, "settings.json");

    /// <summary>The settings file written by the Python player, read once to carry old settings forward.
    /// </summary>
    internal static string LegacySettingsFile { get; } = Path.Combine(RootDirectory, "settings.ini");

    /// <summary>The bookmarks file.</summary>
    internal static string BookmarksFile { get; } = Path.Combine(RootDirectory, "bookmarks.json");

    /// <summary>The file holding how far through each file playing had reached.</summary>
    internal static string PositionsFile { get; } = Path.Combine(RootDirectory, "positions.json");

    /// <summary>What a path is compared and stored under, so that two spellings of one file - a relative
    /// path and an absolute one, or two different cases - are recognised as the same file.</summary>
    ///
    /// <remarks>
    /// The result is for comparison, not for showing anyone: it is lower-cased, which is also what keeps the
    /// keys already written into the bookmarks and positions files matching. Use <see cref="Absolute"/> for a
    /// path to store beside one of these keys or to put in front of a user.
    ///
    /// An empty string means there was nothing to key, and callers that must not store a bogus entry test for
    /// it. A path Windows will not accept keys as itself rather than as nothing, so the file stays consistent
    /// with itself even when it cannot be resolved.
    /// </remarks>
    internal static string Key(string? path)
    {
        var text = (path ?? string.Empty).Trim();
        if (text.Length == 0)
            return string.Empty;
        // A URL is its own key. GetFullPath would resolve it against the working directory and hand back a
        // local path that names nothing - and one that changes if the working directory ever does, which
        // would quietly lose track of a stream that had already been marked.
        return LinkValidator.IsHttpUrl(text)
            ? text.ToLowerInvariant()
            : Absolute(text).ToLowerInvariant();
    }

    /// <summary>Whether two paths name the same thing, by <see cref="Key"/>. Two empty paths are not the same
    /// thing: neither of them names anything.</summary>
    internal static bool AreSame(string? left, string? right)
    {
        var key = Key(left);
        return key.Length > 0 && string.Equals(key, Key(right), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A path resolved against the working directory, or the path unchanged when Windows will not
    /// accept it as one. Never throws, because every caller is holding something a user typed, pasted or
    /// dropped on the window, and a malformed one is an answer rather than a crash.</summary>
    internal static string Absolute(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    /// <summary>A path resolved against the working directory, failing rather than falling back. For a caller
    /// that is filtering a list and would rather leave out what it cannot resolve.</summary>
    internal static bool TryAbsolute(string path, out string absolute)
    {
        try
        {
            absolute = Path.GetFullPath(path);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            absolute = path;
            return false;
        }
    }

    /// <summary>The scratch path a file is written to before it replaces the real one, so a write that fails
    /// half way leaves the previous file where it was.</summary>
    internal static string TemporaryFor(string path) => path + ".tmp";

    /// <summary>Creates the folder <paramref name="path"/> is about to be written into. Does nothing for a
    /// bare file name, which is already in a folder that exists.</summary>
    internal static void EnsureDirectoryFor(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }
}
