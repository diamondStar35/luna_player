using LunaPlayer.Configuration;
using LunaPlayer.Media;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed partial class MainFrame
{
    public FileSelection? ChooseFile(string initialDirectory)
    {
        using var dialog = new FileDialog(_frame, message: "", directory: initialDirectory, wildcard: MediaLibrary.DialogWildcard, style: FileDialogStyle.DefaultOpen);
        return dialog.ShowModal() == StandardId.Ok ? new(dialog.Path, dialog.Directory) : null;
    }
    public string? ChooseFolder(string initialDirectory)
    {
        using var dialog = new DirDialog(_frame, message: "", defaultPath: initialDirectory, style: DirDialogStyle.DirMustExist);
        return dialog.ShowModal() == StandardId.Ok ? dialog.Path : null;
    }
    public string? PromptText(string message, string caption, string value = "")
    { using var dialog = new TextEntryDialog(_frame, message, caption, value); return dialog.ShowModal() == StandardId.Ok ? dialog.Value : null; }
    public bool Confirm(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, _frame) == MessageBoxStyle.Yes;
    public void ShowInfo(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, _frame);
    public void ShowWarning(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _frame);
    public void ShowError(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconError, _frame);
    public double? ChooseTime(double duration, double elapsed) { using var dialog = new GoToTimeDialog(_frame, duration, elapsed); return dialog.Show(); }
    public int? ChooseAudioDevice(IReadOnlyList<string> descriptions, int selectedIndex) { using var dialog = new AudioDeviceDialog(_frame, descriptions, selectedIndex); return dialog.Show(); }
    public BookmarkManagementRequest? ManageBookmarks(IReadOnlyList<BookmarkListItem> bookmarks) { using var dialog = new BookmarkManagerDialog(_frame, bookmarks); return dialog.Show(); }
    public OpenedFilesRequest? ChooseOpenedFile(IReadOnlyList<string> names, int selectedIndex) { using var dialog = new OpenedFilesDialog(_frame, names, selectedIndex); return dialog.Show(); }
    public IProgressView BeginProgress(string title, string message, int maximum) => new ProgressView(_frame, title, message, maximum);
    public void ShowTextInfo(string title, string text) { using var dialog = new TextInfoDialog(_frame, title, text); dialog.Show(); }
    public PlayerSettings? EditPreferences(PlayerSettings settings, PrefsOps operations, Action<string> speakHelp)
    { var editable = settings.Copy(); using var dialog = new PreferencesDialog(_frame, editable, operations, speakHelp); return dialog.Show() ? editable : null; }
}
