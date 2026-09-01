namespace LunaPlayer.Media;

internal sealed record FileOperationResult(IReadOnlyList<string> Succeeded, IReadOnlyList<string> Failed, bool Cancelled);

internal sealed class MarkedFileService
{
    internal FileOperationResult Transfer(IReadOnlyList<string> files, string targetDirectory, bool move,
        Action<ProgressUpdate> report, CancellationToken cancellationToken)
    {
        var succeeded = new List<string>();
        var failed = new List<string>();
        for (var index = 0; index < files.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var source = files[index];
            try
            {
                var destination = ResolveDestination(targetDirectory, source);
                if (move) File.Move(source, destination);
                else File.Copy(source, destination);
                succeeded.Add(source);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                failed.Add(source);
            }
            report(new(index + 1, files.Count, Path.GetFileName(source)));
        }
        return new(succeeded, failed, cancellationToken.IsCancellationRequested);
    }

    internal FileOperationResult Delete(IReadOnlyList<string> files)
    {
        var succeeded = new List<string>();
        var failed = new List<string>();
        foreach (var path in files)
        {
            try
            {
                if (!File.Exists(path))
                {
                    failed.Add(path);
                    continue;
                }
                File.Delete(path);
                succeeded.Add(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            { failed.Add(path); }
        }
        return new(succeeded, failed, false);
    }

    private static string ResolveDestination(string directory, string source)
    {
        var name = Path.GetFileName(source);
        var candidate = Path.Combine(directory, name);
        if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        var stem = Path.GetFileNameWithoutExtension(name);
        var extension = Path.GetExtension(name);
        for (var suffix = 1; ; suffix++)
        {
            candidate = Path.Combine(directory, $"{stem} ({suffix}){extension}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        }
    }
}
