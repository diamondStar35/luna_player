using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Spotify.Playback;

internal sealed partial class Sessions
{
    private Successor? _successor;
    private bool _manualTransition;

    private void CancelSuccessor()
    {
        var successor = _successor;
        _successor = null;
        successor?.Dispose();
        _player.ClearSpotifySuccessor(successor?.Url);
    }

    private void RefreshSuccessor()
    {
        lock (_navigationLock)
        {
            SynchronizeNavigation();
            PrepareSuccessor();
        }
    }

    private void PrepareSuccessor()
    {
        lock (_navigationLock)
        {
            if (!_active || _ended || !IsLoadedCurrent || _audioSettings?.Crossfade != true || _repeatCurrent
                || _audioSettings?.EndBehavior != EndBehavior.Advance || _session is not { } session
                || _current is not { } current) { CancelSuccessor(); return; }
            var next = session.Index + 1;
            if (next >= session.Tracks.Count && session.NextPage is not null)
            {
                CancelSuccessor();
                FetchMore(prepareOnly: true);
                return;
            }
            if (!TryTarget(session.Index, 1, out next)) { CancelSuccessor(); return; }
            var track = session.Tracks[next];
            if (_successor is { } pending && ReferenceEquals(pending.Track, track)
                && pending.Bitrate == RequestedBitrate && pending.Generation == CurrentLoadGeneration) return;
            CancelSuccessor();
            var successor = new Successor(track, RequestedBitrate, CurrentLoadGeneration);
            _successor = successor;
            _ = Task.Run(() => PrepareSuccessorAsync(successor, session, current));
        }
    }

    private async Task PrepareSuccessorAsync(Successor successor, Session session, Track current)
    {
        AudioDecryptStream? audio = null;
        CancellationTokenRegistration registration = default;
        try
        {
            audio = await _prepareAudio(successor.Track.Uri, successor.Bitrate, successor.Token).ConfigureAwait(false);
            var prepared = audio;
            // The dispatcher can discard callbacks at shutdown; cancellation must still release
            // audio whose ownership has not yet reached the engine.
            registration = successor.Token.UnsafeRegister(static value => ((AudioDecryptStream)value!).Dispose(), prepared);
            var pending = registration;
            if (OnPlayerThread(() =>
            {
                pending.Dispose();
                lock (_navigationLock)
                {
                    if (successor.Token.IsCancellationRequested || !_active || !_player.InSpotifySession
                        || !ReferenceEquals(_session, session) || !ReferenceEquals(_current, current)
                        || successor.Generation != CurrentLoadGeneration || !ReferenceEquals(_successor, successor))
                    {
                        prepared.Dispose();
                        return;
                    }
                    try
                    {
                        var url = _player.QueueSpotifyAudio(Link(current), prepared,
                            successor.Track.Title, Link(successor.Track), ResumePosition(successor.Track));
                        if (url is null) CancelSuccessor();
                        else successor.Complete(url);
                    }
                    catch
                    {
                        prepared.Dispose();
                        CancelSuccessor();
                        // Preparation is optional. Ordinary loading reports errors if this track
                        // is later selected or reached at EOF.
                    }
                }
            }))
            {
                audio = null;
                registration = default;
            }
        }
        catch (Exception)
        {
            OnPlayerThread(() =>
            {
                lock (_navigationLock)
                    if (ReferenceEquals(_successor, successor)) CancelSuccessor();
            });
            // A cancelled or failed preparation must not interrupt the current track.
        }
        finally
        {
            registration.Dispose();
            audio?.Dispose();
        }
    }

    private bool AcceptSpotifyAdvance()
    {
        if (_session is not { } session || _successor is not { Url: not null } successor
            || !_player.AcceptQueuedAdvance()) return false;
        // Detach the registration before SetCurrent cancels preparation. The native decoder
        // has already started this URL and must retain its audio.
        successor.TakeUrl();
        var track = successor.Track;
        SavePosition(0);
        session.Index = session.Tracks.FindIndex(item => ReferenceEquals(item, track));
        SetCurrent(track, playing: true);
        _manualTransition = false;
        _loadedUri = track.Uri;
        _currentLoadSent = true;
        Navigated?.Invoke(track);
        if (BeforeTrackEnd?.Invoke() == true && _active) FinishCurrent();
        return true;
    }

    private string? TakePreparedSuccessor(Track track)
    {
        if (_successor is not { Url: not null } successor || !ReferenceEquals(successor.Track, track)) return null;
        var url = successor.TakeUrl();
        CancelSuccessor();
        return url;
    }

    private bool OpenPreparedTrack(Track track, string url)
    {
        if (!_player.OpenPreparedSpotifyAudio(url, track.Title, Link(track), ResumePosition(track),
            paused: !_playRequested, fadeTransition: _manualTransition))
        {
            _player.ClearSpotifySuccessor(url);
            return false;
        }
        MarkTrackLoaded(track.Uri);
        return true;
    }

    private void MarkTrackLoaded(string uri)
    {
        _reportedFailure = null;
        _manualTransition = false;
        _currentLoadSent = true;
        _loadedUri = uri;
        _pendingSeek = null;
        if (!_playRequested) _player.Pause();
        NotifyTrackPlaying(uri);
        PrepareSuccessor();
    }

    private static string Link(Track track) => "https://open.spotify.com/track/" + track.Uri[14..];
}
