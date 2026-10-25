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

internal sealed class BrowserDialog : IDisposable
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
        // Translators: Window title or action label for Spotify. Keep the service name unchanged.
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

    internal DialogOutcome Show(DialogOutcome? playback)
    {
        if (_clientId != _settings.ClientId)
        {
            ClearViews();
            _clientId = _settings.ClientId;
        }
        if (playback is null) _page = Page.Home;
        var selected = playback?.Track;
        UpdateAccount();
        if (!_libraryLoaded && _auth.IsSignedIn(_settings.ClientId)) LoadLibrary();
        while (!_disposed)
        {
            if (_page == Page.Tracks && _tracksDialog is { } tracks)
            {
                if (playback?.Queue is { } loaded) tracks.AppendPlayback(loaded, playback.NextCursor);
                playback = null;
                if (tracks.Show(selected) is { } track)
                    return new DialogOutcome(track, tracks.Tracks, _tracksCollection, tracks.NextCursor);
                selected = null;
                _page = _tracksParent;
                continue;
            }
            if (_page == Page.Search && _searchResults is { } results)
            {
                if (playback?.Queue is { } loaded) results.AppendPlayback(loaded, playback.NextCursor);
                playback = null;
                var result = results.Show(selected);
                selected = null;
                if (result?.Track is { } track)
                    return new DialogOutcome(track, results.Tracks, NextCursor: results.NextCursor,
                        LoadPage: results.LoadTracksPageAsync);
                if (result?.Collection is { } collection)
                {
                    ShowTracks(collection, Page.Search);
                    continue;
                }
                _page = Page.Home;
                continue;
            }
            _visible = true;
            try { _dialog.ShowModal(); }
            finally { _visible = false; }
            if (_collectionToOpen is { } chosen)
            {
                _collectionToOpen = null;
                ShowTracks(chosen, Page.Home);
                continue;
            }
            if (_searchToOpen is { } query)
            {
                _searchToOpen = null;
                if (_searchResults is null || _resultsQuery != query || _resultsFilter != _searchFilterToOpen)
                {
                    _searchResults?.Dispose();
                    _searchResults = new SearchResultsDialog(_owner, _catalog, query, _searchFilterToOpen);
                    _resultsQuery = query;
                    _resultsFilter = _searchFilterToOpen;
                }
                _page = Page.Search;
                continue;
            }
            return new DialogOutcome();
        }
        return new DialogOutcome();
    }

    private void ShowTracks(Collection collection, Page parent)
    {
        if (_tracksDialog is null || _tracksCollection != collection)
        {
            _tracksDialog?.Dispose();
            _tracksDialog = new TracksDialog(_owner, collection.Name, collection.Description,
                (cursor, token) => _catalog.TracksPageAsync(collection, cursor, token),
                // Translators: Empty-list message in the Spotify collection tracks window.
                Tr("No accessible tracks were returned for this collection."),
                deduplicateArtist: collection.Kind == CollectionKind.Artist);
            _tracksCollection = collection;
        }
        _tracksParent = parent;
        _page = Page.Tracks;
    }

    private void ClearViews()
    {
        _libraryRequest++;
        _libraryLoaded = false;
        _tracksDialog?.Dispose();
        _tracksDialog = null;
        _tracksCollection = null;
        _searchResults?.Dispose();
        _searchResults = null;
        _page = Page.Home;
    }

    private void UpdateAccount()
    {
        var configured = _settings.ClientId.Length > 0;
        var signedIn = configured && _auth.IsSignedIn(_settings.ClientId);
        _signIn.Enabled = configured && !signedIn;
        _signOut.Enabled = signedIn;
        _status.Label = !configured
            // Translators: Account connection status at the top of the Spotify browser. Keep Spotify and Client ID unchanged.
            ? Tr("Set your Client ID in Spotify Preferences to connect.")
            // Translators: Account connection status at the top of the Spotify browser. Keep Spotify and Client ID unchanged.
            : signedIn ? Tr("Connected to Spotify.")
            // Translators: Account connection status at the top of the Spotify browser. Keep Spotify and Client ID unchanged.
            : Tr("Spotify credentials are configured. Choose Sign in to Spotify to connect your account.");
    }

    private void SignIn()
    {
        _signIn.Enabled = false;
        // Translators: Account connection status at the top of the Spotify browser. Keep Spotify and Client ID unchanged.
        _status.Label = Tr("Opening Spotify sign in in your browser. Return here after authorizing Luna Player.");
        Run(async () => { await _auth.SignInAsync(_settings.ClientId, _token); return true; },
            _ => { UpdateAccount(); LoadLibrary(); },
            error => { UpdateAccount(); ReportError(error); });
    }

    private void SignOut()
    {
        ClearViews();
        Exception? failure = null;
        try { _auth.SignOut(); }
        catch (Exception error) { failure = error; }
        _home.Clear(); _library.Clear();
        _homeList.Clear(); _libraryList.Clear();
        UpdateAccount();
        if (failure is not null) ReportError(failure);
    }

    private void LoadLibrary()
    {
        var request = ++_libraryRequest;
        if (!_auth.IsSignedIn(_settings.ClientId)) { UpdateAccount(); return; }
        // Translators: Account library loading status at the top of the Spotify browser.
        _status.Label = Tr("Loading your Spotify library...");
        Run(() => _catalog.LibraryAsync(_token), collections =>
        {
            if (request != _libraryRequest || !_auth.IsSignedIn(_settings.ClientId)) return;
            _libraryLoaded = true;
            _library.Clear(); _library.AddRange(collections);
            _libraryList.Set(_library.Select(item => item.Label));
            if (_library.Count > 0) _libraryList.SelectedIndex = 0;
            _home.Clear();
            _home.Add(new("top",
                // Translators: Collection name on the Spotify Home tab for the account's most played songs.
                Tr("Your top tracks"), CollectionKind.TopTracks,
                // Translators: Description of the top tracks collection on the Spotify Home tab.
                Tr("Based on your listening history.")));
            _home.Add(new("recent",
                // Translators: Collection name on the Spotify Home tab for listening history.
                Tr("Recently played"), CollectionKind.RecentlyPlayed,
                // Translators: Description of the recently played collection on the Spotify Home tab.
                Tr("Your recent listening history.")));
            _home.AddRange(collections.Where(item => item.Kind is CollectionKind.Mix or CollectionKind.Radio));
            _homeList.Set(_home.Select(item => item.Label));
            if (_home.Count > 0) _homeList.SelectedIndex = 0;
            // Translators: Library loading status at the top of the Spotify browser. {count} is the number of available playlists and collections.
            _status.Label = TrFormat("Spotify library loaded. {count} collections available.", _library.Count);
        }, error =>
        {
            if (request == _libraryRequest && _auth.IsSignedIn(_settings.ClientId)) ReportError(error);
        });
    }

    private void OpenHome()
    {
        var index = _homeList.SelectedIndex;
        if (index >= 0 && index < _home.Count) OpenCollection(_home[index]);
    }

    private void OpenLibrary()
    {
        var index = _libraryList.SelectedIndex;
        if (index >= 0 && index < _library.Count) OpenCollection(_library[index]);
    }

    private void OpenCollection(Collection collection)
    {
        _collectionToOpen = collection;
        _dialog.EndModal(StandardId.Ok);
    }

    private void Search()
    {
        var query = _searchBox.Value.Trim();
        // Translators: Status or guidance beside the search controls in the Spotify browser and search results window.
        if (query.Length == 0) { _searchStatus.Label = Tr("Type a search, then press Enter."); return; }
        if (!_auth.IsSignedIn(_settings.ClientId))
        // Translators: Status or guidance beside the search controls in the Spotify browser and search results window.
        { _searchStatus.Label = Tr("Sign in to Spotify before searching."); return; }
        _recent.RemoveAll(value => value.Equals(query, StringComparison.OrdinalIgnoreCase));
        _recent.Insert(0, query);
        if (_recent.Count > 10) _recent.RemoveRange(10, _recent.Count - 10);
        _recentList.Set(_recent);
        _recentList.SelectedIndex = 0;
        SaveRecentSearches();
        _searchStatus.Label = string.Empty;
        _searchFilterToOpen = (SearchFilter)Math.Clamp(_searchFilter.SelectedIndex, 0, 3);
        _searchToOpen = query;
        _dialog.EndModal(StandardId.Ok);
    }

    private void SearchRecent()
    {
        var index = _recentList.SelectedIndex;
        if (index < 0 || index >= _recent.Count) return;
        _searchBox.ChangeValue(_recent[index]);
        Search();
    }

    private void LoadRecentSearches()
    {
        try
        {
            if (File.Exists(Paths.SpotifyPreviewSearchesFile))
                _recent.AddRange(File.ReadAllLines(Paths.SpotifyPreviewSearchesFile)
                    .Where(value => !string.IsNullOrWhiteSpace(value)).Take(10));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        // Translators: Status or guidance beside the search controls in the Spotify browser and search results window.
        { _searchStatus.Label = Tr("Could not load recent searches from this computer."); }
        _recentList.Set(_recent);
        if (_recent.Count > 0) _recentList.SelectedIndex = 0;
    }

    private void SaveRecentSearches()
    {
        try
        {
            Paths.EnsureDirectoryFor(Paths.SpotifyPreviewSearchesFile);
            File.WriteAllLines(Paths.SpotifyPreviewSearchesFile, _recent);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Wx.MessageBox(
                // Translators: Warning message box when saving recent searches from the Spotify browser fails.
                Tr("Could not save recent searches on this computer."),
                // Translators: Title of the warning about saving Spotify search history.
                Tr("Warning"),
                MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
        }
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape) { _dialog.EndModal(StandardId.Cancel); return; }
        if (args.Code is Key.Enter or Key.NumpadEnter && !args.Control && !args.Alt)
        {
            var focused = Window.FindFocus();
            if (ReferenceEquals(focused, _homeList)) { OpenHome(); return; }
            if (ReferenceEquals(focused, _libraryList)) { OpenLibrary(); return; }
            if (ReferenceEquals(focused, _recentList)) { SearchRecent(); return; }
        }
        args.Skip();
    }

    private void ReportError(Exception error)
    {
        var message = Catalog.DescribeError(error);
        UpdateAccount();
        _status.Label = message;
        // Translators: Title of a message box reporting a Spotify or playback failure.
        if (_visible) Wx.MessageBox(message, Tr("Error"), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
    }

    private void Run<T>(Func<Task<T>> work, Action<T> success, Action<Exception> failure)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await work().ConfigureAwait(false);
                DialogWork.Post(_token, () => { if (!_disposed) success(result); });
            }
            catch (OperationCanceledException) when (_token.IsCancellationRequested)
            {
                // The dialog has closed, so its canceled request must not update the UI.
            }
            catch (Exception error) { DialogWork.Post(_token, () => { if (!_disposed) failure(error); }); }
        });
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

// Dialog workers carry a captured token so late completions never consult disposed wx objects.
internal static class DialogWork
{
    internal static void Post(CancellationToken token, Action action)
    {
        if (token.IsCancellationRequested) return;
        try
        {
            Wx.CallAfter(() =>
            {
                if (!token.IsCancellationRequested) CrashReport.Guard(action);
            });
        }
        catch (InvalidOperationException) when (token.IsCancellationRequested)
        {
            // Closing the application can end wx between checking the token and posting the result.
        }
    }
}
