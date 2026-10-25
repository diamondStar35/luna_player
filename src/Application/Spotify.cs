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
    private LunaPlayer.Spotify.Playback? _spotifyPlayback;
    private SleepTimer _sleepTimer = null!;

    private LunaPlayer.Spotify.Playback SpotifyPlayback
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_spotifyPlayback is { } existing) return existing;
            var playback = new LunaPlayer.Spotify.Playback(_settings.Spotify, _player, _dispatcher);
            playback.ConfigurePlayback(_settings.Audio, _player.ReadSavedPosition, _player.WriteSavedPosition);
            playback.BeforeTrackEnd = _sleepTimer.OnTrackEnded;
            playback.CommandFailed += OnSpotifyCommandFailed;
            playback.TrackChanged += OnSpotifyTrackChanged;
            playback.Navigated += OnSpotifyNavigated;
            playback.PlayingChanged += OnSpotifyPlayingChanged;
            playback.QueueEmpty += OnSpotifyQueueEmpty;
            _player.CurrentChanged += OnSpotifyCurrentChanged;
            _view.CloseRequested += CancelSpotifyStart;
            return _spotifyPlayback = playback;
        }
    }
    private LunaPlayer.Spotify.Catalog? _spotifyCatalog;
    private LunaPlayer.Spotify.Catalog SpotifyCatalog => _spotifyCatalog ??= new(() => SpotifyAuth, _settings.Spotify);
    private CancellationTokenSource? _spotifyStart;
    private bool _spotifyBrowserOpen;

    private void OpenSpotify(LunaPlayer.UI.Spotify.DialogOutcome? playback = null)
    {
        if (_disposed || _spotifyBrowserOpen) return;
        LunaPlayer.UI.Spotify.DialogOutcome choice;
        _spotifyBrowserOpen = true;
        try { choice = _view.Dialogs.ShowSpotifyBrowser(SpotifyCatalog, SpotifyAuth, _settings.Spotify, playback); }
        finally { _spotifyBrowserOpen = false; }
        if (choice.Track is not { } track) return;
        Func<string?, CancellationToken, Task<LunaPlayer.Spotify.TrackPage>>? pageLoader =
            choice.LoadPage ?? (choice.Collection is { } collection
                ? (cursor, token) => SpotifyCatalog.TracksPageAsync(collection, cursor, token)
                : null);
        StartSpotify(track, choice.Queue, pageLoader: pageLoader, nextPage: choice.NextCursor,
            deduplicateArtist: choice.Collection?.Kind == LunaPlayer.Spotify.CollectionKind.Artist);
    }

    private void LeaveSpotify()
    {
        CancelPendingSpotifyStart();
        if (_spotifyPlayback is { HasSession: true } playback) playback.Stop();
    }

    private bool ReturnToSpotify()
    {
        if (_spotifyPlayback is not { HasSession: true } playback) return false;
        var snapshot = new LunaPlayer.UI.Spotify.DialogOutcome(playback.CurrentTrack,
            playback.IsActive ? playback.SourceQueue : null,
            NextCursor: playback.NextPage);
        LeaveSpotify();
        _controller?.RefreshView();
        _dispatcher.Post(() => OpenSpotify(snapshot));
        return true;
    }

    private void OnSpotifyQueueEmpty() => ReturnToSpotify();

    private void RestartSpotifyForQuality() => _spotifyPlayback?.ReloadQuality();

    private void StartSpotify(LunaPlayer.Spotify.Track track,
        IReadOnlyList<LunaPlayer.Spotify.Track>? queue,
        Func<string?, CancellationToken, Task<LunaPlayer.Spotify.TrackPage>>? pageLoader = null,
        string? nextPage = null, bool deduplicateArtist = false)
    {
        if (_disposed) return;
        var playback = SpotifyPlayback;
        if (!playback.HasSession)
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
        playback.BeginSession();
        _ = Task.Run(async () =>
        {
            try
            {
                await playback.PrepareAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _dispatcher.Post(() =>
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        playback.Play(track, queue, token, pageLoader: pageLoader,
                            nextPage: nextPage, deduplicateArtist: deduplicateArtist);
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
                    playback.Stop();
                    // Translators: Title of a message box reporting a Spotify or playback failure.
                    _view.Dialogs.ShowError(LunaPlayer.Spotify.Catalog.DescribeError(error), Tr("Error"));
                });
            }
        }, token);
    }

    // Spotify navigation selected a track; reflect its title while the audio loads.
    private void OnSpotifyTrackChanged(LunaPlayer.Spotify.Track track) => _dispatcher.Post(() =>
    {
        if (_disposed || _spotifyPlayback is not { IsActive: true } playback) return;
        _view.SetPlaying(playback.IsPlaying);
        _controller?.RefreshView();
    });

    private void OnSpotifyNavigated(LunaPlayer.Spotify.Track track) => _dispatcher.Post(() =>
    {
        if (!_disposed && _spotifyPlayback is { IsActive: true } playback && ReferenceEquals(playback.CurrentTrack, track)
            && _settings.General.SpeakFileOnNavigation)
            _speech.Speak(track.Title, track.Title);
    });

    // Refresh the window when Spotify's transport state changes.
    private void OnSpotifyPlayingChanged(bool playing) => _dispatcher.Post(() =>
    {
        if (_disposed || _spotifyPlayback is not { IsActive: true } playback) return;
        _view.SetPlaying(playback.IsPlaying);
        _controller?.RefreshView();
    });

    // An ordinary file or video replaces Spotify as the current source.
    private void OnSpotifyCurrentChanged()
    {
        if (_player.InSpotifySession || _spotifyPlayback is not { } playback) return;
        CancelPendingSpotifyStart();
        if (playback.HasSession || playback.IsActive)
        {
            // Another source now owns the player. Do not restore Spotify's old session
            // over the new file or YouTube session, including when close-all leaves no path.
            playback.Stop(restoreSession: false);
            _controller?.RefreshView();
        }
    }

    private void OnSpotifyCommandFailed(Exception error)
    {
        if (_spotifyPlayback is not { } playback) return;
        var generation = playback.CurrentLoadGeneration;
        _dispatcher.Post(() =>
        {
            // A queued failure from an earlier load must not interrupt a newly selected track.
            if (_disposed || !playback.IsActive
                || playback.CurrentLoadGeneration != generation) return;
            playback.Stop();
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
        _spotifyPlayback?.SignOut();
        _controller?.RefreshView();
    }
}
