using LunaPlayer.Application;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Servers;
using WxSharp;
using Queue = LunaPlayer.Downloads.DownloadQueue;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog : IDisposable
{
    private readonly Dialog _window;
    private readonly Connections _connections;
    private readonly Player _player;
    private readonly LunaPlayer.Configuration.Models.ShortcutSettings _shortcutSettings;
    private readonly LunaPlayer.Actions.ShortcutManager _shortcuts = new(LunaPlayer.Actions.MusicServerActions.All);
    private readonly IApplicationDispatcher _dispatcher;
    private readonly IDialogService _dialogs;
    private readonly Queue _downloads;
    private readonly Action _showQueue;
    private readonly Action<string> _speak;
    // UI construction steps assign these controls before enabling input and binding their handlers.
    private Choice _servers = null!, _types = null!, _libraries = null!, _sort = null!, _direction = null!;
    private TextCtrl _search = null!;
    private TreeCtrl _tree = null!;
    private ListCtrl _list = null!;
    private StaticText _resultsLabel = null!;
    private Button _back = null!, _play = null!, _append = null!;
    private readonly Panel _panel;
    private readonly BoxSizer _layout;
    private readonly Startup _startup;
    private readonly SongActions _songs;
    private bool _initialized;
    internal bool IsDisposed => _disposed;
    private readonly Dictionary<TreeItemId, Category> _categories = [];
    private readonly HashSet<(Guid Server, Category Category)> _unsupported = [];
    private readonly Dictionary<Guid, Place> _memory = [];
    private readonly List<IDisposable> _editors = [];
    private readonly Services.Lifetime _lifetime = new();
    private CancellationTokenSource _reading = new();
    private readonly CancellationToken _token;
    private IDisposable? _debounce;
    private readonly List<Place> _history = [];
    private Func<CancellationToken, Task<Row[]>>? _derived;
    private DateTimeOffset? _hiddenAt;
    private int _connectionRevision;
    private Row[] _rows = [], _view = [];
    private Column[] _columns = [];
    private Profile[] _profiles = [];
    private Profile? _activeProfile;
    private string[] _libraryIds = [];
    private Guid _server;
    private Location _location = new(Category.Artists);
    private bool _binding, _visible, _disposed;
    private int _revision, _minimumYear, _maximumYear;
    private string _query = "";
    private sealed record Place(Location Location, string Query, int Type, int Sort, int Direction, string? Library,
        Func<CancellationToken, Task<Row[]>>? Derived = null);
    internal BrowserDialog(Connections connections, Player player, IApplicationDispatcher dispatcher,
        IDialogService dialogs, Queue downloads, Action showQueue, Action<string> speak, LunaPlayer.Configuration.Models.ShortcutSettings shortcuts)
    {
        _token = _lifetime.Token;
        _connections = connections; _player = player; _dispatcher = dispatcher; _dialogs = dialogs;
        _downloads = downloads; _showQueue = showQueue; _speak = speak; _shortcutSettings = shortcuts;
        _window = new Dialog(null, title: Words.Title, size: new Size(1120, 740),
            style: DialogStyle.Default | DialogStyle.ResizeBorder | DialogStyle.NoParent);
        _songs = new SongActions(_window, connections, player, dispatcher, downloads, dialogs, speak);
        _panel = new Panel(_window) { Enabled = false };
        _panel.Hide();
        _layout = new BoxSizer(Orientation.Vertical); _panel.SetSizer(_layout);
        var outer = new BoxSizer(Orientation.Vertical); outer.Add(_panel, proportion: 1, flags: SizerFlags.Expand); _window.SetSizer(outer);
        _window.MinSize = new Size(900, 620); _window.SetEscapeId(StandardId.Cancel);
        _startup = new Startup(dispatcher, () => _visible && !_disposed,
            [CreateConnection, CreateSearch, CreateCollections, CreateActions, Ready], error =>
            { Dispose(); _dialogs.ShowMainError(Words.Error(error)); });
        _window.Closing += (_, args) =>
        {
            if (_disposed) { args.Skip(); return; }
            args.Veto(); if (_initialized) Remember();
            _visible = false; _hiddenAt = DateTimeOffset.UtcNow; _reading.Cancel(); _debounce?.Dispose(); _window.Hide();
        };
    }
    private void Ready()
    {
        _initialized = true; Bind();
        _connections.Metadata.Changed += MetadataChanged; _player.MediaUnavailable += Unavailable;
        _panel.Enabled = true; _panel.Show(); Profiles(); UpdateButtons();
        if (Wx.GetActiveWindow()?.NativeHandle == _window.NativeHandle) _servers.Focus();
    }

    private void Categories()
    {
        var binding = _binding; _binding = true;
        _tree.DeleteChildren(_tree.Root); _categories.Clear();
        foreach (var category in Sections.Visible(_activeProfile))
            if (!_unsupported.Contains((_server, category)))
            {
                var item = _tree.Add(_tree.Root, Sections.Name(category)); _categories.Add(item, category);
                if (category == Views.Section(_location)) _tree.Selection = item;
            }
        _binding = binding;
    }
    private static Choice Choice(Window parent, BoxSizer row, string label, string[] items)
    {
        row.Add(new StaticText(parent, label: label), flags: SizerFlags.AlignCenterVertical | SizerFlags.BorderRight, border: 5);
        var choice = new Choice(parent) { Name = label }; foreach (var item in items) choice.Add(item);
        if (items.Length > 0) choice.SelectedIndex = 0;
        row.Add(choice, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderRight, border: 8); return choice;
    }
    private static Button Button(Window parent, BoxSizer row, string label, Action action)
    { var button = new Button(parent, label: label); button.Click += (_, _) => action(); row.Add(button, flags: SizerFlags.BorderRight, border: 6); return button; }
    private void ConfigureColumns()
    {
        var columns = Columns.For(_location, _view, _query.Length > 0);
        if (_columns.SequenceEqual(columns)) return;
        _list.ClearAll(); _columns = columns;
        for (var index = 0; index < columns.Length; index++)
            _list.InsertColumn(index, Columns.Heading(columns[index]), Columns.Width(columns[index]));
    }
    private string Cell(Row row, int column) => column >= 0 && column < _columns.Length ? Columns.Cell(row, _columns[column]) : "";
    private Row[] Selected => _list.GetSelectedIndices().Where(x => x >= 0 && x < _view.Length).Select(x => _view[(int)x]).ToArray();
    private void UpdateButtons()
    {
        var selected = Selected;
        var can = selected.Length > 0 && _server != Guid.Empty && Allows(Command.Play, Context(selected, serverActions: false));
        _back.Show(_history.Count > 0);
        _play.Show(can); _append.Show(can); _window.Layout();
    }
    private void Keep(IDisposable editor, Form form)
    { _editors.Add(editor); form.Closed += _ => { _editors.Remove(editor); editor.Dispose(); }; }
    internal void Show()
    {
        if (_disposed) return;
        if (!_window.Enabled)
        {
            var child = _editors.Select(x => x is Form form ? form.Window : x is ProfileEditor editor ? editor.Window : null)
                .LastOrDefault(x => x?.Enabled == true);
            child?.Raise(); return;
        }
        var stale = !_visible && _hiddenAt is { } hidden && DateTimeOffset.UtcNow - hidden > TimeSpan.FromMinutes(30);
        _visible = true; _window.Show(); _window.Raise();
        if (!_initialized) { _startup.Start(); return; }
        Profiles();
        if (stale && _rows.Length > 0) Refresh();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _connections.Metadata.Changed -= MetadataChanged;
        _player.MediaUnavailable -= Unavailable;
        _disposed = true; _visible = false; _startup.Dispose(); _songs.Dispose(); _lifetime.Dispose(); _reading.Cancel(); _debounce?.Dispose();
        foreach (var editor in _editors.AsEnumerable().Reverse().ToArray()) editor.Dispose(); _editors.Clear();
        _window.Dispose(); _reading.Dispose();
    }
}
