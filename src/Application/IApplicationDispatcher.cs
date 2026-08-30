namespace LunaPlayer.Application;

internal interface IApplicationDispatcher
{
    /// <summary>Runs an action on the UI thread, from any thread.</summary>
    void Post(Action action);

    /// <summary>Runs an action on the UI thread on a repeating interval until the result is disposed.</summary>
    IDisposable Repeat(TimeSpan interval, Action action);
}
