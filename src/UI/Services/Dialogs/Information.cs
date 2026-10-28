using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class InformationDialogs(DialogContext context)
{
    private readonly DialogContext _context = context;
    public void ShowAbout() { using var dialog = new LunaPlayer.UI.Help.AboutDialog(_context.Parent); dialog.Show(); }
    public bool OfferAppUpdate(LunaPlayer.UI.Updates.AppUpdatePrompt prompt) { using var dialog = new LunaPlayer.UI.Updates.UpdateDialog(_context.Parent, prompt); return dialog.Show(); }
    public LunaPlayer.UI.Bookmarks.BookmarkManagementRequest? ManageBookmarks(IReadOnlyList<LunaPlayer.UI.Bookmarks.BookmarkListItem> bookmarks) { using var dialog = new LunaPlayer.UI.Bookmarks.ManagerDialog(_context.Parent, bookmarks); return dialog.Show(); }
    public LunaPlayer.UI.Playlists.OpenedFilesRequest? ChooseOpenedFile(int count, Func<int, string> nameAt, int selectedIndex) { using var dialog = new LunaPlayer.UI.Playlists.OpenedFilesDialog(_context.Parent, count, nameAt, selectedIndex); return dialog.Show(); }
    public void ShowTextInfo(string title, string text) { using var dialog = new LunaPlayer.UI.Help.TextDialog(_context.Parent, title, text); dialog.Show(); }
}
