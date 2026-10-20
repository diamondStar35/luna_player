using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Update;

internal readonly record struct AppUpdateInfo(string Version, string Changes);

internal sealed class UpdateIntegrityException(string message) : Exception(message);

/// <summary>Reads release metadata, downloads the matching package, and starts the updater.</summary>
internal sealed class AppUpdateService : IDisposable
{
    private const string InfoUrl =
        "https://raw.githubusercontent.com/diamondStar35/luna_player/main/info.json";
    private const string InstallerMarker = ".luna_installed";
    private const string ChecksumsFile = "SHA256SUMS.txt";
    private const string UpdaterExecutable = "Updater.exe";
    private const string TemporaryUpdaterExecutable = "Updater.temp.exe";

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

    /// <summary>Whether the release contains both the package this copy needs and its checksums.</summary>
    internal bool PackageExists(AppUpdateInfo update, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            return RemoteFileExists(PackageAddress(update), timeout.Token)
                && RemoteFileExists(ChecksumsAddress(update), timeout.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException("The update server did not respond in time.");
        }
    }

    private bool RemoteFileExists(string address, CancellationToken token)
    {
        // Reading only the headers verifies the actual release asset without downloading its contents.
        using var response = _http.Send(
            new HttpRequestMessage(HttpMethod.Get, address),
            HttpCompletionOption.ResponseHeadersRead,
            token);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <summary>Downloads the installer for an installed copy, or the ZIP for a portable copy.</summary>
    internal string Download(
        AppUpdateInfo update,
        Action<long, long, bool> report,
        CancellationToken token)
    {
        var extension = PackageExtension();
        var address = PackageAddress(update);
        var packageName = PackageName(update);

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
                report(0, total, false);
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
                    report(downloaded, total, false);
                }
                if (total > 0 && downloaded != total)
                    throw new EndOfStreamException("The update package was not downloaded completely.");
            }
            report(1, 1, true);
            VerifyChecksum(scratch, packageName, update, token);
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
        var temporaryUpdaterPath = Path.Combine(applicationDirectory, TemporaryUpdaterExecutable);
        if (!File.Exists(updaterPath) && File.Exists(temporaryUpdaterPath))
            File.Move(temporaryUpdaterPath, updaterPath);
        if (!File.Exists(updaterPath))
            throw new FileNotFoundException("Updater.exe was not found.", updaterPath);

        // Inno Setup owns installed updates, including restarting Luna Player. The updater only waits for
        // this process to exit before starting it, so it does not need the portable archive handoff.
        if (Path.GetExtension(packagePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            StartUpdater(updaterPath, packagePath, applicationDirectory);
            return;
        }

        // The worker must not keep Updater.exe open: the downloaded package contains its replacement.
        // A stale temporary copy can only be left by an interrupted update, when no worker is running.
        if (File.Exists(temporaryUpdaterPath))
            File.Delete(temporaryUpdaterPath);
        File.Move(updaterPath, temporaryUpdaterPath);

        try
        {
            StartUpdater(temporaryUpdaterPath, packagePath, applicationDirectory);
        }
        catch
        {
            // Starting the worker failed, so restore the updater before returning control to the player.
            if (!File.Exists(updaterPath) && File.Exists(temporaryUpdaterPath))
                File.Move(temporaryUpdaterPath, updaterPath);
            throw;
        }
    }

    private static void StartUpdater(string updaterPath, string packagePath, string applicationDirectory)
    {
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
        var assetName = PackageName(update);
        var tag = $"v{update.Version}";
        return $"{AppInfo.RepositoryUrl}/releases/download/" +
            $"{Uri.EscapeDataString(tag)}/{Uri.EscapeDataString(assetName)}";
    }

    private static string PackageName(AppUpdateInfo update)
        => $"Luna_Player-v-{update.Version}{PackageExtension()}";

    private static string ChecksumsAddress(AppUpdateInfo update)
    {
        var tag = $"v{update.Version}";
        return $"{AppInfo.RepositoryUrl}/releases/download/" +
            $"{Uri.EscapeDataString(tag)}/{ChecksumsFile}";
    }

    private void VerifyChecksum(
        string packagePath,
        string packageName,
        AppUpdateInfo update,
        CancellationToken token)
    {
        string checksums;
        try
        {
            using var response = _http.Send(
                new HttpRequestMessage(HttpMethod.Get, ChecksumsAddress(update)),
                HttpCompletionOption.ResponseContentRead,
                token);
            response.EnsureSuccessStatusCode();
            using var stream = response.Content.ReadAsStream(token);
            using var reader = new StreamReader(stream);
            checksums = reader.ReadToEnd();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception failure) when (failure is HttpRequestException or IOException)
        {
            throw new UpdateIntegrityException(
                $"The release checksums could not be downloaded: {failure.Message}");
        }

        var expected = ExpectedChecksum(checksums, packageName);
        using var package = File.OpenRead(packagePath);
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = package.Read(buffer, 0, buffer.Length)) > 0)
        {
            token.ThrowIfCancellationRequested();
            hasher.AppendData(buffer, 0, read);
        }
        token.ThrowIfCancellationRequested();
        var actual = Convert.ToHexString(hasher.GetHashAndReset());
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new UpdateIntegrityException("The downloaded package does not match its published checksum.");
    }

    private static string ExpectedChecksum(string checksums, string packageName)
    {
        foreach (var sourceLine in checksums.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = sourceLine.Trim();
            var separator = line.IndexOfAny([' ', '\t']);
            if (separator <= 0)
                continue;
            var fileName = line[(separator + 1)..].TrimStart(' ', '\t', '*');
            if (!fileName.Equals(packageName, StringComparison.Ordinal))
                continue;
            var checksum = line[..separator];
            if (checksum.Length != 64 || checksum.Any(character => !char.IsAsciiHexDigit(character)))
                break;
            return checksum;
        }
        throw new UpdateIntegrityException("The release checksums do not contain this update package.");
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
