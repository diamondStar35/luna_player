using System.Runtime.InteropServices;
using MpvNet;
using PrismSharp;
using WxSharp;

namespace LunaPlayer;

internal static class NativeLibraryBootstrap
{
    private const string WrapperImportName = "wx";
    private const string WrapperFileName = "wx.dll";
    private const string MpvImportName = "mpv";
    private const string PrismImportName = "prism";

    private static readonly object Sync = new();
    private static string? _nativeDirectory;
    private static nint _wrapperHandle;
    private static nint _mpvHandle;
    private static nint _prismHandle;

    public static void Initialize()
    {
        _nativeDirectory = FindNativeDirectory();
        if (_nativeDirectory is null)
            return;

        LoadRequired(_nativeDirectory, "wxbase333u_vc_x64.dll");
        LoadRequired(_nativeDirectory, "wxmsw333u_core_vc_x64.dll");
        _wrapperHandle = LoadRequired(_nativeDirectory, WrapperFileName);

        NativeLibrary.SetDllImportResolver(typeof(App).Assembly, ResolveWxSharpImport);
        NativeLibrary.SetDllImportResolver(typeof(MPV).Assembly, ResolveApplicationImport);
        NativeLibrary.SetDllImportResolver(typeof(Context).Assembly, ResolvePrismImport);
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

    private static nint ResolveApplicationImport(
        string libraryName,
        System.Reflection.Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        _ = assembly;
        _ = searchPath;
        if (!string.Equals(libraryName, MpvImportName, StringComparison.OrdinalIgnoreCase))
            return nint.Zero;
        lock (Sync)
            return _mpvHandle != 0
                ? _mpvHandle
                : _mpvHandle = LoadRequired(_nativeDirectory!, "mpv.dll");
    }

    private static nint ResolvePrismImport(
        string libraryName,
        System.Reflection.Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        _ = assembly;
        _ = searchPath;
        if (!string.Equals(libraryName, PrismImportName, StringComparison.OrdinalIgnoreCase))
            return nint.Zero;
        lock (Sync)
            return _prismHandle != 0
                ? _prismHandle
                : _prismHandle = LoadRequired(_nativeDirectory!, "prism.dll");
    }
}
