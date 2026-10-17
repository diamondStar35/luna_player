using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Application.Commands;
using LunaPlayer.Application.Commands.Files;
using LunaPlayer.Application.Commands.Playback;
using LunaPlayer.Application.Commands.YouTube;
using LunaPlayer.Application.Commands.Recording;
using LunaPlayer.Application.Commands.Iptv;
using LunaPlayer.Application.Commands.App;
using LunaPlayer.Bookmarks;
using LunaPlayer.Favorites;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.Media;

namespace LunaPlayer.Application;

internal sealed class ApplicationHost : IDisposable
{
    private readonly SingleInstanceService _singleInstance;
    private readonly SettingsStore _settingsStore;
    private readonly Settings _settings;
    private readonly ShortcutManager _shortcuts;
    private readonly ShortcutManager _globalShortcuts;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly IMainView _view;
    private readonly ISpeechOutput _speech;
    private readonly MediaPlayer _player;
    private readonly ApplicationController _controller;
    private readonly Commands.Playback.Equalizer _equalizer;
    private readonly Subtitles _subtitles;
    private readonly LunaPlayer.Equalizer.Library _equalizerLibrary;
    private readonly PathRequestQueue _pathQueue;
    private readonly LunaPlayer.YouTube.Playback.ResolveCache _resolveCache;
    private readonly LunaPlayer.YouTube.Components.Service _components;
    private readonly Updates _appUpdates;
    private readonly LunaPlayer.Recording.AudioCatalog _catalog;
    private readonly LunaPlayer.Recording.RecordingSources _recordingSources;
    private readonly LunaPlayer.Recording.RecordingEngine _recorder;
    private readonly LunaPlayer.YouTube.Playback.Sessions _sessions;
    private readonly LunaPlayer.Spotify.SpotifyAuth _spotifyAuth;
    private readonly LunaPlayer.Spotify.SpotifyPlaybackHost _spotifyPlayback;
    private CancellationTokenSource? _spotifyStart;
    private readonly Tools _tools;
    private bool _disposed;

    internal ApplicationHost(SingleInstanceService singleInstance, IReadOnlyList<string> initialPaths)
    {
        _singleInstance = singleInstance;
        _settingsStore = new SettingsStore(Paths.SettingsFile);
        _settings = _settingsStore.Load();
        var settingsError = _settingsStore.LastError;
        // Before anything else: the action tables and every window below build their strings once, and they
        // have to be built in the user's language.
        Localization.Initialize(_settings.General.Language);
        _shortcuts = new ShortcutManager(ActionRegistry.All);
        _shortcuts.Apply(_settings.Shortcuts.Primary, _settings.Shortcuts.Secondary);
        _globalShortcuts = new ShortcutManager(GlobalActions.All);
        _dispatcher = new WxDispatcher();
        _catalog = new LunaPlayer.Recording.AudioCatalog();
        // Before the window, because the equalizer submenu is built with the presets in it.
        _equalizerLibrary = new LunaPlayer.Equalizer.Library(
            _settings, _settingsStore, new LunaPlayer.Equalizer.PresetStore(Paths.EqualizerFile));
        _view = new MainFrame(
            _shortcuts, ActionRegistry.All, _dispatcher, _catalog, _equalizerLibrary.All);
        if (settingsError.Length > 0)
        {
            _dispatcher.Post(() => _view.Dialogs.ShowError(
                TrFormat("The settings file could not be loaded. Default settings will be used, and the existing file will not be overwritten. Import a valid settings file or reset the settings to replace it.\n\n{error}", settingsError),
                Tr("Settings error")));
        }
        _speech = new SpeechOutput(_settings);
        _player = new MediaPlayer(new MpvPlaybackEngine(_view.NativeHandle, !_settings.General.DisableMediaControls), new PositionStore(Paths.PositionsFile));
        _spotifyAuth = new LunaPlayer.Spotify.SpotifyAuth();
        _spotifyAuth.SignedOut += OnSpotifySignedOut;
        _spotifyPlayback = new LunaPlayer.Spotify.SpotifyPlaybackHost(_settings.Spotify,
            _player, _dispatcher);
        var spotifyPositions = new PositionStore(Paths.PositionsFile);
        _spotifyPlayback.ConfigurePlayback(_settings.Audio, spotifyPositions.Get,
            (link, seconds) => { spotifyPositions.Set(link, seconds); });
        _spotifyPlayback.CommandFailed += OnSpotifyCommandFailed;
        _spotifyPlayback.TrackChanged += OnSpotifyTrackChanged;
        _spotifyPlayback.PageNavigationCompleted += OnSpotifyPageNavigationCompleted;
        _spotifyPlayback.PlayingChanged += OnSpotifyPlayingChanged;
        _player.CurrentChanged += OnSpotifyCurrentChanged;
        _view.CloseRequested += CancelSpotifyStart;
        var clipboard = new WxClipboardService();
        // Now that there is a toolkit, the crash window can offer to copy.
        CrashReport.SetClipboard(clipboard);
        var selection = new PlaybackSelection();
        var router = new ActionRouter();
        _ = new Help(router, _view);
        _appUpdates = new Updates(router, _view, _settings, _dispatcher);
        var fileActions = new Files(router, _view, _player, _settings, _speech, clipboard, _dispatcher,
            new RecentsStore(Paths.RecentsFile), _spotifyPlayback);
        _ = new Commands.Playback.Playback(router, _view, _player, _settings, _settingsStore, _speech, selection, _spotifyPlayback);

        _ = new Edit(router, _view, _player, _speech, clipboard, fileActions);
        _ = new MarkedFiles(router, _view, _player, _settings, _speech, clipboard, _dispatcher);
        var bookmarks = new BookmarkStore(Paths.BookmarksFile);
        _ = new Commands.Playback.Bookmarks(router, _view, _player, _speech, bookmarks);
        _ = new Devices(router, _view, _player, _settings, _settingsStore, _speech);
        _ = new AudioTracks(router, _view, _player, _speech);
        _subtitles = new Subtitles(router, _view, _player, _speech, _settings, _dispatcher);
        _equalizer = new Commands.Playback.Equalizer(
            router, _view, _player, _settings, _settingsStore, _speech, _equalizerLibrary);
        var runner = new LunaPlayer.YouTube.Client.Runner(_settings);
        var resolver = new LunaPlayer.YouTube.Client.Resolver(runner);
        var channels = new LunaPlayer.YouTube.Client.Channels(runner);
        var updater = new LunaPlayer.YouTube.Client.Updater(runner);
        var metadata = new LunaPlayer.YouTube.Metadata.Client();
        var youTube = new LunaPlayer.YouTube.Backend(metadata, channels);
        _resolveCache = new LunaPlayer.YouTube.Playback.ResolveCache(resolver);
        _components = new LunaPlayer.YouTube.Components.Service(_view, _settings, _speech, _dispatcher, updater);
        // Shared by the favourites actions and the results window's Ctrl+Space add, so both write the same store.
        var favorites = new FavoriteStore(Paths.FavoritesFile);
        // Sessions need three YouTube actions. Callbacks avoid giving either object access to the other's
        // unrelated responsibilities.
        Commands.YouTube.YouTube? youTubeActions = null;
        _sessions = new LunaPlayer.YouTube.Playback.Sessions(
            _view, _player, _settings, _speech, _dispatcher, metadata, youTube, _resolveCache,
            url => youTubeActions!.DirectDownloadTo(url),
            (url, title) => youTubeActions!.DownloadTo(url, title),
            url => youTubeActions!.CopyToClipboard(url),
            url => youTubeActions!.OpenInBrowser(url),
            favorites,
            _components);
        youTubeActions = new Commands.YouTube.YouTube(
            router, _view, _player, _settings, _speech, clipboard, youTube, _sessions, _components, _dispatcher);
        _ = new Commands.YouTube.Favorites(router, _view, _player, _speech, favorites, _sessions);
        _ = new Commands.YouTube.Playlist(router, _view, _player, _settings, _speech, _sessions, _spotifyPlayback);
        _ = new Commands.App.Preferences(router, _view, _settings, _settingsStore,
            new BackupService(_settingsStore, bookmarks), new FileAssociations(), _player, _shortcuts,
            _globalShortcuts, _speech, youTube, _components, RestartSpotifyForQuality);
        _recordingSources = new LunaPlayer.Recording.RecordingSources(_settings.Recording);
        _recorder = new LunaPlayer.Recording.RecordingEngine(_catalog);
        _ = new Commands.Recording.Recording(
            router, _view, _settings, _speech, _dispatcher, _catalog, _recordingSources, _recorder);
        _tools = new Tools(router, _view);
        var spotifyCatalog = new LunaPlayer.Spotify.SpotifyCatalog(_spotifyAuth, _settings.Spotify);
        router.Register(ActionId.OpenSpotifyPreview,
            () => OpenSpotify(spotifyCatalog));
        _ = new Commands.Iptv.Iptv(router, _view, _player, _speech, _dispatcher, new IptvSourceStore(Paths.IptvSourcesFile));
        var sleepTimer = new SleepTimer(_dispatcher, _player, _speech, _spotifyPlayback);
        _spotifyPlayback.BeforeTrackEnd = sleepTimer.OnTrackEnded;
        _ = new Sleep(router, _view, _player, sleepTimer, _settings, _settingsStore);
        router.EnsureComplete(ActionRegistry.All);
        _controller = new ApplicationController(
            _view,
            _player,
            _settings,
            _settingsStore,
            _dispatcher,
            router,
            fileActions,
            selection,
            _sessions,
            sleepTimer,
            _speech,
            _spotifyPlayback);
        // After the controller, which sets the rest of the audio state up from the same settings.
        _equalizer.Restore();
        _pathQueue = new PathRequestQueue(HandleExternalPaths, _dispatcher);
        _singleInstance.StartListening(_pathQueue.Enqueue);
        // Posted rather than run here so a refusal is reported over a window the user can already see.
        _dispatcher.Post(() => GlobalShortcutBinder.Apply(_view, _globalShortcuts, _settings, _speech));
        // Looks for a newer yt-dlp only when the setting asks for it, and says nothing unless there is one.
        _dispatcher.Post(_components.CheckForUpdateInBackground);
        // The application check is also quiet at startup: only a newer release opens a window.
        _dispatcher.Post(_appUpdates.CheckAtStartup);

        if (initialPaths.Count > 0)
        {
            // Windows Explorer's "Convert with Luna" verb launches the player with --convert and the file or
            // folder, which opens the converter rather than playing anything. Everything else is opened to
            // play as before.
            if (IsConvertRequest(initialPaths, out var convertFiles))
                _dispatcher.Post(() => _tools.OpenFor(convertFiles));
            else
                _dispatcher.Post(() => _controller.OpenPaths(initialPaths));
        }
        else if (_settings.General.RememberLastPosition
            && LunaPlayer.Spotify.SpotifyPlaybackHost.IsTrack(_settings.Playback.LastSpotifyUri))
        {
            var track = new LunaPlayer.Spotify.SpotifyTrack(
                _settings.Playback.LastSpotifyName, _settings.Playback.LastSpotifyArtists,
                _settings.Playback.LastSpotifyUri, _settings.Playback.LastSpotifyDurationMs);
            _dispatcher.Post(() => StartSpotify(track, [track], _settings.Playback.LastSpotifyPosition));
        }
        else if (_settings.General.RememberLastPosition && File.Exists(_settings.Playback.LastFile))
            _dispatcher.Post(() => fileActions.RestoreSession(_settings.Playback.LastFile, _settings.Playback.LastPosition));
    }

    internal void Show() => _view.Show();

    private void OpenSpotify(LunaPlayer.Spotify.SpotifyCatalog catalog)
    {
        var choice = _view.Dialogs.ShowSpotifyPreview(catalog, _spotifyAuth, _settings.Spotify);
        if (choice.Track is not { } track) return;
        Func<string?, CancellationToken, Task<LunaPlayer.Spotify.SpotifyTrackPage>>? pageLoader =
            choice.Collection is { } collection
                ? (cursor, token) => catalog.TracksPageAsync(collection, cursor, token)
                : null;
        StartSpotify(track, choice.Queue, pageLoader: pageLoader, nextPage: choice.NextCursor,
            deduplicateArtist: choice.Collection?.Kind == LunaPlayer.Spotify.SpotifyCollectionKind.Artist);
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

    private void StartSpotify(LunaPlayer.Spotify.SpotifyTrack track,
        IReadOnlyList<LunaPlayer.Spotify.SpotifyTrack>? queue, double? startPosition = null,
        bool keepPaused = false, bool keepRepeat = false, bool keepShuffle = false,
        Func<string?, CancellationToken, Task<LunaPlayer.Spotify.SpotifyTrackPage>>? pageLoader = null,
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
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error)
            {
                _dispatcher.Post(() =>
                {
                    if (_disposed || token.IsCancellationRequested) return;
                    _spotifyPlayback.Stop();
                    ForgetFailedSpotifyResume(track.Uri);
                    _view.Dialogs.ShowError(LunaPlayer.Spotify.SpotifyCatalog.DescribeError(error), Tr("Error"));
                });
            }
        }, token);
    }

    // Spotify navigation selected a track; reflect its title while the audio loads.
    private void OnSpotifyTrackChanged(LunaPlayer.Spotify.SpotifyTrack track) => _dispatcher.Post(() =>
    {
        if (_disposed || !_spotifyPlayback.IsActive) return;
        _view.SetPlaying(_spotifyPlayback.IsPlaying);
        _controller.RefreshView();
    });

    private void OnSpotifyPageNavigationCompleted(LunaPlayer.Spotify.SpotifyTrack track) => _dispatcher.Post(() =>
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
        _controller.RefreshView();
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
            _controller.RefreshView();
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
            _controller.RefreshView();
            _view.Dialogs.ShowError(LunaPlayer.Spotify.SpotifyCatalog.DescribeError(error), Tr("Error"));
        });
    }

    private void CancelSpotifyStart() => _spotifyStart?.Cancel();

    private void OnSpotifySignedOut()
    {
        _spotifyStart?.Cancel();
        _spotifyPlayback.SignOut();
        _controller.RefreshView();
    }

    private void ForgetFailedSpotifyResume(string? failedUri)
    {
        if (failedUri is null || _settings.Playback.LastSpotifyUri != failedUri) return;
        // Do not reopen the same failed track automatically on the next launch.
        _settings.Playback.LastSpotifyUri = string.Empty;
        _settingsStore.SaveSession(_settings);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _view.CloseRequested -= CancelSpotifyStart;
        _singleInstance.Dispose();
        _pathQueue.Dispose();
        _controller.Dispose();
        // Unsubscribes from the player's subtitle events before the player itself goes below.
        _subtitles.Dispose();
        // Before the player goes: a recording still running has a file to close, and the engine waits for
        // the encoder to flush its last block rather than leaving a truncated one behind.
        _recorder.Dispose();
        _sessions.Dispose();
        _spotifyStart?.Cancel();
        _spotifyStart?.Dispose();
        _player.CurrentChanged -= OnSpotifyCurrentChanged;
        _spotifyPlayback.CommandFailed -= OnSpotifyCommandFailed;
        _spotifyPlayback.TrackChanged -= OnSpotifyTrackChanged;
        _spotifyPlayback.PageNavigationCompleted -= OnSpotifyPageNavigationCompleted;
        _spotifyPlayback.PlayingChanged -= OnSpotifyPlayingChanged;
        _spotifyPlayback.Dispose();
        _spotifyAuth.SignedOut -= OnSpotifySignedOut;
        _spotifyAuth.Dispose();
        _resolveCache.Dispose();
        _appUpdates.Dispose();
        _player.Dispose();
        // Closing a screen-reader backend sends a global stop command. Focus has already moved to the next
        // application here, so doing that would cancel its focus announcement. Process termination releases
        // the remaining speech resources without sending that command.
        _view.Dispose();
    }

    private void HandleExternalPaths(IReadOnlyList<string> paths)
    {
        _view.RestoreAndRaise();
        if (paths.Count == 0)
            return;
        // A second launch carrying "Convert with Luna" opens the converter for its files rather than playing
        // them, the same as the first launch does.
        if (IsConvertRequest(paths, out var convertFiles))
            _tools.OpenFor(convertFiles);
        else
            _controller.OpenPaths(paths);
    }

    /// <summary>Whether a set of launch arguments is a "Convert with Luna" request, and if so the files it
    /// names with any folder among them expanded to the supported files under it.</summary>
    /// <remarks>
    /// The verb passes --convert and one path; several files selected in Explorer launch the player once
    /// each, and those launches are gathered into one list before they reach here, so the flag can arrive
    /// beside any number of paths.
    /// </remarks>
    private static bool IsConvertRequest(IReadOnlyList<string> paths, out IReadOnlyList<string> files)
    {
        var convert = false;
        var targets = new List<string>();
        foreach (var path in paths)
        {
            if (path.Equals("--convert", StringComparison.OrdinalIgnoreCase))
                convert = true;
            else
                targets.Add(path);
        }
        if (!convert)
        {
            files = [];
            return false;
        }
        var collected = new List<string>();
        foreach (var target in targets)
        {
            if (Directory.Exists(target))
                collected.AddRange(MediaLibrary.CollectFiles(target, recursive: true));
            else if (File.Exists(target))
                collected.Add(target);
        }
        files = collected;
        return true;
    }
}
