using System.Runtime.InteropServices;
using WxSharp;

namespace LunaPlayer;

internal static class NativeLibraryBootstrap
{
    private const string WrapperImportName = "wx";
    private const string WrapperFileName = "wx.dll";

    private static nint _wrapperHandle;

    public static void Initialize()
    {
        var nativeDirectory = FindNativeDirectory();
        if (nativeDirectory is null)
            return;

        LoadRequired(nativeDirectory, "wxbase333u_vc_x64.dll");
        LoadRequired(nativeDirectory, "wxmsw333u_core_vc_x64.dll");
        _wrapperHandle = LoadRequired(nativeDirectory, WrapperFileName);

        NativeLibrary.SetDllImportResolver(typeof(App).Assembly, ResolveWxSharpImport);
    }

    private static string? FindNativeDirectory()
    {
        var applicationDirectory = AppContext.BaseDirectory;
        if (File.Exists(Path.Combine(applicationDirectory, WrapperFileName)))
            return applicationDirectory;

        var libDirectory = Path.Combine(applicationDirectory, "lib");
        return File.Exists(Path.Combine(libDirectory, WrapperFileName)) ? libDirectory : null;
    }

    private static nint LoadRequired(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
            throw new DllNotFoundException($"Required native library was not found: {path}");

        return NativeLibrary.Load(path);
    }

    private static nint ResolveWxSharpImport(
        string libraryName,
        System.Reflection.Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        _ = assembly;
        _ = searchPath;

        return string.Equals(libraryName, WrapperImportName, StringComparison.OrdinalIgnoreCase)
            ? _wrapperHandle
            : nint.Zero;
    }
}
