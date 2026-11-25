using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using SharedDownloads = LunaPlayer.Downloads;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class YouTubeDialogs : IDisposable
{
    private readonly DialogContext _context;
    private readonly LunaPlayer.UI.Downloads.DownloadOutcomes _downloadOutcomes;
    private YouTube.ResultsDialog? _youTubeResults;
    private YouTube.FavoritesDialog? _favorites;
    private YouTube.FavoriteEditDialog? _favoriteEditor;
    private Window? _resultsOwner;
    private LunaPlayer.UI.Downloads.DownloadQueueDialog? _queueDialog;
    private readonly HashSet<LunaPlayer.UI.Downloads.DownloadProgressDialog> _downloadProgressDialogs = [];

    internal YouTubeDialogs(DialogContext context)
    {
        _context = context;
        _downloadOutcomes = new LunaPlayer.UI.Downloads.DownloadOutcomes(context.Dispatcher, () => LunaPlayer.UI.Downloads.DownloadOutcomes.SelectOwner(
            Wx.GetActiveWindow(), _resultsOwner, context.Frame, _downloadProgressDialogs.Select(dialog => dialog.Window)));
    }
    public bool ConfirmCloseWithDownloads()
    { using var dialog = new LunaPlayer.UI.Downloads.DownloadExitDialog(_context.Parent); return dialog.Show(); }

    public LunaPlayer.UI.YouTube.YouTubeLinkKind? ChooseYouTubeLinkKind() { using var dialog = new YouTube.LinkKindDialog(_context.Parent); return dialog.Show(); }
    public LunaPlayer.UI.YouTube.YouTubeSearchRequest? SearchYouTube(LunaPlayer.UI.YouTube.YouTubeSearchPrompt prompt) { using var dialog = new YouTube.SearchDialog(_context.Parent, _context.Dispatcher, prompt); return dialog.Show(); }
    public void ManageFavorites(IReadOnlyList<LunaPlayer.UI.YouTube.FavoriteListItem> favorites, Action<LunaPlayer.UI.YouTube.FavoriteRequest> request)
    {
        if (_favoriteEditor is not null) { _favoriteEditor.Activate(); return; }
        if (_favorites is null) _favorites = new YouTube.FavoritesDialog(favorites, request);
        else _favorites.Refresh(favorites);
        _favorites.Show();
    }
    public void RefreshFavorites(IReadOnlyList<LunaPlayer.UI.YouTube.FavoriteListItem> favorites, string? selectedId = null)
        => _favorites?.Refresh(favorites, selectedId);
    public void CloseFavorites() => _favorites?.Hide();
    public void EditFavorite(string caption, LunaPlayer.UI.YouTube.FavoriteDraft value, Action<LunaPlayer.UI.YouTube.FavoriteDraft?> completed)
    {
        if (_favoriteEditor is not null) { _favoriteEditor.Activate(); return; }
        var dialog = new YouTube.FavoriteEditDialog(_context.Parent, caption, value);
        _favoriteEditor = dialog;
        dialog.Show(result =>
        {
            _favoriteEditor = null;
            dialog.Dispose();
            completed(result);
        });
    }
    public LunaPlayer.UI.YouTube.ResultChoice? ShowYouTubeResults(LunaPlayer.UI.YouTube.YouTubeResultsPrompt prompt)
    {
        // A channel's tabbed browser has a different control structure, so it is still built per open.
        if (prompt.Tabs is { Count: > 0 })
        {
            using var channel = new YouTube.ResultsDialog(_context.Parent, prompt);
            return ShowResults(channel);
        }
        // The plain single-list window is built once and reused: reopening refreshes the list instead of
        // reconstructing the window. Parented to the main frame, which outlives every such open.
        if (_youTubeResults is null || !_youTubeResults.IsSingleList)
            _youTubeResults = new YouTube.ResultsDialog(_context.Frame, prompt);
        else
            _youTubeResults.Refresh(prompt);
        return ShowResults(_youTubeResults);
    }

    private YouTube.ResultChoice? ShowResults(YouTube.ResultsDialog dialog)
    {
        // The modal results window remains the completion owner even when Luna is in the
        // background. Restore the previous owner when a nested channel browser closes.
        var previous = _resultsOwner;
        _resultsOwner = dialog.Window;
        try { return dialog.Show(); }
        finally { _resultsOwner = previous; }
    }
    public bool OfferYouTubeComponents() { using var dialog = new YouTube.ComponentsDialog(_context.Parent); return dialog.Show(); }

    public LunaPlayer.YouTube.Download.OptionsResult? ShowDownloadOptions(LunaPlayer.YouTube.Download.Request defaults)
    { using var dialog = new YouTube.DownloadDialog(_context.Parent, defaults); return dialog.Show(); }

    public void ShowDownloadQueue(
        SharedDownloads.DownloadQueue queue,
        Func<IReadOnlyList<string>, string?> addLinks,
        Action start)
    { _queueDialog ??= new LunaPlayer.UI.Downloads.DownloadQueueDialog(_context.Parent, _context.Dispatcher, queue, addLinks, start); _queueDialog.Show(); }

    public void ShowDownloadProgress(
        SharedDownloads.DownloadQueue queue,
        Guid itemId)
    {
        var completionOwner = _resultsOwner ?? _context.Parent;
        var dialog = new LunaPlayer.UI.Downloads.DownloadProgressDialog(_context.Dispatcher, queue, itemId);
        dialog.Closed += (closed, outcome) =>
        {
            _downloadProgressDialogs.Remove(closed);
            _downloadOutcomes.Add(outcome, completionOwner);
        };
        _downloadProgressDialogs.Add(dialog);
        dialog.Show();
    }

    internal void CloseDownloadProgressWindows()
    {
        _downloadOutcomes.Clear();
        foreach (var dialog in _downloadProgressDialogs.ToArray())
            dialog.Dispose();
        _downloadProgressDialogs.Clear();
    }
    public void ShowVideoDescription(string title, string description) { using var dialog = new YouTube.DescriptionDialog(_context.Parent, title, description); dialog.Show(); }
    public void Dispose()
    {
        _downloadOutcomes.Dispose();
        _queueDialog?.Dispose();
        _youTubeResults?.Dispose();
        _youTubeResults = null;
        _favoriteEditor?.Dispose();
        _favoriteEditor = null;
        _favorites?.Dispose();
        _favorites = null;
        CloseDownloadProgressWindows();
    }
}
