using LunaPlayer.Application;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class WxDispatcher : IApplicationDispatcher
{
    public void Post(Action action) => Wx.CallAfter(action);

    // The timer is owned by the App rather than a window so that it keeps running for as long as the
    // application does, independently of the main frame's lifetime.
    public IDisposable Repeat(TimeSpan interval, Action action)
    {
        var app = App.Current ?? throw new InvalidOperationException("No App is running.");
        // Each timer takes an id of its own, so one timer's tick never runs another's handler.
        var timer = new WxSharp.Timer(app);
        timer.Tick += (_, _) => action();
        timer.Start(Math.Max(1, (int)interval.TotalMilliseconds));
        return timer;
    }
}
