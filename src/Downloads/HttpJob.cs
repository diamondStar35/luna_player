using System.Diagnostics;

namespace LunaPlayer.Downloads;

internal static class HttpJob
{
    private static readonly HttpClient Shared = new() { Timeout = Timeout.InfiniteTimeSpan };

    internal static Job Create(Uri address, string destination, HttpClient? client = null)
    {
        var fullPath = Path.GetFullPath(destination);
        return new(Path.GetFileName(fullPath), (progress, token) => Save(address, fullPath, client ?? Shared, progress, token).GetAwaiter().GetResult());
    }

    private static async Task<Outcome> Save(Uri address, string destination, HttpClient client, Action<Progress> report, CancellationToken token)
    {
        using var response = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        var directory = Path.GetDirectoryName(destination)!; Directory.CreateDirectory(directory);
        FileStream output;
        for (var number = 0; ; number++)
        {
            var candidate = number == 0 ? destination : Path.Combine(directory,
                Path.GetFileNameWithoutExtension(destination) + " (" + (number + 1) + ")" + Path.GetExtension(destination));
            try { output = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, useAsync: true); break; }
            catch (IOException) when (File.Exists(candidate)) { }
        }
        var path = output.Name; var completed = false;
        try
        {
            await using (output)
            {
                var buffer = new byte[65536]; long received = 0; var total = response.Content.Headers.ContentLength ?? 0;
                var clock = Stopwatch.StartNew();
                while (await input.ReadAsync(buffer, token).ConfigureAwait(false) is var count && count > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, count), token).ConfigureAwait(false); received += count;
                    var speed = received / Math.Max(clock.Elapsed.TotalSeconds, .01);
                    report(new("downloading", received, total, total > 0 ? received * 100d / total : 0,
                        speed, total > 0 ? (total - received) / speed : 0));
                }
            }
            completed = true; return Outcome.Saved(path);
        }
        finally { if (!completed) File.Delete(path); }
    }
}
