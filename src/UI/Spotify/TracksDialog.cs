using LunaPlayer.Configuration;
using LunaPlayer.Spotify;
using WxSharp;

namespace LunaPlayer.UI.Spotify;

internal sealed class TracksDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Func<string?, CancellationToken, Task<TrackPage>> _load;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _token;
    private readonly HashSet<string> _loadedPages = new(StringComparer.Ordinal);
    private readonly Action<string> _speak;
    private readonly ListBox _songs;
    private readonly List<Track> _tracks = [];
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly bool _deduplicateArtist;
    internal IReadOnlyList<Track> Tracks => _tracks;
    internal string? NextCursor { get; private set; }
    private Track? _selected;
    private bool _disposed;
    private bool _started;
    private bool _visible;
    private CancellationTokenSource? _read;
    private bool _loading;
    private bool _hasMore = true;

    internal TracksDialog(Window parent, string title, string description,
        Func<string?, CancellationToken, Task<TrackPage>> load, Action<string> speak,
        bool deduplicateArtist = false)
    {
        _token = _lifetime.Token;
        _load = load;
        _speak = speak;
        _deduplicateArtist = deduplicateArtist;
        // Translators: Title of a Spotify collection window. {title} is the collection name; keep Spotify unchanged.
        _dialog = new Dialog(parent, title: TrFormat("{title} - Spotify", title), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        // Translators: Label for the track list in a Spotify collection window.
        var tracksLabel = new StaticText(_dialog, label: Tr("Tracks"));
        _songs = new ListBox(_dialog) { Name = tracksLabel.Label };
        _songs.ItemActivated += (_, _) => Select();
        _songs.SelectionChanged += (_, _) =>
        {
            if (_visible && _songs.SelectedIndex >= 0 && _songs.SelectedIndex == _tracks.Count - 1)
                LoadNextPage(announce: true);
        };
        // Translators: Button in a Spotify collection window that plays the selected track.
        var play = new Button(_dialog, label: Tr("Play"));
        play.Click += (_, _) => Select();
        // Translators: Button that returns from Spotify tracks or search results to the browser.
        var back = new Button(_dialog, StandardId.Cancel, Tr("Back"));
        var layout = new BoxSizer(Orientation.Vertical);
        if (!string.IsNullOrWhiteSpace(description))
            layout.Add(new StaticText(_dialog, label: description),
                flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        layout.Add(tracksLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        layout.Add(_songs, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        layout.Add(play, flags: SizerFlags.All, border: 8);
        layout.Add(back, flags: SizerFlags.All, border: 8);
        _dialog.SetSizer(layout);
        _dialog.MinSize = new Size(700, 470);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        _songs.Focus();
    }

    internal Track? Show(Track? selected = null)
    {
        _selected = null;
        if (!_started || (!_loading && _hasMore && _tracks.Count == 0))
        {
            _started = true;
            LoadNextPage();
        }
        if (selected is not null)
        {
            var index = _tracks.FindIndex(item => ReferenceEquals(item, selected));
            if (index < 0) index = _tracks.FindIndex(item => item?.Uri == selected.Uri);
            if (index >= 0) _songs.SelectedIndex = index;
        }
        _visible = true;
        try { _dialog.ShowModal(); }
        finally { _visible = false; CancelRead(); }
        return _selected;
    }

    internal void AppendPlayback(IReadOnlyList<Track> tracks, string? next)
    {
        var known = new HashSet<Track>(_tracks, ReferenceEqualityComparer.Instance);
        var added = tracks.Where(track => known.Add(track)).ToArray();
        if (added.Length > 0)
        {
            _tracks.AddRange(added);
            foreach (var track in added) _seen.Add(track.ArtistSongKey);
            _songs.AppendRange(added.Select(track => track.Label));
        }
        NextCursor = next;
        _hasMore = next is not null;
    }

    private void LoadNextPage(bool announce = false)
    {
        if (_disposed || _loading || !_hasMore) return;
        _loading = true;
        _read?.Dispose();
        _read = CancellationTokenSource.CreateLinkedTokenSource(_token);
        var token = _read.Token;
        if (announce && _visible && _tracks.Count > 0)
            // Translators: Spoken when reaching the end of a Spotify collection's track list requests another page.
            _speak(Tr("Loading more tracks..."));
        var cursor = NextCursor;
        var loadedPages = new HashSet<string>(_loadedPages, StringComparer.Ordinal);
        _ = Task.Run(async () =>
        {
            try
            {
                var page = await _load(cursor, token).ConfigureAwait(false);
                // Only one page is in flight. Stop a repeated cursor before it starts an
                // unbounded sequence of requests through empty or duplicate pages.
                if (page.Next is not null && (page.Next == cursor || loadedPages.Contains(page.Next)))
                    throw new InvalidDataException("Spotify repeated a track page.");
                DialogWork.Post(token, () =>
                {
                    if (_disposed) return;
                    _loadedPages.Add(cursor ?? string.Empty);
                    var previousCount = _tracks.Count;
                    var added = page.Tracks.Where(track => !_deduplicateArtist || _seen.Add(track.ArtistSongKey)).ToArray();
                    _tracks.AddRange(added);
                    NextCursor = page.Next;
                    _hasMore = page.Next is not null;
                    _songs.AppendRange(added.Select(track => track.Label));
                    if (previousCount == 0 && _tracks.Count > 0) _songs.SelectedIndex = 0;
                    _loading = false;
                    if (_tracks.Count == previousCount && _hasMore) LoadNextPage();
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
