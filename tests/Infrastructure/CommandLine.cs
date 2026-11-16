using System.ComponentModel;
using System.Runtime.InteropServices;

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

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CommandLineToArgvW(string command, out int count);
    [DllImport("kernel32.dll")]
    private static extern nint LocalFree(nint memory);
}
