using LunaPlayer.Application;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class WxDispatcher : IApplicationDispatcher
{
    public void Post(Action action) => Wx.CallAfter(action);
}
