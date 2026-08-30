using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class ShortcutPreferences : Preferences
{
    private readonly ShortcutSettings _settings;
    private readonly ActionDefinition[] _actions = [.. ActionRegistry.All];
    private readonly Dictionary<ActionId, Shortcut> _primary = [];
    private readonly Dictionary<ActionId, Shortcut> _secondary = [];
    private readonly ListCtrl _list;
    private readonly Button _editPrimary;
    private readonly Button _editSecondary;
    private readonly Button _reset;

    internal ShortcutPreferences(Window parent, ShortcutSettings settings)
        : base(new Panel(parent), "Keyboard shortcuts. Select an action, then edit its primary or secondary local shortcut.")
    {
        _settings = settings;
        var panel = (Panel)Window;
        var heading = new StaticText(panel, label: "Local Shortcuts");
        _list = new ListCtrl(panel, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection);
        _list.InsertColumn(0, "Action", 210);
        _list.InsertColumn(1, "Primary Shortcut", 125);
        _list.InsertColumn(2, "Secondary Shortcut", 135);
        _editPrimary = new Button(panel, label: "Edit Primary Shortcut");
        _editSecondary = new Button(panel, label: "Edit Secondary Shortcut");
        _reset = new Button(panel, label: "Reset to Defaults");

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(_editPrimary, flags: SizerFlags.BorderRight, border: 6);
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
        _editSecondary.Click += (_, _) => Edit(ShortcutSlot.Secondary);
        _reset.Click += (_, _) => Reset();
        Refresh();
    }

    public override string GetContextHelp(Window? focused) => string.Empty;

    public override void Apply()
    {
        _settings.Primary = Overrides(_primary, primary: true);
        _settings.Secondary = Overrides(_secondary, primary: false);
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
        foreach (var pair in _settings.Primary)
            if (_primary.ContainsKey(pair.Key)) _primary[pair.Key] = pair.Value;
        foreach (var pair in _settings.Secondary)
            if (_secondary.ContainsKey(pair.Key)) _secondary[pair.Key] = pair.Value;
        Rebuild();
    }

    private void Edit(ShortcutSlot slot)
    {
        var index = _list.SelectedIndex;
        if (index < 0 || index >= _actions.Length) return;
        var action = _actions[(int)index];
        if (slot == ShortcutSlot.Secondary && action.SecondaryShortcut is null) return;
        using var dialog = new ShortcutCapture(Window);
        var shortcut = dialog.Show();
        if (shortcut is null) return;
        if (HasConflict(action.Id, slot, shortcut.Value))
        {
            Wx.MessageBox("That shortcut is already assigned to another action.", "Shortcut Conflict",
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, Window);
            return;
        }
        (slot == ShortcutSlot.Primary ? _primary : _secondary)[action.Id] = shortcut.Value;
        Rebuild(index);
    }

    private bool HasConflict(ActionId action, ShortcutSlot slot, Shortcut shortcut)
    {
        foreach (var pair in _primary)
            if (!(pair.Key == action && slot == ShortcutSlot.Primary) && pair.Value == shortcut) return true;
        foreach (var pair in _secondary)
            if (!(pair.Key == action && slot == ShortcutSlot.Secondary) && pair.Value == shortcut) return true;
        return false;
    }

    private void Reset()
    {
        if (!HasChangesFromDefaults()) return;
        if (Wx.MessageBox("Reset all shortcuts to defaults?", "Confirm Reset",
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
        _editSecondary.Enabled = index >= 0 && index < _actions.Length && _actions[(int)index].SecondaryShortcut is not null;
        _reset.Enabled = HasChangesFromDefaults();
    }

    private bool HasChangesFromDefaults()
        => _actions.Any(action => action.PrimaryShortcut is Shortcut primary
                && (!_primary.TryGetValue(action.Id, out var current) || current != primary))
            || _actions.Any(action => action.SecondaryShortcut is Shortcut secondary
                && (!_secondary.TryGetValue(action.Id, out var current) || current != secondary));

    private Dictionary<ActionId, Shortcut> Overrides(IReadOnlyDictionary<ActionId, Shortcut> values, bool primary)
    {
        var result = new Dictionary<ActionId, Shortcut>();
        foreach (var action in _actions)
        {
            var defaultValue = primary ? action.PrimaryShortcut : action.SecondaryShortcut;
            if (defaultValue is null || !values.TryGetValue(action.Id, out var current) || current == defaultValue.Value) continue;
            result[action.Id] = current;
        }
        return result;
    }
}

internal sealed class ShortcutCapture : IDisposable
{
    private const int AcceptId = 17031;
    private readonly Dialog _dialog;
    private Shortcut? _shortcut;

    internal ShortcutCapture(Window parent)
    {
        _dialog = new Dialog(parent, title: "Set Shortcut");
        var label = new StaticText(_dialog, label: "Press the desired shortcut. Escape cancels.");
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
        var key = KeyName(args);
        if (key is null) return;
        var modifiers = ShortcutModifiers.None;
        if (args.Control) modifiers |= ShortcutModifiers.Control;
        if (args.Shift) modifiers |= ShortcutModifiers.Shift;
        if (args.Alt) modifiers |= ShortcutModifiers.Alt;
        _shortcut = new Shortcut(key, modifiers);
        _dialog.EndModal(AcceptId);
    }

    private static string? KeyName(KeyEventArgs args)
    {
        if (args.Code is >= Key.F1 and <= Key.F24) return $"f{(int)args.Code - (int)Key.F1 + 1}";
        var named = args.Code switch
        {
            Key.Space => "space", Key.Tab => "tab", Key.Enter or Key.NumpadEnter => "enter",
            Key.Left => "left", Key.Right => "right", Key.Up => "up", Key.Down => "down",
            Key.PageUp => "page_up", Key.PageDown => "page_down", Key.Home => "home", Key.End => "end",
            Key.Back => "backspace", Key.Delete => "delete", _ => null,
        };
        if (named is not null) return named;
        var code = args.KeyCode;
        return code is >= 32 and <= 126 && code is not 127 ? char.ToLowerInvariant((char)code).ToString() : null;
    }
}
