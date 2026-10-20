using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace LunaPlayer.Updater;

internal static partial class Program
{
    private const string FinalizeSwitch = "--finalize";
    private const string PlayerExecutable = "LunaPlayer.exe";
    private const string UpdaterExecutable = "Updater.exe";
    private const string TemporaryUpdaterExecutable = "Updater.temp.exe";
    private const uint Synchronize = 0x00100000;
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 0x00000102;
    private const uint WaitFailed = 0xffffffff;
    private const int ErrorInvalidParameter = 87;
    private const uint ParentWaitMilliseconds = 300_000;
    private const uint ErrorIcon = 0x00000010;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && string.Equals(args[0], FinalizeSwitch, StringComparison.Ordinal))
                return FinalizeUpdate(ParseFinalization(args));
            return InstallUpdate(ParseUpdate(args));
        }
        catch (Exception failure)
        {
            ShowError($"Update failed:\n{failure.Message}");
            return 1;
        }
    }

    private static UpdateRequest ParseUpdate(string[] args)
    {
        if (args.Length != 3)
            throw new ArgumentException("Expected an update package, installation directory and process ID.");

        var assetPath = Path.GetFullPath(args[0]);
        if (!File.Exists(assetPath))
            throw new FileNotFoundException("The update package was not found.", assetPath);

        var installDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[1]));
        if (!Directory.Exists(installDirectory))
            throw new DirectoryNotFoundException("The installation directory was not found.");
        if (!File.Exists(Path.Combine(installDirectory, PlayerExecutable)))
            throw new InvalidOperationException("The installation directory does not contain Luna Player.");

        if (!uint.TryParse(args[2], out var processId) || processId == 0)
            throw new ArgumentException("The process ID is invalid.");

        return new UpdateRequest(assetPath, installDirectory, processId);
    }

    private static FinalizationRequest ParseFinalization(string[] args)
    {
        if (args.Length != 3 || !uint.TryParse(args[1], out var processId) || processId == 0)
            throw new ArgumentException("Expected a worker process ID and an optional update error.");

        var currentExecutable = Environment.ProcessPath
            ?? throw new InvalidOperationException("The updater executable path is unavailable.");
        if (!Path.GetFileName(currentExecutable).Equals(UpdaterExecutable, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only the installed updater can finalize an update.");

        var installDirectory = Path.GetDirectoryName(Path.GetFullPath(currentExecutable))
            ?? throw new InvalidOperationException("The installation directory is unavailable.");
        return new FinalizationRequest(installDirectory, processId, args[2]);
    }

    private static int InstallUpdate(UpdateRequest request)
    {
        if (Path.GetExtension(request.AssetPath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            WaitForParent(request.ProcessId);
            LaunchInstaller(request.AssetPath, request.InstallDirectory);
            return 0;
        }

        try
        {
            WaitForParent(request.ProcessId);
            if (!Path.GetExtension(request.AssetPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("The update package must be a ZIP archive or an installer executable.");
            ApplyArchive(request.AssetPath, request.InstallDirectory);

            ValidateUpdater(request.InstallDirectory);
            StartFinalizer(request.InstallDirectory, string.Empty);
            return 0;
        }
        catch (Exception failure)
        {
            return RecoverUpdater(request.InstallDirectory, failure);
        }
    }

    private static int FinalizeUpdate(FinalizationRequest request)
    {
        WaitForParent(request.ProcessId);

        var cleanupError = DeleteTemporaryUpdater(request.InstallDirectory);
        if (request.Error.Length > 0)
        {
            var message = "The update was not installed completely because some required files are missing. " +
                "The previous updater has been restored. Please download the update again." +
                $"\n\nDetails: {request.Error}";
            if (cleanupError is not null)
                message += $"\n\nThe temporary updater could not be removed: {cleanupError.Message}";
            ShowError(message);
        }
        else if (cleanupError is not null)
        {
            ShowError($"The update was installed, but the temporary updater could not be removed:\n{cleanupError.Message}");
        }

        Launch(Path.Combine(request.InstallDirectory, PlayerExecutable), request.InstallDirectory);
        return request.Error.Length == 0 ? 0 : 1;
    }

    private static void WaitForParent(uint processId)
    {
        var process = OpenProcess(Synchronize, false, processId);
        if (process == 0)
        {
            var error = Marshal.GetLastPInvokeError();
            if (error == ErrorInvalidParameter)
                return;
            throw new InvalidOperationException($"Could not open the Luna Player process. Windows error {error}.");
        }
        try
        {
            var result = WaitForSingleObject(process, ParentWaitMilliseconds);
            if (result == WaitTimeout)
                throw new TimeoutException("Luna Player did not close within five minutes.");
            if (result is not WaitObject0)
                throw new InvalidOperationException(
                    result == WaitFailed
                        ? $"Could not wait for Luna Player to close. Windows error {Marshal.GetLastPInvokeError()}."
                        : "Waiting for Luna Player returned an unexpected result.");
        }
        finally
        {
            _ = CloseHandle(process);
        }
    }

    private static void ApplyArchive(string archivePath, string installDirectory)
    {
        var stagingDirectory = Path.Combine(
            Path.GetTempPath(), "LunaPlayer", $"Update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);
        try
        {
            ExtractSafely(archivePath, stagingDirectory);
            var payloadDirectory = PayloadRoot(stagingDirectory);
            if (!File.Exists(Path.Combine(payloadDirectory, PlayerExecutable)))
                throw new InvalidDataException("The update archive does not contain Luna Player.");
            ValidateUpdater(payloadDirectory);
            CopyTree(payloadDirectory, installDirectory);
        }
        finally
        {
            try
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static void LaunchInstaller(string installerPath, string installDirectory)
    {
        var start = new ProcessStartInfo(installerPath)
        {
            UseShellExecute = true,
            WorkingDirectory = installDirectory,
        };
        // /SILENT skips the wizard but keeps Inno Setup's installation progress window visible. The custom
        // switch tells the script to launch Luna Player itself when installation has completed.
        start.ArgumentList.Add("/SILENT");
        start.ArgumentList.Add("/LUNAUPDATE");
        using var installer = Process.Start(start)
            ?? throw new InvalidOperationException("The update installer could not be started.");
    }

    private static void ExtractSafely(string archivePath, string destinationDirectory)
    {
        var destinationRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(destinationDirectory)) + Path.DirectorySeparatorChar;
        using var archive = ZipFile.OpenRead(archivePath);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.Length == 0)
                continue;
            var destination = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));
            if (!destination.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update archive contains an unsafe path.");
            if (entry.Name.Length == 0)
            {
                Directory.CreateDirectory(destination);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using var input = entry.Open();
            using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
        }
    }

    private static string PayloadRoot(string stagingDirectory)
    {
        var entries = Directory.GetFileSystemEntries(stagingDirectory);
        return entries.Length == 1 && Directory.Exists(entries[0]) ? entries[0] : stagingDirectory;
    }

    private static void CopyTree(string sourceDirectory, string destinationDirectory)
    {
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relative));
        }
        foreach (var source in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, source);
            var destination = Path.Combine(destinationDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: true);
        }
    }

    private static void ValidateUpdater(string installDirectory)
    {
        var updaterPath = Path.Combine(installDirectory, UpdaterExecutable);
        using var updater = new FileStream(updaterPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (updater.Length < 2 || updater.ReadByte() != 'M' || updater.ReadByte() != 'Z')
            throw new InvalidDataException("The update did not contain a valid Updater.exe.");
    }

    private static int RecoverUpdater(string installDirectory, Exception failure)
    {
        var currentExecutable = Environment.ProcessPath
            ?? throw new InvalidOperationException("The updater executable path is unavailable.");
        var updaterPath = Path.Combine(installDirectory, UpdaterExecutable);
        try
        {
            File.Copy(currentExecutable, updaterPath, overwrite: true);
            ValidateUpdater(installDirectory);
        }
        catch (Exception recoveryFailure)
        {
            ShowError(
                $"Update failed:\n{failure.Message}\n\nThe previous updater could not be restored:\n{recoveryFailure.Message}");
            return 1;
        }

        try
        {
            StartFinalizer(installDirectory, failure.Message);
            return 0;
        }
        catch (Exception finalizationFailure)
        {
            ShowError(
                "The update was not installed completely. The previous updater was restored, but automatic " +
                $"cleanup could not be started:\n{finalizationFailure.Message}");
            return 1;
        }
    }

    private static void StartFinalizer(string installDirectory, string error)
    {
        var updaterPath = Path.Combine(installDirectory, UpdaterExecutable);
        var start = new ProcessStartInfo(updaterPath)
        {
            UseShellExecute = false,
            WorkingDirectory = installDirectory,
        };
        start.ArgumentList.Add(FinalizeSwitch);
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add(error);
        using var finalizer = Process.Start(start)
            ?? throw new InvalidOperationException("The updated updater could not be started.");
    }

    private static Exception? DeleteTemporaryUpdater(string installDirectory)
    {
        var temporaryUpdater = Path.Combine(installDirectory, TemporaryUpdaterExecutable);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                if (File.Exists(temporaryUpdater))
                    File.Delete(temporaryUpdater);
                return null;
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
            {
                if (attempt == 19)
                    return failure;
                Thread.Sleep(100);
            }
        }
        return null;
    }

    private static void Launch(string executable, string workingDirectory)
    {
        if (Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = true,
            WorkingDirectory = workingDirectory,
        }) is null)
        {
            throw new InvalidOperationException("The updated application could not be started.");
        }
    }

    private static void ShowError(string message)
        => _ = MessageBox(0, message, "Luna Player Updater", ErrorIcon);

    private readonly record struct UpdateRequest(string AssetPath, string InstallDirectory, uint ProcessId);
    private readonly record struct FinalizationRequest(string InstallDirectory, uint ProcessId, string Error);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint WaitForSingleObject(nint handle, uint milliseconds);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint handle);

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBox(nint window, string text, string caption, uint type);
}
