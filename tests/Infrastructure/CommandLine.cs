using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace LunaPlayer.Tests.Infrastructure;

internal static class CommandLine
{
    internal static string[] Parse(string command)
    {
        var pointer = CommandLineToArgvW(command, out var count);
        if (pointer == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            var arguments = new string[count];
            for (var index = 0; index < count; index++)
                arguments[index] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(pointer, index * IntPtr.Size)) ?? string.Empty;
            return arguments;
        }
        finally { _ = LocalFree(pointer); }
    }

    internal static string Resolve(RegistryKey association)
    {
        uint length = 0;
        Marshal.ThrowExceptionForHR(AssocQueryStringByKeyW(0, 1, association.Handle, null, null, ref length));
        var command = new StringBuilder((int)length);
        Marshal.ThrowExceptionForHR(AssocQueryStringByKeyW(0, 1, association.Handle, null, command, ref length));
        return command.ToString();
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CommandLineToArgvW(string command, out int count);
    [DllImport("kernel32.dll")]
    private static extern nint LocalFree(nint memory);
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int AssocQueryStringByKeyW(uint flags, uint type, SafeRegistryHandle association,
        string? verb, StringBuilder? command, ref uint length);
}
