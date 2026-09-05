using LunaPlayer.Configuration;
using LunaPlayer.Media;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed partial class MainFrame
{
    /// <summary>The window a new dialog should belong to.</summary>
    ///
    /// <remarks>
    /// The active top-level window, not always the main one. A dialog can be opened from inside another -
    /// the Preferences window's button that fetches the YouTube components is the case in point - and
    /// giving that the main window as its parent makes it the child of a window the Preferences window has
    /// disabled. When it then closes, activation is handed back to a disabled window: the Preferences
    /// window drops behind, nothing takes the focus, and the player looks hung. Which is exactly what it
    /// did.
    ///
    /// Falls back to the main window when nothing is active, which is the case at startup and whenever the
    /// player is not the foreground application.
    /// </remarks>
    private Window DialogParent => Wx.GetActiveWindow() ?? _frame;

    public FileSelection? ChooseFile(string initialDirectory)
    {
        using var dialog = new FileDialog(DialogParent, message: "", directory: initialDirectory, wildcard: MediaLibrary.DialogWildcard, style: FileDialogStyle.DefaultOpen);
        return dialog.ShowModal() == StandardId.Ok ? new(dialog.Path, dialog.Directory) : null;
    }
    public string? ChooseFolder(string initialDirectory, string message = "")
    {
        using var dialog = new DirDialog(DialogParent, message: message, defaultPath: initialDirectory, style: DirDialogStyle.DirMustExist);
        return dialog.ShowModal() == StandardId.Ok ? dialog.Path : null;
    }
    public string? PromptText(string message, string caption, string value = "")
    { using var dialog = new TextEntryDialog(DialogParent, message, caption, value); return dialog.ShowModal() == StandardId.Ok ? dialog.Value : null; }
    public bool Confirm(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, DialogParent) == MessageBoxStyle.Yes;
    public void ShowInfo(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, DialogParent);
    public void ShowWarning(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, DialogParent);
    public void ShowError(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconError, DialogParent);
    public double? ChooseTime(double duration, double elapsed) { using var dialog = new GoToTimeDialog(DialogParent, duration, elapsed); return dialog.Show(); }
    public int? ChooseAudioDevice(IReadOnlyList<string> descriptions, int selectedIndex) { using var dialog = new AudioDeviceDialog(DialogParent, descriptions, selectedIndex); return dialog.Show(); }
    public BookmarkManagementRequest? ManageBookmarks(IReadOnlyList<BookmarkListItem> bookmarks) { using var dialog = new BookmarkManagerDialog(DialogParent, bookmarks); return dialog.Show(); }
    public OpenedFilesRequest? ChooseOpenedFile(int count, Func<int, string> nameAt, int selectedIndex) { using var dialog = new OpenedFilesDialog(DialogParent, count, nameAt, selectedIndex); return dialog.Show(); }
    public IProgressView BeginProgress(string title, string message, bool proportional, bool detailed) => new ProgressView(DialogParent, title, message, proportional, detailed);
    public YouTubeLinkKind? ChooseYouTubeLinkKind() { using var dialog = new YouTube.LinkKindDialog(DialogParent); return dialog.Show(); }
    public FavoriteRequest? ManageFavorites(IReadOnlyList<FavoriteListItem> favorites, string selectedId) { using var dialog = new YouTube.FavoritesDialog(DialogParent, favorites, selectedId); return dialog.Show(); }
    public FavoriteDraft? EditFavorite(string caption, FavoriteDraft value) { using var dialog = new YouTube.FavoriteEditDialog(DialogParent, caption, value); return dialog.Show(); }
    public int? ShowYouTubeResults(YouTubeResultsPrompt prompt) { using var dialog = new YouTube.ResultsDialog(DialogParent, prompt); return dialog.Show(); }
    public bool OfferYouTubeComponents(out bool doNotAskAgain) { using var dialog = new YouTube.ComponentsDialog(DialogParent); return dialog.Show(out doNotAskAgain); }
    public void ShowTextInfo(string title, string text) { using var dialog = new TextInfoDialog(DialogParent, title, text); dialog.Show(); }
    public void ShowRecording(
        LunaPlayer.Recording.AudioCatalog catalog, LunaPlayer.Recording.RecordingSources sources, LunaPlayer.Recording.RecordingEngine engine)
    { using var dialog = new Recording.RecordingDialog(DialogParent, _dispatcher, catalog, sources, engine); dialog.Show(); }
    public PlayerSettings? EditPreferences(PlayerSettings settings, PrefsOps operations, Action<string> speakHelp)
    { var editable = settings.Copy(); using var dialog = new PreferencesDialog(DialogParent, editable, operations, speakHelp, _dispatcher, _catalog, _globalShortcuts); return dialog.Show() ? editable : null; }
}
