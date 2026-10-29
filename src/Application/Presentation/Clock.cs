namespace LunaPlayer.Application.Presentation;

/// <summary>A shared app-thread tick keeps all playback presenters on the same one-second sample.</summary>
internal sealed class Clock(IApplicationDispatcher dispatcher, Action tick) : IDisposable
{
    private IDisposable? _timer;
    private bool _disposed;

    internal void Update(bool wanted)
    {
        if (_disposed) return;
        if (wanted && _timer is null) _timer = dispatcher.Repeat(TimeSpan.FromSeconds(1), tick);
        else if (!wanted)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose();
        _timer = null;
    }
}
