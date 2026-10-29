using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Spotify.Playback;

/// <summary>Prepares one successor and adopts native transitions without reopening their audio.</summary>
internal sealed partial class Sessions
{
    private CancellationTokenSource? _successorCancellation;
    private Track? _successor;
    private string? _successorUrl;
    private bool _manualTransition;
    private int _successorBitrate;

    private void CancelSuccessor()
    {
        _successorCancellation?.Cancel();
        _successorCancellation?.Dispose();
        _successorCancellation = null;
        _player.ClearSpotifySuccessor(_successorUrl);
        _successorUrl = null;
        _successor = null;
    }

    private void RefreshSuccessor()
    {
        lock (_navigationLock)
        {
            // Native playback may have committed a transition before its UI event is delivered.
            // Adopt that entry before replacing its identity during shuffle or settings changes.
            if (_active && _player.InSpotifySession) AcceptSpotifyAdvance(prepareNext: false);
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
            if (ReferenceEquals(_successor, track) && _successorBitrate == RequestedBitrate
                && (_successorCancellation is not null || _successorUrl is not null)) return;
            CancelSuccessor();
            _successorBitrate = RequestedBitrate;
            _successor = track;
            var cancellation = new CancellationTokenSource();
            _successorCancellation = cancellation;
            var token = cancellation.Token;
            var generation = CurrentLoadGeneration;
            var bitrate = RequestedBitrate;
            _ = Task.Run(async () =>
            {
                AudioDecryptStream? audio = null;
                CancellationTokenRegistration registration = default;
                try
                {
                    audio = await _prepareAudio(track.Uri, bitrate, token).ConfigureAwait(false);
                    var prepared = audio;
                    registration = token.UnsafeRegister(static value => ((AudioDecryptStream)value!).Dispose(), prepared);
                    var pending = registration;
                    if (OnPlayerThread(() =>
                    {
                        pending.Dispose();
                        lock (_navigationLock)
                        {
                            if (token.IsCancellationRequested || !_active || !_player.InSpotifySession
                                || !ReferenceEquals(_session, session) || !ReferenceEquals(_current, current)
                                || generation != CurrentLoadGeneration || !ReferenceEquals(_successor, track))
                            {
                                prepared.Dispose();
                                return;
                            }
                            try
                            {
                                _successorUrl = _player.QueueSpotifyAudio(Link(current), prepared,
                                    track.Title, Link(track), Math.Clamp(SavedPosition(track), 0,
                                        track.DurationMs > 0 ? Math.Max(0, track.DurationMs / 1000.0 - 1) : double.MaxValue));
                                // Registration transfers ownership to the engine. Its protocol adapter
                                // releases unclaimed entries; mpv closes streams already opened for prefetch.
                                _successorCancellation = null;
                                cancellation.Dispose();
                            }
                            catch
                            {
                                prepared.Dispose();
                                CancelSuccessor();
                                // Speculation is best effort; EOF uses the ordinary load/error path.
                            }
                        }
                    }))
                    {
                        audio = null;
                        registration = default;
                    }
                }
                catch (Exception error)
                {
                    Diagnostics.Write("prefetch.failed", track.Uri, "cancelled=" + token.IsCancellationRequested, error);
                    OnPlayerThread(() =>
                    {
                        lock (_navigationLock)
                            if (ReferenceEquals(_successorCancellation, cancellation)) CancelSuccessor();
                    });
                    // Cancellation and speculative failures leave the ordinary EOF path intact.
                }
                finally
                {
                    registration.Dispose();
                    audio?.Dispose();
                }
            });
        }
    }

    private bool AcceptSpotifyAdvance(bool prepareNext = true)
    {
        lock (_navigationLock)
        {
            if (_session is not { } session || _successor is not { } track
                || _successorUrl is null || !_player.AcceptQueuedAdvance()) return false;
            // Registration already transferred the stream lifetime to mpv. Clear the
            // speculative identity before SetCurrent invalidates the outgoing selection.
            _successorUrl = null;
            _successor = null;
            SavePosition(0);
            session.Index = session.Tracks.FindIndex(item => ReferenceEquals(item, track));
            SetCurrent(track, playing: true);
            _manualTransition = false;
            _loadedUri = track.Uri;
            _currentLoadSent = true;
            Navigated?.Invoke(track);
            if (BeforeTrackEnd?.Invoke() == true)
            {
                if (_active) FinishCurrent();
                return true;
            }
            if (prepareNext) RefreshSuccessor();
            return true;
        }
    }

    private bool OpenPreparedTrack(Track track, string url)
    {
        var position = Math.Clamp(SavedPosition(track), 0,
            track.DurationMs > 0 ? Math.Max(0, track.DurationMs / 1000.0 - 1) : double.MaxValue);
        _lastLoadPosition = position;
        if (!_player.OpenPreparedSpotifyAudio(url, track.Title, Link(track), position,
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
        Diagnostics.Write("load.accepted", uri, "attempt=" + _loadAttempt + " revision=" + _player.PlaybackRevision);
        _manualTransition = false;
        _currentLoadSent = true;
        _loadedUri = uri;
        _pendingSeek = null;
        if (!_playRequested) _player.Pause();
        NotifyTrackPlaying(uri);
        RefreshSuccessor();
    }

    private static string Link(Track track) => "https://open.spotify.com/track/" + track.Uri[14..];
}
