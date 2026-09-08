using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LunaPlayer.Configuration;

namespace LunaPlayer.Update;

internal readonly record struct AppUpdateInfo(string Version, string Changes);

/// <summary>Reads release metadata, downloads the matching package, and starts the updater.</summary>
internal sealed class AppUpdateService : IDisposable
{
    private const string InfoUrl =
        "https://raw.githubusercontent.com/diamondStar35/luna_player/main/info.json";
    private const string InstallerMarker = ".luna_installed";
    private const string UpdaterExecutable = "Updater.exe";

    private readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };

    internal AppUpdateService()
        => _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(AppInfo.Identifier, AppInfo.Version));

    internal AppUpdateInfo Fetch(CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            using var response = _http.Send(
                new HttpRequestMessage(HttpMethod.Get, InfoUrl), timeout.Token);
            response.EnsureSuccessStatusCode();
            using var stream = response.Content.ReadAsStream(timeout.Token);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out var versionValue)
                || versionValue.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("info.json does not contain a version.");

            var version = versionValue.GetString()?.Trim() ?? string.Empty;
            if (version.Length == 0)
                throw new InvalidDataException("info.json does not contain a version.");
            _ = VersionParts(version);

            var changes = root.TryGetProperty("changes", out var changesValue)
                ? Changes(changesValue)
                : string.Empty;
            return new AppUpdateInfo(version, changes);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException("The update server did not respond in time.");
        }
    }

    internal static bool IsNewer(string available, string current)
    {
        var left = VersionParts(available);
        var right = VersionParts(current);
        var count = Math.Max(left.Count, right.Count);
        for (var index = 0; index < count; index++)
        {
            var leftPart = index < left.Count ? left[index] : 0;
            var rightPart = index < right.Count ? right[index] : 0;
            if (leftPart != rightPart)
                return leftPart > rightPart;
        }
        return false;
    }

    /// <summary>Whether the release already contains the package this copy of the player needs.</summary>
    internal bool PackageExists(AppUpdateInfo update, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            // Reading only the headers verifies the actual asset without downloading its contents.
            using var response = _http.Send(
                new HttpRequestMessage(HttpMethod.Get, PackageAddress(update)),
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException("The update server did not respond in time.");
        }
    }

    /// <summary>Downloads the installer for an installed copy, or the ZIP for a portable copy.</summary>
    internal string Download(
        AppUpdateInfo update,
        Action<long, long> report,
        CancellationToken token)
    {
        var extension = PackageExtension();
        var address = PackageAddress(update);

        var directory = Path.Combine(Path.GetTempPath(), AppInfo.Identifier, "Updates");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(
            directory,
            Paths.SafeFileName($"Luna_Player-v-{update.Version}", "Luna_Player_Update") + extension);
        var scratch = Paths.TemporaryFor(destination);
        try
        {
            using (var response = _http.Send(
                new HttpRequestMessage(HttpMethod.Get, address),
                HttpCompletionOption.ResponseHeadersRead,
                token))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? 0;
                report(0, total);
                using var source = response.Content.ReadAsStream(token);
                using var target = File.Create(scratch);
                var buffer = new byte[64 * 1024];
                long downloaded = 0;
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    token.ThrowIfCancellationRequested();
                    target.Write(buffer, 0, read);
                    downloaded += read;
                    report(downloaded, total);
                }
                if (total > 0 && downloaded != total)
                    throw new EndOfStreamException("The update package was not downloaded completely.");
            }
            File.Move(scratch, destination, overwrite: true);
            return destination;
        }
        catch
        {
            Delete(scratch);
            throw;
        }
    }

    internal void LaunchUpdater(string packagePath)
    {
        var applicationDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var updaterPath = Path.Combine(applicationDirectory, UpdaterExecutable);
        if (!File.Exists(updaterPath))
            throw new FileNotFoundException("Updater.exe was not found.", updaterPath);

        var start = new ProcessStartInfo(updaterPath)
        {
            UseShellExecute = false,
            WorkingDirectory = applicationDirectory,
        };
        start.ArgumentList.Add(packagePath);
        start.ArgumentList.Add(applicationDirectory);
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("The updater could not be started.");
    }

    public void Dispose() => _http.Dispose();

    private static string PackageAddress(AppUpdateInfo update)
    {
        var assetName = $"Luna_Player-v-{update.Version}{PackageExtension()}";
        var tag = $"v{update.Version}";
        return $"{AppInfo.RepositoryUrl}/releases/download/" +
            $"{Uri.EscapeDataString(tag)}/{Uri.EscapeDataString(assetName)}";
    }

    private static string PackageExtension()
        => File.Exists(Path.Combine(AppContext.BaseDirectory, InstallerMarker)) ? ".exe" : ".zip";

    private static string Changes(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            return string.Join(Environment.NewLine, value.EnumerateArray().Select(item =>
                item.ValueKind == JsonValueKind.String
                    ? item.GetString() ?? string.Empty
                    : item.ToString()));
        }
        return value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? string.Empty
            : value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : value.ToString();
    }

    private static List<int> VersionParts(string value)
    {
        var parts = new List<int>();
        for (var index = 0; index < value.Length;)
        {
            if (!char.IsAsciiDigit(value[index]))
            {
                index++;
                continue;
            }
            var start = index;
            while (index < value.Length && char.IsAsciiDigit(value[index]))
                index++;
            if (!int.TryParse(value.AsSpan(start, index - start), out var part))
                throw new FormatException("The update version is invalid.");
            parts.Add(part);
        }
        if (parts.Count == 0)
            throw new FormatException("The update version is invalid.");
        return parts;
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
        }
    }
}
