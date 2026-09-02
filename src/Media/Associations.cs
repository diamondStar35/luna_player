using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using LunaPlayer.Configuration;

namespace LunaPlayer.Media;

internal sealed class FileAssociations
{
    private const string ProgId = $"{AppInfo.Identifier}.Media";
    private const string AppName = AppInfo.Name;
    private const string Classes = @"Software\Classes";
    private const string Capabilities = $@"Software\{AppInfo.Identifier}\Capabilities";
    private const string RegisteredApps = @"Software\RegisteredApplications";

    internal bool Register(out string error)
    {
        if (!OperatingSystem.IsWindows())
        {
            // Translators: Shown when the user asks the player to open its file types but the system is not Windows.
            error = Tr("File associations are available only on Windows.");
            return false;
        }
        try
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("The executable path is unavailable.");
            var alias = Path.GetFileName(executable);
            var command = $"\"{executable}\" \"%1\"";
            WriteProgram(command);
            WriteApplication(alias, command);
            WriteCapabilities();
            foreach (var extension in MediaLibrary.SupportedExtensions) WriteExtension(extension);
            NotifyShell();
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException
            or System.Security.SecurityException)
        {
            error = exception.Message;
            return false;
        }
    }

    internal bool Unregister(out string error)
    {
        if (!OperatingSystem.IsWindows())
        {
            error = Tr("File associations are available only on Windows.");
            return false;
        }
        try
        {
            var alias = Path.GetFileName(Environment.ProcessPath ?? $"{AppInfo.Identifier}.exe");
            foreach (var extension in MediaLibrary.SupportedExtensions) RemoveExtension(extension);
            Registry.CurrentUser.DeleteSubKeyTree($@"{Classes}\{ProgId}", throwOnMissingSubKey: false);
            Registry.CurrentUser.DeleteSubKeyTree($@"{Classes}\Applications\{alias}", throwOnMissingSubKey: false);
            Registry.CurrentUser.DeleteSubKeyTree(Capabilities, throwOnMissingSubKey: false);
            using (var registered = Registry.CurrentUser.OpenSubKey(RegisteredApps, writable: true))
                registered?.DeleteValue(AppName, throwOnMissingValue: false);
            NotifyShell();
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            error = exception.Message;
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private static void WriteProgram(string command)
    {
        SetDefault($@"{Classes}\{ProgId}", "Luna Player media file");
        SetDefault($@"{Classes}\{ProgId}\shell\open\command", command);
        SetDefault($@"{Classes}\{ProgId}\shell\play_with_luna", "Play with Luna Player");
        SetDefault($@"{Classes}\{ProgId}\shell\play_with_luna\command", command);
    }

    [SupportedOSPlatform("windows")]
    private static void WriteApplication(string alias, string command)
    {
        var root = $@"{Classes}\Applications\{alias}";
        SetValue(root, "FriendlyAppName", AppName);
        SetDefault($@"{root}\shell\open\command", command);
        SetDefault($@"{root}\shell\play_with_luna", "Play with Luna Player");
        SetDefault($@"{root}\shell\play_with_luna\command", command);
        foreach (var extension in MediaLibrary.SupportedExtensions)
            SetValue($@"{root}\SupportedTypes", extension, string.Empty);
    }

    [SupportedOSPlatform("windows")]
    private static void WriteCapabilities()
    {
        SetValue(Capabilities, "ApplicationName", AppName);
        SetValue(Capabilities, "ApplicationDescription", "Play audio and media files with Luna Player.");
        foreach (var extension in MediaLibrary.SupportedExtensions)
            SetValue($@"{Capabilities}\FileAssociations", extension, ProgId);
        SetValue(RegisteredApps, AppName, Capabilities);
    }

    [SupportedOSPlatform("windows")]
    private static void WriteExtension(string extension)
    {
        var root = $@"{Classes}\{extension}";
        SetDefault(root, ProgId);
        using var key = Registry.CurrentUser.CreateSubKey($@"{root}\OpenWithProgids", writable: true);
        key.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.Binary);
    }

    [SupportedOSPlatform("windows")]
    private static void RemoveExtension(string extension)
    {
        var root = $@"{Classes}\{extension}";
        using (var key = Registry.CurrentUser.OpenSubKey(root, writable: true))
        {
            if (string.Equals(Convert.ToString(key?.GetValue(null)), ProgId, StringComparison.Ordinal))
                key?.SetValue(null, string.Empty, RegistryValueKind.String);
        }
        using (var openWith = Registry.CurrentUser.OpenSubKey($@"{root}\OpenWithProgids", writable: true))
            openWith?.DeleteValue(ProgId, throwOnMissingValue: false);
    }

    [SupportedOSPlatform("windows")]
    private static void SetDefault(string path, string value) => SetValue(path, null, value);

    [SupportedOSPlatform("windows")]
    private static void SetValue(string path, string? name, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path, writable: true);
        key.SetValue(name, value, RegistryValueKind.String);
    }

    private static void NotifyShell() => SHChangeNotify(0x08000000, 0, 0, 0);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, nint item1, nint item2);
}
