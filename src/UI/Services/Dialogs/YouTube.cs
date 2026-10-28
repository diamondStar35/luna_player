using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class YouTubeDialogs(DialogContext context) : IDisposable
{
    private readonly DialogContext _context = context;
    private YouTube.ResultsDialog? _youTubeResults;
    private readonly HashSet<YouTube.DownloadProgressDialog> _downloadProgressDialogs = [];
    public bool ConfirmCloseWithDownloads()
    { using var dialog = new YouTube.DownloadExitDialog(_context.Parent); return dialog.Show(); }

    public LunaPlayer.UI.YouTube.YouTubeLinkKind? ChooseYouTubeLinkKind() { using var dialog = new YouTube.LinkKindDialog(_context.Parent); return dialog.Show(); }
    public LunaPlayer.UI.YouTube.YouTubeSearchRequest? SearchYouTube(LunaPlayer.UI.YouTube.YouTubeSearchPrompt prompt) { using var dialog = new YouTube.SearchDialog(_context.Parent, _context.Dispatcher, prompt); return dialog.Show(); }
    public LunaPlayer.UI.YouTube.FavoriteRequest? ManageFavorites(IReadOnlyList<LunaPlayer.UI.YouTube.FavoriteListItem> favorites, string selectedId) { using var dialog = new YouTube.FavoritesDialog(_context.Parent, favorites, selectedId); return dialog.Show(); }
    public LunaPlayer.UI.YouTube.FavoriteDraft? EditFavorite(string caption, LunaPlayer.UI.YouTube.FavoriteDraft value) { using var dialog = new YouTube.FavoriteEditDialog(_context.Parent, caption, value); return dialog.Show(); }
    public LunaPlayer.UI.YouTube.ResultChoice? ShowYouTubeResults(LunaPlayer.UI.YouTube.YouTubeResultsPrompt prompt)
    {
        // A channel's tabbed browser has a different control structure, so it is still built per open.
        if (prompt.Tabs is { Count: > 0 })
        {
            using var channel = new YouTube.ResultsDialog(_context.Parent, prompt);
            return channel.Show();
        }
        // The plain single-list window is built once and reused: reopening refreshes the list instead of
        // reconstructing the window. Parented to the main frame, which outlives every such open.
        if (_youTubeResults is null || !_youTubeResults.IsSingleList)
            _youTubeResults = new YouTube.ResultsDialog(_context.Frame, prompt);
        else
            _youTubeResults.Refresh(prompt);
        return _youTubeResults.Show();
    }
    public bool OfferYouTubeComponents() { using var dialog = new YouTube.ComponentsDialog(_context.Parent); return dialog.Show(); }

    public Downloads.OptionsResult? ShowDownloadOptions(Downloads.Request defaults)
    { using var dialog = new YouTube.DownloadDialog(_context.Parent, defaults); return dialog.Show(); }

    public void ShowDownloadQueue(
        Downloads.DownloadQueue queue,
        Func<IReadOnlyList<string>, string?> addLinks,
        Action start)
    { using var dialog = new YouTube.DownloadQueueDialog(_context.Parent, _context.Dispatcher, queue, addLinks, start); dialog.Show(); }

    public void ShowDownloadProgress(
        Downloads.DownloadQueue queue,
        Guid itemId,
        Action<Downloads.Outcome?> completed)
    {
        var dialog = new YouTube.DownloadProgressDialog(_context.Dispatcher, queue, itemId);
        dialog.Closed += (closed, outcome) =>
        {
            _downloadProgressDialogs.Remove(closed);
            completed(outcome);
        };
        _downloadProgressDialogs.Add(dialog);
        dialog.Show();
    }

    internal void CloseDownloadProgressWindows()
    {
        foreach (var dialog in _downloadProgressDialogs.ToArray())
            dialog.Dispose();
        _downloadProgressDialogs.Clear();
    }
    public void ShowVideoDescription(string title, string description) { using var dialog = new YouTube.DescriptionDialog(_context.Parent, title, description); dialog.Show(); }
    public void Dispose() { _youTubeResults?.Dispose(); _youTubeResults = null; CloseDownloadProgressWindows(); }
}
