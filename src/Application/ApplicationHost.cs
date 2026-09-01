using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Application.ActionHandlers;
using LunaPlayer.Bookmarks;
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
        _view = new MainFrame(_shortcuts, ActionRegistry.All);
        _speech = new SpeechOutput(_settings);
        _player = new MediaPlayer(new MpvPlaybackEngine(_view.NativeHandle), new PositionStore(Paths.PositionsFile));
        var clipboard = new WxClipboardService();
        var selection = new PlaybackSelection();
        var router = new ActionRouter();
        var fileActions = new FileActions(router, _view, _player, _settings, _speech, clipboard);
        _ = new PlaybackActions(router, _view, _player, _settings, _settingsStore, _speech, selection);
        _ = new PlaylistActions(router, _view, _player, _settings, _speech);
        _ = new EditActions(router, _view, _player, _speech, clipboard, fileActions);
        _ = new MarkedFileActions(router, _view, _player, _settings, _speech, clipboard);
        var bookmarks = new BookmarkStore(Paths.BookmarksFile);
        _ = new BookmarkActions(router, _view, _player, _speech, bookmarks);
        _ = new DeviceActions(router, _view, _player, _settings, _settingsStore, _speech);
        _ = new SettingsActions(router, _view, _settings, _settingsStore,
            new BackupService(_settingsStore, bookmarks), new FileAssociations(), _player, _shortcuts,
            _globalShortcuts, _speech);
        router.EnsureComplete(ActionRegistry.All);
        _controller = new ApplicationController(
            _view,
            _player,
            _settings,
            _settingsStore,
            _dispatcher,
            router,
            fileActions,
            selection);
        _pathQueue = new PathRequestQueue(HandleExternalPaths, _dispatcher);
        _singleInstance.StartListening(_pathQueue.Enqueue);
        // Posted rather than run here so a refusal is reported over a window the user can already see.
        _dispatcher.Post(() => GlobalShortcutBinder.Apply(_view, _globalShortcuts, _settings, _speech));

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
