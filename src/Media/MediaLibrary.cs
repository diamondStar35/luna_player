namespace LunaPlayer.Media;

internal static class MediaLibrary
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".aac", ".aiff", ".alac", ".flac", ".m4a", ".mp3",
        ".ogg", ".opus", ".wav", ".wma", ".3gp", ".avi", ".flv",
        ".m2ts", ".m4v", ".mkv", ".mov", ".mpeg", ".mp4", ".mpg",
        ".ts", ".webm", ".wmv",
    };

    internal static IReadOnlyList<string> SupportedExtensions { get; } = Extensions.Order(StringComparer.OrdinalIgnoreCase).ToArray();

    internal static string DialogWildcard
    {
        get
        {
            var pattern = string.Join(';', SupportedExtensions.Select(extension => $"*{extension}"));
            return $"Media Files ({pattern})|{pattern}|All Files (*.*)|*.*";
        }
    }

    /// <summary>How a path is shown when nothing better is known. A local path shows its file name; a URL
    /// shows host and last segment, since its file name is often meaningless. Mirrors the Python player's
    /// show_name.</summary>
    internal static string DisplayName(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        if (TryGetHttpUrl(path, out var uri))
        {
            var tail = uri.AbsolutePath.Trim('/').Split('/')[^1];
            return tail.Length > 0 ? $"{uri.Authority}/{tail}" : uri.Authority;
        }
        var name = Path.GetFileName(path);
        return name.Length > 0 ? name : path;
    }

    /// <summary>Whether a path is a network stream this player will open: an absolute http or https URL with
    /// an authority. Anything else is treated as a local path.</summary>
    internal static bool IsHttpUrl(string path) => TryGetHttpUrl(path, out _);

    private static bool TryGetHttpUrl(string path, out Uri uri)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps)
            && parsed.Authority.Length > 0)
        {
            uri = parsed;
            return true;
        }
        uri = null!;
        return false;
    }

    internal static IReadOnlyList<string> CollectFiles(string folderPath, bool recursive = false)
    {
        try
        {
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.EnumerateFiles(folderPath, "*", option)
                .Where(path => Extensions.Contains(Path.GetExtension(path)))
                .ToList();
            files.Sort(recursive ? StringComparer.OrdinalIgnoreCase : FileNameComparer.Instance);
            return files;
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private sealed class FileNameComparer : IComparer<string>
    {
        internal static FileNameComparer Instance { get; } = new();

        public int Compare(string? left, string? right)
            => StringComparer.OrdinalIgnoreCase.Compare(Path.GetFileName(left), Path.GetFileName(right));
    }
}
