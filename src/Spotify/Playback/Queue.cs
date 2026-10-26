using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Spotify.Playback;

/// <summary>Keeps the Spotify queue order and paged navigation separate from its audio transport.</summary>
internal sealed partial class Sessions
{
    private readonly object _navigationLock = new();
    private Session? _session;

    // A manager exists at startup; queue and navigation state belong only to a selected session.
    private sealed class Session
    {
        internal List<Track> Tracks { get; set; } = [];
        internal List<Track> Source { get; set; } = [];
        internal int Index { get; set; } = -1;
        internal bool Shuffle { get; set; }
        internal bool NavigationStopped { get; set; }
        internal Func<string?, CancellationToken, Task<TrackPage>>? PageLoader { get; set; }
        internal string? NextPage { get; set; }
        internal CancellationTokenSource? PageCancellation { get; set; }
        internal bool FetchingMore { get; set; }
        internal bool PageFetchAfterEnded { get; set; }
        internal bool DeduplicateArtist { get; set; }
        internal CancellationTokenSource? NavigationDelay { get; set; }
        internal HashSet<string> PageCursors { get; } = new(StringComparer.Ordinal);
    }

    internal IReadOnlyList<Track> CurrentQueue => _session?.Tracks.ToArray() ?? Array.Empty<Track>();
    internal IReadOnlyList<Track> SourceQueue => _session?.Source.ToArray() ?? Array.Empty<Track>();
    internal int CurrentIndex => _session?.Index ?? -1;
    internal bool CanMoveNext => _active && _session is { } session
        && (session.Index + 1 < session.Tracks.Count || session.NextPage is not null
            || _audioSettings?.WrapPlaylist == true && session.Tracks.Count > 1);
    internal bool CanMovePrevious => _active && _session is { } session
        && (session.Index > 0 || _audioSettings?.WrapPlaylist == true && session.Tracks.Count > 1);
    internal bool IsShuffleEnabled => _session?.Shuffle == true;
    internal bool ToggleShuffle()
    {
        if (_session is not { } session) return false;
        lock (_navigationLock)
        {
            if (!_active) return false;
            var current = _current;
            session.Shuffle = !session.Shuffle;
            session.Tracks = [.. session.Source];
            if (session.Shuffle)
            {
                // Like Luna's local shuffle, randomize the entire order and then locate
                // the current track. Pinning it first prevents ordinary Previous navigation.
                for (var index = session.Tracks.Count - 1; index > 0; index--)
                {
                    var other = Random.Shared.Next(index + 1);
                    (session.Tracks[index], session.Tracks[other]) = (session.Tracks[other], session.Tracks[index]);
                }
            }
            session.Index = session.Tracks.FindIndex(track => ReferenceEquals(track, current));
            if (session.Index < 0) session.Index = 0;
            return session.Shuffle;
        }
    }
    internal string? NextPage => _session?.NextPage;
    internal void Play(Track track, IReadOnlyList<Track>? queue,
        CancellationToken cancellationToken, double? startPosition = null,
        Func<string?, CancellationToken, Task<TrackPage>>? pageLoader = null,
        string? nextPage = null, bool deduplicateArtist = false, bool shuffle = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeginSession();
        var session = _session ?? throw new InvalidOperationException("No Spotify session is open.");
        lock (_navigationLock)
        {
            SavePosition();
            if (_active) _player.Stop();
            CancelPageRead();
            session.PageCursors.Clear();
            session.PageLoader = pageLoader;
            session.NextPage = nextPage;
            session.DeduplicateArtist = deduplicateArtist;
            session.NavigationStopped = false;
            var tracks = queue?.Where(item => IsTrack(item.Uri)).ToList() ?? [];
            var selected = tracks.FindIndex(item => ReferenceEquals(item, track));
            if (selected < 0) selected = tracks.FindIndex(item => item.Uri == track.Uri);
            if (selected < 0) { tracks.Insert(0, track); selected = 0; }
            session.Tracks = tracks;
            session.Source = [.. tracks];
            session.Index = selected;
            _active = true;
            session.Shuffle = false;
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
        if (_session is not { } session) return false;
        lock (_navigationLock)
        {
            if (!HasSession) return false;
            if (!_active || _current is null)
            {
                QueueEmpty?.Invoke();
                return true;
            }
            SavePosition();
            var removed = _current;
            var position = session.Index;
            CancelPageRead();
            _player.Stop();
            session.Tracks.RemoveAt(position);
            var original = session.Source.FindIndex(track => ReferenceEquals(track, removed));
            if (original >= 0) session.Source.RemoveAt(original);
            if (session.Tracks.Count > 0)
            {
                session.Index = Math.Min(position, session.Tracks.Count - 1);
                var next = session.Tracks[session.Index];
                SetCurrent(next, playing: false);
                LoadTrack(next, SavedPosition(next));
            }
            else
            {
                ClearCurrent();
                session.Index = -1;
                if (session.NextPage is not null) FetchMore(afterTrackEnded: true);
                else QueueEmpty?.Invoke();
            }
            return true;
        }
    }

    private void CancelPageRead()
    {
        if (_session is not { } session) return;
        session.PageCancellation?.Cancel();
        session.PageCancellation?.Dispose();
        session.PageCancellation = null;
        session.FetchingMore = false;
        session.PageFetchAfterEnded = false;
    }
    internal bool GoToIndex(int index)
    {
        lock (_navigationLock)
            return _active && _session is { } session && index >= 0 && index < session.Tracks.Count
                && Step(index - session.Index);
    }

    private bool Step(int delta, bool savePosition = true, bool delay = true)
    {
        if (_session is not { } session) return false;
        lock (_navigationLock)
        {
            if (delta > 0 && session.Index == session.Tracks.Count - 1 && session.NextPage is not null)
            {
                _playRequested = true;
                FetchMore();
                return true;
            }
            if (!TryTarget(session.Index, delta, out var target)) return false;
            CancelPageRead();
            if (savePosition) SavePosition();
            session.Index = target;
            var track = session.Tracks[target];
            // Explicit navigation starts the selected file, matching ordinary local playback.
            SetCurrent(track, playing: false);
            Navigated?.Invoke(track);
            // Each key changes Luna's selection immediately. Only the final selection
            // after a short burst starts fetching audio, avoiding wasted key requests.
            if (delay)
            {
                if (!session.NavigationStopped) { _player.Stop(); session.NavigationStopped = true; }
                session.NavigationDelay = new CancellationTokenSource();
                _ = LoadAfterNavigationAsync(track, CurrentLoadGeneration, session.NavigationDelay.Token);
            }
            else LoadTrack(track, SavedPosition(track));
            return true;
        }
    }

    private void FetchMore(bool afterTrackEnded = false)
    {
        if (_session is not { } session) return;
        if (session.FetchingMore)
        {
            if (afterTrackEnded) session.PageFetchAfterEnded = true;
            return;
        }
        if (session.PageLoader is not { } loader || session.NextPage is not { } cursor) return;
        session.FetchingMore = true;
        session.PageCancellation = new CancellationTokenSource();
        var cancellation = session.PageCancellation;
        var generation = Volatile.Read(ref _loadGeneration);
        var current = _current;
        var token = cancellation.Token;
        var visited = new HashSet<string>(session.PageCursors, StringComparer.Ordinal) { cursor };
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
                        if (!ReferenceEquals(_session, session) || !_active || cancellation.IsCancellationRequested
                            || Volatile.Read(ref _loadGeneration) != generation || _current != current) return;
                        List<Track> added;
                        if (session.DeduplicateArtist)
                        {
                            var known = new HashSet<string>(session.Tracks.Select(track => track.ArtistSongKey), StringComparer.Ordinal);
                            added = page.Tracks.Where(track => known.Add(track.ArtistSongKey)).ToList();
                        }
                        else added = page.Tracks.ToList();
                        session.Source.AddRange(added);
                        if (session.Shuffle)
                        {
                            // Newly discovered tracks join random positions in the full order,
                            // matching how Luna repairs its shuffle when a playlist grows.
                            foreach (var addedTrack in added)
                                session.Tracks.Insert(Random.Shared.Next(session.Tracks.Count + 1), addedTrack);
                            session.Index = session.Tracks.FindIndex(track => ReferenceEquals(track, _current));
                        }
                        else session.Tracks.AddRange(added);
                        session.PageCursors.Add(cursor);
                        session.NextPage = page.Next;
                        var ended = afterTrackEnded || session.PageFetchAfterEnded;
                        session.FetchingMore = false;
                        session.PageFetchAfterEnded = false;
                        session.PageCancellation = null;
                        cancellation.Dispose();
                        if (session.Index + 1 < session.Tracks.Count)
                        {
                            Step(1, savePosition: !ended, delay: !ended);
                        }
                        else if (session.NextPage is not null)
                            FetchMore(ended);
                        else if (session.Tracks.Count == 0)
                            QueueEmpty?.Invoke();
                        else if (ended)
                            AdvanceAtEndCore();
                        else if (_audioSettings?.WrapPlaylist == true && session.Tracks.Count > 1)
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
                        if (!ReferenceEquals(_session, session) || !_active || cancellation.IsCancellationRequested
                            || Volatile.Read(ref _loadGeneration) != generation) return;
                        var ended = afterTrackEnded || session.PageFetchAfterEnded;
                        session.FetchingMore = false;
                        session.PageFetchAfterEnded = false;
                        session.PageCancellation = null;
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
        if (_session is not { } session) return false;
        if (session.Tracks.Count == 0) return false;
        if (target >= 0 && target < session.Tracks.Count) return true;
        if (_audioSettings?.WrapPlaylist != true || session.Tracks.Count < 2) return false;
        target = (target + session.Tracks.Count) % session.Tracks.Count;
        return true;
    }

    private void CancelNavigationDelay()
    {
        if (_session is not { } session) return;
        session.NavigationDelay?.Cancel();
        session.NavigationDelay?.Dispose();
        session.NavigationDelay = null;
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
        if (_session is not { } session) return;
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
        if (_audioSettings?.EndBehavior == EndBehavior.None || session.Tracks.Count == 0)
        {
            FinishCurrent();
            return;
        }
        var next = session.Index + 1;
        if (next >= session.Tracks.Count)
        {
            if (session.NextPage is not null)
            {
                SetPlaying(false);
                FetchMore(afterTrackEnded: true);
                return;
            }
            if (_audioSettings?.WrapPlaylist != true || session.Tracks.Count < 2)
            {
                FinishCurrent();
                return;
            }
            next = 0;
        }
        session.Index = next;
        var track = session.Tracks[session.Index];
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
