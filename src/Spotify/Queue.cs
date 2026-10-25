using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Spotify;

/// <summary>Keeps the Spotify queue order and paged navigation separate from its audio transport.</summary>
internal sealed partial class Playback
{
    private readonly object _navigationLock = new();
    private List<Track> _queue = [];
    private List<Track> _sourceQueue = [];
    private int _index;
    private bool _shuffle;
    private bool _navigationStopSent;
    private Func<string?, CancellationToken, Task<TrackPage>>? _pageLoader;
    private string? _nextPage;
    private CancellationTokenSource? _pageCancellation;
    private bool _fetchingMore;
    private bool _pageFetchAfterEnded;
    private bool _deduplicateArtist;
    private CancellationTokenSource? _navigationDelay;
    private readonly HashSet<string> _pageCursors = new(StringComparer.Ordinal);

    internal IReadOnlyList<Track> CurrentQueue => _queue.ToArray();
    internal IReadOnlyList<Track> SourceQueue => _sourceQueue.ToArray();
    internal int CurrentIndex => _index;
    internal bool CanMoveNext => _active && (_index + 1 < _queue.Count || _nextPage is not null
        || _audioSettings?.WrapPlaylist == true && _queue.Count > 1);
    internal bool CanMovePrevious => _active && (_index > 0
        || _audioSettings?.WrapPlaylist == true && _queue.Count > 1);
    internal bool IsShuffleEnabled => _shuffle;
    internal bool ToggleShuffle()
    {
        lock (_navigationLock)
        {
            if (!_active) return false;
            var current = _current;
            _shuffle = !_shuffle;
            _queue = [.. _sourceQueue];
            if (_shuffle)
            {
                // Like Luna's local shuffle, randomize the entire order and then locate
                // the current track. Pinning it first prevents ordinary Previous navigation.
                for (var index = _queue.Count - 1; index > 0; index--)
                {
                    var other = Random.Shared.Next(index + 1);
                    (_queue[index], _queue[other]) = (_queue[other], _queue[index]);
                }
            }
            _index = _queue.FindIndex(track => ReferenceEquals(track, current));
            if (_index < 0) _index = 0;
            return _shuffle;
        }
    }
    internal Func<string?, CancellationToken, Task<TrackPage>>? PageLoader => _pageLoader;
    internal string? NextPage => _nextPage;
    internal bool DeduplicateArtist => _deduplicateArtist;
    internal void Play(Track track, IReadOnlyList<Track>? queue,
        CancellationToken cancellationToken, double? startPosition = null,
        Func<string?, CancellationToken, Task<TrackPage>>? pageLoader = null,
        string? nextPage = null, bool deduplicateArtist = false, bool shuffle = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeginSession();
        lock (_navigationLock)
        {
            SavePosition();
            if (_active) _player.Stop();
            CancelPageRead();
            _pageCursors.Clear();
            _pageLoader = pageLoader;
            _nextPage = nextPage;
            _deduplicateArtist = deduplicateArtist;
            _navigationStopSent = false;
            var tracks = queue?.Where(item => IsTrack(item.Uri)).ToList() ?? [];
            var selected = tracks.FindIndex(item => ReferenceEquals(item, track));
            if (selected < 0) selected = tracks.FindIndex(item => item.Uri == track.Uri);
            if (selected < 0) { tracks.Insert(0, track); selected = 0; }
            _queue = tracks;
            _sourceQueue = [.. tracks];
            _index = selected;
            _active = true;
            _shuffle = false;
            // The shared mpv engine retains Luna's volume and audio effects.
            SetCurrent(track, playing: false);
            if (shuffle) ToggleShuffle();
            cancellationToken.ThrowIfCancellationRequested();
            LoadTrack(track, startPosition ?? SavedPosition(track));
        }
    }

    internal bool Next() => Step(1);
    internal bool Previous() => Step(-1);

    internal bool CloseCurrent()
    {
        lock (_navigationLock)
        {
            if (!_ownsPlayerSession) return false;
            if (!_active || _current is null)
            {
                QueueEmpty?.Invoke();
                return true;
            }
            SavePosition();
            var removed = _current;
            var position = _index;
            CancelPageRead();
            _player.Stop();
            _queue.RemoveAt(position);
            var original = _sourceQueue.FindIndex(track => ReferenceEquals(track, removed));
            if (original >= 0) _sourceQueue.RemoveAt(original);
            if (_queue.Count > 0)
            {
                _index = Math.Min(position, _queue.Count - 1);
                var next = _queue[_index];
                SetCurrent(next, playing: false);
                LoadTrack(next, SavedPosition(next));
            }
            else
            {
                ClearCurrent();
                _index = -1;
                if (_nextPage is not null) FetchMore(afterTrackEnded: true);
                else QueueEmpty?.Invoke();
            }
            return true;
        }
    }

    private void CancelPageRead()
    {
        _pageCancellation?.Cancel();
        _pageCancellation?.Dispose();
        _pageCancellation = null;
        _fetchingMore = false;
        _pageFetchAfterEnded = false;
    }
    internal bool GoToIndex(int index)
    {
        lock (_navigationLock)
            return _active && index >= 0 && index < _queue.Count
                && Step(index - _index);
    }

    private bool Step(int delta, bool savePosition = true, bool delay = true)
    {
        lock (_navigationLock)
        {
            if (delta > 0 && _index == _queue.Count - 1 && _nextPage is not null)
            {
                _playRequested = true;
                FetchMore();
                return true;
            }
            if (!TryTarget(_index, delta, out var target)) return false;
            CancelPageRead();
            if (savePosition) SavePosition();
            _index = target;
            var track = _queue[target];
            // Explicit navigation starts the selected file, matching ordinary local playback.
            SetCurrent(track, playing: false);
            Navigated?.Invoke(track);
            // Each key changes Luna's selection immediately. Only the final selection
            // after a short burst starts fetching audio, avoiding wasted key requests.
            if (delay)
            {
                if (!_navigationStopSent) { _player.Stop(); _navigationStopSent = true; }
                _navigationDelay = new CancellationTokenSource();
                _ = LoadAfterNavigationAsync(track, CurrentLoadGeneration, _navigationDelay.Token);
            }
            else LoadTrack(track, SavedPosition(track));
            return true;
        }
    }

    private void FetchMore(bool afterTrackEnded = false)
    {
        if (_fetchingMore)
        {
            if (afterTrackEnded) _pageFetchAfterEnded = true;
            return;
        }
        if (_pageLoader is not { } loader || _nextPage is not { } cursor) return;
        _fetchingMore = true;
        _pageCancellation = new CancellationTokenSource();
        var cancellation = _pageCancellation;
        var generation = Volatile.Read(ref _loadGeneration);
        var current = _current;
        var token = cancellation.Token;
        var visited = new HashSet<string>(_pageCursors, StringComparer.Ordinal) { cursor };
        _ = Task.Run(async () =>
        {
            try
            {
                var page = await loader(cursor, token).ConfigureAwait(false);
                if (page.Next is not null && visited.Contains(page.Next))
                    throw new InvalidDataException("Spotify repeated a track page.");
                OnPlayerThread(() =>
                {
                    lock (_navigationLock)
                    {
                        if (!_active || cancellation.IsCancellationRequested
                            || Volatile.Read(ref _loadGeneration) != generation || _current != current) return;
                        List<Track> added;
                        if (_deduplicateArtist)
                        {
                            var known = new HashSet<string>(_queue.Select(track => track.ArtistSongKey), StringComparer.Ordinal);
                            added = page.Tracks.Where(track => known.Add(track.ArtistSongKey)).ToList();
                        }
                        else added = page.Tracks.ToList();
                        _sourceQueue.AddRange(added);
                        if (_shuffle)
                        {
                            // Newly discovered tracks join random positions in the full order,
                            // matching how Luna repairs its shuffle when a playlist grows.
                            foreach (var addedTrack in added)
                                _queue.Insert(Random.Shared.Next(_queue.Count + 1), addedTrack);
                            _index = _queue.FindIndex(track => ReferenceEquals(track, _current));
                        }
                        else _queue.AddRange(added);
                        _pageCursors.Add(cursor);
                        _nextPage = page.Next;
                        var ended = afterTrackEnded || _pageFetchAfterEnded;
                        _fetchingMore = false;
                        _pageFetchAfterEnded = false;
                        _pageCancellation = null;
                        cancellation.Dispose();
                        if (_index + 1 < _queue.Count)
                        {
                            Step(1, savePosition: !ended, delay: !ended);
                        }
                        else if (_nextPage is not null)
                            FetchMore(ended);
                        else if (_queue.Count == 0)
                            QueueEmpty?.Invoke();
                        else if (ended)
                            AdvanceAtEndCore();
                        else if (_audioSettings?.WrapPlaylist == true && _queue.Count > 1)
                            Step(1);
                    }
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Stopping playback or selecting another track intentionally cancels this request.
            }
            catch (Exception error)
            {
                OnPlayerThread(() =>
                {
                    lock (_navigationLock)
                    {
                        if (!_active || cancellation.IsCancellationRequested
                            || Volatile.Read(ref _loadGeneration) != generation) return;
                        var ended = afterTrackEnded || _pageFetchAfterEnded;
                        _fetchingMore = false;
                        _pageFetchAfterEnded = false;
                        _pageCancellation = null;
                        cancellation.Dispose();
                        if (ended) FinishCurrent();
                        CommandFailed?.Invoke(error);
                    }
                });
            }
        });
    }

    private bool TryTarget(int index, int delta, out int target)
    {
        target = index + delta;
        if (_queue.Count == 0) return false;
        if (target >= 0 && target < _queue.Count) return true;
        if (_audioSettings?.WrapPlaylist != true || _queue.Count < 2) return false;
        target = (target + _queue.Count) % _queue.Count;
        return true;
    }

    private void CancelNavigationDelay()
    {
        _navigationDelay?.Cancel();
        _navigationDelay?.Dispose();
        _navigationDelay = null;
    }

    private async Task LoadAfterNavigationAsync(Track track, int generation, CancellationToken token)
    {
        try
        {
            await Task.Delay(250, token).ConfigureAwait(false);
            OnPlayerThread(() =>
            {
                lock (_navigationLock)
                {
                    if (token.IsCancellationRequested || !_active || CurrentLoadGeneration != generation
                        || !ReferenceEquals(_current, track)) return;
                    LoadTrack(track, SavedPosition(track));
                }
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer selection, session exit or source switch supersedes this Spotify-only delay.
        }
    }

    private void AdvanceAtEnd(string endedUri)
    {
        lock (_navigationLock)
        {
            if (!_active || _current?.Uri != endedUri) return;
            // A natural track change invalidates skip targets calculated before it.
            AdvanceAtEndCore();
        }
    }

    private void AdvanceAtEndCore()
    {
        SavePosition(0);
        if (_repeatCurrent && _current is { } current)
        {
            SetCurrent(current, playing: false);
            LoadTrack(current, 0);
            return;
        }
        if (_audioSettings?.EndBehavior == EndBehavior.Loop && _current is { } loopTrack)
        {
            SetCurrent(loopTrack, playing: false);
            LoadTrack(loopTrack, 0);
            return;
        }
        if (_audioSettings?.EndBehavior == EndBehavior.None || _queue.Count == 0)
        {
            FinishCurrent();
            return;
        }
        var next = _index + 1;
        if (next >= _queue.Count)
        {
            if (_nextPage is not null)
            {
                SetPlaying(false);
                FetchMore(afterTrackEnded: true);
                return;
            }
            if (_audioSettings?.WrapPlaylist != true || _queue.Count < 2)
            {
                FinishCurrent();
                return;
            }
            next = 0;
        }
        _index = next;
        var track = _queue[_index];
        SetCurrent(track, playing: false);
        Navigated?.Invoke(track);
        LoadTrack(track, SavedPosition(track));
    }

    private void FinishCurrent()
    {
        _playRequested = false;
        _ended = true;
        if (_clock is { } clock)
            _clock = clock with { Elapsed = clock.Duration, ObservedAt = DateTimeOffset.UtcNow };
        SetPlaying(false);
    }

}
