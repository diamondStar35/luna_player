using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.YouTube;

/// <summary>Fetches yt-dlp and Deno from the projects that publish them.</summary>
///
/// <remarks>
/// A port of the Python player's <c>youtube/components.py</c>. Both are taken from their own GitHub
/// releases, straight from the "latest" address rather than from a version this player decides on, so a
/// build of the player from a year ago still fetches something that works with YouTube today.
///
/// Everything is written to a scratch file beside its destination and moved into place at the end. A
/// download interrupted half way through then leaves nothing behind rather than an executable that is
/// present, is the wrong size, and fails in a way nobody can read.
/// </remarks>
internal sealed class ComponentInstaller
{
    /// <summary>Named after the player, as courtesy to the projects being asked and because GitHub's
    /// interface refuses a request that does not name itself.</summary>
    private static readonly ProductInfoHeaderValue Agent = new(AppInfo.Identifier, "1.0");

    /// <summary>Where Deno's Windows build is published. One file, one architecture: the player is built
    /// for x64 and would not run anywhere this did not.</summary>
    private const string DenoUrl =
        "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(10) };

    internal ComponentInstaller()
        => _http.DefaultRequestHeaders.UserAgent.Add(Agent);

    /// <summary>How many of the two are missing, so the window can say "one of two" before it starts.
    /// </summary>
    internal static int MissingCount => Tools.Missing.Count;

    /// <summary>Fetches whichever of the two are not already there.</summary>
    ///
    /// <remarks>
    /// One that is present is left alone rather than replaced, which is what makes this safe to call from
    /// the point of use: a user who has yt-dlp and no Deno waits for Deno alone.
    /// </remarks>
    /// <param name="report">The component, its version where one is known, which of the two it is, the
    /// bytes so far and the bytes expected.</param>
    internal void Install(
        YtDlpChannel channel,
        Action<string, int, long, long> report,
        CancellationToken token)
    {
        var wanted = new List<(string Name, string Url, bool Archive, string Destination)>(2);
        if (!Tools.HasYtDlp)
        {
            var repository = YtDlpClient.ChannelRepository(channel);
            wanted.Add((
                $"yt-dlp{Version(LatestTag(repository, token))}",
                $"https://github.com/{repository}/releases/latest/download/yt-dlp.exe",
                false,
                Tools.YtDlpPath));
        }
        if (!Tools.HasDeno)
        {
            wanted.Add((
                $"Deno{Version(LatestTag("denoland/deno", token))}",
                DenoUrl,
                true,
                Path.Combine(Tools.Directory, "deno.zip")));
        }
        for (var index = 0; index < wanted.Count; index++)
        {
            var item = wanted[index];
            var step = index + 1;
            Fetch(item.Url, item.Destination,
                (got, size) => report(item.Name, step, got, size), token);
            if (!item.Archive)
                continue;
            ExtractDeno(item.Destination, token);
            File.Delete(item.Destination);
        }
    }

    /// <summary>The newest version tag a project has published, or an empty string.</summary>
    ///
    /// <remarks>
    /// Only ever used to put a version in the progress window, so a failure here is not a failure of the
    /// install: the download address is the "latest" one either way and does not need this to work.
    /// </remarks>
    internal string LatestTag(string repository, CancellationToken token)
    {
        try
        {
            using var response = _http.Send(
                new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repository}/releases/latest"),
                token);
            if (!response.IsSuccessStatusCode)
                return string.Empty;
            using var document = JsonDocument.Parse(response.Content.ReadAsStream(token));
            return document.RootElement.TryGetProperty("tag_name", out var tag)
                && tag.ValueKind == JsonValueKind.String
                    ? tag.GetString() ?? string.Empty
                    : string.Empty;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException)
        {
            return string.Empty;
        }
    }

    private void Fetch(string url, string destination, Action<long, long> report, CancellationToken token)
    {
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? Tools.Directory);
        var scratch = Paths.TemporaryFor(destination);
        try
        {
            using (var response = _http.Send(
                new HttpRequestMessage(HttpMethod.Get, url), HttpCompletionOption.ResponseHeadersRead, token))
            {
                response.EnsureSuccessStatusCode();
                var size = response.Content.Headers.ContentLength ?? 0;
                report(0, size);
                using var source = response.Content.ReadAsStream(token);
                using var target = File.Create(scratch);
                var buffer = new byte[32 * 1024];
                long got = 0;
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    token.ThrowIfCancellationRequested();
                    target.Write(buffer, 0, read);
                    got += read;
                    report(got, size);
                }
            }
            File.Move(scratch, destination, overwrite: true);
        }
        catch (Exception)
        {
            Delete(scratch);
            throw;
        }
    }

    /// <summary>Takes deno.exe out of the archive Deno publishes and leaves nothing else behind.</summary>
    private static void ExtractDeno(string archivePath, CancellationToken token)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.Entries.FirstOrDefault(candidate =>
            candidate.Name.Equals("deno.exe", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("The Deno archive does not contain deno.exe.");
        var scratch = Paths.TemporaryFor(Tools.DenoPath);
        try
        {
            using (var source = entry.Open())
            using (var target = File.Create(scratch))
            {
                var buffer = new byte[64 * 1024];
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    token.ThrowIfCancellationRequested();
                    target.Write(buffer, 0, read);
                }
            }
            File.Move(scratch, Tools.DenoPath, overwrite: true);
        }
        catch (Exception)
        {
            Delete(scratch);
            throw;
        }
    }

    private static void Delete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A scratch file that will not go is not worth failing an install over; it is named after the
            // real one and will be overwritten by the next attempt.
        }
    }

    private static string Version(string tag) => tag.Length > 0 ? $" ({tag})" : string.Empty;

    /// <summary>One report from a component download, in the four fields a progress update has.</summary>
    /// <remarks>
    /// Bytes rather than a percentage, because the window shows the sizes as well as the bar and a
    /// percentage cannot be turned back into them. Both fit an <c>int</c> with room to spare: the larger of
    /// the two downloads is some tens of megabytes.
    /// </remarks>
    internal static ProgressUpdate Step(string name, int step, long got, long size)
        => new((int)Math.Min(got, int.MaxValue), (int)Math.Min(size, int.MaxValue), name, step);
}
