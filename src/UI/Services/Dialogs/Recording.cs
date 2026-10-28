using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class RecordingDialogs(DialogContext context)
{
    private readonly DialogContext _context = context;
    public void ShowRecording(
        LunaPlayer.Recording.AudioCatalog catalog, LunaPlayer.Recording.RecordingSources sources, LunaPlayer.Recording.RecordingEngine engine)
    { using var dialog = new LunaPlayer.UI.Recording.MainDialog(_context.Parent, _context.Dispatcher, catalog, sources, engine); dialog.Show(); }
}
