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
            // Translators: The two names in the Open dialog's file-type list. The patterns beside them are
            // literal and must not be translated.
            return $"{Tr("Media Files")} ({pattern})|{pattern}|{Tr("All Files")} (*.*)|*.*";
        }
    }

    /// <summary>How a path is shown when nothing better is known. A local path shows its file name; a URL
    /// shows host and last segment, since its file name is often meaningless. Mirrors the Python player's
    /// show_name.</summary>
    internal static string DisplayName(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        if (LinkValidator.TryGetHttpUrl(path, out var uri))
        {
            var tail = uri.AbsolutePath.Trim('/').Split('/')[^1];
            return tail.Length > 0 ? $"{uri.Authority}/{tail}" : uri.Authority;
        }
        var name = Path.GetFileName(path);
        return name.Length > 0 ? name : path;
    }

    internal static IReadOnlyList<string> CollectFiles(string folderPath, bool recursive = false)
        => CollectFiles(folderPath, recursive, report: null, CancellationToken.None);

    /// <summary>The media files in a folder, reporting how many it has found as it goes.</summary>
    ///
    /// <remarks>
    /// The tree is walked once. Knowing how far through it is would mean walking it twice - nothing can say
    /// how many files there are without looking at them all - and on a large tree that doubles the wait for
    /// a bar that only becomes meaningful halfway through. The count of what has been found is reported
    /// instead, which is the useful part of the answer and costs nothing.
    /// </remarks>
    /// <param name="report">Told how many have been found so far, or null when nobody is watching.</param>
    internal static IReadOnlyList<string> CollectFiles(
        string folderPath, bool recursive, Action<ProgressUpdate>? report, CancellationToken cancellationToken)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            // A folder the walk is not allowed to read is stepped over rather than ending it. Without this
            // the whole scan throws at the first one - on a drive root that is Config.Msi or System Volume
            // Information - and the caller is told the folder holds nothing at all.
            IgnoreInaccessible = true,
            // Past the operating system's own bookkeeping. The recycle bin is readable and full of files
            // that were deliberately thrown away; they are not somebody's playlist.
            AttributesToSkip = FileAttributes.System,
        };
        try
        {
            var files = new List<string>();
            var processed = 0;
            foreach (var path in Directory.EnumerateFiles(folderPath, "*", options))
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;
                if (Extensions.Contains(Path.GetExtension(path)))
                    files.Add(path);
                // Reported in batches: a report per file would spend the walk queueing messages nobody has
                // time to read.
                if (report is not null && processed % ReportBatch == 0)
                    report(new ProgressUpdate(processed, 0, string.Empty, files.Count));
            }
            // Sorted by whole path when the tree was walked, so a folder's files stay together; by name
            // alone otherwise, which is what a single folder's listing should read like.
            files.Sort(recursive ? StringComparer.OrdinalIgnoreCase : FileNameComparer.Instance);
            report?.Invoke(new ProgressUpdate(processed, 0, string.Empty, files.Count));
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

    /// <summary>How many files are walked between reports.</summary>
    private const int ReportBatch = 200;

    private sealed class FileNameComparer : IComparer<string>
    {
        internal static FileNameComparer Instance { get; } = new();

        public int Compare(string? left, string? right)
            => StringComparer.OrdinalIgnoreCase.Compare(Path.GetFileName(left), Path.GetFileName(right));
    }
}
