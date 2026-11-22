using LunaPlayer.Subsonic.Browsing;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void Bind()
    {
        _servers.SelectionChanged += (_, _) => { if (!_binding) SelectServer(); };
        _tree.SelectionChanged += (_, _) =>
        { if (!_binding && _categories.TryGetValue(_tree.Selection, out var category)) Navigate(new(category)); };
        _tree.ItemActivated += (_, _) =>
        {
            if (!_binding && _categories.TryGetValue(_tree.Selection, out var category)
                && (_location.Container is not null || _derived is not null || _search.Value.Trim().Length > 0)) Navigate(new(category));
        };
        _search.TextChanged += (_, _) =>
        {
            if (_binding) return;
            if (_derived is not null) { _history.Add(Capture()); _derived = null; }
            _debounce?.Dispose(); _reading.Cancel(); _revision++; _sortRevision++;
            _debounce = _dispatcher.Repeat(TimeSpan.FromMilliseconds(300), () => { _debounce?.Dispose(); _debounce = null; Reload(); });
        };
        _search.EnterPressed += (_, _) => { Reload(); _list.Focus(); };
        _types.SelectionChanged += (_, _) => { if (!_binding) { _derived = null; Reload(); } };
        _libraries.SelectionChanged += (_, _) => { if (!_binding) { _derived = null; Reload(); } };
        _sort.SelectionChanged += (_, _) => Sort(); _direction.SelectionChanged += (_, _) => Sort();
        _list.ItemSelected += (_, _) => UpdateButtons(); _list.ItemDeselected += (_, _) => UpdateButtons();
        _list.ItemActivated += (_, _) => Open(); _list.Bind(WxEvents.ContextMenu, (_, _) => Menu(true));
        _tree.Bind(WxEvents.ContextMenu, (_, _) => Menu(true, _tree));
        _window.Bind(WxEvents.CharHook, (_, key) =>
        {
            if (Shortcut(key, false)) return;
            else if (key.Alt && key.Code is (Key)'B' or (Key)'b') _sort.Focus();
            else if (key.Alt && key.Code is (Key)'O' or (Key)'o') _direction.Focus();
            else if (key.Code == Key.F1) Info(Words.HelpText);
            else if (key.Code == Key.Escape) _window.Close();
            else key.Skip();
        });
        _list.Bind(WxEvents.CharHook, (_, key) =>
        {
            if (Shortcut(key, true)) return;
            else if (key.Code is Key.Delete or Key.NumpadDelete) Execute(Command.Remove);
            else if (key.Code == Key.F2) Execute(Command.Rename);
            else key.Skip();
        });
    }
    private bool Shortcut(KeyEventArgs key, bool items)
    {
        _shortcuts.Apply(_shortcutSettings.MusicServers, new Dictionary<LunaPlayer.Actions.ActionId, LunaPlayer.Actions.Shortcut>());
        var value = LunaPlayer.Actions.ShortcutKeys.NameOf(key.Code, (int)key.Code);
        if (value is null) return false;
        var modifiers = (key.Control ? LunaPlayer.Actions.ShortcutModifiers.Control : 0)
            | (key.Shift ? LunaPlayer.Actions.ShortcutModifiers.Shift : 0) | (key.Alt ? LunaPlayer.Actions.ShortcutModifiers.Alt : 0);
        var binding = _shortcuts.GetBindings().FirstOrDefault(x => x.Shortcut == new LunaPlayer.Actions.Shortcut(value, modifiers));
        if (binding == default) return false;
        switch (binding.Action)
        {
            case LunaPlayer.Actions.ActionId.ServerSearch: _search.Focus(); return true;
            case LunaPlayer.Actions.ActionId.ServerRefresh: Refresh(); return true;
            case LunaPlayer.Actions.ActionId.ServerBack: Back(); return true;
            case LunaPlayer.Actions.ActionId.ServerQueue: _showQueue(); return true;
            case LunaPlayer.Actions.ActionId.ServerPlay when items: Open(); return true;
            case LunaPlayer.Actions.ActionId.ServerDetails when items:
                Execute(Bindings.Inspect(Selected)); return true;
            case LunaPlayer.Actions.ActionId.ServerRate when items: Execute(Command.Rate); return true;
            case LunaPlayer.Actions.ActionId.ServerLyrics when items: Execute(Command.Lyrics); return true;
            case LunaPlayer.Actions.ActionId.ServerBookmark: Execute(Command.Bookmark); return true;
            case LunaPlayer.Actions.ActionId.ServerCreatePlaylist: Execute(Command.CreatePlaylist); return true;
            case LunaPlayer.Actions.ActionId.ServerAppend when items: Execute(Command.Append); return true;
            case LunaPlayer.Actions.ActionId.ServerDirectDownload when items: Execute(Command.Direct); return true;
            case LunaPlayer.Actions.ActionId.ServerDownload when items: Execute(Command.Download); return true;
            case LunaPlayer.Actions.ActionId.ServerEnqueue when items: Execute(Command.Enqueue); return true;
            default: return false;
        }
    }
}
