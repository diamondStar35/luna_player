using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>Turns keys into requests on the main window: accelerators, global shortcuts, menu clicks, and
/// Escape. Resolves menu ids through the MenuController and raises the window's input events.</summary>
internal sealed class WindowInput : IDisposable
{
    private readonly Frame _frame;
    private readonly MenuController _menus;
    private readonly LunaPlayer.UI.Services.GlobalShortcuts _globalShortcuts;

    internal WindowInput(Frame frame, MenuController menus, ShortcutManager shortcuts)
    {
        _frame = frame;
        _menus = menus;
        _globalShortcuts = new LunaPlayer.UI.Services.GlobalShortcuts();
        _globalShortcuts.Pressed += Request;
        BuildAccelerators(shortcuts);
        _frame.MenuCommand += OnMenuCommand;
        _frame.Bind(WxEvents.CharHook, OnCharHook);
    }

    internal event Action<ActionId>? ActionRequested;
    internal event Action<string?>? EqualizerPresetRequested;
    internal event Action<int?>? SubtitleTrackRequested;
    internal event Action<LunaPlayer.UI.Playlists.RecentCommand>? RecentRequested;
    internal event Action<int>? ChapterRequested;

    /// <summary>True means Escape was handled and should go no further.</summary>
    internal event Func<bool>? EscapePressed;

    /// <summary>The system-wide shortcut watcher, shared with the dialogs. Owned and disposed here.</summary>
    internal LunaPlayer.UI.Services.GlobalShortcuts GlobalShortcuts => _globalShortcuts;

    /// <summary>Raises an action request from outside the keyboard (the transport buttons).</summary>
    internal void Request(ActionId action) => ActionRequested?.Invoke(action);

    internal bool ApplyGlobalShortcuts(ShortcutManager shortcuts)
        => _globalShortcuts.Apply(shortcuts.GetBindings());

    /// <summary>Rebuilds the local accelerator table from a new set of shortcuts.</summary>
    internal void ApplyAccelerators(ShortcutManager shortcuts) => BuildAccelerators(shortcuts);

    /// <summary>Stops global shortcuts early for the close sequence. Disposing is idempotent.</summary>
    internal void StopGlobalShortcuts() => _globalShortcuts.Dispose();

    public void Dispose() => _globalShortcuts.Dispose();

    private void BuildAccelerators(ShortcutManager shortcuts)
    {
        var accelerators = new List<AcceleratorEntry>();
        foreach (var binding in shortcuts.GetBindings())
        {
            // The Windows key can't go in an accelerator table; it's left to the global hot-key registration.
            if ((binding.Shortcut.Modifiers & ShortcutModifiers.Win) != 0) continue;
            var commandId = _menus.CommandIds[binding.Action];
            var display = binding.Shortcut.ToDisplayString();
            if (!AcceleratorEntry.TryParse(display, commandId, out var accelerator))
                throw new InvalidOperationException($"'{display}' is not a valid local shortcut.");
            accelerators.Add(accelerator);
        }
        _frame.SetAcceleratorTable([.. accelerators]);
    }

    private void OnMenuCommand(object? sender, CommandEventArgs args)
    {
        if (_menus.TryGetAction(args.Id, out var action))
        {
            Request(action);
            return;
        }
        // Guarded: wxWidgets is calling in, so an exception must not unwind into C++ frames.
        if (_menus.TryGetEqualizerPreset(args.Id, out var presetId))
        {
            LunaPlayer.Application.CrashReport.Guard(() => EqualizerPresetRequested?.Invoke(presetId));
            return;
        }
        if (_menus.TryGetSubtitleTrack(args.Id, out var trackId))
        {
            LunaPlayer.Application.CrashReport.Guard(() => SubtitleTrackRequested?.Invoke(trackId));
            return;
        }
        if (_menus.TryGetChapter(args.Id, out var chapter))
        {
            LunaPlayer.Application.CrashReport.Guard(() => ChapterRequested?.Invoke(chapter));
            return;
        }
        if (_menus.TryGetRecent(args.Id, out var recent))
            LunaPlayer.Application.CrashReport.Guard(() => RecentRequested?.Invoke(recent));
    }

    /// <summary>Only a bare Escape, and only when something handles it; everything else is skipped.</summary>
    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape && args.Modifiers == KeyModifiers.None)
        {
            // Guarded: this runs inside a wxWidgets key event.
            var handled = false;
            LunaPlayer.Application.CrashReport.Guard(() => handled = EscapePressed?.Invoke() == true);
            if (handled)
                return;
        }
        args.Skip();
    }
}
