using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Application.ActionHandlers;
using LunaPlayer.Bookmarks;
using LunaPlayer.Favorites;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.Media;

namespace LunaPlayer.Application;

internal sealed class ApplicationHost : IDisposable
{
    private readonly SingleInstanceService _singleInstance;
    private readonly SettingsStore _settingsStore;
    private readonly PlayerSettings _settings;
    private readonly ShortcutManager _shortcuts;
    private readonly ShortcutManager _globalShortcuts;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly IMainView _view;
    private readonly ISpeechOutput _speech;
    private readonly MediaPlayer _player;
    private readonly ApplicationController _controller;
    private readonly PathRequestQueue _pathQueue;
    private readonly LunaPlayer.YouTube.ResolveCache _resolveCache;
    private readonly LunaPlayer.YouTube.Components _components;
    private readonly LunaPlayer.Recording.AudioCatalog _catalog;
    private readonly LunaPlayer.Recording.RecordingSources _recordingSources;
    private readonly LunaPlayer.Recording.RecordingEngine _recorder;
    private readonly LunaPlayer.YouTube.YouTubeSessions _sessions;
    private bool _disposed;

    internal ApplicationHost(SingleInstanceService singleInstance, IReadOnlyList<string> initialPaths)
    {
        _singleInstance = singleInstance;
        _settingsStore = new SettingsStore(Paths.SettingsFile, Paths.LegacySettingsFile);
        _settings = _settingsStore.Load();
        // Before anything else: the action tables and every window below build their strings once, and they
        // have to be built in the user's language.
        Localization.Initialize(_settings.General.Language);
        _shortcuts = new ShortcutManager(ActionRegistry.All);
        _shortcuts.Apply(_settings.Shortcuts.Primary, _settings.Shortcuts.Secondary);
        _globalShortcuts = new ShortcutManager(GlobalActionDefinitions.All);
        _dispatcher = new WxDispatcher();
        _catalog = new LunaPlayer.Recording.AudioCatalog();
        _view = new MainFrame(_shortcuts, ActionRegistry.All, _dispatcher, _catalog);
        _speech = new SpeechOutput(_settings);
        _player = new MediaPlayer(new MpvPlaybackEngine(_view.NativeHandle), new PositionStore(Paths.PositionsFile));
        var clipboard = new WxClipboardService();
        // Now that there is a toolkit, the crash window can offer to copy.
        CrashReport.Install(clipboard);
        var selection = new PlaybackSelection();
        var router = new ActionRouter();
        var fileActions = new FileActions(router, _view, _player, _settings, _speech, clipboard, _dispatcher);
        _ = new PlaybackActions(router, _view, _player, _settings, _settingsStore, _speech, selection);

        _ = new EditActions(router, _view, _player, _speech, clipboard, fileActions);
        _ = new MarkedFileActions(router, _view, _player, _settings, _speech, clipboard, _dispatcher);
        var bookmarks = new BookmarkStore(Paths.BookmarksFile);
        _ = new BookmarkActions(router, _view, _player, _speech, bookmarks);
        _ = new DeviceActions(router, _view, _player, _settings, _settingsStore, _speech);
        var explode = new LunaPlayer.YouTube.ExplodeClient();
        var ytDlp = new LunaPlayer.YouTube.YtDlpClient();
        var youTube = new LunaPlayer.YouTube.Backend(explode, ytDlp, _settings);
        _resolveCache = new LunaPlayer.YouTube.ResolveCache(explode, ytDlp, youTube);
        _components = new LunaPlayer.YouTube.Components(_view, _settings, _speech, _dispatcher, ytDlp);
        // The handler and the sessions each need the other: the sessions play what the list offers, and the
        // handler saves, copies and opens what it names. The knot is tied with the three actions the
        // sessions borrow rather than by handing over the whole handler, so neither can reach into the
        // other for anything else.
        YouTubeActions? youTubeActions = null;
        _sessions = new LunaPlayer.YouTube.YouTubeSessions(
            _view, _player, _settings, _speech, _dispatcher, explode, youTube, _resolveCache,
            url => youTubeActions!.DownloadTo(url),
            url => youTubeActions!.CopyToClipboard(url),
            url => youTubeActions!.OpenInBrowser(url));
        youTubeActions = new YouTubeActions(
            router, _view, _player, _settings, _speech, clipboard, youTube, _sessions, _components, _dispatcher);
        _ = new FavoriteActions(router, _view, _player, _speech, new FavoriteStore(Paths.FavoritesFile), _sessions);
        _ = new PlaylistActions(router, _view, _player, _settings, _speech, _sessions);
        _ = new SettingsActions(router, _view, _settings, _settingsStore,
            new BackupService(_settingsStore, bookmarks), new FileAssociations(), _player, _shortcuts,
            _globalShortcuts, _speech, youTube, _components);
        _recordingSources = new LunaPlayer.Recording.RecordingSources(_settings.Recording);
        _recorder = new LunaPlayer.Recording.RecordingEngine(_catalog);
        _ = new RecordingActions(
            router, _view, _settings, _speech, _dispatcher, _catalog, _recordingSources, _recorder);
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
            _sessions);
        _pathQueue = new PathRequestQueue(HandleExternalPaths, _dispatcher);
        _singleInstance.StartListening(_pathQueue.Enqueue);
        // Posted rather than run here so a refusal is reported over a window the user can already see.
        _dispatcher.Post(() => GlobalShortcutBinder.Apply(_view, _globalShortcuts, _settings, _speech));
        // Looks for a newer yt-dlp only when the setting asks for it, and says nothing unless there is one.
        _dispatcher.Post(_components.CheckForUpdateInBackground);

        if (initialPaths.Count > 0)
            _dispatcher.Post(() => _controller.OpenPaths(initialPaths));
        else if (_settings.General.RememberLastPosition && File.Exists(_settings.Playback.LastFile))
            _dispatcher.Post(() => fileActions.RestoreSession(_settings.Playback.LastFile, _settings.Playback.LastPosition));
    }

    internal void Show() => _view.Show();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _singleInstance.Dispose();
        _pathQueue.Dispose();
        _controller.Dispose();
        // Before the player goes: a recording still running has a file to close, and the engine waits for
        // the encoder to flush its last block rather than leaving a truncated one behind.
        _recorder.Dispose();
        _sessions.Dispose();
        _resolveCache.Dispose();
        _player.Dispose();
        _speech.Dispose();
        _view.Dispose();
    }

    private void HandleExternalPaths(IReadOnlyList<string> paths)
    {
        _view.RestoreAndRaise();
        if (paths.Count > 0)
            _controller.OpenPaths(paths);
    }
}
