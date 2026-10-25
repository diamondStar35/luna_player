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

    internal IReadOnlyList<Track> CurrentQueue => _queue.ToArray();
    internal IReadOnlyList<Track> SourceQueue => _sourceQueue.ToArray();
    internal int CurrentIndex => _index;
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
            _pageCancellation?.Cancel();
            _pageCancellation?.Dispose();
            _pageCancellation = null;
            _fetchingMore = false;
            _pageFetchAfterEnded = false;
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
    internal bool GoToIndex(int index)
    {
        lock (_navigationLock)
            return _active && index >= 0 && index < _queue.Count
                && Step(index - _index);
    }

    private bool Step(int delta, bool savePosition = true)
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
            if (_fetchingMore)
            {
                _pageCancellation?.Cancel();
                _pageCancellation?.Dispose();
                _pageCancellation = null;
                _fetchingMore = false;
                _pageFetchAfterEnded = false;
            }
            if (savePosition) SavePosition();
            _index = target;
            var track = _queue[target];
            // Explicit navigation starts the selected file, matching ordinary local playback.
            SetCurrent(track, playing: false);
            Navigated?.Invoke(track);
            // Each key changes Luna's selection immediately. Only the final selection
            // after a short burst starts fetching audio, avoiding wasted key requests.
            if (!_navigationStopSent) { _player.Stop(); _navigationStopSent = true; }
            _ = LoadAfterNavigationAsync(track, Volatile.Read(ref _loadGeneration));
            return true;
        }
    }

    private void FetchMore(bool afterTrackEnded = false, int pagesRemaining = 3)
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
        _ = Task.Run(async () =>
        {
            try
            {
                var page = await loader(cursor, cancellation.Token).ConfigureAwait(false);
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
                        _nextPage = page.Next == cursor ? null : page.Next;
                        var ended = afterTrackEnded || _pageFetchAfterEnded;
                        _fetchingMore = false;
                        _pageFetchAfterEnded = false;
                        _pageCancellation = null;
                        cancellation.Dispose();
                        if (_index + 1 < _queue.Count)
                        {
                            Step(1, savePosition: !ended);
                        }
                        else if (_nextPage is not null && pagesRemaining > 1)
                            FetchMore(ended, pagesRemaining - 1);
                        else if (ended) FinishCurrent();
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

    private async Task LoadAfterNavigationAsync(Track track, int generation)
    {
        await Task.Delay(250).ConfigureAwait(false);
        OnPlayerThread(() =>
        {
            lock (_navigationLock)
            {
                if (!_active || Volatile.Read(ref _loadGeneration) != generation || _current?.Uri != track.Uri)
                    return;
                LoadTrack(track, SavedPosition(track));
                if (!_playRequested) PauseLoaded();
            }
        });
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
