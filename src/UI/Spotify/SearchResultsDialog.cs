using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
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
    private readonly StaticText _status;
    private readonly ListBox _list;
    private readonly List<SearchItem> _results = [];
    private int _offset;
    private bool _hasMore = true;
    private bool _loading;
    private bool _disposed;
    private SearchItem? _selected;

    internal IReadOnlyList<Track> Tracks => _results.Select(item => item.Track)
        .OfType<Track>().ToArray();

    internal SearchResultsDialog(Window parent, Catalog catalog, string query,
        SearchFilter filter)
    {
        _token = _lifetime.Token;
        _catalog = catalog;
        _query = query;
        _filter = filter;
        // Translators: Title of the Spotify search results window. Keep Spotify unchanged.
        _dialog = new Dialog(parent, title: Tr("Search results - Spotify"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var heading = new StaticText(_dialog, label: query);
        // Translators: Status or guidance beside the search controls in the Spotify browser and search results window.
        _status = new StaticText(_dialog, label: Tr("Loading search results..."));
        // Translators: Label for the list of songs, artists and playlists in the Spotify search results window.
        var resultsLabel = new StaticText(_dialog, label: Tr("Search results"));
        _list = new ListBox(_dialog) { Name = resultsLabel.Label };
        _list.ItemActivated += (_, _) => Select();
        // Translators: Button in a Spotify list that opens the selected collection or search result.
        var open = new Button(_dialog, label: Tr("Open selection"));
        open.Click += (_, _) => Select();
        // Translators: Button that returns from Spotify tracks or search results to the browser.
        var back = new Button(_dialog, StandardId.Cancel, Tr("Back"));
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(heading, flags: SizerFlags.All, border: 8);
        layout.Add(_status, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
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

    internal SearchItem? Show()
    {
        LoadNextPage(selectNew: false);
        _dialog.ShowModal();
        return _selected;
    }

    private void LoadNextPage(bool selectNew)
    {
        if (_disposed || _loading || !_hasMore) return;
        _loading = true;
        // Translators: Status or guidance beside the search controls in the Spotify browser and search results window.
        _status.Label = Tr("Loading more search results...");
        var offset = _offset;
        _ = Task.Run(async () =>
        {
            try
            {
                var page = await _catalog.SearchAsync(_query, offset, _filter, _token).ConfigureAwait(false);
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
                    // Translators: Empty-list status in the Spotify search results window when no matching tracks, artists or playlists were found.
                    _status.Label = _results.Count == 0 ? Tr("No results found.")
                        // Translators: Status in the Spotify search results window. {count} is the number of results already loaded; Down means the keyboard arrow key.
                        : _hasMore ? TrFormat("{count} results loaded. Press Down at the end for more.", _results.Count)
                        // Translators: Status in the Spotify search results window. {count} is the number of results already loaded; Down means the keyboard arrow key.
                        : TrFormat("{count} results loaded.", _results.Count);
                    if (page.Items.Count == 0 && _hasMore) LoadNextPage(selectNew);
                });
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
            {
                // The dialog has closed, so its canceled request must not update the UI.
            }
            catch (Exception error)
            {
                Wx.CallAfter(() =>
                {
                    if (_disposed) return;
                    _loading = false;
                    var message = Catalog.DescribeError(error);
                    _status.Label = message;
                    // Translators: Title of a message box reporting a Spotify or playback failure.
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
