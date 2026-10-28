using LunaPlayer.Accessibility;
using LunaPlayer.Application;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playlist;
using LunaPlayer.UI;
using LunaPlayer.YouTube.Playback;
using Player = LunaPlayer.Playback.Player;

namespace LunaPlayer.Radio;

/// <summary>Owns one radio category session. Visited entries are retained, while the category defines their order.</summary>
internal sealed class Playback : IDisposable
{
    private readonly IMainView _view;
    private readonly Player _player;
    private readonly Settings _settings;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly ISpeechOutput _speech;
    private readonly IClipboardService _clipboard;
    private readonly Action _leaveYouTube;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<Task> _writes = [];
    private Data? _data;
    private Session? _session;
    private CancellationTokenSource? _navigation;
    private int _generation;
    private bool _opening;
    private bool _disposed;
    private bool _shuttingDown;

    private sealed class Data
    {
        internal Catalog Catalog { get; } = new();
        internal Library Library { get; } = new();
        internal Browsing Browsing { get; } = new();
    }

    private sealed class Session(StationView view, int index)
    {
        internal StationView View { get; } = view;
        internal int Index { get; set; } = index;
        internal Dictionary<Guid, int> Entries { get; } = [];
        internal ShuffleOrder Shuffle { get; } = new();
        internal bool Shuffled { get; set; }
        internal int Position => Shuffled ? Shuffle.IndexOf(Index) : Index;
        internal int At(int position) => Shuffled ? Shuffle[position] : position;
    }

    internal Playback(IMainView view, Player player, Settings settings, IApplicationDispatcher dispatcher,
        ISpeechOutput speech, IClipboardService clipboard, Action leaveYouTube)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _dispatcher = dispatcher;
        _speech = speech;
        _clipboard = clipboard;
        _leaveYouTube = leaveYouTube;
        player.CurrentChanged += OnCurrentChanged;
    }

    internal bool IsActive => _session is not null && _player.InRadioSession;
    internal int Count => _session?.View.Stations.Count ?? 0;
    internal int CurrentIndex => _session?.Position ?? -1;
    internal bool IsShuffleEnabled => _session?.Shuffled == true;
    internal bool CanMoveNext => IsActive && (CurrentIndex + 1 < Count || _session!.View.HasMore
        || _settings.Audio.WrapPlaylist && Count > 1);
    internal bool CanMovePrevious => IsActive && (CurrentIndex > 0 || _settings.Audio.WrapPlaylist && Count > 1);
    internal IReadOnlyList<Station> Stations => _session is { } session
        ? Enumerable.Range(0, Count).Select(index => session.View.Stations[session.At(index)]).ToArray() : [];

    internal void Open() => ShowBrowser(leavePlayback: false);

    private void ShowBrowser(bool leavePlayback)
    {
        if (_opening || _disposed) return;
        _opening = true;
        try
        {
            var returnToStation = IsActive;
            if (returnToStation)
            {
                _data!.Browsing.Restore(_session!.View);
                if (leavePlayback) Stop(restore: true);
            }
            var data = _data ??= new Data();
            if (!returnToStation)
            {
                data.Browsing.Invalidate(CategoryKind.Favorites);
                data.Browsing.Invalidate(CategoryKind.Recent);
            }
            var choice = _view.Dialogs.ShowRadioBrowser(data.Browsing, data.Catalog, data.Library,
                text => _speech.SpeakText(text), _clipboard, returnToStation);
            if (choice is null || _disposed) return;
            var index = choice.Stations.FindIndex(station => station.StationUuid == choice.Selected);
            if (index < 0) return;
            CancelNavigation();
            _leaveYouTube();
            _player.BeginRadioSession();
            _session = new Session(choice, index);
            Play(index, announce: false);
        }
        finally { _opening = false; }
    }

    internal bool HandleEscape()
    {
        if (!IsActive) return false;
        ShowBrowser(leavePlayback: true);
        return true;
    }

    internal NavigationOutcome TryMove(int direction)
    {
        if (!IsActive || _session is not { } session) return NavigationOutcome.NotOurs;
        CancelNavigation();
        var target = session.Position + Math.Sign(direction);
        if (target >= Count && session.View.HasMore && direction > 0)
        {
            FetchMore(session);
            return NavigationOutcome.Pending;
        }
        if (target < 0 || target >= Count)
        {
            if (!_settings.Audio.WrapPlaylist || Count < 2) return NavigationOutcome.Exhausted;
            target = (target + Count) % Count;
        }
        return GoToIndex(target);
    }

    internal NavigationOutcome GoToIndex(int index)
    {
        if (!IsActive || _session is not { } session) return NavigationOutcome.NotOurs;
        CancelNavigation();
        if (index < 0 || index >= Count || session.At(index) == session.Index) return NavigationOutcome.Exhausted;
        return Play(session.At(index), announce: false);
    }

    internal bool ToggleShuffle()
    {
        if (_session is not { } session) return false;
        session.Shuffled = !session.Shuffled;
        if (session.Shuffled) session.Shuffle.Create(Count, session.Index);
        _player.RefreshState();
        return session.Shuffled;
    }

    internal void ShowOpenedFiles()
    {
        while (IsActive)
        {
            var stations = Stations;
            var request = _view.Dialogs.ChooseOpenedFile(stations.Count,
                index => index >= 0 && index < stations.Count ? stations[index].Name : string.Empty, CurrentIndex);
            if (request is null) return;
            if (request.Value.Action == LunaPlayer.UI.Playlists.OpenedFilesAction.Jump)
            {
                if (GoToIndex(request.Value.SelectedIndex) == NavigationOutcome.Advanced
                    && _session is { } session) Announce(session.View.Stations[session.Index]);
                return;
            }
            // A live station has no useful duration to scan or add to a playlist total.
            _view.Dialogs.ShowTextInfo(
                // Translators: Title of the summary of loaded stations in a radio playback session.
                Tr("Radio session"),
                // Translators: Radio session summary. {count} is the number of stations loaded for the selected category.
                TrFormat("Loaded stations: {count}", stations.Count));
        }
    }

    private NavigationOutcome Play(int index, bool announce)
    {
        if (_session is not { } session) return NavigationOutcome.Exhausted;
        var station = session.View.Stations[index];
        if (Catalog.StreamUrl(station) is { } url)
            return OpenStation(session, station, url, index, announce) ? NavigationOutcome.Advanced : NavigationOutcome.Exhausted;
        // Keep an existing station audible while its replacement is resolved. The initial transition
        // stops only after BeginRadioSession has captured the original file and its exact position.
        if (session.Entries.Count == 0) _player.Stop();
        var token = BeginNavigation();
        var generation = _generation;
        _ = ResolveAsync(session, station, index, generation, token);
        return NavigationOutcome.Pending;
    }

    private async Task ResolveAsync(Session session, Station station, int index, int generation, CancellationToken token)
    {
        try
        {
            var found = await _data!.Catalog.FindStationAsync(station.StationUuid, token).ConfigureAwait(false);
            var updated = found.FirstOrDefault(item => item.StationUuid == station.StationUuid);
            if (updated is null || Catalog.StreamUrl(updated) is not { } url) throw new InvalidDataException();
            _dispatcher.Post(() =>
            {
                if (!Current(session, generation, token)) return;
                session.View.Stations[index] = updated;
                OpenStation(session, updated, url, index, announce: true);
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer station, session exit or shutdown owns playback now.
        }
        catch (Exception error) { Report(error, session, generation, token); }
    }

    private bool OpenStation(Session session, Station station, string url, int index, bool announce)
    {
        var previous = session.Index;
        session.Index = index;
        session.View.Selected = station.StationUuid;
        var source = $"radio-browser://{station.StationUuid:D}";
        var existing = session.Entries.TryGetValue(station.StationUuid, out var entry) ? (int?)entry : null;
        if (!_player.PlayRadioStation(url, station.Name, source, existing, out entry))
        {
            session.Index = previous;
            session.View.Selected = session.View.Stations[previous].StationUuid;
            // Translators: Error shown when MPV could not accept the selected radio station.
            _view.Dialogs.ShowError(Tr("Could not open the radio station."),
                // Translators: Title of a radio playback error.
                Tr("Radio error"));
            return false;
        }
        session.Entries[station.StationUuid] = entry;
        if (announce) Announce(station);
        TrackWrite(_data!.Library.RememberAsync(station));
        _ = CountClickAsync(station.StationUuid, _lifetime.Token);
        return true;
    }

    private async Task CountClickAsync(Guid id, CancellationToken token)
    {
        try { await _data!.Catalog.Client.CountClickAsync(id, token).ConfigureAwait(false); }
        catch (Exception error) when (error is OperationCanceledException or Client.RequestException or ObjectDisposedException)
        {
            // Click reporting is best effort. It neither gates playback nor retries a possible mutation.
        }
    }

    private void FetchMore(Session session)
    {
        var token = BeginNavigation();
        var generation = _generation;
        var request = session.View.Request();
        _ = Task.Run(async () =>
        {
            try
            {
                var pages = new List<StationPage>();
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    var page = await _data!.Catalog.StationsAsync(request, _data.Library, token).ConfigureAwait(false);
                    pages.Add(page);
                    request.Offset = page.NextOffset;
                    if (page.Stations.Count > 0 || !page.HasMore) break;
                }
                _dispatcher.Post(() =>
                {
                    if (!Current(session, generation, token)) return;
                    foreach (var page in pages) session.View.Append(page);
                    if (session.Shuffled) session.Shuffle.Sync(Count, session.Index);
                    var next = session.Position + 1;
                    if (next >= Count && !session.View.HasMore && _settings.Audio.WrapPlaylist && Count > 1) next = 0;
                    if (next < Count) Play(session.At(next), announce: true);
                });
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Navigation in the other direction or a new source superseded this page request.
            }
            catch (Exception error) { Report(error, session, generation, token); }
        });
    }

    private bool Current(Session session, int generation, CancellationToken token)
        => !_disposed && !token.IsCancellationRequested && ReferenceEquals(_session, session)
            && _generation == generation && _player.InRadioSession;

    private void Announce(Station station)
    {
        if (_settings.General.SpeakFileOnNavigation) _speech.Speak(station.Name, station.Name);
    }

    private CancellationToken BeginNavigation()
    {
        CancelNavigation();
        _navigation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        return _navigation.Token;
    }

    private void CancelNavigation()
    {
        _generation++;
        _navigation?.Cancel();
        _navigation?.Dispose();
        _navigation = null;
    }

    private void Report(Exception error, Session session, int generation, CancellationToken token)
        => _dispatcher.Post(() =>
        {
            if (!Current(session, generation, token)) return;
            // Translators: Title of a message reporting a radio station navigation or preparation failure.
            _view.Dialogs.ShowError(Catalog.DescribeError(error), Tr("Radio error"));
        });

    private void TrackWrite(Task work)
    {
        _writes.RemoveAll(task => task.IsCompleted);
        _writes.Add(ObserveWriteAsync(work));
    }

    private async Task ObserveWriteAsync(Task work)
    {
        try { await work.ConfigureAwait(false); }
        catch (Exception error)
        {
            _dispatcher.Post(() =>
            {
                if (_disposed || _shuttingDown) return;
                // Translators: Title of a warning when local radio favorites or history could not be saved.
                _view.Dialogs.ShowWarning(Catalog.DescribeError(error), Tr("Radio library"));
            });
        }
    }

    internal Task FlushAsync() => Task.WhenAll(Task.WhenAll(_writes.ToArray()),
        _data?.Library.FlushAsync() ?? Task.CompletedTask);

    internal Task ShutdownAsync()
    {
        _shuttingDown = true;
        Stop(restore: false);
        return FlushAsync();
    }

    internal void Stop(bool restore)
    {
        CancelNavigation();
        _session = null;
        if (restore && _player.InRadioSession) _player.LeaveSession();
        else _player.DiscardRadioSession();
    }

    private void OnCurrentChanged()
    {
        if (_session is not null && !_player.InRadioSession) Stop(restore: false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelNavigation();
        _lifetime.Cancel();
        _player.CurrentChanged -= OnCurrentChanged;
        _data?.Catalog.Dispose();
        _lifetime.Dispose();
    }
}
