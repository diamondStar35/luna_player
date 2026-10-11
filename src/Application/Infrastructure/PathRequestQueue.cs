namespace LunaPlayer.Application;

internal sealed class PathRequestQueue : IDisposable
{
    private readonly object _sync = new();
    private readonly List<string> _paths = [];
    private readonly Action<IReadOnlyList<string>> _handler;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly System.Threading.Timer _timer;
    private bool _disposed;

    internal PathRequestQueue(Action<IReadOnlyList<string>> handler, IApplicationDispatcher dispatcher)
    {
        _handler = handler;
        _dispatcher = dispatcher;
        _timer = new System.Threading.Timer(Flush);
    }

    internal void Enqueue(IEnumerable<string> paths)
    {
        lock (_sync)
        {
            if (_disposed)
                return;
            _paths.AddRange(paths.Where(static path => !string.IsNullOrWhiteSpace(path)));
            _timer.Change(120, Timeout.Infinite);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            _paths.Clear();
        }
        _timer.Dispose();
    }

    private void Flush(object? state)
    {
        string[] paths;
        lock (_sync)
        {
            if (_disposed || _paths.Count == 0)
                return;
            paths = [.. _paths];
            _paths.Clear();
        }
        _dispatcher.Post(() => _handler(paths));
    }
}
