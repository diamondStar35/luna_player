using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace LunaPlayer.Updater;

internal static partial class Program
{
    private const string ApplySwitch = "--apply";
    private const string PlayerExecutable = "LunaPlayer.exe";
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
            if (args.Length > 0 && string.Equals(args[0], ApplySwitch, StringComparison.Ordinal))
                return Apply(Parse(args, 1));
            return Relocate(Parse(args, 0));
        }
        catch (Exception failure)
        {
            ShowError($"Update failed:\n{failure.Message}");
            return 1;
        }
    }

    private static UpdateRequest Parse(string[] args, int offset)
    {
        if (args.Length - offset != 3)
            throw new ArgumentException("Expected an update package, installation directory and process ID.");

        var assetPath = Path.GetFullPath(args[offset]);
        if (!File.Exists(assetPath))
            throw new FileNotFoundException("The update package was not found.", assetPath);

        var installDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[offset + 1]));
        if (!Directory.Exists(installDirectory))
            throw new DirectoryNotFoundException("The installation directory was not found.");
        if (!File.Exists(Path.Combine(installDirectory, PlayerExecutable)))
            throw new InvalidOperationException("The installation directory does not contain Luna Player.");

        if (!uint.TryParse(args[offset + 2], out var processId) || processId == 0)
            throw new ArgumentException("The process ID is invalid.");

        return new UpdateRequest(assetPath, installDirectory, processId);
    }

    private static int Relocate(UpdateRequest request)
    {
        var currentExecutable = Environment.ProcessPath
            ?? throw new InvalidOperationException("The updater executable path is unavailable.");
        var helperDirectory = Path.Combine(Path.GetTempPath(), "LunaPlayer", "Updater");
        var helperPath = Path.Combine(helperDirectory, "Updater.exe");
        Directory.CreateDirectory(helperDirectory);

        if (!Path.GetFullPath(currentExecutable).Equals(Path.GetFullPath(helperPath), StringComparison.OrdinalIgnoreCase))
            File.Copy(currentExecutable, helperPath, overwrite: true);

        var start = new ProcessStartInfo(helperPath)
        {
            UseShellExecute = false,
            WorkingDirectory = helperDirectory,
        };
        start.ArgumentList.Add(ApplySwitch);
        start.ArgumentList.Add(request.AssetPath);
        start.ArgumentList.Add(request.InstallDirectory);
        start.ArgumentList.Add(request.ProcessId.ToString());
        if (Process.Start(start) is null)
            throw new InvalidOperationException("The temporary updater could not be started.");
        return 0;
    }

    private static int Apply(UpdateRequest request)
    {
        WaitForParent(request.ProcessId);
        switch (Path.GetExtension(request.AssetPath).ToLowerInvariant())
        {
            case ".zip":
                ApplyArchive(request.AssetPath, request.InstallDirectory);
                Launch(Path.Combine(request.InstallDirectory, PlayerExecutable), request.InstallDirectory);
                return 0;
            case ".exe":
                Launch(request.AssetPath, request.InstallDirectory);
                return 0;
            default:
                throw new NotSupportedException("The update package must be a ZIP archive or an installer executable.");
        }
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
