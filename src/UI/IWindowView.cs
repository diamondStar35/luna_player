using LunaPlayer.Actions;

namespace LunaPlayer.UI;

/// <summary>The main window itself: its lifetime, the plain window state, and the input events everything
/// else is driven from.</summary>
internal interface IWindowView
{
    event Action<ActionId>? ActionRequested;
    /// <summary>Asked before a close that can still be cancelled. Every subscriber must agree.</summary>
    event Func<bool>? CloseConfirmationRequested;
    /// <summary>Starts accepted shutdown work. The window remains alive until every returned task ends.</summary>
    event Func<Task>? CloseRequested;

    /// <summary>Asked when Escape is pressed on the main window with no modifier. Returning true means it
    /// was dealt with, and the key goes no further.</summary>
    event Func<bool>? EscapePressed;

    nint NativeHandle { get; }
    void Show();
    void Close();
    void RestoreAndRaise();
    void SetPlaying(bool isPlaying);

    /// <summary>Sets the window title. A screen reader announces the foreground window when its title changes,
    /// so the caller sets it only when the title feature is on and the text has actually changed.</summary>
    void SetWindowTitle(string title);

    /// <summary>Fills the whole screen with the window, or restores it, ticking the menu item to match.</summary>
    void SetFullScreen(bool fullScreen);

    /// <summary>Whether the window is filling the screen right now.</summary>
    bool IsFullScreen { get; }

    void ApplyShortcuts(ShortcutManager shortcuts);

    /// <summary>Starts watching for the system-wide shortcuts in <paramref name="shortcuts"/>, replacing any
    /// set already being watched. Returns false when the system would not let the player watch the keyboard at
    /// all, which is worth telling the user about: no global shortcut will work.</summary>
    bool ApplyGlobalShortcuts(ShortcutManager shortcuts);
}
