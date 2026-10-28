using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class AudioDialogs(DialogContext context)
{
    private readonly DialogContext _context = context;
    public int? ChooseAudioDevice(IReadOnlyList<string> descriptions, int selectedIndex) { using var dialog = new LunaPlayer.UI.Audio.DeviceDialog(_context.Parent, descriptions, selectedIndex); return dialog.Show(); }
    public int? ChooseAudioTrack(IReadOnlyList<string> labels, int selectedIndex) { using var dialog = new LunaPlayer.UI.Audio.TracksDialog(_context.Parent, labels, selectedIndex); return dialog.Show(); }
}
