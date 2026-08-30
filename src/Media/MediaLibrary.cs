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
