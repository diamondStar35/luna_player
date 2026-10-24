using LunaPlayer.Configuration;
using LunaPlayer.Playback;

namespace LunaPlayer.Application;

internal sealed partial class Features
{
    private readonly LunaPlayer.Spotify.Auth _spotifyAuth = new();
    private LunaPlayer.Spotify.PlaybackHost _spotifyPlayback = null!;
    private CancellationTokenSource? _spotifyStart;

    private void OpenSpotify(LunaPlayer.Spotify.Catalog catalog)
    {
        var choice = _view.Dialogs.ShowSpotifyPreview(catalog, _spotifyAuth, _settings.Spotify);
        if (choice.Track is not { } track) return;
        Func<string?, CancellationToken, Task<LunaPlayer.Spotify.TrackPage>>? pageLoader =
            choice.Collection is { } collection
                ? (cursor, token) => catalog.TracksPageAsync(collection, cursor, token)
                : null;
        StartSpotify(track, choice.Queue, pageLoader: pageLoader, nextPage: choice.NextCursor,
            deduplicateArtist: choice.Collection?.Kind == LunaPlayer.Spotify.CollectionKind.Artist);
    }

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
        _spotifyStart?.Cancel();
        _spotifyStart?.Dispose();
        _spotifyStart = new CancellationTokenSource();
        var token = _spotifyStart.Token;
        _player.SavePosition();
        _player.Stop();
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
                        _spotifyPlayback.PlayAsync(track, queue, token, startPosition,
                            pageLoader, nextPage, deduplicateArtist, keepShuffle).GetAwaiter().GetResult();
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
                    ForgetFailedSpotifyResume(track.Uri);
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

    private void OnSpotifyPageNavigationCompleted(LunaPlayer.Spotify.Track track) => _dispatcher.Post(() =>
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
        if (_spotifyPlayback.IsActive && _player.CurrentPath is { } spotifyPath
            && spotifyPath.StartsWith("luna-spotify://", StringComparison.Ordinal)) return;
        if (_player.CurrentPath is not null) _spotifyStart?.Cancel();
        if (_spotifyPlayback.IsActive && _player.CurrentPath is not null)
        {
            _spotifyPlayback.Stop();
            _controller?.RefreshView();
        }
    }

    private void OnSpotifyCommandFailed(Exception error)
    {
        var generation = _spotifyPlayback.CurrentLoadGeneration;
        var failedUri = _spotifyPlayback.CurrentTrack?.Uri;
        _dispatcher.Post(() =>
        {
            // A queued failure from an earlier load must not interrupt a newly selected track.
            if (_disposed || !_spotifyPlayback.IsActive
                || _spotifyPlayback.CurrentLoadGeneration != generation) return;
            _spotifyPlayback.Stop();
            ForgetFailedSpotifyResume(failedUri);
            _controller?.RefreshView();
            // Translators: Title of a message box reporting a Spotify or playback failure.
            _view.Dialogs.ShowError(LunaPlayer.Spotify.Catalog.DescribeError(error), Tr("Error"));
        });
    }

    private Task CancelSpotifyStart()
    {
        _spotifyStart?.Cancel();
        return Task.CompletedTask;
    }

    private void OnSpotifySignedOut()
    {
        _spotifyStart?.Cancel();
        _spotifyPlayback.SignOut();
        _controller?.RefreshView();
    }

    private void ForgetFailedSpotifyResume(string? failedUri)
    {
        if (failedUri is null || _settings.Playback.LastSpotifyUri != failedUri) return;
        // Do not reopen the same failed track automatically on the next launch.
        _settings.Playback.LastSpotifyUri = string.Empty;
        _ = _settingsStore.SaveSessionAsync(_settings);
    }

}
