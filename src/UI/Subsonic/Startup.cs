using LunaPlayer.Application;

namespace LunaPlayer.UI.Subsonic;

/// <summary>Creates native controls on the UI thread while allowing input between construction steps.</summary>
internal sealed class Startup(IApplicationDispatcher dispatcher, Func<bool> visible, Action[] steps, Action<Exception> failed) : IDisposable
{
    private IDisposable? _next;
    private int _index;
    private bool _disposed;

    internal void Start()
    {
        if (_disposed || _next is not null || _index == steps.Length) return;
        // A one-shot UI timer yields to window messages. Posting all steps as pending callbacks
        // could let wx drain the entire construction queue before processing input.
        _next = dispatcher.Repeat(TimeSpan.FromMilliseconds(1), Advance);
    }

    private void Advance()
    {
        _next?.Dispose(); _next = null;
        if (_disposed || !visible()) return;
        try { steps[_index](); _index++; Start(); }
        catch (Exception error) { Dispose(); failed(error); }
    }

    public void Dispose()
    {
        _disposed = true; _next?.Dispose(); _next = null;
    }
}
