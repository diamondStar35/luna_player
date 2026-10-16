using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Spotify;
using WxSharp;

namespace LunaPlayer.UI.Spotify;

internal sealed record SpotifyDialogOutcome(SpotifyTrack? Track = null,
    IReadOnlyList<SpotifyTrack>? Queue = null, SpotifyCollection? Collection = null,
    string? NextCursor = null);

internal sealed class SpotifyDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Window _owner;
    private readonly SpotifyCatalog _catalog;
    private readonly SpotifyAuth _auth;
    private readonly SpotifySettings _settings;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly StaticText _status;
    private readonly Button _signIn;
    private readonly Button _signOut;
    private readonly ListBox _homeList;
    private readonly ListBox _libraryList;
    private readonly TextCtrl _searchBox;
    private readonly Choice _searchFilter;
    private readonly ListBox _recentList;
    private readonly StaticText _searchStatus;
    private readonly List<SpotifyCollection> _home = [];
    private readonly List<SpotifyCollection> _library = [];
    private readonly List<string> _recent = [];
    private SpotifyCollection? _collectionToOpen;
    private string? _searchToOpen;
    private SpotifySearchFilter _searchFilterToOpen;
    private bool _disposed;
    private int _libraryRequest;

    internal SpotifyDialog(Window parent, SpotifyCatalog catalog, SpotifyAuth auth, SpotifySettings settings)
    {
        _owner = parent;
        _catalog = catalog;
        _auth = auth;
        _settings = settings;
        _dialog = new Dialog(parent, title: Tr("Spotify"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _status = new StaticText(_dialog, label: string.Empty);
        _signIn = new Button(_dialog, label: Tr("Sign in to Spotify"));
        _signIn.Click += (_, _) => SignIn();
        _signOut = new Button(_dialog, label: Tr("Sign out"));
        _signOut.Click += (_, _) => SignOut();
        var refresh = new Button(_dialog, label: Tr("Refresh"));
        refresh.Click += (_, _) => LoadLibrary();
        var accountRow = new BoxSizer(Orientation.Horizontal);
        accountRow.Add(_signIn, flags: SizerFlags.BorderRight, border: 8);
        accountRow.Add(_signOut, flags: SizerFlags.BorderRight, border: 8);
        accountRow.Add(refresh);

        var tabs = new Notebook(_dialog);
        var home = new Panel(tabs);
        _homeList = new ListBox(home);
        _homeList.ItemActivated += (_, _) => OpenHome();
        var homeOpen = new Button(home, label: Tr("Open selection"));
        homeOpen.Click += (_, _) => OpenHome();
        var homeSizer = new BoxSizer(Orientation.Vertical);
        homeSizer.Add(_homeList, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        homeSizer.Add(homeOpen, flags: SizerFlags.BorderLeft | SizerFlags.BorderBottom, border: 8);
        home.SetSizer(homeSizer);
        tabs.AddPage(home, Tr("Home"));

        var search = new Panel(tabs);
        var searchLabel = new StaticText(search, label: Tr("Search Spotify for a song, artist or playlist"));
        _searchBox = new TextCtrl(search, style: TextCtrlStyle.ProcessEnter);
        _searchBox.EnterPressed += (_, _) => Search();
        var filterLabel = new StaticText(search, label: Tr("Filter by"));
        _searchFilter = new Choice(search);
        _searchFilter.Add(Tr("All"));
        _searchFilter.Add(Tr("Songs"));
        _searchFilter.Add(Tr("Artists"));
        _searchFilter.Add(Tr("Playlists"));
        _searchFilter.SelectedIndex = 0;
        var searchButton = new Button(search, label: Tr("Search"));
        searchButton.Click += (_, _) => Search();
        var searchRow = new BoxSizer(Orientation.Horizontal);
        searchRow.Add(_searchBox, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderRight, border: 8);
        searchRow.Add(searchButton);
        var recentLabel = new StaticText(search, label: Tr("Recent searches"));
        _recentList = new ListBox(search);
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
        tabs.AddPage(search, Tr("Search"));

        var library = new Panel(tabs);
        var libraryLabel = new StaticText(library, label: Tr("Your playlists and Liked Songs"));
        _libraryList = new ListBox(library);
        _libraryList.ItemActivated += (_, _) => OpenLibrary();
        var libraryOpen = new Button(library, label: Tr("Open selection"));
        libraryOpen.Click += (_, _) => OpenLibrary();
        var librarySizer = new BoxSizer(Orientation.Vertical);
        librarySizer.Add(libraryLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderTop, border: 8);
        librarySizer.Add(_libraryList, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        librarySizer.Add(libraryOpen, flags: SizerFlags.BorderLeft | SizerFlags.BorderBottom, border: 8);
        library.SetSizer(librarySizer);
        tabs.AddPage(library, Tr("Library"));

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

    internal SpotifyDialogOutcome Show()
    {
        if (_auth.IsSignedIn(_settings.ClientId)) LoadLibrary();
        while (true)
        {
            _dialog.ShowModal();
            if (_collectionToOpen is SpotifyCollection collection)
            {
                _collectionToOpen = null;
                using var detail = new SpotifyTracksDialog(_owner, collection.Name, collection.Description,
                    (cursor, token) => _catalog.TracksPageAsync(collection, cursor, token),
                    Tr("No accessible tracks were returned for this collection."),
                    deduplicateArtist: collection.Kind == SpotifyCollectionKind.Artist);
                if (detail.Show() is SpotifyTrack track)
                    return new SpotifyDialogOutcome(track, detail.Tracks, collection, detail.NextCursor);
                continue;
            }
            if (_searchToOpen is string query)
            {
                _searchToOpen = null;
                using var results = new SpotifySearchResultsDialog(_owner, _catalog, query, _searchFilterToOpen);
                var selected = results.Show();
                if (selected?.Track is SpotifyTrack track)
                    return new SpotifyDialogOutcome(track, results.Tracks);
                if (selected?.Collection is SpotifyCollection found)
                {
                    using var detail = new SpotifyTracksDialog(_owner, found.Name,
                        found.Kind == SpotifyCollectionKind.Artist ? string.Empty : found.Description,
                        (cursor, token) => _catalog.TracksPageAsync(found, cursor, token),
                        Tr("No accessible tracks were returned for this collection."),
                        deduplicateArtist: found.Kind == SpotifyCollectionKind.Artist);
                    if (detail.Show() is SpotifyTrack chosen)
                        return new SpotifyDialogOutcome(chosen, detail.Tracks, found, detail.NextCursor);
                }
                continue;
            }
            return new SpotifyDialogOutcome();
        }
    }

    private void UpdateAccount()
    {
        var configured = _settings.ClientId.Length > 0;
        var signedIn = configured && _auth.IsSignedIn(_settings.ClientId);
        _signIn.Enabled = configured && !signedIn;
        _signOut.Enabled = signedIn;
        _status.Label = !configured
            ? Tr("Set your Client ID in Spotify Preferences to connect.")
            : signedIn ? Tr("Connected to Spotify.")
            : Tr("Spotify credentials are configured. Choose Sign in to Spotify to connect your account.");
    }

    private void SignIn()
    {
        _signIn.Enabled = false;
        _status.Label = Tr("Opening Spotify sign in in your browser. Return here after authorizing Luna Player.");
        Run(async () => { await _auth.SignInAsync(_settings.ClientId, _lifetime.Token); return true; },
            _ => { UpdateAccount(); LoadLibrary(); },
            error => { UpdateAccount(); ReportError(error); });
    }

    private void SignOut()
    {
        _libraryRequest++;
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
        _status.Label = Tr("Loading your Spotify library...");
        Run(() => _catalog.LibraryAsync(_lifetime.Token), collections =>
        {
            if (request != _libraryRequest || !_auth.IsSignedIn(_settings.ClientId)) return;
            _library.Clear(); _library.AddRange(collections);
            _libraryList.Set(_library.Select(item => item.Label));
            if (_library.Count > 0) _libraryList.SelectedIndex = 0;
            _home.Clear();
            _home.Add(new("top", Tr("Your top tracks"), SpotifyCollectionKind.TopTracks, Tr("Based on your listening history.")));
            _home.Add(new("recent", Tr("Recently played"), SpotifyCollectionKind.RecentlyPlayed, Tr("Your recent listening history.")));
            _home.AddRange(collections.Where(item => item.Kind is SpotifyCollectionKind.Mix or SpotifyCollectionKind.Radio));
            _homeList.Set(_home.Select(item => item.Label));
            if (_home.Count > 0) _homeList.SelectedIndex = 0;
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

    private void OpenCollection(SpotifyCollection collection)
    {
        _collectionToOpen = collection;
        _dialog.EndModal(StandardId.Ok);
    }

    private void Search()
    {
        var query = _searchBox.Value.Trim();
        if (query.Length == 0) { _searchStatus.Label = Tr("Type a search, then press Enter."); return; }
        if (!_auth.IsSignedIn(_settings.ClientId))
        { _searchStatus.Label = Tr("Sign in to Spotify before searching."); return; }
        _recent.RemoveAll(value => value.Equals(query, StringComparison.OrdinalIgnoreCase));
        _recent.Insert(0, query);
        if (_recent.Count > 10) _recent.RemoveRange(10, _recent.Count - 10);
        _recentList.Set(_recent);
        _recentList.SelectedIndex = 0;
        SaveRecentSearches();
        _searchStatus.Label = string.Empty;
        _searchFilterToOpen = (SpotifySearchFilter)Math.Clamp(_searchFilter.SelectedIndex, 0, 3);
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
            Wx.MessageBox(Tr("Could not save recent searches on this computer."), Tr("Warning"),
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
        var message = SpotifyCatalog.DescribeError(error);
        UpdateAccount();
        _status.Label = message;
        Wx.MessageBox(message, Tr("Error"), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
    }

    private void Run<T>(Func<Task<T>> work, Action<T> success, Action<Exception> failure)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await work().ConfigureAwait(false);
                Wx.CallAfter(() => { if (!_disposed) success(result); });
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
            catch (Exception error) { Wx.CallAfter(() => { if (!_disposed) failure(error); }); }
        });
    }

    public void Dispose() { _disposed = true; _lifetime.Cancel(); _dialog.Dispose(); _lifetime.Dispose(); }
}

internal sealed class SpotifyTracksDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Func<string?, CancellationToken, Task<SpotifyTrackPage>> _load;
    private readonly string _emptyMessage;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly StaticText _status;
    private readonly ListBox _songs;
    private readonly List<SpotifyTrack> _tracks = [];
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly bool _deduplicateArtist;
    internal IReadOnlyList<SpotifyTrack> Tracks => _tracks;
    internal string? NextCursor { get; private set; }
    private SpotifyTrack? _selected;
    private bool _disposed;
    private bool _loading;
    private bool _hasMore = true;

    internal SpotifyTracksDialog(Window parent, string title, string description,
        Func<string?, CancellationToken, Task<SpotifyTrackPage>> load, string emptyMessage,
        bool deduplicateArtist = false)
    {
        _load = load;
        _emptyMessage = emptyMessage;
        _deduplicateArtist = deduplicateArtist;
        _dialog = new Dialog(parent, title: title + " - Spotify", style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _status = new StaticText(_dialog, label: Tr("Loading tracks from Spotify..."));
        var play = new Button(_dialog, label: Tr("Play"));
        play.Click += (_, _) => Select();
        _songs = new ListBox(_dialog);
        _songs.ItemActivated += (_, _) => Select();
        var back = new Button(_dialog, StandardId.Cancel, Tr("Back"));
        var layout = new BoxSizer(Orientation.Vertical);
        if (!string.IsNullOrWhiteSpace(description))
            layout.Add(new StaticText(_dialog, label: description),
                flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        layout.Add(_status, flags: SizerFlags.All, border: 8);
        layout.Add(play, flags: SizerFlags.All, border: 8);
        layout.Add(_songs, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        layout.Add(back, flags: SizerFlags.All, border: 8);
        _dialog.SetSizer(layout);
        _dialog.MinSize = new Size(700, 470);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        _songs.Focus();
    }

    internal SpotifyTrack? Show()
    {
        LoadNextPage(selectNew: false);
        _dialog.ShowModal();
        return _selected;
    }

    private void LoadNextPage(bool selectNew)
    {
        if (_loading || !_hasMore) return;
        _loading = true;
        _status.Label = Tr("Loading tracks from Spotify...");
        var cursor = NextCursor;
        _ = Task.Run(async () =>
        {
            try
            {
                var page = await _load(cursor, _lifetime.Token).ConfigureAwait(false);
                Wx.CallAfter(() =>
                {
                    if (_disposed) return;
                    _loading = false;
                    var previousCount = _tracks.Count;
                    var previousSelection = _songs.SelectedIndex;
                    foreach (var track in page.Tracks)
                        if (!_deduplicateArtist || _seen.Add(track.ArtistSongKey)) _tracks.Add(track);
                    NextCursor = page.Next;
                    _hasMore = page.Next is not null;
                    _songs.Set(_tracks.Select(item => item.Label));
                    if (_tracks.Count > 0)
                    {
                        _songs.SelectedIndex = selectNew && _tracks.Count > previousCount ? previousCount
                            : Math.Clamp(previousSelection, 0, _tracks.Count - 1);
                        _songs.Focus();
                    }
                    _status.Label = _tracks.Count == 0 ? _emptyMessage
                        : _hasMore ? TrFormat("{count} tracks loaded. Press Down at the end for more.", _tracks.Count)
                        : TrFormat("{count} tracks loaded.", _tracks.Count);
                    if (_tracks.Count == previousCount && _hasMore) LoadNextPage(selectNew);
                });
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
            catch (Exception error)
            {
                Wx.CallAfter(() =>
                {
                    if (_disposed) return;
                    _loading = false;
                    var message = SpotifyCatalog.DescribeError(error);
                    _status.Label = message;
                    Wx.MessageBox(message, Tr("Error"), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
                });
            }
        });
    }

    private void Select()
    {
        var index = _songs.SelectedIndex;
        if (index < 0 || index >= _tracks.Count) return;
        _selected = _tracks[index];
        _dialog.EndModal(StandardId.Ok);
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape) { _dialog.EndModal(StandardId.Cancel); return; }
        if (ReferenceEquals(Window.FindFocus(), _songs))
        {
            if (args.Code is Key.Enter or Key.NumpadEnter) { Select(); return; }
            if (args.Code is Key.Down or Key.NumpadDown && !args.Control && !args.Alt
                && _songs.SelectedIndex == _tracks.Count - 1 && _hasMore)
            {
                LoadNextPage(selectNew: true);
                return;
            }
        }
        args.Skip();
    }

    public void Dispose() { _disposed = true; _lifetime.Cancel(); _dialog.Dispose(); _lifetime.Dispose(); }
}

internal sealed class SpotifySearchResultsDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly SpotifyCatalog _catalog;
    private readonly string _query;
    private readonly SpotifySearchFilter _filter;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly StaticText _status;
    private readonly ListBox _list;
    private readonly List<SpotifySearchItem> _results = [];
    private int _offset;
    private bool _hasMore = true;
    private bool _loading;
    private bool _disposed;
    private SpotifySearchItem? _selected;

    internal IReadOnlyList<SpotifyTrack> Tracks => _results.Select(item => item.Track)
        .OfType<SpotifyTrack>().ToArray();

    internal SpotifySearchResultsDialog(Window parent, SpotifyCatalog catalog, string query,
        SpotifySearchFilter filter)
    {
        _catalog = catalog;
        _query = query;
        _filter = filter;
        _dialog = new Dialog(parent, title: Tr("Search results") + " - Spotify",
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var heading = new StaticText(_dialog, label: query);
        _status = new StaticText(_dialog, label: Tr("Loading search results..."));
        _list = new ListBox(_dialog);
        _list.ItemActivated += (_, _) => Select();
        var open = new Button(_dialog, label: Tr("Open selection"));
        open.Click += (_, _) => Select();
        var back = new Button(_dialog, StandardId.Cancel, Tr("Back"));
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(heading, flags: SizerFlags.All, border: 8);
        layout.Add(_status, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        layout.Add(_list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        layout.Add(open, flags: SizerFlags.BorderLeft | SizerFlags.BorderBottom, border: 8);
        layout.Add(back, flags: SizerFlags.BorderLeft | SizerFlags.BorderBottom, border: 8);
        _dialog.SetSizer(layout);
        _dialog.MinSize = new Size(700, 470);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        _list.Focus();
    }

    internal SpotifySearchItem? Show()
    {
        LoadNextPage(selectNew: false);
        _dialog.ShowModal();
        return _selected;
    }

    private void LoadNextPage(bool selectNew)
    {
        if (_loading || !_hasMore) return;
        _loading = true;
        _status.Label = Tr("Loading more search results...");
        var offset = _offset;
        _ = Task.Run(async () =>
        {
            try
            {
                var page = await _catalog.SearchAsync(_query, offset, _filter, _lifetime.Token).ConfigureAwait(false);
                Wx.CallAfter(() =>
                {
                    if (_disposed) return;
                    _loading = false;
                    var previousCount = _results.Count;
                    var previousSelection = _list.SelectedIndex;
                    _results.AddRange(page.Items);
                    _offset += 10;
                    _hasMore = page.HasMore;
                    _list.Set(_results.Select(item => item.Label));
                    if (_results.Count > 0)
                    {
                        _list.SelectedIndex = selectNew && page.Items.Count > 0 ? previousCount
                            : Math.Clamp(previousSelection, 0, _results.Count - 1);
                        _list.Focus();
                    }
                    _status.Label = _results.Count == 0 ? Tr("No results found.")
                        : _hasMore ? TrFormat("{count} results loaded. Press Down at the end for more.", _results.Count)
                        : TrFormat("{count} results loaded.", _results.Count);
                    if (page.Items.Count == 0 && _hasMore) LoadNextPage(selectNew);
                });
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
            catch (Exception error)
            {
                Wx.CallAfter(() =>
                {
                    if (_disposed) return;
                    _loading = false;
                    var message = SpotifyCatalog.DescribeError(error);
                    _status.Label = message;
                    Wx.MessageBox(message, Tr("Error"), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
                });
            }
        });
    }

    private void Select()
    {
        var index = _list.SelectedIndex;
        if (index < 0 || index >= _results.Count) return;
        _selected = _results[index];
        _dialog.EndModal(StandardId.Ok);
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape) { _dialog.EndModal(StandardId.Cancel); return; }
        if (ReferenceEquals(Window.FindFocus(), _list))
        {
            if (args.Code is Key.Enter or Key.NumpadEnter) { Select(); return; }
            if (args.Code is Key.Down or Key.NumpadDown && !args.Control && !args.Alt
                && _list.SelectedIndex == _results.Count - 1 && _hasMore)
            {
                LoadNextPage(selectNew: true);
                return;
            }
        }
        args.Skip();
    }

    public void Dispose() { _disposed = true; _lifetime.Cancel(); _dialog.Dispose(); _lifetime.Dispose(); }
}
