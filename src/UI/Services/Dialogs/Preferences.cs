using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class PreferencesDialogs(DialogContext context)
{
    private readonly DialogContext _context = context;
    public Settings? EditPreferences(Settings settings, LunaPlayer.UI.Preferences.PrefsOps operations, Action<string> speakHelp,
        bool openSpotifyCredentials = false)
    { var editable = settings.Copy(); using var dialog = new LunaPlayer.UI.Preferences.MainDialog(_context.Parent, editable, operations, speakHelp, _context.Dispatcher, _context.Catalog, _context.GlobalShortcuts, openSpotifyCredentials); return dialog.Show() ? editable : null; }
}
