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
    /// <summary>Why the last backup or restore failed, for the message that reports it.</summary>
    Func<string> LastBackupError,
    Func<bool> OpenSettingsFolder,
    Func<UiOperation> RegisterFiles,
    Func<UiOperation> UnregisterFiles,
    Action<PlayerSettings> ApplyImmediate);

internal interface IProgressView : IDisposable
{
    /// <summary>Shows how far the job has got, as a percentage from nought to a hundred.</summary>
    /// <remarks>
    /// A percentage rather than a count, because a job can change what it is counting part way through - the
    /// folder scan counts files it has sized up and then files it has looked at, two different totals - and a
    /// bar told raw counts has no way to know that. It is also what a screen reader reads out.
    /// </remarks>
    void Update(int percent, string message);

    /// <summary>Whether the user has pressed Cancel. A plain flag rather than something reported back out
    /// of Update, so a job with nothing new to report can still be cancelled.</summary>
    bool Cancelled { get; }
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
    /// <summary>Shows the list of loaded files. The names are asked for a row at a time rather than handed
    /// over up front, so a playlist of any size opens at once.</summary>
    /// <param name="nameAt">The name to show for one row, called only as that row is drawn.</param>
    OpenedFilesRequest? ChooseOpenedFile(int count, Func<int, string> nameAt, int selectedIndex);
    /// <param name="proportional">Whether the job can say how far through it is. A job that cannot gets a
    /// window with no bar on it, because a bar that never moves is worse than none.</param>
    IProgressView BeginProgress(string title, string message, bool proportional);
    void ShowTextInfo(string title, string text);
    PlayerSettings? EditPreferences(PlayerSettings settings, PrefsOps operations, Action<string> speakHelp);
    void ApplyShortcuts(ShortcutManager shortcuts);
    /// <summary>Starts watching for the system-wide shortcuts in <paramref name="shortcuts"/>, replacing any
    /// set already being watched. Returns false when the system would not let the player watch the keyboard at
    /// all, which is worth telling the user about: no global shortcut will work.</summary>
    bool ApplyGlobalShortcuts(ShortcutManager shortcuts);
}
