using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Spotify;
using WxSharp;

namespace LunaPlayer.UI.Spotify;

internal sealed class TracksDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Func<string?, CancellationToken, Task<TrackPage>> _load;
    private readonly string _emptyMessage;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _token;
    private readonly HashSet<string> _loadedPages = new(StringComparer.Ordinal);
    private readonly StaticText _status;
    private readonly ListBox _songs;
    private readonly List<Track> _tracks = [];
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly bool _deduplicateArtist;
    internal IReadOnlyList<Track> Tracks => _tracks;
    internal string? NextCursor { get; private set; }
    private Track? _selected;
    private bool _disposed;
    private bool _loading;
    private bool _hasMore = true;

    internal TracksDialog(Window parent, string title, string description,
        Func<string?, CancellationToken, Task<TrackPage>> load, string emptyMessage,
        bool deduplicateArtist = false)
    {
        _token = _lifetime.Token;
        _load = load;
        _emptyMessage = emptyMessage;
        _deduplicateArtist = deduplicateArtist;
        // Translators: Title of a Spotify collection window. {title} is the collection name; keep Spotify unchanged.
        _dialog = new Dialog(parent, title: TrFormat("{title} - Spotify", title), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        // Translators: Loading status in the Spotify collection window while its track list is fetched.
        _status = new StaticText(_dialog, label: Tr("Loading tracks from Spotify..."));
        // Translators: Label for the track list in a Spotify collection window.
        var tracksLabel = new StaticText(_dialog, label: Tr("Tracks"));
        _songs = new ListBox(_dialog) { Name = tracksLabel.Label };
        _songs.ItemActivated += (_, _) => Select();
        // Translators: Button in a Spotify collection window that plays the selected track.
        var play = new Button(_dialog, label: Tr("Play"));
        play.Click += (_, _) => Select();
        // Translators: Button that returns from Spotify tracks or search results to the browser.
        var back = new Button(_dialog, StandardId.Cancel, Tr("Back"));
        var layout = new BoxSizer(Orientation.Vertical);
        if (!string.IsNullOrWhiteSpace(description))
            layout.Add(new StaticText(_dialog, label: description),
                flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        layout.Add(_status, flags: SizerFlags.All, border: 8);
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

    internal Track? Show()
    {
        LoadNextPage(selectNew: false);
        _dialog.ShowModal();
        return _selected;
    }

    private void LoadNextPage(bool selectNew)
    {
        if (_disposed || _loading || !_hasMore) return;
        _loading = true;
        // Translators: Loading status in the Spotify collection window while its track list is fetched.
        _status.Label = Tr("Loading tracks from Spotify...");
        var cursor = NextCursor;
        _ = Task.Run(async () =>
        {
            try
            {
                var page = await _load(cursor, _token).ConfigureAwait(false);
                // Only one page is in flight. Stop a repeated cursor before it starts an
                // unbounded sequence of requests through empty or duplicate pages.
                if (page.Next is not null && (page.Next == cursor || _loadedPages.Contains(page.Next)))
                    throw new InvalidDataException("Spotify repeated a track page.");
                Wx.CallAfter(() =>
                {
                    if (_disposed) return;
                    _loading = false;
                    _loadedPages.Add(cursor ?? string.Empty);
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
                        // Translators: Status in the Spotify collection window. {count} is the number of tracks loaded; Down means the keyboard arrow key.
                        : _hasMore ? TrFormat("{count} tracks loaded. Press Down at the end for more.", _tracks.Count)
                        // Translators: Status in the Spotify collection window. {count} is the number of tracks loaded; Down means the keyboard arrow key.
                        : TrFormat("{count} tracks loaded.", _tracks.Count);
                    if (_tracks.Count == previousCount && _hasMore) LoadNextPage(selectNew);
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
