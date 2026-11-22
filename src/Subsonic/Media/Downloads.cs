namespace LunaPlayer.Subsonic.Media;

internal sealed record DownloadProgress(long BytesReceived, long? TotalBytes);
internal sealed record DownloadItem(string SongId, string Destination);
internal sealed record DownloadResult(DownloadItem Item, long Bytes, Exception? Error);

internal sealed class Downloads(Media media)
{
    internal async Task<long> CopyAsync(string id, Stream destination, IProgress<DownloadProgress>? progress = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException(nameof(destination));
        await using var response = await media.OpenDownloadAsync(id, token).ConfigureAwait(false);
        var buffer = new byte[81920];
        long received = 0;
        progress?.Report(new(0, response.Length));
        while (true)
        {
            var count = await response.Stream.ReadAsync(buffer, token).ConfigureAwait(false);
            if (count == 0) break;
            await destination.WriteAsync(buffer.AsMemory(0, count), token).ConfigureAwait(false);
            received += count;
            progress?.Report(new(received, response.Length));
        }
        if (response.Length is { } expected && received != expected) throw new IOException("Incomplete media response.");
        return received;
    }
    internal async Task<long> SaveAsync(string id, string destination, bool overwrite = false,
        IProgress<DownloadProgress>? progress = null, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var target = Path.GetFullPath(destination);
        if (!overwrite && File.Exists(target)) throw new IOException("Destination exists.");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            long count;
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous))
            {
                count = await CopyAsync(id, output, progress, token).ConfigureAwait(false);
                await output.FlushAsync(token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            File.Move(temporary, target, overwrite);
            return count;
        }
        catch (Exception error)
        {
            try { File.Delete(temporary); }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            { throw new AggregateException(error, cleanup); }
            throw;
        }
    }
    internal async Task<DownloadResult[]> SaveBatchAsync(IReadOnlyList<DownloadItem> items, int concurrency = 2,
        IProgress<(DownloadItem Item, DownloadProgress Progress)>? progress = null, CancellationToken token = default)
    {
        if (concurrency < 1) throw new ArgumentOutOfRangeException(nameof(concurrency));
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
            if (!destinations.Add(Path.GetFullPath(item.Destination))) throw new ArgumentException(nameof(items));
        var results = new DownloadResult[items.Count];
        await Parallel.ForEachAsync(Enumerable.Range(0, items.Count), new ParallelOptions
        { MaxDegreeOfParallelism = concurrency, CancellationToken = token }, async (index, cancellation) =>
        {
            var item = items[index];
            try
            {
                var bytes = await SaveAsync(item.SongId, item.Destination,
                    progress: new CallbackProgress(value => progress?.Report((item, value))), token: cancellation).ConfigureAwait(false);
                results[index] = new(item, bytes, null);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or RequestException)
            { results[index] = new(item, 0, error); }
        }).ConfigureAwait(false);
        return results;
    }
    private sealed class CallbackProgress(Action<DownloadProgress> report) : IProgress<DownloadProgress>
    { public void Report(DownloadProgress value) => report(value); }
}
