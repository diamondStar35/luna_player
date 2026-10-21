using LunaPlayer.Actions;
using LunaPlayer.Application.Commands.Files;
using LunaPlayer.Configuration;
using LunaPlayer.UI;

namespace LunaPlayer.Application;

/// <summary>Stages settings, background services, and window initialization without changing their thread
/// ownership. The host receives one completed startup only after menus and input handlers are installed.</summary>
internal sealed class Startup : IDisposable
{
    private readonly MainFrame _view;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly IReadOnlyList<string> _initialPaths;
    private readonly Action<StartupResult> _complete;
    private readonly Lock _gate = new();
    private readonly TaskCompletionSource _shellPainted = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private StartupSettings? _settings;
    private WindowResources? _window;
    private StartupResources? _services;
    private ShortcutManager _shortcuts = null!;
    private ShortcutManager _globalShortcuts = null!;
    private Task? _nativeTask;
    private Task<PreparedLaunch>? _launchTask;
    private bool _shellReady;
    private bool _windowInitializationStarted;
    private bool _windowInitialized;
    private bool _stopping;
    private bool _failed;
    private bool _disposed;

    internal Startup(
        MainFrame view,
        IApplicationDispatcher dispatcher,
        IReadOnlyList<string> initialPaths,
        Action<StartupResult> complete)
    {
        _view = view;
        _dispatcher = dispatcher;
        _initialPaths = initialPaths.ToArray();
        _complete = complete;
    }

    internal void Start(Task<StartupSettings> settingsTask)
    {
        var nativeHandle = _view.NativeHandle;
        // Always queue this entry point. If settings finished early, an inline async call would run service
        // setup synchronously on the UI thread before Show.
        _ = Task.Run(() => BeginAsync(nativeHandle, settingsTask));
    }

    internal void ShellPainted() => _shellPainted.TrySetResult();

    internal void WindowShown()
    {
        _shellReady = true;
        TryInitializeWindow();
    }

    private async Task BeginAsync(nint nativeHandle, Task<StartupSettings> settingsTask)
    {
        StartupSettings settings;
        try
        {
            settings = await settingsTask.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            PostFailure(failure);
            return;
        }
        lock (_gate)
        {
            if (_disposed || _stopping)
                return;
            // Start audio first, on its dedicated worker. Publish its lifetime under the close gate so a
            // close cannot release the native window while MPV is still being created against its handle.
            _nativeTask = CreateServicesAsync(nativeHandle, settings);
        }
        // Optional DLL discovery, stores, and folder walking wait for the first paint. Folder walking keeps
        // its dedicated normal-priority worker so it cannot be held behind optional pool work or starve audio.
        await _shellPainted.Task.ConfigureAwait(false);
        lock (_gate)
        {
            if (_disposed || _stopping)
                return;
            _launchTask = _initialPaths.Count == 0
                ? Task.FromResult(new PreparedLaunch(false, [], null, null, null))
                : Task.Factory.StartNew(
                    () => Resources.PrepareLaunch(_initialPaths, settings.Settings),
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
        }
        try
        {
            // The prepared launch task must be published before UI callbacks can finish command wiring.
            _dispatcher.Post(() => OnSettingsReady(settings));
        }
        catch (InvalidOperationException)
        {
            // The application closed before its settings could be attached.
        }
    }

    private void OnSettingsReady(StartupSettings settings)
    {
        if (_disposed || _stopping || _failed)
            return;
        _settings = settings;
        TryInitializeWindow();
    }

    private void TryInitializeWindow()
    {
        if (_disposed || _stopping || _failed || !_shellReady || _settings is null
            || _windowInitializationStarted)
            return;
        _windowInitializationStarted = true;
        try
        {
            // Only the localization-bound work belongs here. The ordinary settings values were normalized
            // on the settings worker; action labels and shipped preset names require the wx thread.
            Localization.Initialize(_settings.Settings.General.Language);
            _settings.Settings.NormalizeShortcuts();
            _ = LunaPlayer.Equalizer.Presets.All;
            _ = LoadWindowAsync(_settings);
        }
        catch (Exception failure)
        {
            Fail(failure);
        }
    }

    private async Task CreateServicesAsync(nint nativeHandle, StartupSettings settings)
    {
        StartupResources services;
        try
        {
            services = await Resources.CreateServicesAsync(
                nativeHandle, settings.Settings, _shellPainted.Task).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            PostFailure(failure);
            return;
        }
        try
        {
            _dispatcher.Post(() => OnServicesReady(services));
        }
        catch (InvalidOperationException)
        {
            // wx has already shut down. Release MPV without interrupting another app's speech.
            services.Dispose(disposeSpeech: false);
        }
    }

    private async Task LoadWindowAsync(StartupSettings settings)
    {
        WindowResources window;
        try
        {
            window = await Resources.LoadWindowResourcesAsync(
                settings.Settings, settings.Store).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            PostFailure(failure);
            return;
        }
        try
        {
            _dispatcher.Post(() => OnWindowReady(window));
        }
        catch (InvalidOperationException)
        {
            // The application closed before its menu data had finished loading.
        }
    }

    private void OnWindowReady(WindowResources window)
    {
        if (_disposed || _stopping || _failed)
            return;
        try
        {
            var settings = _settings!.Settings;
            _shortcuts = new ShortcutManager(ActionRegistry.All);
            _shortcuts.Apply(settings.Shortcuts.Primary, settings.Shortcuts.Secondary);
            _globalShortcuts = new ShortcutManager(GlobalActions.All);
            _window = window;
            _view.Initialize(_shortcuts, ActionRegistry.All, _dispatcher, window.Catalog,
                window.Equalizer.All, Files.BuildRecentMenu(window.Recents));
            _windowInitialized = true;
            TryComplete();
        }
        catch (Exception failure)
        {
            Fail(failure);
        }
    }

    private void OnServicesReady(StartupResources services)
    {
        if (_disposed || _stopping || _failed)
        {
            services.Dispose(disposeSpeech: false);
            return;
        }
        _services = services;
        TryComplete();
    }

    private void TryComplete()
    {
        // Native menu creation can pump messages and deliver the service callback re-entrantly. Hold its
        // resources until Initialize has returned and input/menu controllers can safely receive commands.
        if (_disposed || _stopping || _failed || !_windowInitialized || _services is not { } services)
            return;
        _services = null;
        try
        {
            _complete(new StartupResult(_settings!, _window!, services, _shortcuts, _globalShortcuts,
                _initialPaths.Count > 0 ? _launchTask : null));
        }
        catch (Exception failure)
        {
            services.Dispose();
            Fail(failure);
        }
    }

    internal Task StopAsync()
    {
        lock (_gate)
        {
            _stopping = true;
            return _nativeTask ?? Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _stopping = true;
        }
        _services?.Dispose(disposeSpeech: false);
        _services = null;
    }

    private void PostFailure(Exception failure)
    {
        try
        {
            _dispatcher.Post(() => Fail(failure));
        }
        catch (InvalidOperationException)
        {
            // wx has already shut down, leaving no window in which to report startup.
        }
    }

    private void Fail(Exception failure)
    {
        if (_disposed || _stopping || _failed)
            return;
        _failed = true;
        CrashReport.Report(failure);
        _view.Close();
    }
}
