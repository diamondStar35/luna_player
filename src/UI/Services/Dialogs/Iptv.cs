using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class IptvDialogs(DialogContext context)
{
    private readonly DialogContext _context = context;
    public LunaPlayer.UI.Iptv.IptvSourceRequest? ManageIptvSources(IReadOnlyList<LunaPlayer.UI.Iptv.IptvSourceListItem> sources, string selectedId) { using var dialog = new LunaPlayer.UI.Iptv.SourcesDialog(_context.Parent, sources, selectedId); return dialog.Show(); }
    public LunaPlayer.Iptv.IptvSourceDraft? EditIptvSource(string caption, LunaPlayer.Iptv.IptvSourceDraft value) { using var dialog = new LunaPlayer.UI.Iptv.SourceEditDialog(_context.Parent, caption, value); return dialog.Show(); }
    public int? BrowseChannels(LunaPlayer.UI.Iptv.ChannelBrowserPrompt prompt) { using var dialog = new Iptv.ChannelBrowserDialog(_context.Parent, prompt); return dialog.Show(); }
}
