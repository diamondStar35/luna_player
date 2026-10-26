using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Application.Commands;
using LunaPlayer.Application.Commands.App;
using LunaPlayer.Application.Commands.Files;
using LunaPlayer.Application.Commands.Playback;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application;

/// <summary>Wires application commands and owns the services used by the running window. Startup workers
/// hand their resources over here only after the window's menus and input handling are ready.</summary>
internal sealed partial class Features : IDisposable
{
    private readonly IMainView _view;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly SettingsStore _settingsStore;
    private readonly Settings _settings;
    private readonly string _settingsError;
    private readonly ISpeechOutput _speech;
    private readonly Player _player;
    private readonly ShortcutManager _globalShortcuts;
    private ApplicationController? _controller;
    private Subtitles? _subtitles;
    private Commands.Playback.Chapters? _chapters;
    private LunaPlayer.YouTube.Playback.ResolveCache? _resolveCache;
    private LunaPlayer.YouTube.Components.Service _components = null!;
    private Updates? _appUpdates;
    private LunaPlayer.Recording.RecordingEngine? _recorder;
    private LunaPlayer.YouTube.Playback.Sessions? _sessions;
    private LunaPlayer.Radio.Playback? _radio;
    private LunaPlayer.YouTube.Download.DownloadQueue? _downloadQueue;
    private Tools _tools = null!;
    private Files _fileActions = null!;
    private bool _disposed;

    private Features(IMainView view, IApplicationDispatcher dispatcher, StartupResult startup)
    {
        _view = view;
        _dispatcher = dispatcher;
        _settingsStore = startup.Settings.Store;
        _settings = startup.Settings.Settings;
        _settingsError = startup.Settings.Error;
        _globalShortcuts = startup.GlobalShortcuts;
        _speech = startup.Services.TakeSpeech();
        _player = startup.Services.TakePlayer();
    }

    internal static Features Create(IMainView view, IApplicationDispatcher dispatcher, StartupResult startup)
    {
        var features = new Features(view, dispatcher, startup);
        SystemMediaControls? mediaControls = null;
        try
        {
            mediaControls = startup.Services.TakeMediaControls();
            features.Initialize(mediaControls, startup.Services.TakeFeatures(), startup.Window,
                startup.Shortcuts, startup.GlobalShortcuts);
            return features;
        }
        catch
        {
            // Creation may fail after some event handlers have been installed. Release partial ownership
            // here as well as any media controls that have not yet reached the application controller.
            mediaControls?.Dispose();
            features.Dispose();
            throw;
        }
    }

    private void Initialize(
        SystemMediaControls mediaControls,
        FeatureResources features,
        WindowResources window,
        ShortcutManager shortcuts,
        ShortcutManager globalShortcuts)
    {
        var speech = _speech;
        var player = _player;
        var clipboard = new WxClipboardService();
        // Now that there is a toolkit, the crash window can offer to copy.
        CrashReport.SetClipboard(clipboard);
        _spotifyPlayback = new LunaPlayer.Spotify.Playback.Sessions(_settings.Spotify, player, _dispatcher);
        var selection = features.Selection;
        var selections = features.Selections;
        var router = new ActionRouter();
        var radio = new LunaPlayer.Radio.Playback(_view, player, _settings, _dispatcher, speech, clipboard,
            () => { LeaveSpotify(); _sessions?.Clear(); });
        _radio = radio;
        router.Register(ActionId.OpenRadioBrowser, radio.Open);
        _ = new Help(router, _view);
        var appUpdates = new Updates(router, _view, _settings, _dispatcher, speech);
        _appUpdates = appUpdates;
        var fileActions = new Files(router, _view, player, _settings, speech, clipboard, _dispatcher,
            window.Recents, radio, _spotifyPlayback, CancelPendingSpotifyStart);
        _fileActions = fileActions;
        _ = new Commands.Playback.Playback(
            router, _view, player, _settings, _settingsStore, speech, selection, selections, _dispatcher, _spotifyPlayback);

        _ = new Edit(router, _view, player, speech, clipboard, fileActions);
        _ = new MarkedFiles(router, _view, player, _settings, speech, clipboard, _dispatcher);
        var bookmarks = features.Bookmarks;
        _ = new Commands.Playback.Bookmarks(router, _view, player, speech, bookmarks, selections);
        _ = new Devices(router, _view, player, _settings, _settingsStore, speech);
        _ = new AudioTracks(router, _view, player, speech);
        _chapters = new Commands.Playback.Chapters(router, _view, player, _settings, _settingsStore, speech, _dispatcher);
        _subtitles = new Subtitles(router, _view, player, speech, _settings, _dispatcher);
        var equalizer = new Commands.Playback.Equalizer(
            router, _view, player, _settings, _settingsStore, speech, window.Equalizer);
        var resolver = features.Resolver;
        var updater = features.Updater;
        var metadata = features.Metadata;
        var youTube = features.YouTube;
        var resolveCache = new LunaPlayer.YouTube.Playback.ResolveCache(resolver);
        _resolveCache = resolveCache;
        var components = new LunaPlayer.YouTube.Components.Service(
            _view, _settings, speech, _dispatcher, updater, features.Installer);
        _components = components;
        var downloadQueue = new LunaPlayer.YouTube.Download.DownloadQueue(_settings);
        _downloadQueue = downloadQueue;
        downloadQueue.AllCompleted += () => _dispatcher.Post(() =>
            _view.Dialogs.ShowNotification(
                // Translators: Title of the notification shown when every queued download has finished.
                Tr("Downloads completed"),
                // Translators: Body of the notification shown when every queued download has finished.
                Tr("All items in the download queue have completed.")));
        // Shared by the favourites actions and the results window's Ctrl+Space add, so both write the same store.
        var favorites = features.Favorites;
        // Sessions need the related YouTube actions. Callbacks avoid giving either object access to the other's
        // unrelated responsibilities.
        Commands.YouTube.YouTube? youTubeActions = null;
        var sessions = new LunaPlayer.YouTube.Playback.Sessions(
            _view, player, _settings, speech, _dispatcher, metadata, youTube, resolveCache,
            (url, title) => youTubeActions!.DirectDownloadTo(url, title),
            (url, title) => youTubeActions!.DownloadTo(url, title),
            (url, title) => youTubeActions!.AddToDownloadQueue(url, title),
            url => youTubeActions!.CopyToClipboard(url),
            url => youTubeActions!.OpenInBrowser(url),
            favorites,
            components);
        _sessions = sessions;
        var playbackSessions = new PlaybackSessions(sessions, radio, player, _spotifyPlayback, ReturnToSpotify);
        youTubeActions = new Commands.YouTube.YouTube(
            router, _view, player, _settings, speech, clipboard, youTube, sessions, components, _dispatcher,
            downloadQueue);
        _ = new Commands.YouTube.Favorites(router, _view, player, speech, favorites, sessions);
        _ = new Commands.YouTube.Playlist(router, _view, player, _settings, speech, playbackSessions);
        _ = new Commands.App.Preferences(router, _view, _settings, _settingsStore,
            new BackupService(_settingsStore, bookmarks), new FileAssociations(), player, shortcuts,
            globalShortcuts, speech, youTube, components, RestartSpotifyForQuality);
        var recordingSources = features.RecordingSources;
        var recorder = new LunaPlayer.Recording.RecordingEngine(window.Catalog);
        _recorder = recorder;
        _ = new Commands.Recording.Recording(
            router, _view, _settings, speech, _dispatcher, window.Catalog, recordingSources, recorder);
        var tools = new Tools(router, _view);
        _tools = tools;
        _ = new Commands.Iptv.Iptv(router, _view, player, speech, _dispatcher,
            features.IptvSources);
        router.Register(ActionId.OpenSpotifyBrowser, () => OpenSpotify(SpotifyBrowserPosition()));
        var sleepTimer = new SleepTimer(_dispatcher, player, speech, _spotifyPlayback, CancelPendingSpotifyStart);
        _sleepTimer = sleepTimer;
        _ = new Sleep(router, _view, player, sleepTimer, _settings, _settingsStore);
        router.EnsureComplete(ActionRegistry.All);
        var controller = new ApplicationController(
            _view,
            player,
            _settings,
            _settingsStore,
            _dispatcher,
            router,
            fileActions,
            selection,
            selections,
            playbackSessions,
            sleepTimer,
            speech,
            mediaControls,
            _spotifyPlayback);
        _controller = controller;
        // After the controller, which sets the rest of the audio state up from the same settings.
        equalizer.Restore();
    }

    internal void QueueStartupWork()
    {
        if (_settingsError.Length > 0)
        {
            _dispatcher.Post(() => _view.Dialogs.ShowError(
                // Translators: Shown when settings could not be read. {error} is the underlying file or data error.
                TrFormat("The settings file could not be loaded. Default settings will be used, and the existing file will not be overwritten. Import a valid settings file or reset the settings to replace it.\n\n{error}", _settingsError),
                // Translators: Title of the message shown when settings could not be read.
                Tr("Settings error")));
        }
        // Posted rather than run here so a refusal is reported over a window the user can already see.
        _dispatcher.Post(() => GlobalShortcutBinder.Apply(_view, _globalShortcuts, _settings, _speech));
        // Update checks are quiet unless a newer release is available.
        _dispatcher.Post(_components.CheckForUpdateInBackground);
        _dispatcher.Post(_appUpdates!.CheckAtStartup);
    }

    internal void OpenPaths(IReadOnlyList<string> paths)
    {
        if (Resources.IsConvertRequest(paths, out var files))
            _tools.OpenFor(files);
        else
            _controller!.OpenPaths(paths);
    }

    internal void OpenPrepared(PreparedLaunch launch)
    {
        if (launch.Convert)
            _tools.OpenFor(launch.Files ?? []);
        else
            _fileActions.OpenPrepared(launch);
    }

    internal void RestoreSession()
    {
        if (_settings.General.RememberLastPosition && File.Exists(_settings.Playback.LastFile))
            _dispatcher.Post(() => _fileActions.RestoreSession(
                _settings.Playback.LastFile, _settings.Playback.LastPosition));
    }

    internal bool ConfirmClose()
        => _downloadQueue is null
            || !_downloadQueue.HasUnfinishedItems
            || _view.Dialogs.ConfirmCloseWithDownloads();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _spotifyStart?.Cancel();
        _chapters?.Dispose();
        _controller?.Dispose();
        _spotifyStart?.Dispose();
        if (_spotifyPlayback is not null)
        {
            if (_spotifyEventsBound)
            {
                _view.CloseRequested -= CancelSpotifyStart;
                _player.CurrentChanged -= OnSpotifyCurrentChanged;
                _spotifyPlayback.CommandFailed -= OnSpotifyCommandFailed;
                _spotifyPlayback.TrackChanged -= OnSpotifyTrackChanged;
                _spotifyPlayback.Navigated -= OnSpotifyNavigated;
                _spotifyPlayback.PlayingChanged -= OnSpotifyPlayingChanged;
                _spotifyPlayback.QueueEmpty -= OnSpotifyQueueEmpty;
            }
            _spotifyPlayback.Dispose();
        }
        if (_spotifyAuth is not null)
        {
            _spotifyAuth.SignedOut -= OnSpotifySignedOut;
            _spotifyAuth.Dispose();
        }
        _radio?.Dispose();
        // Unsubscribe subtitle handlers before the player is released.
        _subtitles?.Dispose();
        // A running recording still has an encoder to flush before its engine goes away.
        _recorder?.Dispose();
        _sessions?.Dispose();
        _resolveCache?.Dispose();
        _downloadQueue?.Dispose();
        _appUpdates?.Dispose();
        _player.Dispose();
        // Disposing speech sends a global stop command after focus has already moved to another app.
        // Process termination releases the backend without cancelling that app's focus announcement.
    }
}
