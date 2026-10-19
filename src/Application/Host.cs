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
    private readonly IApplicationDispatcher _dispatcher;
    private readonly MainFrame _view;
    private readonly PathRequestQueue _pathQueue;
    private readonly IReadOnlyList<string> _initialPaths;
    private readonly List<IReadOnlyList<string>> _pendingPathRequests = [];
    private SettingsStore _settingsStore = null!;
    private Settings _settings = null!;
    private ShortcutManager _shortcuts = null!;
    private ShortcutManager _globalShortcuts = null!;
    private LunaPlayer.Equalizer.Library _equalizerLibrary = null!;
    private LunaPlayer.Recording.AudioCatalog _catalog = null!;
    private RecentsStore _recents = null!;
    private string _settingsError = string.Empty;
    private ISpeechOutput? _speech;
    private MediaPlayer? _player;
    private ApplicationController? _controller;
    private Commands.Playback.Equalizer? _equalizer;
    private Subtitles? _subtitles;
    private LunaPlayer.YouTube.Playback.ResolveCache? _resolveCache;
    private LunaPlayer.YouTube.Components.Service? _components;
    private Updates? _appUpdates;
    private LunaPlayer.Recording.RecordingSources? _recordingSources;
    private LunaPlayer.Recording.RecordingEngine? _recorder;
    private LunaPlayer.YouTube.Playback.Sessions? _sessions;
    private LunaPlayer.YouTube.Download.DownloadQueue? _downloadQueue;
    private Tools? _tools;
    private Files? _fileActions;
    private StartupResources? _pendingStartupResources;
    private bool _shown;
    private bool _shellReady;
    private bool _settingsReady;
    private bool _windowInitializationStarted;
    private bool _windowInitialized;
    private bool _ready;
    private bool _closeWhenReady;
    private bool _startupFailed;
    private bool _disposed;

    internal ApplicationHost(SingleInstanceService singleInstance, IReadOnlyList<string> initialPaths)
    {
        _singleInstance = singleInstance;
        _initialPaths = initialPaths.ToArray();
        // Configuration parsing and value normalization do not use wxWidgets. Begin them before even the
        // shell frame is constructed so disk access overlaps native window creation instead of following it.
        var settingsTask = Task.Run(LoadStartupSettings);
        _dispatcher = new WxDispatcher();
        _view = new MainFrame();
        // A close while mpv is still attaching to the window must wait for that short initialization to
        // finish; destroying the native handle underneath it can leave the backend unable to unwind.
        _view.CloseConfirmationRequested += ConfirmClose;
        // Construct the request queue now, but do not start its listener thread until Show has synchronously
        // presented the shell. A request arriving while services start is held and replayed when ready.
        _pathQueue = new PathRequestQueue(HandleExternalPaths, _dispatcher);
        // mpv/audio startup depends only on the normalized playback settings and the already-created native
        // handle. Always dispatch the coordinator itself: calling an async method here used to run its setup
        // synchronously whenever settings happened to finish early, delaying Show on only some launches.
        var nativeHandle = _view.NativeHandle;
        _ = Task.Run(() => BeginBackgroundInitializationAsync(nativeHandle, settingsTask));
    }

    /// <summary>Shows the inexpensive shell while configuration and native services continue on workers.
    /// Only localization, native controls and command wiring return to the UI thread.</summary>
    internal void Show()
    {
        if (_shown || _disposed)
            return;
        _shown = true;
        _view.Show();
        // Thread creation is allowed to follow the forced first repaint. A second instance retries its pipe
        // connection, so no request is lost during this short interval.
        _singleInstance.StartListening(_pathQueue.Enqueue);
        _shellReady = true;
        TryInitializeWindowResources();
    }

    private async Task BeginBackgroundInitializationAsync(
        nint nativeHandle,
        Task<StartupSettings> settingsTask)
    {
        StartupSettings startup;
        try
        {
            startup = await settingsTask.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            PostStartupFailure(failure);
            return;
        }
        if (_disposed)
            return;

        // Start the sound path first. Its dedicated worker cannot be held behind optional feature work in
        // the thread-pool queue, and it starts whether or not Windows has painted the shell yet.
        _ = InitializeBackgroundServicesAsync(nativeHandle, startup.Settings);
        try
        {
            _dispatcher.Post(() => OnStartupSettingsReady(startup));
        }
        catch (InvalidOperationException)
        {
            // The application closed before the settings read completed.
        }
    }

    private void OnStartupSettingsReady(StartupSettings startup)
    {
        if (_disposed || _startupFailed)
            return;
        _settingsStore = startup.Store;
        _settings = startup.Settings;
        _settingsError = startup.Error;
        _settingsReady = true;
        TryInitializeWindowResources();
    }

    private void TryInitializeWindowResources()
    {
        if (_disposed || _startupFailed || !_shellReady || !_settingsReady
            || _windowInitializationStarted)
            return;
        _windowInitializationStarted = true;
        try
        {
            // Shortcut normalization builds translated action definitions. Keep only that localization-bound
            // part on the wx thread; all ordinary setting values were normalized on the settings worker.
            Localization.Initialize(_settings.General.Language);
            _settings.NormalizeShortcuts();
            // Preset names are translated too. Materialize the shipped table here; the worker below can then
            // merge user presets without ever calling wxWidgets from outside its owning thread.
            _ = LunaPlayer.Equalizer.Presets.All;
            _ = InitializeWindowResourcesAsync();
        }
        catch (Exception failure)
        {
            HandleStartupFailure(failure);
        }
    }

    private async Task InitializeBackgroundServicesAsync(nint nativeHandle, Settings settings)
    {
        var playerTask = Task.Factory.StartNew(
            () =>
            {
                // This is a dedicated startup thread, so raising it cannot starve the thread pool. Audio is
                // the only background service on the launch critical path; optional feature construction
                // remains at normal priority and may not delay mpv when the machine is busy.
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                return CreatePlayer(nativeHandle, settings);
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default);
        var speechTask = Task.Run<ISpeechOutput>(() => new SpeechOutput(settings));
        var mediaControlsTask = Task.Run(() => new SystemMediaControls());
        var featuresTask = Task.Run(() => FeatureResources.Create(settings));
        try
        {
            await Task.WhenAll(speechTask, playerTask, mediaControlsTask, featuresTask).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            if (speechTask.Status == TaskStatus.RanToCompletion)
                speechTask.Result.Dispose();
            if (playerTask.Status == TaskStatus.RanToCompletion)
                playerTask.Result.Dispose();
            if (mediaControlsTask.Status == TaskStatus.RanToCompletion)
                mediaControlsTask.Result.Dispose();
            PostStartupFailure(failure);
            return;
        }

        var resources = new StartupResources(
            speechTask.Result, playerTask.Result, mediaControlsTask.Result, featuresTask.Result);
        try
        {
            _dispatcher.Post(() => OnNativeServicesReady(resources));
        }
        catch (InvalidOperationException)
        {
            // The event loop has already gone away. Dispose mpv, but let process termination release speech
            // without sending a global stop command after focus has moved to another application.
            resources.Dispose(disposeSpeech: false);
        }
    }

    private static StartupSettings LoadStartupSettings()
    {
        var store = new SettingsStore(Paths.SettingsFile);
        var settings = store.LoadForStartup();
        return new StartupSettings(store, settings, store.LastError);
    }

    private static MediaPlayer CreatePlayer(nint nativeHandle, Settings settings)
    {
        var engine = new MpvPlaybackEngine(nativeHandle, !settings.General.DisableMediaControls);
        try
        {
            var player = new MediaPlayer(engine, new PositionStore(Paths.PositionsFile));
            ConfigurePlayer(player, settings);
            return player;
        }
        catch
        {
            engine.Dispose();
            throw;
        }
    }

    private async Task InitializeWindowResourcesAsync()
    {
        var catalogTask = Task.Run(() => new LunaPlayer.Recording.AudioCatalog());
        var equalizerTask = Task.Run(() => new LunaPlayer.Equalizer.Library(
            _settings, _settingsStore, new LunaPlayer.Equalizer.PresetStore(Paths.EqualizerFile)));
        var recentsTask = Task.Run(() => new RecentsStore(Paths.RecentsFile));
        try
        {
            await Task.WhenAll(catalogTask, equalizerTask, recentsTask).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            PostStartupFailure(failure);
            return;
        }
        try
        {
            _dispatcher.Post(() => OnWindowResourcesReady(
                catalogTask.Result, equalizerTask.Result, recentsTask.Result));
        }
        catch (InvalidOperationException)
        {
            // The application closed before its menu data had finished loading.
        }
    }

    private void OnWindowResourcesReady(
        LunaPlayer.Recording.AudioCatalog catalog,
        LunaPlayer.Equalizer.Library equalizerLibrary,
        RecentsStore recents)
    {
        if (_disposed || _startupFailed)
            return;
        try
        {
            InitializeWindow(catalog, equalizerLibrary, recents);
            _windowInitialized = true;
            if (_pendingStartupResources is { } resources)
            {
                _pendingStartupResources = null;
                CompleteStartup(resources);
            }
        }
        catch (Exception failure)
        {
            HandleStartupFailure(failure);
        }
    }

    private void OnNativeServicesReady(StartupResources resources)
    {
        if (_disposed || _startupFailed)
        {
            resources.Dispose(disposeSpeech: false);
            return;
        }
        // Creating native menus can pump Windows messages. If this callback arrives re-entrantly while the
        // frame is still being filled in, hold the services until InitializeWindow has installed its input
        // and menu controllers rather than exposing a half-initialized view to feature constructors.
        if (!_windowInitialized)
        {
            _pendingStartupResources = resources;
            return;
        }
        CompleteStartup(resources);
    }

    private void InitializeWindow(
        LunaPlayer.Recording.AudioCatalog catalog,
        LunaPlayer.Equalizer.Library equalizerLibrary,
        RecentsStore recents)
    {
        _shortcuts = new ShortcutManager(ActionRegistry.All);
        _shortcuts.Apply(_settings.Shortcuts.Primary, _settings.Shortcuts.Secondary);
        _globalShortcuts = new ShortcutManager(GlobalActions.All);
        _catalog = catalog;
        _equalizerLibrary = equalizerLibrary;
        _recents = recents;
        var recentMenu = Files.BuildRecentMenu(_recents);
        _view.Initialize(
            _shortcuts, ActionRegistry.All, _dispatcher, _catalog, _equalizerLibrary.All, recentMenu);
    }

    private void CompleteStartup(StartupResources resources)
    {
        if (_disposed || _startupFailed)
        {
            resources.Dispose(disposeSpeech: false);
            return;
        }
        SystemMediaControls? mediaControls = null;
        try
        {
            _speech = resources.TakeSpeech();
            _player = resources.TakePlayer();
            mediaControls = resources.TakeMediaControls();
            var features = resources.TakeFeatures();
            InitializeFeatures(_speech, _player, mediaControls, features);
            mediaControls = null;
            _ready = true;
            if (_closeWhenReady)
            {
                _view.Close();
                return;
            }
            QueuePostStartupWork();
        }
        catch (Exception failure)
        {
            mediaControls?.Dispose();
            resources.Dispose();
            HandleStartupFailure(failure);
        }
    }

    private void InitializeFeatures(
        ISpeechOutput speech,
        MediaPlayer player,
        SystemMediaControls mediaControls,
        FeatureResources features)
    {
        var clipboard = new WxClipboardService();
        // Now that there is a toolkit, the crash window can offer to copy.
        CrashReport.SetClipboard(clipboard);
        var selection = features.Selection;
        var selections = features.Selections;
        var router = new ActionRouter();
        _ = new Help(router, _view);
        var appUpdates = new Updates(router, _view, _settings, _dispatcher);
        _appUpdates = appUpdates;
        var fileActions = new Files(router, _view, player, _settings, speech, clipboard, _dispatcher, _recents);
        _fileActions = fileActions;
        _ = new Commands.Playback.Playback(
            router, _view, player, _settings, _settingsStore, speech, selection, selections, _dispatcher);

        _ = new Edit(router, _view, player, speech, clipboard, fileActions);
        _ = new MarkedFiles(router, _view, player, _settings, speech, clipboard, _dispatcher);
        var bookmarks = features.Bookmarks;
        _ = new Commands.Playback.Bookmarks(router, _view, player, speech, bookmarks, selections);
        _ = new Devices(router, _view, player, _settings, _settingsStore, speech);
        _ = new AudioTracks(router, _view, player, speech);
        _subtitles = new Subtitles(router, _view, player, speech, _settings, _dispatcher);
        _equalizer = new Commands.Playback.Equalizer(
            router, _view, player, _settings, _settingsStore, speech, _equalizerLibrary);
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
                Tr("Downloads completed"),
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
        youTubeActions = new Commands.YouTube.YouTube(
            router, _view, player, _settings, speech, clipboard, youTube, sessions, components, _dispatcher,
            downloadQueue);
        _ = new Commands.YouTube.Favorites(router, _view, player, speech, favorites, sessions);
        _ = new Commands.YouTube.Playlist(router, _view, player, _settings, speech, sessions);
        _ = new Commands.App.Preferences(router, _view, _settings, _settingsStore,
            new BackupService(_settingsStore, bookmarks), new FileAssociations(), player, _shortcuts,
            _globalShortcuts, speech, youTube, components);
        var recordingSources = features.RecordingSources;
        _recordingSources = recordingSources;
        var recorder = new LunaPlayer.Recording.RecordingEngine(_catalog);
        _recorder = recorder;
        _ = new Commands.Recording.Recording(
            router, _view, _settings, speech, _dispatcher, _catalog, recordingSources, recorder);
        var tools = new Tools(router, _view);
        _tools = tools;
        _ = new Commands.Iptv.Iptv(router, _view, player, speech, _dispatcher,
            features.IptvSources);
        var sleepTimer = new SleepTimer(_dispatcher, player, speech);
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
            sessions,
            sleepTimer,
            speech,
            mediaControls);
        _controller = controller;
        // After the controller, which sets the rest of the audio state up from the same settings.
        _equalizer.Restore();
    }

    private void QueuePostStartupWork()
    {
        var speech = _speech!;
        var components = _components!;
        var updates = _appUpdates!;
        var fileActions = _fileActions!;
        var controller = _controller!;
        var tools = _tools!;

        if (_settingsError.Length > 0)
        {
            _dispatcher.Post(() => _view.Dialogs.ShowError(
                TrFormat("The settings file could not be loaded. Default settings will be used, and the existing file will not be overwritten. Import a valid settings file or reset the settings to replace it.\n\n{error}", _settingsError),
                Tr("Settings error")));
        }
        // Posted rather than run here so a refusal is reported over a window the user can already see.
        _dispatcher.Post(() => GlobalShortcutBinder.Apply(_view, _globalShortcuts, _settings, speech));
        // Looks for a newer yt-dlp only when the setting asks for it, and says nothing unless there is one.
        _dispatcher.Post(components.CheckForUpdateInBackground);
        // The application check is also quiet at startup: only a newer release opens a window.
        _dispatcher.Post(updates.CheckAtStartup);

        if (_initialPaths.Count > 0)
        {
            // Windows Explorer's "Convert with Luna" verb launches the player with --convert and the file or
            // folder, which opens the converter rather than playing anything. Everything else is opened to
            // play as before.
            if (IsConvertRequest(_initialPaths, out var convertFiles))
                _dispatcher.Post(() => tools.OpenFor(convertFiles));
            else
                _dispatcher.Post(() => controller.OpenPaths(_initialPaths));
        }
        else if (_settings.General.RememberLastPosition && File.Exists(_settings.Playback.LastFile))
            _dispatcher.Post(() => fileActions.RestoreSession(_settings.Playback.LastFile, _settings.Playback.LastPosition));

        foreach (var paths in _pendingPathRequests)
            _dispatcher.Post(() => HandleExternalPaths(paths));
        _pendingPathRequests.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _view.CloseConfirmationRequested -= ConfirmClose;
        _singleInstance.Dispose();
        _pathQueue.Dispose();
        _pendingStartupResources?.Dispose(disposeSpeech: false);
        _pendingStartupResources = null;
        _controller?.Dispose();
        // Unsubscribes from the player's subtitle events before the player itself goes below.
        _subtitles?.Dispose();
        // Before the player goes: a recording still running has a file to close, and the engine waits for
        // the encoder to flush its last block rather than leaving a truncated one behind.
        _recorder?.Dispose();
        _sessions?.Dispose();
        _resolveCache?.Dispose();
        _downloadQueue?.Dispose();
        _appUpdates?.Dispose();
        _player?.Dispose();
        // Closing a screen-reader backend sends a global stop command. Focus has already moved to the next
        // application here, so doing that would cancel its focus announcement. Process termination releases
        // the remaining speech resources without sending that command.
        _view.Dispose();
    }

    private bool ConfirmClose()
    {
        if (!_ready && !_startupFailed)
        {
            _closeWhenReady = true;
            return false;
        }
        return _downloadQueue is null
            || !_downloadQueue.HasUnfinishedItems
            || _view.Dialogs.ConfirmCloseWithDownloads();
    }

    private void HandleExternalPaths(IReadOnlyList<string> paths)
    {
        _view.RestoreAndRaise();
        if (paths.Count == 0)
            return;
        if (!_ready || _tools is null || _controller is null)
        {
            _pendingPathRequests.Add(paths.ToArray());
            return;
        }
        // A second launch carrying "Convert with Luna" opens the converter for its files rather than playing
        // them, the same as the first launch does.
        if (IsConvertRequest(paths, out var convertFiles))
            _tools.OpenFor(convertFiles);
        else
            _controller.OpenPaths(paths);
    }

    private void PostStartupFailure(Exception failure)
    {
        try
        {
            _dispatcher.Post(() => HandleStartupFailure(failure));
        }
        catch (InvalidOperationException)
        {
            // The application has already closed, so there is no UI left in which to report startup.
        }
    }

    private void HandleStartupFailure(Exception failure)
    {
        if (_disposed || _startupFailed)
            return;
        _startupFailed = true;
        CrashReport.Report(failure);
        _view.Close();
    }

    private sealed record StartupSettings(SettingsStore Store, Settings Settings, string Error);

    private static void ConfigurePlayer(MediaPlayer player, Settings settings)
    {
        player.SetVolume(settings.Audio.Volume);
        settings.Audio.Pitch = player.SetPitch(settings.Audio.Pitch);
        player.SetSpeed(settings.Audio.Speed);
        player.TrackPositions(settings.Audio.SaveFilePositions);
        player.SetEndBehavior(settings.Audio.EndBehavior);
        player.ConfigureSilence(settings.Silence);
        player.SetNormalization(settings.Audio.NormalizeAudio);
        player.SetMono(settings.Audio.MonoAudio);
        player.SetPan(settings.Audio.Pan);
        player.SetSilenceRemoval(settings.Silence.Enabled);
        settings.Audio.NormalizeAudio = player.IsNormalizationEnabled;
        settings.Audio.MonoAudio = player.IsMonoEnabled;
        settings.Silence.Enabled = player.IsSilenceRemovalEnabled;
        if (!string.IsNullOrWhiteSpace(settings.Audio.Device))
            player.SetAudioDevice(settings.Audio.Device);
    }

    /// <summary>Owns the playback, speech, Windows-media and managed feature resources while they cross from
    /// worker startup to the UI thread. Taking one transfers its lifetime to the application host.</summary>
    private sealed class StartupResources(
        ISpeechOutput speech,
        MediaPlayer player,
        SystemMediaControls mediaControls,
        FeatureResources features)
    {
        private ISpeechOutput? _speech = speech;
        private MediaPlayer? _player = player;
        private SystemMediaControls? _mediaControls = mediaControls;
        private FeatureResources? _features = features;

        internal ISpeechOutput TakeSpeech()
            => Interlocked.Exchange(ref _speech, null)
                ?? throw new InvalidOperationException("Speech startup resources were already transferred.");

        internal MediaPlayer TakePlayer()
            => Interlocked.Exchange(ref _player, null)
                ?? throw new InvalidOperationException("Playback startup resources were already transferred.");

        internal SystemMediaControls TakeMediaControls()
            => Interlocked.Exchange(ref _mediaControls, null)
                ?? throw new InvalidOperationException("Media controls startup resources were already transferred.");

        internal FeatureResources TakeFeatures()
            => Interlocked.Exchange(ref _features, null)
                ?? throw new InvalidOperationException("Feature startup resources were already transferred.");

        internal void Dispose(bool disposeSpeech = true)
        {
            Interlocked.Exchange(ref _mediaControls, null)?.Dispose();
            Interlocked.Exchange(ref _player, null)?.Dispose();
            _ = Interlocked.Exchange(ref _features, null);
            var remainingSpeech = Interlocked.Exchange(ref _speech, null);
            if (disposeSpeech)
                remainingSpeech?.Dispose();
        }
    }

    /// <summary>Managed foundations for features added after the original player startup path. Their
    /// constructors neither create windows nor translate text, so they can be prepared together on a worker
    /// while wxWidgets builds the visible interface.</summary>
    private sealed record FeatureResources(
        PlaybackSelection Selection,
        SelectionSlots Selections,
        BookmarkStore Bookmarks,
        FavoriteStore Favorites,
        IptvSourceStore IptvSources,
        LunaPlayer.Recording.RecordingSources RecordingSources,
        LunaPlayer.YouTube.Client.Resolver Resolver,
        LunaPlayer.YouTube.Client.Updater Updater,
        LunaPlayer.YouTube.Metadata.Client Metadata,
        LunaPlayer.YouTube.Backend YouTube,
        LunaPlayer.YouTube.Components.Installer Installer)
    {
        internal static FeatureResources Create(Settings settings)
        {
            var runner = new LunaPlayer.YouTube.Client.Runner(settings);
            var channels = new LunaPlayer.YouTube.Client.Channels(runner);
            var metadata = new LunaPlayer.YouTube.Metadata.Client();
            return new FeatureResources(
                new PlaybackSelection(),
                new SelectionSlots(),
                new BookmarkStore(Paths.BookmarksFile),
                new FavoriteStore(Paths.FavoritesFile),
                new IptvSourceStore(Paths.IptvSourcesFile),
                new LunaPlayer.Recording.RecordingSources(settings.Recording),
                new LunaPlayer.YouTube.Client.Resolver(runner),
                new LunaPlayer.YouTube.Client.Updater(runner),
                metadata,
                new LunaPlayer.YouTube.Backend(metadata, channels),
                new LunaPlayer.YouTube.Components.Installer());
        }
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
