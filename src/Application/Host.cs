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
    private Features? _features;
    private bool _shown;
    private bool _stopping;
    private bool _disposed;

    internal ApplicationHost(SingleInstanceService singleInstance, IReadOnlyList<string> initialPaths)
    {
        _singleInstance = singleInstance;
        // Begin settings before constructing even the shell, preserving overlap with native window creation.
        var settingsTask = Task.Run(Resources.LoadSettings);
        _dispatcher = new WxDispatcher();
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
        _singleInstance.StartListening(paths => _dispatcher.Post(() => HandleExternalPaths(paths)));
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
        _features?.Dispose();
        _view.Dispose();
    }
}
