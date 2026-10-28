using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class RadioDialogs(DialogContext context) : IDisposable
{
    private readonly DialogContext _context = context;
    private Radio.BrowserDialog? _radioBrowser;

    public LunaPlayer.Radio.StationView? ShowRadioBrowser(LunaPlayer.Radio.Browsing state,
        LunaPlayer.Radio.Catalog catalog, LunaPlayer.Radio.Library library, Action<string> speak,
        IClipboardService clipboard, bool returnToStation)
    {
        // Keep the controls and cached browsing locations while a radio session is playing.
        _radioBrowser ??= new Radio.BrowserDialog(_context.Frame, _context.Dispatcher, state, catalog, library, speak, clipboard);
        return _radioBrowser.Show(returnToStation);
    }
    public void Dispose() { _radioBrowser?.Dispose(); _radioBrowser = null; }
}
