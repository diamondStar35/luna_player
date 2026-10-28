using LunaPlayer.Actions;
using LunaPlayer.Application;
using WxSharp;
namespace LunaPlayer.UI.Services.Dialogs;

internal sealed record DialogContext(Frame Frame, IApplicationDispatcher Dispatcher,
    LunaPlayer.Recording.AudioCatalog Catalog, LunaPlayer.UI.Services.GlobalShortcuts GlobalShortcuts,
    Action<ActionId> RequestAction, IDialogService Dialogs)
{
    internal Window Parent => Wx.GetActiveWindow() ?? Frame;
}
