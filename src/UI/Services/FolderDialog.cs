using System.Runtime.InteropServices;
using WxSharp;

namespace LunaPlayer.UI.Services;

/// <summary>The Windows folder picker, modal only to its owner rather than every wx window.</summary>
internal sealed unsafe partial class FolderDialog : IDisposable
{
    private const int Cancelled = unchecked((int)0x800704c7);
    private static readonly Guid ClassId = new("dc1c5a9c-e88a-4dde-a5a1-60f82a20aef7");
    private static readonly Guid DialogId = new("42f85136-db7e-439c-85f1-e4075d135fc8");
    private static readonly Guid ItemId = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
    private readonly Window _owner;
    private nint _dialog;
    private bool _showing;
    private bool _disposed;
    private bool _comInitialized;

    // IFileDialog's SDK vtable slots; IUnknown occupies slots 0-2 and IModalWindow::Show slot 3.
    private enum Method { Show = 3, SetOptions = 9, GetOptions = 10, SetFolder = 12, SetTitle = 17, GetResult = 20, Close = 23 }

    internal FolderDialog(Window owner, string title, string initialPath)
    {
        _owner = owner;
        Marshal.ThrowExceptionForHR(CoInitializeEx(0, 2));
        _comInitialized = true;
        try
        {
            Marshal.ThrowExceptionForHR(CoCreateInstance(in ClassId, 0, 1, in DialogId, out _dialog));
            Configure(title, initialPath);
        }
        catch { ReleaseDialog(); throw; }
    }

    private void Configure(string title, string initialPath)
    {
        uint options;
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, uint*, int>)Slot(Method.GetOptions))(_dialog, &options));
        // FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST.
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, uint, int>)Slot(Method.SetOptions))(_dialog, options | 0x20 | 0x40 | 0x800));
        fixed (char* text = title)
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, char*, int>)Slot(Method.SetTitle))(_dialog, text));
        if (SHCreateItemFromParsingName(initialPath, 0, in ItemId, out var initial) >= 0)
        {
            try
            {
                // A missing or inaccessible saved folder must not prevent choosing a replacement.
                _ = ((delegate* unmanaged[Stdcall]<nint, nint, int>)Slot(Method.SetFolder))(_dialog, initial);
            }
            finally { Release(initial); }
        }
    }

    internal string? Show()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _showing = true;
        try
        {
            var result = ((delegate* unmanaged[Stdcall]<nint, nint, int>)Slot(Method.Show))(_dialog, _owner.NativeHandle);
            if (result == Cancelled) return null;
            Marshal.ThrowExceptionForHR(result);
            nint item;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Slot(Method.GetResult))(_dialog, &item));
            try
            {
                nint path;
                // IShellItem::GetDisplayName (slot 5), SIGDN_FILESYSPATH.
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)(*(nint**)item)[5])(item, 0x80058000, &path));
                try { return Marshal.PtrToStringUni(path); }
                finally { Marshal.FreeCoTaskMem(path); }
            }
            finally { Release(item); }
        }
        finally
        {
            _showing = false;
            if (_disposed) ReleaseDialog();
        }
    }

    private nint Slot(Method method) => (*(nint**)_dialog)[(int)method];
    private static void Release(nint pointer) => _ = ((delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)pointer)[2])(pointer);
    private void ReleaseDialog()
    {
        if (_dialog != 0)
        {
            Release(_dialog);
            _dialog = 0;
        }
        if (_comInitialized)
        {
            _comInitialized = false;
            CoUninitialize();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Shutdown can re-enter while the native picker is running its message loop.
        // Close it now and release its COM reference after Show has unwound.
        if (_showing) _ = ((delegate* unmanaged[Stdcall]<nint, int, int>)Slot(Method.Close))(_dialog, Cancelled);
        else ReleaseDialog();
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid classId, nint outer, uint context, in Guid interfaceId, out nint result);

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(nint reserved, uint model);

    [LibraryImport("ole32.dll")]
    private static partial void CoUninitialize();

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHCreateItemFromParsingName(string path, nint context, in Guid interfaceId, out nint result);
}
