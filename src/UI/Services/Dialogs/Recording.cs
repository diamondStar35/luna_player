using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class RecordingDialogs(DialogContext context) : IDisposable
{
    private readonly DialogContext _context = context;
    private LunaPlayer.UI.Recording.MainDialog? _dialog;
    public void ShowRecording(
        LunaPlayer.Recording.AudioCatalog catalog, LunaPlayer.Recording.RecordingSources sources, LunaPlayer.Recording.RecordingEngine engine)
    {
        _dialog ??= new LunaPlayer.UI.Recording.MainDialog(_context.Dispatcher, catalog, sources, engine);
        _dialog.Show();
    }

    public void Dispose()
    {
        _dialog?.Dispose();
        _dialog = null;
    }
}
