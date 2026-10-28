using LunaPlayer.UI;

namespace LunaPlayer.Application;

/// <summary>Owns the main window and hands launch requests to the running application. Startup staging
/// and feature ownership have separate lifetimes so the host does not construct individual commands.</summary>
internal sealed class ApplicationHost : IDisposable
{
    private readonly SingleInstanceService _singleInstance;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly MainFrame _view;
    private readonly Startup _startup;
    private readonly List<IReadOnlyList<string>> _pendingPathRequests = [];
    private IReadOnlyList<string>? _pendingExternalPaths;
    private int _externalOpenQueued;
    private Features? _features;
    private bool _shown;
    private bool _stopping;
    private bool _disposed;

    internal ApplicationHost(SingleInstanceService singleInstance, IReadOnlyList<string> initialPaths)
    {
        _singleInstance = singleInstance;
        // Begin settings before constructing even the shell, preserving overlap with native window creation.
        var settingsTask = Task.Run(Resources.LoadSettings);
        _dispatcher = new LunaPlayer.UI.Services.WxDispatcher();
        _view = new MainFrame();
        _startup = new Startup(_view, _dispatcher, initialPaths, CompleteStartup);
        // Register close handling before the first background service can be attached.
        _view.CloseRequested += StopStartupAsync;
        _view.CloseConfirmationRequested += ConfirmClose;
        _startup.Start(settingsTask);
    }

    internal void Show()
    {
        if (_shown || _disposed)
            return;
        _shown = true;
        _view.Show();
        _startup.ShellPainted();
        // Named-pipe construction stays on its own thread and starts only after the shell is painted.
        _singleInstance.StartListening(QueueExternalPaths);
        _startup.WindowShown();
    }

    private void CompleteStartup(StartupResult result)
    {
        _features = Features.Create(_view, _dispatcher, result);
        _features.QueueStartupWork();
        if (result.Launch is { } launch)
            QueueInitialPaths(launch);
        else
            _features.RestoreSession();

        foreach (var paths in _pendingPathRequests)
            _dispatcher.Post(() => HandleExternalPaths(paths));
        _pendingPathRequests.Clear();
    }

    private void QueueInitialPaths(Task<PreparedLaunch> launch)
    {
        void OpenPrepared(Task<PreparedLaunch> completed)
        {
            if (!completed.IsCompletedSuccessfully || _disposed || _stopping)
                return;
            try
            {
                _dispatcher.Post(() =>
                {
                    if (!_disposed && !_stopping)
                        _features!.OpenPrepared(completed.Result);
                });
            }
            catch (InvalidOperationException)
            {
                // A launch scan can finish after wx has already shut down.
            }
        }
        if (launch.IsCompleted)
            OpenPrepared(launch);
        else
            _ = launch.ContinueWith(OpenPrepared, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void QueueExternalPaths(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            _dispatcher.Post(() => HandleExternalPaths(paths));
            return;
        }
        // Explorer key repeat can produce opens faster than playback can replace a file. Keep only the
        // latest unhandled request, rather than making recovery replay every stale file-opening command.
        Interlocked.Exchange(ref _pendingExternalPaths, paths);
        if (Interlocked.Exchange(ref _externalOpenQueued, 1) == 0)
            _dispatcher.Post(OpenPendingExternalPaths);
    }

    private void OpenPendingExternalPaths()
    {
        // Release the posting flag before taking the request. A concurrent arrival is either included in
        // this exchange or schedules another callback; it can never be left pending without a callback.
        Interlocked.Exchange(ref _externalOpenQueued, 0);
        var paths = Interlocked.Exchange(ref _pendingExternalPaths, null);
        if (paths is not null)
            HandleExternalPaths(paths);
    }

    private void HandleExternalPaths(IReadOnlyList<string> paths)
    {
        if (_disposed || _stopping)
            return;
        if (paths.Count == 0)
        {
            // A plain launch activates the existing window; opening a file leaves its focus and state alone.
            _view.RestoreAndRaise();
            return;
        }
        if (_features is null)
        {
            _pendingPathRequests.Add(paths.ToArray());
            return;
        }
        _features.OpenPaths(paths);
    }

    private bool ConfirmClose() => _features?.ConfirmClose() ?? true;

    private Task StopStartupAsync()
    {
        _stopping = true;
        return _startup.StopAsync();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _view.CloseConfirmationRequested -= ConfirmClose;
        _view.CloseRequested -= StopStartupAsync;
        _singleInstance.Dispose();
        _startup.Dispose();
        try { _features?.Dispose(); }
        finally { _view.Dispose(); }
    }
}
