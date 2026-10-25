using LunaPlayer.Configuration;
using LunaPlayer.Spotify;
using WxSharp;

namespace LunaPlayer.UI.Spotify;

internal sealed class SearchResultsDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Catalog _catalog;
    private readonly string _query;
    private readonly SearchFilter _filter;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _token;
    private readonly Action<string> _speak;
    private readonly ListBox _list;
    private readonly List<SearchItem> _results = [];
    private int _offset;
    private bool _hasMore = true;
    private bool _loading;
    private bool _disposed;
    private bool _started;
    private bool _visible;
    private CancellationTokenSource? _read;
    private SearchItem? _selected;

    internal IReadOnlyList<Track> Tracks => _results.Select(item => item.Track)
        .OfType<Track>().ToArray();

    internal SearchResultsDialog(Window parent, Catalog catalog, string query,
        SearchFilter filter, Action<string> speak)
    {
        _token = _lifetime.Token;
        _catalog = catalog;
        _query = query;
        _filter = filter;
        _speak = speak;
        // Translators: Title of the Spotify search results window. Keep Spotify unchanged.
        _dialog = new Dialog(parent, title: Tr("Search results - Spotify"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var heading = new StaticText(_dialog, label: query);
        // Translators: Label for the list of songs, artists and playlists in the Spotify search results window.
        var resultsLabel = new StaticText(_dialog, label: Tr("Search results"));
        _list = new ListBox(_dialog) { Name = resultsLabel.Label };
        _list.ItemActivated += (_, _) => Select();
        _list.SelectionChanged += (_, _) =>
        {
            if (_visible && _list.SelectedIndex >= 0 && _list.SelectedIndex == _results.Count - 1)
                LoadNextPage(announce: true);
        };
        // Translators: Button in a Spotify list that opens the selected collection or search result.
        var open = new Button(_dialog, label: Tr("Open selection"));
        open.Click += (_, _) => Select();
        // Translators: Button that returns from Spotify tracks or search results to the browser.
        var back = new Button(_dialog, StandardId.Cancel, Tr("Back"));
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(heading, flags: SizerFlags.All, border: 8);
        layout.Add(resultsLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        layout.Add(_list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        layout.Add(open, flags: SizerFlags.BorderLeft | SizerFlags.BorderBottom, border: 8);
        layout.Add(back, flags: SizerFlags.BorderLeft | SizerFlags.BorderBottom, border: 8);
        _dialog.SetSizer(layout);
        _dialog.MinSize = new Size(700, 470);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        _list.Focus();
    }

    internal SearchItem? Show(Track? selected = null)
    {
        _selected = null;
        if (!_started || (!_loading && _hasMore && _results.Count == 0))
        {
            _started = true;
            LoadNextPage();
        }
        if (selected is not null)
        {
            var index = _results.FindIndex(item => ReferenceEquals(item.Track, selected));
            if (index < 0) index = _results.FindIndex(item => item.Track?.Uri == selected.Uri);
            if (index >= 0) _list.SelectedIndex = index;
        }
        _visible = true;
        try { _dialog.ShowModal(); }
        finally { _visible = false; CancelRead(); }
        return _selected;
    }

    internal string? NextCursor => _hasMore ? _offset.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;

    internal async Task<TrackPage> LoadTracksPageAsync(string? cursor, CancellationToken token)
    {
        if (!int.TryParse(cursor, out var offset) || offset < 0 || offset > 1000)
            throw new InvalidDataException("Invalid Spotify search cursor.");
        var page = await _catalog.SearchAsync(_query, offset, _filter, token).ConfigureAwait(false);
        return new TrackPage(page.Items.Select(item => item.Track).OfType<Track>().ToArray(),
            page.HasMore ? (offset + 10).ToString(System.Globalization.CultureInfo.InvariantCulture) : null);
    }

    internal void AppendPlayback(IReadOnlyList<Track> tracks, string? next)
    {
        var known = new HashSet<Track>(Tracks, ReferenceEqualityComparer.Instance);
        var added = tracks.Where(track => known.Add(track)).ToArray();
        if (added.Length > 0)
        {
            var rows = added.Select(track => new SearchItem(track, null)).ToArray();
            _results.AddRange(rows);
            _list.AppendRange(rows.Select(item => item.Label));
        }
        _hasMore = next is not null;
        if (int.TryParse(next, out var offset)) _offset = offset;
    }

    private void LoadNextPage(bool announce = false)
    {
        if (_disposed || _loading || !_hasMore) return;
        _loading = true;
        _read?.Dispose();
        _read = CancellationTokenSource.CreateLinkedTokenSource(_token);
        var token = _read.Token;
        if (announce && _visible && _results.Count > 0)
            // Translators: Spoken when reaching the end of the Spotify search results list requests another page.
            _speak(Tr("Loading more results..."));
        var offset = _offset;
        _ = Task.Run(async () =>
        {
            try
            {
                var page = await _catalog.SearchAsync(_query, offset, _filter, token).ConfigureAwait(false);
                DialogWork.Post(token, () =>
                {
                    if (_disposed) return;
                    var previousCount = _results.Count;
                    _results.AddRange(page.Items);
                    _offset += 10;
                    _hasMore = page.HasMore;
                    _list.AppendRange(page.Items.Select(item => item.Label));
                    if (previousCount == 0 && _results.Count > 0) _list.SelectedIndex = 0;
                    _loading = false;
                    if (page.Items.Count == 0 && _hasMore) LoadNextPage();
                });
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // The dialog has closed, so its canceled request must not update the UI.
            }
            catch (Exception error)
            {
                DialogWork.Post(token, () =>
                {
                    if (_disposed) return;
                    _loading = false;
                    var message = Catalog.DescribeError(error);
                    // Translators: Title of a message box reporting a Spotify or playback failure.
                    if (_visible) Wx.MessageBox(message, Tr("Error"), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
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
                LoadNextPage(announce: true);
                return;
            }
        }
        args.Skip();
    }

    private void CancelRead()
    {
        _read?.Cancel();
        _read?.Dispose();
        _read = null;
        _loading = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        CancelRead();
        _dialog.Dispose();
        _lifetime.Dispose();
    }
}
