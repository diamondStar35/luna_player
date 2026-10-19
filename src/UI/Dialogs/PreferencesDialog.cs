using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class PreferencesDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly TreeCtrl _tree;
    private readonly Panel _book;
    private readonly BoxSizer _bookSizer;
    // A page is built the first time its tree item is chosen, then kept. The factory map deliberately has no
    // entry for branch items: they organize their child pages without owning a page. Opening the window
    // builds only the first page, so every other page costs nothing until it is opened.
    private readonly Dictionary<TreeItemId, Func<IPreferencesPage>> _factories = [];
    private readonly Dictionary<TreeItemId, IPreferencesPage> _pages = [];
    private readonly Action<string> _speakHelp;
    private readonly Settings _settings;
    private readonly PrefsOps _operations;
    private IPreferencesPage? _current;

    // A generous fixed size, chosen rather than measured: measuring would mean building every page up front,
    // which is the cost this lazy loading exists to avoid. A page taller or wider than this scrolls - every
    // page large enough to need it is a ScrolledWindow with a scroll rate set.
    private static readonly Size InitialSize = new(600, 540);

    /// <param name="dispatcher">Needed by the Recording page, which asks Windows what its encoders will
    /// accept and must not do that on the thread drawing this window.</param>
    internal PreferencesDialog(Window parent, Settings settings, PrefsOps operations, Action<string> speakHelp,
        LunaPlayer.Application.IApplicationDispatcher dispatcher, LunaPlayer.Recording.AudioCatalog catalog,
        GlobalShortcuts? globalShortcuts = null)
    {
        _settings = settings;
        _operations = operations;
        _speakHelp = speakHelp;
        // Translators: Title of the window holding every setting of the player.
        _dialog = new Dialog(parent, title: Tr("Preferences"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _tree = new TreeCtrl(_dialog, style: TreeCtrlStyle.HideRoot | TreeCtrlStyle.Default | TreeCtrlStyle.HasButtons);
        _book = new Panel(_dialog);
        _bookSizer = new BoxSizer(Orientation.Vertical);
        _book.SetSizer(_bookSizer);

        // Translators: Name of the settings category holding the language, speech and file-opening settings.
        var generalLabel = Tr("General");
        // Translators: Name of the settings category for saving a copy of the settings and bookmarks and putting them back.
        var backupLabel = Tr("Backup and restore");
        // Translators: Name of the settings category holding the loudness, speed and seeking settings.
        var audioLabel = Tr("Audio");
        // Translators: Name of the settings category for trimming the silent parts out of what is played.
        var silenceLabel = Tr("Silence removal");
        // Translators: Name of the expandable settings category containing the general and download pages for YouTube.
        var youTubeLabel = Tr("YouTube");
        // Translators: Name of the general settings page inside the YouTube category.
        var youTubeGeneralLabel = Tr("General");
        // Translators: Name of the download settings page inside the YouTube category.
        var youTubeDownloadLabel = Tr("Download");
        // Translators: Name of the settings category for recording sound.
        var recordingLabel = Tr("Recording");
        // Translators: Name of the settings category for the key combinations that work while the player is the program in front.
        var shortcutsLabel = Tr("Keyboard Shortcuts");
        // Translators: Name of the settings category for the key combinations that work while another program is in front.
        var globalsLabel = Tr("Global Shortcuts");

        var root = _tree.AddRoot("root");
        // AddPage binds a tree item to its lazy page factory. Branches such as YouTube use the same tree
        // structure without a factory of their own, so more nested groups can be added without changing the
        // page-switching code.
        var generalItem = AddPage(root, generalLabel,
            () => new GeneralPreferences(_book, settings.General, operations));
        AddPage(root, backupLabel, () => new BackupPreferences(_book, operations, ReplaceSettings));
        AddPage(root, audioLabel, () => new AudioPreferences(_book, settings.Audio));
        AddPage(root, silenceLabel, () => new SilencePreferences(_book, settings.Silence));
        var youTubeItem = _tree.Add(root, youTubeLabel);
        AddPage(youTubeItem, youTubeGeneralLabel,
            () => new YouTube.GeneralPreferences(_book, settings.YouTube, operations));
        AddPage(youTubeItem, youTubeDownloadLabel,
            () => new YouTube.DownloadPreferences(_book, settings.YouTube.Download));
        AddPage(root, recordingLabel,
            () => new RecordingPreferences(_book, settings.Recording, dispatcher, catalog));
        AddPage(root, shortcutsLabel,
            () => new ShortcutPreferences(_book, settings.Shortcuts, ShortcutScope.Local, globalShortcuts));
        AddPage(root, globalsLabel,
            () => new ShortcutPreferences(_book, settings.Shortcuts, ShortcutScope.Global, globalShortcuts));

        // A parent with children can otherwise retain expansion state assigned by the native control while
        // items are inserted. Preferences always opens with every optional branch closed.
        _tree.Expand(youTubeItem, expand: false);
        SizeTreeToLabels([
            generalLabel, backupLabel, audioLabel, silenceLabel, youTubeLabel,
            youTubeGeneralLabel, youTubeDownloadLabel, recordingLabel, shortcutsLabel, globalsLabel]);

        _tree.SelectionChanged += OnTreeChanged;
        _dialog.Bind(WxEvents.CharHook, OnCharHook);

        // Only the first page is built now; the rest build the first time their category is chosen.
        ShowPage(generalItem);
        _tree.Selection = generalItem;

        var body = new BoxSizer(Orientation.Horizontal);
        body.Add(_tree, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        body.Add(_book, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        var main = new BoxSizer(Orientation.Vertical);
        main.Add(body, proportion: 1, flags: SizerFlags.Expand);
        var buttons = _dialog.CreateButtonSizer(ButtonSizerFlags.OkCancel);
        if (buttons is not null)
            main.Add(buttons, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        _dialog.SetSizer(main);
        // A fixed, generous size rather than one fitted to a page: with only one page built there is nothing
        // to fit the others to, and the taller pages scroll inside this instead of forcing the dialog to grow.
        _dialog.MinSize = new Size(460, 360);
        _dialog.Size = InitialSize;
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.ButtonClicked, OnAccept, StandardId.Ok);

        // CreateButtonSizer makes OK the default button, which sends DM_SETDEFID to the dialog. wx dialogs
        // are real #32770 windows on MSW, so DefDlgProc then hands the initial focus to that default button
        // and a screen reader announces OK instead of the category tree. Claim the focus back.
        _tree.Focus();
    }

    internal bool Show() => _dialog.ShowModal() == StandardId.Ok;
    public void Dispose() => _dialog.Dispose();

    // A wxTreeCtrl's best size is a fixed default rather than a measurement of its items, so tree labels
    // would be truncated behind a horizontal scrollbar. Widen it to the longest label.
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

    /// <summary>Adds a selectable leaf and records how to create its page on the first visit.</summary>
    private TreeItemId AddPage(TreeItemId parent, string label, Func<IPreferencesPage> factory)
    {
        var item = _tree.Add(parent, label);
        _factories[item] = factory;
        return item;
    }

    /// <summary>Returns the page for a category, building it the first time and keeping it thereafter. A new
    /// page is added to the book hidden; the switch in <see cref="ShowPage"/> shows it.</summary>
    private IPreferencesPage GetOrBuildPage(TreeItemId item)
    {
        if (_pages.TryGetValue(item, out var existing))
            return existing;
        var page = _factories[item]();
        page.Window.Show(false);
        _bookSizer.Add(page.Window, proportion: 1, flags: SizerFlags.Expand);
        _pages[item] = page;
        return page;
    }

    /// <summary>Shows a chosen leaf's page, building it on first visit. Branches have no factory and leave
    /// the visible page alone. Hidden behind Freeze/Thaw so a new page is not seen half-assembled.</summary>
    private void ShowPage(TreeItemId item)
    {
        if (!_factories.ContainsKey(item))
            return;
        _book.Freeze();
        try
        {
            var page = GetOrBuildPage(item);
            if (ReferenceEquals(page, _current))
                return;
            _current?.Window.Show(false);
            page.Window.Show();
            _current = page;
            _book.Layout();
        }
        finally { _book.Thaw(); }
    }

    private void OnTreeChanged(object? sender, TreeEventArgs args)
    {
        ShowPage(args.Item);
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
            // Translators: Help text for the settings tree down the left of the Preferences window.
            _speakHelp(Tr("Use the Arrow keys to move through settings pages. Press the Right Arrow key on a category to show its pages and the Left Arrow key to hide them. The selected page's controls appear on the right side of the window."));
            return;
        }

        var message = _current?.GetContextHelp(focused);
        _speakHelp(string.IsNullOrWhiteSpace(message)
            // Translators: Spoken when the user asks for help on a control that has none.
            ? Tr("No detailed help is available for this control.")
            : message);
    }

    private void OnAccept(object? sender, CommandEventArgs args)
    {
        // Only the pages the user actually opened exist, and only they can carry an unsaved change or an
        // invalid value - a page never opened leaves its settings exactly as they were. Validate them all
        // before applying any, so a bad value stops the save with nothing half-written.
        foreach (var page in _pages.Values)
        {
            var error = page.Validate();
            if (string.IsNullOrEmpty(error)) continue;
            Wx.MessageBox(error, Tr("Preferences"), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
            return;
        }
        foreach (var page in _pages.Values) page.Apply();
        _dialog.EndModal(StandardId.Ok);
    }

    private void ReplaceSettings(Settings replacement)
    {
        _settings.Apply(replacement);
        // Only built pages need refreshing; one built later reads the replaced settings when it is made.
        foreach (var page in _pages.Values) page.Refresh();
        _operations.ApplyImmediate(_settings.Copy());
    }
}
