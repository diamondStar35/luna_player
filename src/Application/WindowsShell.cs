using System.Runtime.InteropServices;

namespace LunaPlayer.Application;

/// <summary>Operations exposed by the Windows shell rather than by ordinary process launching.</summary>
internal static partial class WindowsShell
{
    private const uint ShopFilePath = 0x2;
    private const int ShowNormal = 1;

    /// <summary>Shows Explorer's standard property sheet for a file.</summary>
    internal static bool ShowFileProperties(string path)
    {
        var target = Path.GetFullPath(path);
        try
        {
            if (SHObjectProperties(0, ShopFilePath, target, null))
                return true;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            // ShellExecuteW below is available on older shell versions where SHObjectProperties is absent.
        }

        try
        {
            return ShellExecuteW(0, "properties", target, null, null, ShowNormal) > 32;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SHObjectProperties(
        nint owner, uint objectType, string objectName, string? propertyPage);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint ShellExecuteW(
        nint owner, string operation, string file, string? parameters, string? directory, int showCommand);
}
