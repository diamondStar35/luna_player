using LunaPlayer.Actions;
using LunaPlayer.Configuration;

namespace LunaPlayer.UI;

internal readonly record struct FileSelection(string Path, string Directory);
internal readonly record struct BookmarkListItem(string Id, string Name, string Position);
internal enum BookmarkManagementAction { Jump, Rename, Delete }
internal readonly record struct BookmarkManagementRequest(BookmarkManagementAction Action, string Id);
internal enum OpenedFilesAction { Jump, Information }
internal readonly record struct OpenedFilesRequest(OpenedFilesAction Action, int SelectedIndex);
internal readonly record struct UiOperation(bool Success, string Error = "");
internal sealed record PrefsOps(
    string SettingsPath,
    string BookmarksPath,
    string SettingsFolder,
    Func<string, bool> ExportSettings,
    Func<string, PlayerSettings?> ImportSettings,
    Func<PlayerSettings?> ResetSettings,
    Func<string, bool> ExportBookmarks,
    Func<string, bool> ImportBookmarks,
    Func<bool> OpenSettingsFolder,
    Func<UiOperation> RegisterFiles,
    Func<UiOperation> UnregisterFiles,
    Action<PlayerSettings> ApplyImmediate);

internal interface IProgressView : IDisposable
{
    bool Update(int value, string message);
    bool Pulse(string message);
}

internal interface IMainView : IDisposable
{
    event Action<ActionId>? ActionRequested;
    event Action? CloseRequested;

    nint NativeHandle { get; }
    void Show();
    void Close();
    void RestoreAndRaise();
    void SetPlaying(bool isPlaying);
    void SetMediaLoaded(bool loaded);
    void SetShuffleChecked(bool isChecked);
    void SetRepeatFileChecked(bool isChecked);
    void SetSilenceRemovalChecked(bool isChecked);
    void SetEditState(bool hasLocalFile, bool hasMedia);
    void SetBookmarkState(bool enabled);
    void SetMarkState(bool currentMarked, bool allMarked);
    void SetMarkedActionsEnabled(bool enabled);
    FileSelection? ChooseFile(string initialDirectory);
    string? ChooseFolder(string initialDirectory);
    string? PromptText(string message, string caption, string value = "");
    bool Confirm(string message, string caption);
    void ShowInfo(string message, string caption);
    void ShowWarning(string message, string caption);
    void ShowError(string message, string caption);
    double? ChooseTime(double duration, double elapsed);
    int? ChooseAudioDevice(IReadOnlyList<string> descriptions, int selectedIndex);
    BookmarkManagementRequest? ManageBookmarks(IReadOnlyList<BookmarkListItem> bookmarks);
    OpenedFilesRequest? ChooseOpenedFile(IReadOnlyList<string> names, int selectedIndex);
    IProgressView BeginProgress(string title, string message, int maximum);
    void ShowTextInfo(string title, string text);
    PlayerSettings? EditPreferences(PlayerSettings settings, PrefsOps operations, Action<string> speakHelp);
    void ApplyShortcuts(ShortcutManager shortcuts);
    /// <summary>Starts watching for the system-wide shortcuts in <paramref name="shortcuts"/>, replacing any
    /// set already being watched. Returns false when the system would not let the player watch the keyboard at
    /// all, which is worth telling the user about: no global shortcut will work.</summary>
    bool ApplyGlobalShortcuts(ShortcutManager shortcuts);
}
