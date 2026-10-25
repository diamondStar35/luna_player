using LunaPlayer.Application;
using LunaPlayer.Radio;
using WxSharp;

namespace LunaPlayer.UI.Radio;

/// <summary>A hierarchical radio directory and one virtual station list, retained between visits.</summary>
internal sealed partial class BrowserDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly Updates _updates;
    private readonly Browsing _state;
    private readonly Catalog _catalog;
    private readonly Library _library;
    private readonly RowText _text;
    private readonly Action<string> _speak;
    private readonly IClipboardService _clipboard;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly TextCtrl _search;
    private readonly Button _searchButton;
    private readonly Button _filters;
    private readonly TreeCtrl _tree;
    private readonly ListCtrl _list;
    private readonly Button _play;
    private readonly Button _details;
    private readonly Menu _actions = new();
    private readonly Menu _treeActions = new();
    private readonly MenuItem _clearHistoryAction;
    private readonly MenuItem _favoriteAction;
    private readonly MenuItem _websiteAction;
    private readonly MenuItem _copyUrlAction;
    private readonly Dictionary<TreeItemId, Category> _categories = [];
    private readonly Dictionary<Category, TreeItemId> _nodes = [];
    private readonly Dictionary<Category, Results> _sources = [];
    private Results _results;
    private CancellationTokenSource? _request;
    private bool _visible;
    private volatile bool _disposed;
    private bool _changingTree;
    private bool _binding;
    private bool _bound;
    private int _selectedRow = -1;
    private bool _pageQueued;
    private bool _rowsChanged;
    private bool _rowValuesChanged;
    private bool _canPlay;
    private bool _canShowDetails;
    private string _searchText = string.Empty;
    private bool _clearSearch;
    private bool _favoriteBusy;
    private bool _voting;
    private bool _libraryWarningShown;
    private Exception? _pendingError;
    private Guid? _restoreStation;
    private StationView? _choice;
    private Station? Selected => _selectedRow is var index && index >= 0 && index < _results.Rows.Count
        ? _results.Rows[index].Station : null;

    internal BrowserDialog(Window parent, IApplicationDispatcher dispatcher, Browsing state,
        Catalog catalog, Library library, Action<string> speak, IClipboardService clipboard)
    {
        _dispatcher = dispatcher;
        _updates = new Updates(dispatcher, RenderRows, ShowError);
        _state = state;
        _catalog = catalog;
        _library = library;
        _text = new RowText(library);
        _speak = speak;
        _clipboard = clipboard;
        _selectedCategory = state.ActiveCategory;
        _results = Source(state.ActiveCategory);
        // Translators: Title of the window for browsing internet radio stations.
        _dialog = new Dialog(parent, title: Tr("Radio browser"), size: new Size(960, 600),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var panel = new Panel(_dialog);
        var layout = new BoxSizer(Orientation.Vertical);
        var search = new BoxSizer(Orientation.Horizontal);
        // Translators: Label of the radio browser box for searching the station list.
        search.Add(new StaticText(panel, label: Tr("Search")), flags: SizerFlags.AlignCenterVertical | SizerFlags.BorderRight, border: 6);
        _search = new TextCtrl(panel, style: TextCtrlStyle.ProcessEnter);
        search.Add(_search, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderRight, border: 6);
        // Translators: Button searching the current radio directory or station list.
        _searchButton = new Button(panel, label: Tr("Search"));
        search.Add(_searchButton, flags: SizerFlags.BorderRight, border: 6);
        // Translators: Button opening radio filters for this category or every category.
        _filters = new Button(panel, label: Tr("Filters…"));
        search.Add(_filters);
        layout.Add(search, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        var body = new BoxSizer(Orientation.Horizontal);
        var navigation = new BoxSizer(Orientation.Vertical);
        // Translators: Label above the radio browser's navigation tree.
        navigation.Add(new StaticText(panel, label: Tr("Categories")), flags: SizerFlags.BorderBottom, border: 4);
        _tree = new TreeCtrl(panel, style: TreeCtrlStyle.Default | TreeCtrlStyle.HideRoot | TreeCtrlStyle.HasButtons);
        _tree.MinSize = new Size(230, 250);
        // Translators: Accessible name of the radio browser's navigation tree.
        _tree.Name = Tr("Categories");
        navigation.Add(_tree, proportion: 1, flags: SizerFlags.Expand);
        body.Add(navigation, flags: SizerFlags.Expand | SizerFlags.BorderRight, border: 8);
        var results = new BoxSizer(Orientation.Vertical);
        // Translators: Label above the virtual radio station list.
        results.Add(new StaticText(panel, label: Tr("Stations")), flags: SizerFlags.BorderBottom, border: 4);
        // A plain virtual ListCtrl uses its data callback directly, without window-override dispatch.
        _list = new ListCtrl(panel, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual | ListCtrlStyle.SingleSelection);
        // Translators: Accessible name of the virtual radio station list.
        _list.Name = Tr("Stations");
        // Translators: Column heading for a radio station's name.
        _list.InsertColumn(0, Tr("Name"), 270);
        // Translators: Column heading for a radio station's country.
        _list.InsertColumn(1, Tr("Country"), 125);
        // Translators: Column heading for the languages spoken by a radio station.
        _list.InsertColumn(2, Tr("Language"), 125);
        // Translators: Column heading for a radio stream's audio codec.
        _list.InsertColumn(3, Tr("Codec"), 80);
        // Translators: Column heading for a radio stream's bitrate in kilobits per second.
        _list.InsertColumn(4, Tr("Bitrate"), 90);
        results.Add(_list, proportion: 1, flags: SizerFlags.Expand);
        var stationButtons = new BoxSizer(Orientation.Horizontal);
        // Translators: Button playing the selected radio station.
        _play = new Button(panel, label: Tr("Play"));
        // Translators: Button opening details of the selected radio station.
        _details = new Button(panel, label: Tr("Station details"));
        _play.Enabled = false;
        _details.Enabled = false;
        stationButtons.Add(_play, flags: SizerFlags.BorderRight, border: 6);
        stationButtons.Add(_details);
        results.Add(stationButtons, flags: SizerFlags.BorderTop, border: 6);
        body.Add(results, proportion: 1, flags: SizerFlags.Expand);
        layout.Add(body, proportion: 1, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.Expand, border: 8);
        var buttons = new BoxSizer(Orientation.Horizontal);
        // Translators: Button reloading the current radio directory or station list.
        var refresh = new Button(panel, label: Tr("Refresh"));
        // Translators: Button closing the radio browser without choosing a station.
        var close = new Button(panel, StandardId.Cancel, Tr("Close"));
        buttons.Add(refresh);
        buttons.AddStretchSpacer();
        buttons.Add(close);
        layout.Add(buttons, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        panel.SetSizer(layout);
        var outer = new BoxSizer(Orientation.Vertical);
        outer.Add(panel, proportion: 1, flags: SizerFlags.Expand);
        _dialog.SetSizer(outer);
        _dialog.MinSize = new Size(760, 460);
        _dialog.Center();

        AddAction(_actions, Play,
            // Translators: Radio station context-menu action that starts playback.
            Tr("Play"));
        AddAction(_actions, Details,
            // Translators: Radio station context-menu action showing station metadata.
            Tr("Station details"));
        _favoriteAction = AddAction(_actions, ToggleFavorite,
            // Translators: Radio station context-menu action saving the selected station locally.
            Tr("Add to favorites"));
        _websiteAction = AddAction(_actions, OpenWebsite,
            // Translators: Radio station context-menu action opening the broadcaster's website.
            Tr("Open website"));
        _copyUrlAction = AddAction(_actions, CopyUrl,
            // Translators: Radio station context-menu action copying the stream address.
            Tr("Copy stream URL"));
        AddAction(_actions, CopyName,
            // Translators: Radio station context-menu action copying the station's name.
            Tr("Copy station name"));
        AddAction(_actions, Vote,
            // Translators: Radio station context-menu action sending one public vote to Radio Browser.
            Tr("Vote for station"));
        AddAction(_treeActions, Refresh,
            // Translators: Radio category context-menu action reloading its contents.
            Tr("Refresh"));
        _clearHistoryAction = AddAction(_treeActions, ClearHistory,
            // Translators: Radio category context-menu action deleting this user's listening history.
            Tr("Clear history…"));
        BuildTree();
        BindEvents();
        refresh.Click += (_, _) => Refresh();
        _filters.Click += (_, _) => EditFilters();
        _play.Click += (_, _) => Play();
        _details.Click += (_, _) => Details();
    }

    internal StationView? Show(bool returnToStation)
    {
        _choice = null;
        _visible = true;
        try
        {
            var restore = returnToStation ? _state.GetView(_state.ActiveCategory).Selected : null;
            var category = returnToStation ? _state.ActiveCategory : _selectedCategory;
            Navigate(category, load: !category.IsContainer);
            RenderRows();
            _restoreStation = restore;
            RestoreStation();
            SelectTreeRoot();
            ContinueDirectory();
            if (returnToStation) _list.Focus(); else _search.Focus();
            if (_pendingError is { } error)
            {
                _pendingError = null;
                _dispatcher.Post(() => ShowError(error));
            }
            _dialog.ShowModal();
            return _choice;
        }
        finally
        {
            _visible = false;
            CancelRead();
            CancelDirectory();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelRead();
        CancelDirectory();
        _updates.Dispose();
        _actions.Dispose();
        _treeActions.Dispose();
        _dialog.Dispose();
        _ = CancelAsync(_lifetime);
    }
}
