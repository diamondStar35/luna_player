using System.Runtime.InteropServices;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>Which set of bindings a <see cref="ShortcutPreferences"/> page edits.</summary>
internal enum ShortcutScope
{
    /// <summary>Shortcuts that work while the player has the focus. Two slots per action.</summary>
    Local,
    /// <summary>Shortcuts registered with the system, which work from any application. One slot per action.
    /// </summary>
    Global,
}

internal sealed class ShortcutPreferences : Preferences
{
    private readonly ShortcutSettings _settings;
    private readonly ShortcutScope _scope;
    private readonly GlobalShortcuts? _globals;
    private readonly ActionDefinition[] _actions;
    private readonly HashSet<ActionId> _editable;
    private readonly Dictionary<ActionId, Shortcut> _primary = [];
    private readonly Dictionary<ActionId, Shortcut> _secondary = [];
    private readonly ListCtrl _list;
    private readonly Button _editPrimary;
    private readonly Button? _editSecondary;
    private readonly Button _reset;

    /// <param name="globals">The live hot key registrations, released while the user is pressing a
    /// combination. Without that, a combination already in use fires its action instead of being recorded.
    /// </param>
    internal ShortcutPreferences(Window parent, ShortcutSettings settings, ShortcutScope scope, GlobalShortcuts? globals)
        : base(new Panel(parent), scope == ShortcutScope.Local
            // Translators: Spoken description of the Keyboard shortcuts settings page, read when the page is opened.
            ? Tr("Keyboard shortcuts. Select an action, then edit its primary or secondary local shortcut.")
            // Translators: Spoken description of the Global shortcuts settings page, read when the page is opened.
            : Tr("System-wide shortcuts. Select an action, then edit the combination that triggers it from any application."))
    {
        _settings = settings;
        _scope = scope;
        _globals = globals;
        _actions = [.. scope == ShortcutScope.Local ? ActionRegistry.All : GlobalActionDefinitions.All];
        _editable = [.. _actions.Select(action => action.Id)];
        var panel = (Panel)Window;
        var heading = new StaticText(panel, label: scope == ShortcutScope.Local
            // Translators: Heading above the list of shortcuts that work while the player is the program in front.
            ? Tr("Local Shortcuts")
            // Translators: Heading above the list of shortcuts that work while another program is in front.
            : Tr("Global Shortcuts"));
        _list = new ListCtrl(panel, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection);
        // Translators: Heading of the shortcut list column naming each command.
        _list.InsertColumn(0, Tr("Action"), 210);
        _list.InsertColumn(1, HasSecondary
            // Translators: Heading of the shortcut list column holding the first key combination of each command.
            ? Tr("Primary Shortcut")
            // Translators: Heading of the shortcut list column holding the key combination of each command, on the page
            // where a command has only one.
            : Tr("Shortcut"), 125);
        if (HasSecondary)
            // Translators: Heading of the shortcut list column holding the second, alternative key combination of each command.
            _list.InsertColumn(2, Tr("Secondary Shortcut"), 135);
        _editPrimary = new Button(panel, label: HasSecondary
            // Translators: Button that changes the first key combination of the chosen command.
            ? Tr("Edit Primary Shortcut")
            // Translators: Button that changes the key combination of the chosen command, on the page where a command has only one.
            : Tr("Edit Shortcut"));
        // Translators: Button that changes the second, alternative key combination of the chosen command.
        _editSecondary = HasSecondary ? new Button(panel, label: Tr("Edit Secondary Shortcut")) : null;
        // Translators: Button that puts every shortcut on this page back to the combination the player came with.
        _reset = new Button(panel, label: Tr("Reset to Defaults"));

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(_editPrimary, flags: SizerFlags.BorderRight, border: 6);
        if (_editSecondary is not null)
            buttons.Add(_editSecondary, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_reset);
        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(heading, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        sizer.Add(_list, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        panel.SetSizer(sizer);

        _list.ItemSelected += (_, _) => UpdateButtons();
        _list.ItemDeselected += (_, _) => UpdateButtons();
        _list.ItemActivated += (_, _) => Edit(ShortcutSlot.Primary);
        _editPrimary.Click += (_, _) => Edit(ShortcutSlot.Primary);
        if (_editSecondary is not null)
            _editSecondary.Click += (_, _) => Edit(ShortcutSlot.Secondary);
        _reset.Click += (_, _) => Reset();
        Refresh();
    }

    private bool HasSecondary => _scope == ShortcutScope.Local;

    public override string GetContextHelp(Window? focused) => string.Empty;

    public override void Apply()
    {
        if (_scope == ShortcutScope.Local)
        {
            _settings.Primary = Overrides(_primary, primary: true);
            _settings.Secondary = Overrides(_secondary, primary: false);
        }
        else
        {
            _settings.Global = Overrides(_primary, primary: true);
        }
    }

    public override void Refresh()
    {
        _primary.Clear();
        _secondary.Clear();
        foreach (var action in _actions)
        {
            if (action.PrimaryShortcut is Shortcut primary) _primary[action.Id] = primary;
            if (action.SecondaryShortcut is Shortcut secondary) _secondary[action.Id] = secondary;
        }
        // Keyed on the page's own action set rather than on the loaded defaults: an action that ships without
        // a shortcut can still have been given one, and that binding has to come back.
        foreach (var pair in _scope == ShortcutScope.Local ? _settings.Primary : _settings.Global)
            if (_editable.Contains(pair.Key)) _primary[pair.Key] = pair.Value;
        if (HasSecondary)
            foreach (var pair in _settings.Secondary)
                if (_secondary.ContainsKey(pair.Key)) _secondary[pair.Key] = pair.Value;
        Rebuild();
    }

    private void Edit(ShortcutSlot slot)
    {
        var index = _list.SelectedIndex;
        if (index < 0 || index >= _actions.Length) return;
        var action = _actions[(int)index];
        if (slot == ShortcutSlot.Secondary && (!HasSecondary || action.SecondaryShortcut is null)) return;
        var shortcut = Capture();
        if (shortcut is null) return;
        if (HasConflict(action.Id, slot, shortcut.Value))
        {
            Wx.MessageBox(
                // Translators: Shown when the combination the user pressed is already used by another command.
                Tr("That shortcut is already assigned to another action."),
                // Translators: Title of the message shown when the combination pressed is already used by another command.
                Tr("Shortcut Conflict"),
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, Window);
            return;
        }
        (slot == ShortcutSlot.Primary ? _primary : _secondary)[action.Id] = shortcut.Value;
        Rebuild(index);
    }

    private Shortcut? Capture()
    {
        using var dialog = new ShortcutCapture(Window, allowWin: _scope == ShortcutScope.Global);
        // Hot keys are exclusive, so any that are live would swallow the key press instead of it being recorded.
        _globals?.Suspend();
        try
        {
            return dialog.Show();
        }
        finally
        {
            _globals?.Resume();
        }
    }

    private bool HasConflict(ActionId action, ShortcutSlot slot, Shortcut shortcut)
    {
        foreach (var pair in _primary)
            if (!(pair.Key == action && slot == ShortcutSlot.Primary) && pair.Value == shortcut) return true;
        foreach (var pair in _secondary)
            if (!(pair.Key == action && slot == ShortcutSlot.Secondary) && pair.Value == shortcut) return true;
        // A collision with the other scope matters just as much: a global bound to a local combination would
        // run the action twice whenever the player has the focus.
        return OtherScope().Contains(shortcut);
    }

    /// <summary>The shortcuts currently in force on the page this one is not editing.</summary>
    private HashSet<Shortcut> OtherScope()
    {
        var actions = _scope == ShortcutScope.Local ? GlobalActionDefinitions.All : ActionRegistry.All;
        var effective = new Dictionary<ActionId, Shortcut>();
        var secondary = new Dictionary<ActionId, Shortcut>();
        foreach (var action in actions)
        {
            if (action.PrimaryShortcut is Shortcut primary) effective[action.Id] = primary;
            if (action.SecondaryShortcut is Shortcut value) secondary[action.Id] = value;
        }
        foreach (var pair in _scope == ShortcutScope.Local ? _settings.Global : _settings.Primary)
            if (effective.ContainsKey(pair.Key)) effective[pair.Key] = pair.Value;
        if (_scope == ShortcutScope.Global)
            foreach (var pair in _settings.Secondary)
                if (secondary.ContainsKey(pair.Key)) secondary[pair.Key] = pair.Value;
        return [.. effective.Values, .. secondary.Values];
    }

    private void Reset()
    {
        if (!HasChangesFromDefaults()) return;
        if (Wx.MessageBox(
            // Translators: Asks the user to confirm putting every shortcut on this page back to the combination the player came with.
            Tr("Reset all shortcuts to defaults?"),
            // Translators: Title of the window that asks the user to confirm putting every shortcut back as it was.
            Tr("Confirm Reset"),
            MessageBoxStyle.YesNo | MessageBoxStyle.IconQuestion, Window) != MessageBoxStyle.Yes) return;
        // Only the working copy is cleared; the stored overrides are rewritten from it in Apply.
        _primary.Clear();
        _secondary.Clear();
        foreach (var action in _actions)
        {
            if (action.PrimaryShortcut is Shortcut primary) _primary[action.Id] = primary;
            if (action.SecondaryShortcut is Shortcut secondary) _secondary[action.Id] = secondary;
        }
        Rebuild();
    }

    private void Rebuild(long selected = -1)
    {
        _list.Clear();
        foreach (var action in _actions)
        {
            var row = _list.AddItem(action.Label);
            _list.SetItem(row, 1, _primary.TryGetValue(action.Id, out var primary) ? primary.ToDisplayString() : string.Empty);
            if (HasSecondary)
                _list.SetItem(row, 2, _secondary.TryGetValue(action.Id, out var secondary) ? secondary.ToDisplayString() : string.Empty);
        }
        if (selected >= 0 && selected < _actions.Length)
        {
            _list.SelectedIndex = selected;
            _list.SetFocused(selected);
            _list.EnsureVisible(selected);
            _list.Focus();
        }
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var index = _list.SelectedIndex;
        _editPrimary.Enabled = index >= 0;
        if (_editSecondary is not null)
            _editSecondary.Enabled = index >= 0 && index < _actions.Length && _actions[(int)index].SecondaryShortcut is not null;
        _reset.Enabled = HasChangesFromDefaults();
    }

    /// <summary>Whether anything on the page differs from what the player ships with - a rebinding, a shortcut
    /// added to an action that has none by default, or one taken away. This is what "Reset to Defaults" has to
    /// offer to undo, so it decides whether that button is available.</summary>
    private bool HasChangesFromDefaults()
        => _actions.Any(action => Current(_primary, action.Id) != action.PrimaryShortcut)
            || (HasSecondary && _actions.Any(action => Current(_secondary, action.Id) != action.SecondaryShortcut));

    private static Shortcut? Current(IReadOnlyDictionary<ActionId, Shortcut> values, ActionId action)
        => values.TryGetValue(action, out var value) ? value : null;

    private Dictionary<ActionId, Shortcut> Overrides(IReadOnlyDictionary<ActionId, Shortcut> values, bool primary)
    {
        var result = new Dictionary<ActionId, Shortcut>();
        foreach (var action in _actions)
        {
            var defaultValue = primary ? action.PrimaryShortcut : action.SecondaryShortcut;
            // A secondary slot only exists where the action defines one; a primary binding is storable for
            // every action, so one given to an action that ships without a shortcut is a delta like any other.
            if (!primary && defaultValue is null) continue;
            if (!values.TryGetValue(action.Id, out var current) || current == defaultValue) continue;
            result[action.Id] = current;
        }
        return result;
    }
}

internal sealed partial class ShortcutCapture : IDisposable
{
    private const int AcceptId = 17031;
    private const int VirtualKeyLeftWindows = 0x5B;
    private const int VirtualKeyRightWindows = 0x5C;
    private readonly Dialog _dialog;
    private readonly bool _allowWin;
    private Shortcut? _shortcut;

    /// <param name="allowWin">Whether the Windows key counts as a modifier. Off for a local shortcut, which
    /// cannot use it: an accelerator table has no way to express that modifier.</param>
    internal ShortcutCapture(Window parent, bool allowWin = false)
    {
        _allowWin = allowWin;
        // Translators: Title of the small window that waits for the user to press the combination they want.
        _dialog = new Dialog(parent, title: Tr("Set Shortcut"));
        // Translators: Message in the window that waits for a key combination. Escape is the name of the key that closes it.
        var label = new StaticText(_dialog, label: Tr("Press the desired shortcut. Escape cancels."));
        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(label, flags: SizerFlags.All, border: 10);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnKey);
    }

    internal Shortcut? Show() => _dialog.ShowModal() == AcceptId ? _shortcut : null;
    public void Dispose() => _dialog.Dispose();

    private void OnKey(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
        {
            _dialog.EndModal(StandardId.Cancel);
            return;
        }
        var key = ShortcutKeys.NameOf(args.Code, args.KeyCode);
        if (key is null) return;
        var modifiers = ShortcutModifiers.None;
        if (args.Control) modifiers |= ShortcutModifiers.Control;
        if (args.Shift) modifiers |= ShortcutModifiers.Shift;
        if (args.Alt) modifiers |= ShortcutModifiers.Alt;
        if (_allowWin && IsWindowsKeyDown()) modifiers |= ShortcutModifiers.Win;
        _shortcut = new Shortcut(key, modifiers);
        _dialog.EndModal(AcceptId);
    }

    /// <summary>wxMSW fills only shift, control and alt on a key event, so the Windows key is invisible to it
    /// and has to be read from the platform. Asked once, as a key arrives - this is not polling.</summary>
    private static bool IsWindowsKeyDown()
        => (GetAsyncKeyState(VirtualKeyLeftWindows) & 0x8000) != 0
            || (GetAsyncKeyState(VirtualKeyRightWindows) & 0x8000) != 0;

    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int virtualKey);
}
