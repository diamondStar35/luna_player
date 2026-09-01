using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed partial class MainFrame : IMainView
{

    private readonly Frame _frame;
    private readonly CustomButton _playButton;
    private readonly Dictionary<ActionId, int> _commandIds = [];
    private readonly Dictionary<int, ActionId> _commands = [];
    private readonly List<int> _ownedCommandIds = [];
    private readonly List<MenuItem> _playbackItems = [];
    private readonly List<MenuItem> _mediaFileItems = [];
    private readonly List<MenuItem> _localFileItems = [];
    private readonly List<MenuItem> _markedItems = [];
    private readonly List<MenuItem> _localEditItems = [];
    private readonly List<MenuItem> _bookmarkItems = [];
    private readonly MenuBar _menuBar;
    private readonly int _bookmarksMenuIndex;
    private readonly int _markedMenuIndex;
    private readonly MenuItem _markCurrentItem;
    private readonly MenuItem _markAllItem;
    private readonly MenuItem _shuffleItem;
    private readonly MenuItem _repeatFileItem;
    private readonly MenuItem _silenceRemovalItem;
    private readonly GlobalShortcuts _globalShortcuts;
    private bool _disposed;

    internal MainFrame(ShortcutManager shortcuts, IReadOnlyList<ActionDefinition> actions)
    {
        _frame = new Frame(title: "Luna Player", size: new Size(420, 160));
        BuildCommandIds(actions);
        var menu = MainMenuBuilder.Build(_frame, _commandIds, shortcuts);
        _menuBar = menu.MenuBar;
        _bookmarksMenuIndex = menu.BookmarksMenuIndex;
        _markedMenuIndex = menu.MarkedMenuIndex;
        _playbackItems.AddRange(menu.PlaybackItems);
        _mediaFileItems.AddRange(menu.MediaFileItems);
        _localFileItems.AddRange(menu.LocalFileItems);
        _markedItems.AddRange(menu.MarkedItems);
        _localEditItems.AddRange(menu.LocalEditItems);
        _bookmarkItems.AddRange(menu.BookmarkItems);
        _markCurrentItem = menu.MarkCurrentItem;
        _markAllItem = menu.MarkAllItem;
        _shuffleItem = menu.ShuffleItem;
        _repeatFileItem = menu.RepeatFileItem;
        _silenceRemovalItem = menu.SilenceRemovalItem;
        BuildAccelerators(shortcuts);

        var previousButton = new CustomButton(_frame, Tr("Previous"));
        var rewindButton = new CustomButton(_frame, Tr("Rewind"));
        _playButton = new CustomButton(_frame, Tr("Play"));
        var forwardButton = new CustomButton(_frame, Tr("Forward"));
        var nextButton = new CustomButton(_frame, Tr("Next"));

        var buttonSizer = new BoxSizer(Orientation.Horizontal);
        buttonSizer.Insert(0, previousButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(rewindButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(_playButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(forwardButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(nextButton, flags: SizerFlags.All, border: 5);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(buttonSizer, flags: SizerFlags.AlignCenterHorizontal | SizerFlags.All, border: 15);
        _frame.SetSizer(sizer);
        _frame.Fit();

        previousButton.Click += (_, _) => Request(ActionId.PreviousTrack);
        rewindButton.Click += (_, _) => Request(ActionId.SeekBackward);
        _playButton.Click += (_, _) => Request(ActionId.PlayPause);
        forwardButton.Click += (_, _) => Request(ActionId.SeekForward);
        nextButton.Click += (_, _) => Request(ActionId.NextTrack);
        _globalShortcuts = new GlobalShortcuts();
        _globalShortcuts.Pressed += Request;
        _frame.MenuCommand += OnMenuCommand;
        _frame.Closing += OnClosing;
        SetMediaLoaded(false);
    }

    public event Action<ActionId>? ActionRequested;
    public event Action? CloseRequested;

    public nint NativeHandle => _frame.NativeHandle;

    public void Show() => _frame.Show();

    public void Close() => _frame.Close();

    public void RestoreAndRaise()
    {
        _frame.Iconize(false);
        _frame.Raise();
    }

    public void SetPlaying(bool isPlaying) => _playButton.Label = isPlaying ? Tr("Pause") : Tr("Play");

    public void SetShuffleChecked(bool isChecked) => _shuffleItem.Checked = isChecked;

    public void SetRepeatFileChecked(bool isChecked) => _repeatFileItem.Checked = isChecked;

    public void SetSilenceRemovalChecked(bool isChecked) => _silenceRemovalItem.Checked = isChecked;

    public bool ApplyGlobalShortcuts(ShortcutManager shortcuts)
        => _globalShortcuts.Apply(shortcuts.GetBindings());

    public void ApplyShortcuts(ShortcutManager shortcuts)
    {
        BuildAccelerators(shortcuts);
        foreach (var pair in _commandIds)
        {
            var item = _menuBar.FindItem(pair.Value);
            if (item is null || item.Kind == MenuItemKind.Separator) continue;
            var baseLabel = item.Label.Split('\t')[0];
            item.Label = shortcuts.Get(pair.Key) is Shortcut shortcut
                ? $"{baseLabel}\t{shortcut.ToDisplayString()}"
                : baseLabel;
        }
    }

    public void SetMediaLoaded(bool loaded)
    {
        foreach (var item in _playbackItems)
            item.Enabled = loaded;
        foreach (var item in _mediaFileItems)
            item.Enabled = loaded;
    }

    public void SetEditState(bool hasLocalFile, bool hasMedia)
    {
        foreach (var item in _localEditItems)
            item.Enabled = hasLocalFile;
        foreach (var item in _localFileItems)
            item.Enabled = hasLocalFile;
        _markCurrentItem.Enabled = hasMedia;
        _markAllItem.Enabled = hasMedia;
    }

    public void SetBookmarkState(bool enabled)
    {
        foreach (var item in _bookmarkItems)
            item.Enabled = enabled;
        _menuBar.EnableTop(_bookmarksMenuIndex, enabled);
    }

    public void SetMarkState(bool currentMarked, bool allMarked)
    {
        _markCurrentItem.Checked = currentMarked;
        _markAllItem.Checked = allMarked;
    }

    public void SetMarkedActionsEnabled(bool enabled)
    {
        foreach (var item in _markedItems) item.Enabled = enabled;
        _menuBar.EnableTop(_markedMenuIndex, enabled);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        // Before the frame goes: a keyboard hook left installed keeps being called into a process that is
        // shutting down.
        _globalShortcuts.Dispose();
        _frame.Dispose();
        foreach (var id in _ownedCommandIds)
            IdManager.Release(id);
        _ownedCommandIds.Clear();
    }

    private void BuildCommandIds(IEnumerable<ActionDefinition> actions)
    {
        foreach (var action in actions)
        {
            if (action.Id == ActionId.OpenFile)
                AddCommand(action.Id, StandardId.Open, owned: false);
            else if (action.Id == ActionId.Exit)
                AddCommand(action.Id, StandardId.Exit, owned: false);
            else
                AddCommand(action.Id, IdManager.NewId(), owned: true);
        }
    }

    private void AddCommand(ActionId action, int id, bool owned)
    {
        _commandIds[action] = id;
        _commands[id] = action;
        if (owned)
            _ownedCommandIds.Add(id);
    }

    private void BuildAccelerators(ShortcutManager shortcuts)
    {
        var accelerators = new List<AcceleratorEntry>();
        foreach (var binding in shortcuts.GetBindings())
        {
            // An accelerator table cannot express the Windows key, so a binding using it is left to the
            // global hot key registration instead of failing the whole table.
            if ((binding.Shortcut.Modifiers & ShortcutModifiers.Win) != 0) continue;
            var commandId = _commandIds[binding.Action];
            var display = binding.Shortcut.ToDisplayString();
            if (!AcceleratorEntry.TryParse(display, commandId, out var accelerator))
                throw new InvalidOperationException($"'{display}' is not a valid local shortcut.");
            accelerators.Add(accelerator);
        }
        _frame.SetAcceleratorTable([.. accelerators]);
    }

    private void OnMenuCommand(object? sender, CommandEventArgs args)
    {
        if (_commands.TryGetValue(args.Id, out var action))
            Request(action);
    }

    private void OnClosing(object? sender, CloseEventArgs args)
    {
        CloseRequested?.Invoke();
        args.Skip();
    }

    private void Request(ActionId action) => ActionRequested?.Invoke(action);
}
