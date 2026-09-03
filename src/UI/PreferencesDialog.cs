using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class PreferencesDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly TreeCtrl _tree;
    private readonly Panel _book;
    private readonly Dictionary<TreeItemId, IPreferences> _pages = [];
    private readonly IReadOnlyList<IPreferences> _allPages;
    private readonly Action<string> _speakHelp;
    private readonly PlayerSettings _settings;
    private readonly PrefsOps _operations;
    private IPreferences _current;

    internal PreferencesDialog(Window parent, PlayerSettings settings, PrefsOps operations, Action<string> speakHelp,
        GlobalShortcuts? globalShortcuts = null)
    {
        _settings = settings;
        _operations = operations;
        _speakHelp = speakHelp;
        // Translators: Title of the window holding every setting of the player.
        _dialog = new Dialog(parent, title: Tr("Preferences"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _tree = new TreeCtrl(_dialog, style: TreeCtrlStyle.HideRoot | TreeCtrlStyle.Default | TreeCtrlStyle.HasButtons);
        _book = new Panel(_dialog);
        var general = new GeneralPreferences(_book, settings.General, operations);
        var backup = new BackupPreferences(_book, operations, ReplaceSettings);
        var audio = new AudioPreferences(_book, settings.Audio);
        var silence = new SilencePreferences(_book, settings.Silence);
        var youTube = new YouTube.Preferences(_book, settings.YouTube, operations);
        var shortcuts = new ShortcutPreferences(_book, settings.Shortcuts, ShortcutScope.Local, globalShortcuts);
        var globals = new ShortcutPreferences(_book, settings.Shortcuts, ShortcutScope.Global, globalShortcuts);
        _allPages = [general, backup, audio, silence, youTube, shortcuts, globals];
        _current = general;

        string[] categories = [
            // Translators: Name of the settings category holding the language, speech and file-opening settings.
            Tr("General"),
            // Translators: Name of the settings category for saving a copy of the settings and bookmarks and putting them back.
            Tr("Backup and restore"),
            // Translators: Name of the settings category holding the loudness, speed and seeking settings.
            Tr("Audio"),
            // Translators: Name of the settings category for trimming the silent parts out of what is played.
            Tr("Silence removal"),
            // Translators: Name of the settings category for playing videos from YouTube.
            Tr("YouTube"),
            // Translators: Name of the settings category for the key combinations that work while the player is the program in front.
            Tr("Keyboard Shortcuts"),
            // Translators: Name of the settings category for the key combinations that work while another program is in front.
            Tr("Global Shortcuts")];
        var root = _tree.AddRoot("root");
        var generalItem = _tree.Add(root, categories[0]);
        var backupItem = _tree.Add(root, categories[1]);
        var audioItem = _tree.Add(root, categories[2]);
        var silenceItem = _tree.Add(root, categories[3]);
        var youTubeItem = _tree.Add(root, categories[4]);
        var shortcutsItem = _tree.Add(root, categories[5]);
        var globalsItem = _tree.Add(root, categories[6]);
        SizeTreeToLabels(categories);
        _pages[generalItem] = general;
        _pages[backupItem] = backup;
        _pages[audioItem] = audio;
        _pages[silenceItem] = silence;
        _pages[youTubeItem] = youTube;
        _pages[shortcutsItem] = shortcuts;
        _pages[globalsItem] = globals;
        _tree.Selection = generalItem;

        var bookSizer = new BoxSizer(Orientation.Vertical);
        foreach (var page in _allPages)
        {
            bookSizer.Add(page.Window, proportion: 1, flags: SizerFlags.Expand);
            page.Window.Show(page == _current);
        }
        _book.SetSizer(bookSizer);
        _tree.SelectionChanged += OnTreeChanged;
        _dialog.Bind(WxEvents.CharHook, OnCharHook);

        var body = new BoxSizer(Orientation.Horizontal);
        body.Add(_tree, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        body.Add(_book, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        var main = new BoxSizer(Orientation.Vertical);
        main.Add(body, proportion: 1, flags: SizerFlags.Expand);
        var buttons = _dialog.CreateButtonSizer(ButtonSizerFlags.OkCancel);
        if (buttons is not null)
            main.Add(buttons, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        SizeBookToLargestPage();
        _dialog.SetSizer(main);
        _dialog.Fit();
        _dialog.MinSize = new Size(420, 260);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.ButtonClicked, OnAccept, StandardId.Ok);

        // CreateButtonSizer makes OK the default button, which sends DM_SETDEFID to the dialog. wx dialogs
        // are real #32770 windows on MSW, so DefDlgProc then hands the initial focus to that default button
        // and a screen reader announces OK instead of the category tree. Claim the focus back, the same way
        // NVDA's own settings dialog does in postInit().
        _tree.Focus();
    }

    internal bool Show() => _dialog.ShowModal() == StandardId.Ok;
    public void Dispose() => _dialog.Dispose();

    // A wxTreeCtrl's best size is a fixed default rather than a measurement of its items, so the category
    // names would be truncated behind a horizontal scrollbar. Widen it to the longest label.
    private void SizeTreeToLabels(IEnumerable<string> labels)
    {
        var widest = 0;
        foreach (var label in labels)
            widest = Math.Max(widest, _tree.GetTextExtent(label).Size.Width);
        if (widest > 0)
            _tree.MinSize = new Size(widest + Indent, _tree.MinSize.Height);
    }

    // Room for the tree's expand button, item indent and a vertical scrollbar.
    private const int Indent = 48;

    // Every page shares one stacked sizer with all but the current one hidden, and a hidden item adds
    // nothing to that sizer's minimum. A ScrolledWindow also reports almost no best size of its own, so
    // Fit() would size the dialog to whatever page happens to be showing and leave the taller pages
    // clipped behind scrollbars - which cuts the silence group box in half. Reserve the largest page's
    // requirement up front so every page gets the room it asked for.
    private void SizeBookToLargestPage()
    {
        var required = new Size(0, 0);
        foreach (var page in _allPages)
        {
            var size = page.Window.GetSizer()?.MinSize ?? page.Window.BestSize;
            // A page that starts with part of itself collapsed publishes its full requirement as MinSize.
            var declared = page.Window.MinSize;
            required = new Size(
                Math.Max(required.Width, Math.Max(size.Width, declared.Width)),
                Math.Max(required.Height, Math.Max(size.Height, declared.Height)));
        }
        if (required.Width > 0 && required.Height > 0)
            _book.MinSize = required;
    }

    private void OnTreeChanged(object? sender, TreeEventArgs args)
    {
        if (!_pages.TryGetValue(args.Item, out var page) || page == _current) return;
        _current.Window.Show(false);
        page.Window.Show();
        _current = page;
        _book.Layout();
        args.Skip();
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code != Key.F1)
        {
            args.Skip();
            return;
        }

        var focused = Window.FindFocus();
        if (ReferenceEquals(focused, _tree))
        {
            // Translators: Help text for the list of settings categories down the left of the Preferences window. General
            // and Audio are two of those categories and should read the same here as they do there.
            _speakHelp(Tr("Settings categories tree. Use up and down arrows to choose a category like General or Audio."));
            return;
        }

        var message = _current.GetContextHelp(focused);
        _speakHelp(string.IsNullOrWhiteSpace(message)
            // Translators: Spoken when the user asks for help on a control that has none.
            ? Tr("No detailed help is available for this control.")
            : message);
    }

    private void OnAccept(object? sender, CommandEventArgs args)
    {
        foreach (var page in _allPages)
        {
            var error = page.Validate();
            if (string.IsNullOrEmpty(error)) continue;
            Wx.MessageBox(error, Tr("Preferences"), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
            return;
        }
        foreach (var page in _allPages) page.Apply();
        _dialog.EndModal(StandardId.Ok);
    }

    private void ReplaceSettings(PlayerSettings replacement)
    {
        _settings.Apply(replacement);
        foreach (var page in _allPages) page.Refresh();
        _operations.ApplyImmediate(_settings.Copy());
    }
}
