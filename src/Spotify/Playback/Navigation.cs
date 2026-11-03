using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Spotify.Playback;

internal sealed partial class Sessions
{
    private readonly object _navigationLock = new();

    private Session? SynchronizeNavigation()
    {
        lock (_navigationLock)
        {
            // mpv may advance before its UI notification is processed. Adopt that track before
            // a command reads the queue index, position, or playback state.
            if (_active && _player.InSpotifySession) AcceptSpotifyAdvance();
            return _active && _player.InSpotifySession ? _session : null;
        }
    }

    internal bool Next() => Step(1);
    internal bool Previous() => Step(-1);

    internal bool ToggleShuffle()
    {
        lock (_navigationLock)
        {
            if (SynchronizeNavigation() is not { } session) return false;
            var current = _current;
            session.Shuffle = !session.Shuffle;
            session.Tracks = [.. session.Source];
            if (session.Shuffle)
            {
                // Keep the current track at its shuffled position so Previous can reach
                // tracks on either side of it.
                for (var index = session.Tracks.Count - 1; index > 0; index--)
                {
                    var other = Random.Shared.Next(index + 1);
                    (session.Tracks[index], session.Tracks[other]) = (session.Tracks[other], session.Tracks[index]);
                }
            }
            session.Index = session.Tracks.FindIndex(track => ReferenceEquals(track, current));
            if (session.Index < 0) session.Index = 0;
            PrepareSuccessor();
            return session.Shuffle;
        }
    }

    internal bool CloseCurrent()
    {
        lock (_navigationLock)
        {
            var session = SynchronizeNavigation() ?? _session;
            if (session is null) return false;
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

    internal bool GoToIndex(int index)
    {
        lock (_navigationLock)
        {
            if (SynchronizeNavigation() is not { } session || index < 0 || index >= session.Tracks.Count) return false;
            return SelectTrack(session, index);
        }
    }

    private bool Step(int delta, bool savePosition = true, bool delay = true)
    {
        lock (_navigationLock)
        {
            if (SynchronizeNavigation() is not { } session) return false;
            if (delta > 0 && session.Index == session.Tracks.Count - 1 && session.NextPage is not null)
            {
                _playRequested = true;
                FetchMore();
                return true;
            }
            return TryTarget(session.Index, delta, out var target) && SelectTrack(session, target, savePosition, delay);
        }
    }

    private bool SelectTrack(Session session, int target, bool savePosition = true, bool delay = true)
    {
        CancelPageRead();
        if (savePosition) SavePosition();
        var keepPlaying = _manualTransition ? _playRequested : _player.IsPlaying;
        var overlap = _audioSettings is { Crossfade: true, CrossfadeManualNavigation: true } && _player.IsLoaded;
        session.Index = target;
        var track = session.Tracks[target];
        var preparedUrl = TakePreparedSuccessor(track);
        SetCurrent(track, playing: false);
        _manualTransition = overlap;
        if (overlap) _playRequested = keepPlaying;
        Navigated?.Invoke(track);
        if (preparedUrl is not null && OpenPreparedTrack(track, preparedUrl)) return true;
        if (delay)
        {
            if (!overlap && !session.NavigationStopped) { _player.Stop(); session.NavigationStopped = true; }
            session.NavigationDelay = new CancellationTokenSource();
            _ = LoadAfterNavigationAsync(track, CurrentLoadGeneration, session.NavigationDelay.Token);
        }
        else LoadTrack(track, SavedPosition(track));
        return true;
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
            await Task.Delay(75, token).ConfigureAwait(false);
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
}
