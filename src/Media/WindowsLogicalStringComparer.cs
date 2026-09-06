using System.Runtime.InteropServices;

namespace LunaPlayer.Media;

/// <summary>Uses the Windows shell's Explorer-style comparison for names containing numbers.</summary>
internal sealed partial class WindowsLogicalStringComparer : IComparer<string>
{
    internal static WindowsLogicalStringComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        if (ReferenceEquals(left, right))
            return 0;
        if (left is null)
            return -1;
        if (right is null)
            return 1;
        return StrCmpLogicalW(left, right);
    }

    [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int StrCmpLogicalW(string left, string right);
}
