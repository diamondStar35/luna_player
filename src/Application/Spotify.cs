using LunaPlayer.Configuration;
using LunaPlayer.Playback;

namespace LunaPlayer.Application;

internal sealed partial class Features
{
    private LunaPlayer.Spotify.Auth? _spotifyAuth;
    private LunaPlayer.Spotify.Auth SpotifyAuth
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_spotifyAuth is null)
            {
                _spotifyAuth = new LunaPlayer.Spotify.Auth();
                _spotifyAuth.SignedOut += OnSpotifySignedOut;
            }
            return _spotifyAuth;
        }
    }
    private LunaPlayer.Spotify.Playback _spotifyPlayback = null!;
    private LunaPlayer.Spotify.Catalog _spotifyCatalog = null!;
    private CancellationTokenSource? _spotifyStart;
    private bool _spotifyBrowserOpen;

    private void OpenSpotify(LunaPlayer.UI.Spotify.DialogOutcome? playback = null)
    {
        if (_disposed || _spotifyBrowserOpen) return;
        LunaPlayer.UI.Spotify.DialogOutcome choice;
        _spotifyBrowserOpen = true;
        try { choice = _view.Dialogs.ShowSpotifyPreview(_spotifyCatalog, SpotifyAuth, _settings.Spotify, playback); }
        finally { _spotifyBrowserOpen = false; }
        if (choice.Track is not { } track) return;
        Func<string?, CancellationToken, Task<LunaPlayer.Spotify.TrackPage>>? pageLoader =
            choice.LoadPage ?? (choice.Collection is { } collection
                ? (cursor, token) => _spotifyCatalog.TracksPageAsync(collection, cursor, token)
                : null);
        StartSpotify(track, choice.Queue, pageLoader: pageLoader, nextPage: choice.NextCursor,
            deduplicateArtist: choice.Collection?.Kind == LunaPlayer.Spotify.CollectionKind.Artist);
    }

    private void LeaveSpotify()
    {
        CancelPendingSpotifyStart();
        if (_spotifyPlayback.HasSession) _spotifyPlayback.Stop();
    }

    private bool ReturnToSpotify()
    {
        if (!_spotifyPlayback.HasSession) return false;
        var snapshot = new LunaPlayer.UI.Spotify.DialogOutcome(_spotifyPlayback.CurrentTrack,
            _spotifyPlayback.IsActive ? _spotifyPlayback.SourceQueue : null,
            NextCursor: _spotifyPlayback.NextPage);
        LeaveSpotify();
        _controller?.RefreshView();
        _dispatcher.Post(() => OpenSpotify(snapshot));
        return true;
    }

    private void OnSpotifyQueueEmpty() => ReturnToSpotify();

    private void RestartSpotifyForQuality()
    {
        if (!_spotifyPlayback.IsActive || _spotifyPlayback.CurrentTrack is not { } track)
            return;
        StartSpotify(track, _spotifyPlayback.SourceQueue, _spotifyPlayback.Elapsed,
            keepPaused: !_spotifyPlayback.IsPlaying, keepRepeat: _spotifyPlayback.IsRepeatCurrentEnabled,
            keepShuffle: _spotifyPlayback.IsShuffleEnabled,
            pageLoader: _spotifyPlayback.PageLoader,
            nextPage: _spotifyPlayback.NextPage,
            deduplicateArtist: _spotifyPlayback.DeduplicateArtist);
    }

    private void StartSpotify(LunaPlayer.Spotify.Track track,
        IReadOnlyList<LunaPlayer.Spotify.Track>? queue, double? startPosition = null,
        bool keepPaused = false, bool keepRepeat = false, bool keepShuffle = false,
        Func<string?, CancellationToken, Task<LunaPlayer.Spotify.TrackPage>>? pageLoader = null,
        string? nextPage = null, bool deduplicateArtist = false)
    {
        if (_disposed) return;
        if (!_spotifyPlayback.HasSession)
        {
            if (_radio?.IsActive == true) _radio.Stop(restore: true);
            else _sessions?.Clear();
        }
        _spotifyStart?.Cancel();
        _spotifyStart?.Dispose();
        _spotifyStart = new CancellationTokenSource();
        var token = _spotifyStart.Token;
        // Capture the loaded file and its exact position before preparation stops mpv.
        // This in-memory snapshot is needed even when per-file position saving is disabled.
        _spotifyPlayback.BeginSession();
        _ = Task.Run(async () =>
        {
            try
            {
                await _spotifyPlayback.PrepareAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _dispatcher.Post(() =>
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        _spotifyPlayback.Play(track, queue, token, startPosition,
                            pageLoader, nextPage, deduplicateArtist, keepShuffle);
                        if (keepRepeat && !_spotifyPlayback.IsRepeatCurrentEnabled)
                            _spotifyPlayback.ToggleRepeatCurrent();
                        if (keepPaused) _spotifyPlayback.Pause();
                        started.TrySetResult(true);
                    }
                    catch (Exception error) { started.TrySetException(error); }
                });
                await started.Task.WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Stopping playback or selecting another track intentionally cancels this request.
            }
            catch (Exception error)
            {
                _dispatcher.Post(() =>
                {
                    if (_disposed || token.IsCancellationRequested) return;
                    _spotifyPlayback.Stop();
                    // Translators: Title of a message box reporting a Spotify or playback failure.
                    _view.Dialogs.ShowError(LunaPlayer.Spotify.Catalog.DescribeError(error), Tr("Error"));
                });
            }
        }, token);
    }

    // Spotify navigation selected a track; reflect its title while the audio loads.
    private void OnSpotifyTrackChanged(LunaPlayer.Spotify.Track track) => _dispatcher.Post(() =>
    {
        if (_disposed || !_spotifyPlayback.IsActive) return;
        _view.SetPlaying(_spotifyPlayback.IsPlaying);
        _controller?.RefreshView();
    });

    private void OnSpotifyNavigated(LunaPlayer.Spotify.Track track) => _dispatcher.Post(() =>
    {
        if (!_disposed && _spotifyPlayback.IsActive && _spotifyPlayback.CurrentTrack == track
            && _settings.General.SpeakFileOnNavigation)
            _speech.Speak(track.Name + " - " + track.Artists, track.Name + " - " + track.Artists);
    });

    // Refresh the window when Spotify's transport state changes.
    private void OnSpotifyPlayingChanged(bool playing) => _dispatcher.Post(() =>
    {
        if (_disposed || !_spotifyPlayback.IsActive) return;
        _view.SetPlaying(_spotifyPlayback.IsPlaying);
        _controller?.RefreshView();
    });

    // An ordinary file or video replaces Spotify as the current source.
    private void OnSpotifyCurrentChanged()
    {
        if (_player.InSpotifySession) return;
        CancelPendingSpotifyStart();
        if (_spotifyPlayback.HasSession || _spotifyPlayback.IsActive)
        {
            // Another source now owns the player. Do not restore Spotify's old session
            // over the new file or YouTube session, including when close-all leaves no path.
            _spotifyPlayback.Stop(restoreSession: false);
            _controller?.RefreshView();
        }
    }

    private void OnSpotifyCommandFailed(Exception error)
    {
        var generation = _spotifyPlayback.CurrentLoadGeneration;
        _dispatcher.Post(() =>
        {
            // A queued failure from an earlier load must not interrupt a newly selected track.
            if (_disposed || !_spotifyPlayback.IsActive
                || _spotifyPlayback.CurrentLoadGeneration != generation) return;
            _spotifyPlayback.Stop();
            _controller?.RefreshView();
            // Translators: Title of a message box reporting a Spotify or playback failure.
            _view.Dialogs.ShowError(LunaPlayer.Spotify.Catalog.DescribeError(error), Tr("Error"));
        });
    }

    private Task CancelSpotifyStart()
    {
        CancelPendingSpotifyStart();
        return Task.CompletedTask;
    }

    private void CancelPendingSpotifyStart() => _spotifyStart?.Cancel();

    private void OnSpotifySignedOut()
    {
        _spotifyStart?.Cancel();
        _spotifyPlayback.SignOut();
        _controller?.RefreshView();
    }
}
