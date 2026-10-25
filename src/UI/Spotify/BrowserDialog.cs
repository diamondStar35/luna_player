using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Spotify;
using WxSharp;

namespace LunaPlayer.UI.Spotify;

internal sealed record DialogOutcome(Track? Track = null,
    IReadOnlyList<Track>? Queue = null, Collection? Collection = null,
    string? NextCursor = null,
    Func<string?, CancellationToken, Task<TrackPage>>? LoadPage = null);

internal sealed partial class BrowserDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Window _owner;
    private readonly Catalog _catalog;
    private readonly Auth _auth;
    private readonly SpotifySettings _settings;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _token;
    private readonly StaticText _status;
    private readonly Button _signIn;
    private readonly Button _signOut;
    private readonly ListBox _homeList;
    private readonly ListBox _libraryList;
    private readonly TextCtrl _searchBox;
    private readonly Choice _searchFilter;
    private readonly ListBox _recentList;
    private readonly StaticText _searchStatus;
    private readonly List<Collection> _home = [];
    private readonly List<Collection> _library = [];
    private readonly List<string> _recent = [];
    private Collection? _collectionToOpen;
    private string? _searchToOpen;
    private SearchFilter _searchFilterToOpen;
    private bool _disposed;
    private int _libraryRequest;
    private bool _libraryLoaded;
    private bool _visible;
    private string _clientId = string.Empty;
    private TracksDialog? _tracksDialog;
    private SearchResultsDialog? _searchResults;
    private Collection? _tracksCollection;
    private string? _resultsQuery;
    private SearchFilter _resultsFilter;
    private Page _page;
    private Page _tracksParent;

    private enum Page { Home, Search, Tracks }

    internal BrowserDialog(Window parent, Catalog catalog, Auth auth, SpotifySettings settings)
    {
        _token = _lifetime.Token;
        _owner = parent;
        _catalog = catalog;
        _auth = auth;
        _settings = settings;
        // Translators: Title of the Spotify home, search and library browser. Keep Spotify unchanged.
        _dialog = new Dialog(parent, title: Tr("Spotify"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _status = new StaticText(_dialog, label: string.Empty);
        // Translators: Button in the Spotify browser that opens account authorization. Keep Spotify unchanged.
        _signIn = new Button(_dialog, label: Tr("Sign in to Spotify"));
        _signIn.Click += (_, _) => SignIn();
        // Translators: Button in the Spotify browser that disconnects the account.
        _signOut = new Button(_dialog, label: Tr("Sign out"));
        _signOut.Click += (_, _) => SignOut();
        // Translators: Button in the Spotify browser that reloads the account library.
        var refresh = new Button(_dialog, label: Tr("Refresh"));
        refresh.Click += (_, _) => LoadLibrary();
        var accountRow = new BoxSizer(Orientation.Horizontal);
        accountRow.Add(_signIn, flags: SizerFlags.BorderRight, border: 8);
        accountRow.Add(_signOut, flags: SizerFlags.BorderRight, border: 8);
        accountRow.Add(refresh);

        var tabs = new Notebook(_dialog);
        var home = new Panel(tabs);
        // Translators: Label for the collection list on the Spotify Home tab.
        var homeLabel = new StaticText(home, label: Tr("Collections"));
        _homeList = new ListBox(home) { Name = homeLabel.Label };
        _homeList.ItemActivated += (_, _) => OpenHome();
        // Translators: Button in a Spotify list that opens the selected collection or search result.
        var homeOpen = new Button(home, label: Tr("Open selection"));
        homeOpen.Click += (_, _) => OpenHome();
        var homeSizer = new BoxSizer(Orientation.Vertical);
        homeSizer.Add(homeLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderTop, border: 8);
        homeSizer.Add(_homeList, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        homeSizer.Add(homeOpen, flags: SizerFlags.BorderLeft | SizerFlags.BorderBottom, border: 8);
        home.SetSizer(homeSizer);
        // Translators: Tab in the Spotify browser showing listening history and recommendations.
        tabs.AddPage(home, Tr("Home"));

        var search = new Panel(tabs);
        // Translators: Label for the search entry on the Spotify Search tab. Keep Spotify unchanged.
        var searchLabel = new StaticText(search, label: Tr("Search Spotify for a song, artist or playlist"));
        _searchBox = new TextCtrl(search, style: TextCtrlStyle.ProcessEnter) { Name = searchLabel.Label };
        _searchBox.EnterPressed += (_, _) => Search();
        // Translators: Label for the result type selector on the Spotify Search tab.
        var filterLabel = new StaticText(search, label: Tr("Filter by"));
        _searchFilter = new Choice(search) { Name = filterLabel.Label };
        // Translators: Option in the Spotify search filter to include every result type.
        _searchFilter.Add(Tr("All"));
        // Translators: Option in the Spotify search filter to show tracks.
        _searchFilter.Add(Tr("Songs"));
        // Translators: Option in the Spotify search filter to show musicians and groups.
        _searchFilter.Add(Tr("Artists"));
        // Translators: Option in the Spotify search filter to show playlists.
        _searchFilter.Add(Tr("Playlists"));
        _searchFilter.SelectedIndex = 0;
        // Translators: Search button or tab in the Spotify browser.
        var searchButton = new Button(search, label: Tr("Search"));
        searchButton.Click += (_, _) => Search();
        var searchRow = new BoxSizer(Orientation.Horizontal);
        searchRow.Add(_searchBox, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderRight, border: 8);
        searchRow.Add(searchButton);
        // Translators: Label above the search history on the Spotify Search tab.
        var recentLabel = new StaticText(search, label: Tr("Recent searches"));
        _recentList = new ListBox(search) { Name = recentLabel.Label };
        _recentList.ItemActivated += (_, _) => SearchRecent();
        _searchStatus = new StaticText(search, label: string.Empty);
        var searchSizer = new BoxSizer(Orientation.Vertical);
        searchSizer.Add(searchLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        searchSizer.Add(searchRow, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        searchSizer.Add(filterLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        searchSizer.Add(_searchFilter, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        searchSizer.Add(recentLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        searchSizer.Add(_recentList, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        searchSizer.Add(_searchStatus, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        search.SetSizer(searchSizer);
        // Translators: Search button or tab in the Spotify browser.
        tabs.AddPage(search, Tr("Search"));

        var library = new Panel(tabs);
        // Translators: Label above the account collections on the Spotify Library tab. Liked Songs means saved favorite tracks.
        var libraryLabel = new StaticText(library, label: Tr("Your playlists and Liked Songs"));
        _libraryList = new ListBox(library) { Name = libraryLabel.Label };
        _libraryList.ItemActivated += (_, _) => OpenLibrary();
        // Translators: Button in a Spotify list that opens the selected collection or search result.
        var libraryOpen = new Button(library, label: Tr("Open selection"));
        libraryOpen.Click += (_, _) => OpenLibrary();
        var librarySizer = new BoxSizer(Orientation.Vertical);
        librarySizer.Add(libraryLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderTop, border: 8);
        librarySizer.Add(_libraryList, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        librarySizer.Add(libraryOpen, flags: SizerFlags.BorderLeft | SizerFlags.BorderBottom, border: 8);
        library.SetSizer(librarySizer);
        // Translators: Tab in the Spotify browser listing account playlists and saved songs.
        tabs.AddPage(library, Tr("Library"));

        // Translators: Button that closes the Spotify browser.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));
        var footer = new BoxSizer(Orientation.Horizontal);
        footer.AddStretchSpacer();
        footer.Add(close);
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(_status, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        layout.Add(accountRow, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        layout.Add(tabs, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        layout.Add(footer, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        _dialog.SetSizer(layout);
        _dialog.MinSize = new Size(800, 540);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        LoadRecentSearches();
        UpdateAccount();
        _homeList.Focus();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        ClearViews();
        _dialog.Dispose();
        _lifetime.Dispose();
    }
}
