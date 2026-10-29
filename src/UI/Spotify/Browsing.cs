using LunaPlayer.UI.Services;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Spotify;
using WxSharp;

namespace LunaPlayer.UI.Spotify;

internal sealed partial class BrowserDialog
{
    internal DialogOutcome Show(DialogOutcome? playback)
    {
        if (_clientId != _settings.ClientId)
        {
            ClearViews();
            _home.Clear();
            _library.Clear();
            _homeList.Clear();
            _libraryList.Clear();
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
                    _searchResults = new SearchResultsDialog(_owner, _catalog, query, _searchFilterToOpen, _speak);
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
            _tracksDialog = new TracksDialog(_owner, collection.DisplayName, collection.DisplayDescription,
                (cursor, token) => _catalog.TracksPageAsync(collection, cursor, token),
                _speak,
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
            if (File.Exists(Paths.SpotifySearchesFile))
                _recent.AddRange(File.ReadAllLines(Paths.SpotifySearchesFile)
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
            Paths.EnsureDirectoryFor(Paths.SpotifySearchesFile);
            File.WriteAllLines(Paths.SpotifySearchesFile, _recent);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Messages.Show(
                // Translators: Warning message box when saving recent searches from the Spotify browser fails.
                Tr("Could not save recent searches on this computer."),
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
        if (_visible) Messages.Show(message, MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
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
