using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class ConversionDialogs(DialogContext context)
{
    private readonly DialogContext _context = context;
    public ConversionRequest? ShowMediaConverter(IReadOnlyList<string>? initialFiles = null)
    { using var dialog = new LunaPlayer.UI.Conversion.MainDialog(_context.Parent, initialFiles); return dialog.Show(); }
    public ConversionOutcome? RunConversion(
        Func<Action<ConversionProgress>, CancellationToken, ConversionOutcome> work)
    { using var dialog = new LunaPlayer.UI.Conversion.ProgressDialog(_context.Parent, _context.Dispatcher, work); return dialog.Show(); }
    public void ShowConversionReport(string title, string message, string details)
    { using var dialog = new LunaPlayer.UI.Conversion.ReportDialog(_context.Parent, title, message, details); dialog.Show(); }
}
