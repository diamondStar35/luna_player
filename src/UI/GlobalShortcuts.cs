using System.Runtime.InteropServices;
using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>Watches the whole keyboard for the configured system-wide shortcuts and reports them as actions.
/// </summary>
///
/// <remarks>
/// A low-level keyboard hook rather than <c>RegisterHotKey</c>, because Windows itself owns several of the
/// combinations a media player wants - Win+Alt with the arrow keys is taken by window snapping, and
/// registration for those simply fails. A hook sees them anyway.
///
/// Matched combinations are passed on rather than swallowed, as the Python player did: the modifiers keep
/// reaching Windows, so a chord it owns still does its own job as well. The trade is that seeking with
/// Win+Alt+Left also snaps the window; suppressing it instead would hide the Win key press from the shell and
/// leave the Start menu opening on release.
///
/// The callback runs on the thread that installed the hook - the UI thread - and Windows drops a hook whose
/// callback is slow, so it only ever looks up the combination and posts the action.
/// </remarks>
internal sealed partial class GlobalShortcuts : IDisposable
{
    private const int LowLevelKeyboardHook = 13;
    private const int HookActionCode = 0;
    private const nint KeyDown = 0x0100;
    private const nint SystemKeyDown = 0x0104;
    private const int VirtualKeyShift = 0x10;
    private const int VirtualKeyControl = 0x11;
    private const int VirtualKeyAlt = 0x12;
    private const int VirtualKeyLeftWindows = 0x5B;
    private const int VirtualKeyRightWindows = 0x5C;

    private readonly HookProcedure _callback;
    private readonly Dictionary<(int VirtualKey, ShortcutModifiers Modifiers), ActionId> _combinations = [];
    private nint _hook;
    private bool _suspended;
    private bool _disposed;

    internal GlobalShortcuts()
    {
        // Held in a field: the hook keeps a bare function pointer, which would not stop the delegate being
        // collected.
        _callback = OnKey;
    }

    /// <summary>A configured combination was pressed. Raised on the UI thread.</summary>
    internal event Action<ActionId>? Pressed;

    /// <summary>Replaces the watched set, installing the hook the first time there is something to watch for.
    /// Returns false when Windows refused the hook, in which case no global shortcut will work.</summary>
    internal bool Apply(IReadOnlyList<ShortcutBinding> bindings)
    {
        if (_disposed) return false;
        _combinations.Clear();
        foreach (var binding in bindings)
        {
            if (ShortcutKeys.TryGetVirtualKey(binding.Shortcut.Key, out var virtualKey))
                _combinations[(virtualKey, binding.Shortcut.Modifiers)] = binding.Action;
        }
        if (_combinations.Count == 0) return true;
        if (_hook == 0)
            _hook = SetWindowsHookExW(LowLevelKeyboardHook, Marshal.GetFunctionPointerForDelegate(_callback), 0, 0);
        return _hook != 0;
    }

    /// <summary>Stops acting on the watched set while the user is being asked to press a combination. Without
    /// this, pressing the combination being replaced would also fire its action.</summary>
    internal void Suspend() => _suspended = true;

    internal void Resume() => _suspended = false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hook != 0)
            UnhookWindowsHookEx(_hook);
        _hook = 0;
        _combinations.Clear();
    }

    private nint OnKey(int code, nint message, nint data)
    {
        if (code == HookActionCode && (message == KeyDown || message == SystemKeyDown)
            && !_suspended && !_disposed)
        {
            // vkCode is the first field of KBDLLHOOKSTRUCT.
            var virtualKey = Marshal.ReadInt32(data);
            if (_combinations.TryGetValue((virtualKey, CurrentModifiers()), out var action))
            {
                // Posted, not invoked: an action can load a file or seek, and Windows removes a hook whose
                // callback overruns LowLevelHooksTimeout.
                var pressed = Pressed;
                if (pressed is not null) Wx.CallAfter(() => pressed(action));
            }
        }
        return CallNextHookEx(0, code, message, data);
    }

    /// <summary>The modifiers held right now. A hook reports the key alone, and reading the modifier state as
    /// its event arrives is the same thing wxWidgets does for an ordinary key press.</summary>
    private static ShortcutModifiers CurrentModifiers()
    {
        var modifiers = ShortcutModifiers.None;
        if (IsDown(VirtualKeyControl)) modifiers |= ShortcutModifiers.Control;
        if (IsDown(VirtualKeyShift)) modifiers |= ShortcutModifiers.Shift;
        if (IsDown(VirtualKeyAlt)) modifiers |= ShortcutModifiers.Alt;
        if (IsDown(VirtualKeyLeftWindows) || IsDown(VirtualKeyRightWindows)) modifiers |= ShortcutModifiers.Win;
        return modifiers;
    }

    private static bool IsDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private delegate nint HookProcedure(int code, nint message, nint data);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint SetWindowsHookExW(int hookId, nint procedure, nint module, uint threadId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(nint hook);

    [LibraryImport("user32.dll")]
    private static partial nint CallNextHookEx(nint hook, int code, nint message, nint data);

    /// <summary>The physical key state, not the calling thread's queued state: the hook runs ahead of the
    /// message queue, so the queued state has not caught up with the key that is arriving.</summary>
    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int virtualKey);
}
