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
/// Like pynput's Windows listener, the hook belongs to a dedicated thread with its own message loop. Stopping
/// posts a message to that loop; the listener thread then removes its own hook before it exits. The callback
/// only looks up the combination and posts the action to the UI thread, because Windows drops a hook whose
/// callback exceeds LowLevelHooksTimeout.
/// </remarks>
internal sealed partial class GlobalShortcuts : IDisposable
{
    private const int LowLevelKeyboardHook = 13;
    private const int HookActionCode = 0;
    private const nint KeyDown = 0x0100;
    private const nint KeyUp = 0x0101;
    private const nint SystemKeyDown = 0x0104;
    private const nint SystemKeyUp = 0x0105;
    private const uint CreateQueueMessage = 0x0400;
    private const uint StopMessage = 0x0401;
    private const uint NoRemove = 0;
    private const int VirtualKeyShift = 0x10;
    private const int VirtualKeyControl = 0x11;
    private const int VirtualKeyAlt = 0x12;
    private const int VirtualKeyLeftWindows = 0x5B;
    private const int VirtualKeyRightWindows = 0x5C;
    private const int VirtualKeyLeftShift = 0xA0;
    private const int VirtualKeyRightShift = 0xA1;
    private const int VirtualKeyLeftControl = 0xA2;
    private const int VirtualKeyRightControl = 0xA3;
    private const int VirtualKeyLeftAlt = 0xA4;
    private const int VirtualKeyRightAlt = 0xA5;

    private readonly Lock _sync = new();
    private readonly HookProcedure _callback;
    private readonly Dictionary<(int VirtualKey, ShortcutModifiers Modifiers), ActionId> _combinations = [];
    private readonly HashSet<int> _pressedKeys = [];
    private Thread? _listener;
    private uint _listenerThreadId;
    private nint _hook;
    private ShortcutModifiers _modifiers;
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

    /// <summary>Replaces the watched set, starting the listener when there is something to watch and stopping
    /// it when there is not. Returns false when Windows refused to install the hook.</summary>
    internal bool Apply(IReadOnlyList<ShortcutBinding> bindings)
    {
        lock (_sync)
        {
            if (_disposed)
                return false;
            _combinations.Clear();
            foreach (var binding in bindings)
            {
                if (ShortcutKeys.TryGetVirtualKey(binding.Shortcut.Key, out var virtualKey))
                    _combinations[(virtualKey, binding.Shortcut.Modifiers)] = binding.Action;
            }
            if (_combinations.Count > 0 && _listener is not null)
                return _hook != 0;
        }

        if (!HasCombinations())
        {
            StopListener();
            return true;
        }
        return StartListener();
    }

    /// <summary>Stops acting on the watched set while the user is being asked to press a combination. Without
    /// this, pressing the combination being replaced would also fire its action.</summary>
    internal void Suspend()
    {
        lock (_sync)
        {
            _suspended = true;
            ClearKeyState();
        }
    }

    internal void Resume()
    {
        lock (_sync)
        {
            ClearKeyState();
            _suspended = false;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            _combinations.Clear();
            ClearKeyState();
        }
        StopListener();
        Pressed = null;
    }

    private bool HasCombinations()
    {
        lock (_sync)
            return _combinations.Count > 0;
    }

    private bool StartListener()
    {
        using var ready = new ManualResetEventSlim();
        Thread listener;
        lock (_sync)
        {
            if (_disposed)
                return false;
            if (_listener is not null)
                return _hook != 0;
            listener = new Thread(() => Listen(ready))
            {
                IsBackground = true,
                Name = "Luna Player global shortcuts",
            };
            _listener = listener;
        }
        listener.Start();
        ready.Wait();
        lock (_sync)
            return _hook != 0;
    }

    private void StopListener()
    {
        Thread? listener;
        uint threadId;
        lock (_sync)
        {
            listener = _listener;
            threadId = _listenerThreadId;
        }
        if (listener is null)
            return;
        if (threadId != 0)
            PostThreadMessageW(threadId, StopMessage, 0, 0);
        if (listener != Thread.CurrentThread)
            listener.Join();
    }

    private void Listen(ManualResetEventSlim ready)
    {
        nint hook = 0;
        var signalled = false;
        try
        {
            // GetMessage has no queue to wait on until the thread asks Windows to create one. Peek first so
            // StopListener can safely post even if no keyboard event has arrived yet.
            PeekMessageW(out _, 0, CreateQueueMessage, CreateQueueMessage, NoRemove);
            var threadId = GetCurrentThreadId();
            hook = SetWindowsHookExW(
                LowLevelKeyboardHook, Marshal.GetFunctionPointerForDelegate(_callback), 0, 0);
            lock (_sync)
            {
                _listenerThreadId = threadId;
                _hook = hook;
                ClearKeyState();
            }
            ready.Set();
            signalled = true;
            if (hook == 0)
                return;

            while (GetMessageW(out var message, 0, 0, 0) > 0 && message.Message != StopMessage)
            {
                // The low-level hook callback is dispatched as part of pumping this thread's queue. There
                // are no windows on this thread, so other messages need no translation or dispatch.
            }
        }
        finally
        {
            if (!signalled)
                ready.Set();
            if (hook != 0)
                UnhookWindowsHookEx(hook);
            lock (_sync)
            {
                ClearKeyState();
                _hook = 0;
                _listenerThreadId = 0;
                if (_listener == Thread.CurrentThread)
                    _listener = null;
            }
        }
    }

    private nint OnKey(int code, nint message, nint data)
    {
        ActionId? action = null;
        var isKeyDown = message == KeyDown || message == SystemKeyDown;
        var isKeyUp = message == KeyUp || message == SystemKeyUp;
        if (code == HookActionCode && (isKeyDown || isKeyUp))
        {
            // vkCode is the first field of KBDLLHOOKSTRUCT.
            var virtualKey = Marshal.ReadInt32(data);
            lock (_sync)
            {
                if (!_suspended && !_disposed)
                {
                    if (TryGetModifier(virtualKey, out var modifier))
                    {
                        if (isKeyDown)
                            _modifiers |= modifier;
                        else
                            _modifiers &= ~modifier;
                    }
                    else if (isKeyUp)
                    {
                        _pressedKeys.Remove(virtualKey);
                    }
                    else if (_pressedKeys.Add(virtualKey)
                        && _combinations.TryGetValue((virtualKey, _modifiers), out var matched))
                    {
                        action = matched;
                    }
                }
            }
        }
        if (action.HasValue)
            Wx.CallAfter(() => Dispatch(action.Value));
        return CallNextHookEx(0, code, message, data);
    }

    private void Dispatch(ActionId action)
    {
        lock (_sync)
        {
            if (_disposed)
                return;
        }
        Pressed?.Invoke(action);
    }

    private void ClearKeyState()
    {
        _modifiers = ShortcutModifiers.None;
        _pressedKeys.Clear();
    }

    private static bool TryGetModifier(int virtualKey, out ShortcutModifiers modifier)
    {
        modifier = virtualKey switch
        {
            VirtualKeyShift or VirtualKeyLeftShift or VirtualKeyRightShift => ShortcutModifiers.Shift,
            VirtualKeyControl or VirtualKeyLeftControl or VirtualKeyRightControl => ShortcutModifiers.Control,
            VirtualKeyAlt or VirtualKeyLeftAlt or VirtualKeyRightAlt => ShortcutModifiers.Alt,
            VirtualKeyLeftWindows or VirtualKeyRightWindows => ShortcutModifiers.Win,
            _ => ShortcutModifiers.None,
        };
        return modifier != ShortcutModifiers.None;
    }

    private delegate nint HookProcedure(int code, nint message, nint data);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        internal nint Window;
        internal uint Message;
        internal nuint WParam;
        internal nint LParam;
        internal uint Time;
        internal NativePoint Point;
        internal uint Private;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint SetWindowsHookExW(int hookId, nint procedure, nint module, uint threadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(nint hook);

    [LibraryImport("user32.dll")]
    private static partial nint CallNextHookEx(nint hook, int code, nint message, nint data);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int GetMessageW(out NativeMessage message, nint window, uint minimum, uint maximum);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PeekMessageW(
        out NativeMessage message, nint window, uint minimum, uint maximum, uint remove);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostThreadMessageW(uint threadId, uint message, nuint wParam, nint lParam);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();

}
