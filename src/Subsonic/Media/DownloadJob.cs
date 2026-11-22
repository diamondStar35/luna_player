using System.Diagnostics;
using LunaPlayer.Downloads;
using LunaPlayer.Subsonic.Servers;

namespace LunaPlayer.Subsonic.Media;

internal static class DownloadJob
{
    internal static Job Create(Profile profile, Client client, Models.Entry song, string? name = null, string? folder = null)
    {
        var title = string.IsNullOrWhiteSpace(name) ? song.Title : name;
        if (string.IsNullOrWhiteSpace(title)) title = song.Id;
        var destination = string.IsNullOrWhiteSpace(folder) ? profile.Downloads.Folder : folder;
        if (string.IsNullOrWhiteSpace(destination)) destination = Configuration.Paths.DefaultDownloadsDirectory;
        if (profile.Downloads.AlbumFolders) destination = Path.Combine(destination, Clean(song.Artist), Clean(song.Album));
        var stem = Clean(title);
        var extension = new string(song.Suffix.Where(char.IsAsciiLetterOrDigit).Take(10).ToArray());
        if (extension.Length == 0) extension = "bin";
        var directory = Path.GetFullPath(destination);
        return new(title, (report, token) =>
        {
            var clock = Stopwatch.StartNew();
            var progress = new Callback(value =>
            {
                var speed = value.BytesReceived / Math.Max(clock.Elapsed.TotalSeconds, 0.01);
                report(new("downloading", value.BytesReceived, value.TotalBytes ?? 0,
                    value.TotalBytes is > 0 ? value.BytesReceived * 100d / value.TotalBytes.Value : 0,
                    speed, value.TotalBytes is > 0 && speed > 0 ? (value.TotalBytes.Value - value.BytesReceived) / speed : 0));
            });
            // Reserve names rather than overwriting an existing download or racing another job.
            Directory.CreateDirectory(directory);
            for (var attempt = 0; ; attempt++)
            {
                token.ThrowIfCancellationRequested();
                var path = Path.Combine(directory, stem + (attempt == 0 ? "" : " (" + (attempt + 1) + ")") + "." + extension);
                FileStream reservation;
                try { reservation = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None); }
                catch (IOException) when (File.Exists(path)) { continue; }
                reservation.Dispose();
                try
                {
                    client.Downloads.SaveAsync(song.Id, path, overwrite: true, progress, token).GetAwaiter().GetResult();
                    return Outcome.Saved(path);
                }
                catch { File.Delete(path); throw; }
            }
        });
    }
    private static string Clean(string value)
    {
        var text = new string(value.Select(x => Path.GetInvalidFileNameChars().Contains(x) ? '_' : x).ToArray()).Trim().TrimEnd('.');
        var reserved = text.Split('.')[0].ToUpperInvariant();
        if (reserved is "CON" or "PRN" or "AUX" or "NUL" || reserved.Length == 4
            && (reserved.StartsWith("COM", StringComparison.Ordinal) || reserved.StartsWith("LPT", StringComparison.Ordinal)) && reserved[3] is >= '1' and <= '9') text = "_" + text;
        return text.Length == 0 ? "_" : text.Length > 160 ? text[..160] : text;
    }
    private sealed class Callback(Action<DownloadProgress> report) : IProgress<DownloadProgress>
    { public void Report(DownloadProgress value) => report(value); }
}
