using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class EqualizerDialogs(DialogContext context)
{
    private readonly DialogContext _context = context;

    public LunaPlayer.UI.Equalizer.EqualizerEditResult? EditEqualizerPreset(LunaPlayer.UI.Equalizer.EqualizerEditContext context)
    {
        using var dialog = new LunaPlayer.UI.Equalizer.EditDialog(
            _context.Frame, context.Title, context.Name, context.CanRename, context.BasePresetId, context.Slots,
            context.Library.All, context.Library.Slots, context.Library.DefaultSlots, context.Preview);
        return dialog.Show();
    }

    public bool ManageEqualizerPresets(LunaPlayer.Equalizer.Library library)
    {
        using var dialog = new LunaPlayer.UI.Equalizer.PresetsDialog(_context.Frame, library);
        return dialog.Show();
    }
}
