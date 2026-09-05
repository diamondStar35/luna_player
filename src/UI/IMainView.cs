using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Favorites;
using LunaPlayer.YouTube;

namespace LunaPlayer.UI;

internal readonly record struct FileSelection(string Path, string Directory);
internal readonly record struct BookmarkListItem(string Id, string Name, string Position);
internal enum BookmarkManagementAction { Jump, Rename, Delete }
internal readonly record struct BookmarkManagementRequest(BookmarkManagementAction Action, string Id);
internal enum OpenedFilesAction { Jump, Information }
internal readonly record struct OpenedFilesRequest(OpenedFilesAction Action, int SelectedIndex);
internal readonly record struct UiOperation(bool Success, string Error = "");

/// <summary>Which half of a link naming a video and a playlist at once the user meant.</summary>
internal enum YouTubeLinkKind { Video, Playlist }

internal readonly record struct FavoriteListItem(string Id, string Name, string Type, string Link);
internal enum FavoriteAction { Open, Add, Edit, Remove }
internal readonly record struct FavoriteRequest(FavoriteAction Action, string Id);

/// <summary>What the user typed into the favourite editor, before anything has checked it.</summary>
internal readonly record struct FavoriteDraft(string Name, FavoriteKind Kind, string Link);

/// <summary>How the results window talks back to whoever opened it.</summary>
///
/// <remarks>
/// Everything the window can do except play a video is done through here, with the window still open. Only
/// playing one closes it, because only playing one replaces what the window is for. Copying an address,
/// opening a browser, saving a video: all of those leave the user where they were, on the row they were on,
/// which is the whole point of a list.
///
/// A window cannot speak and must not wait: the list is the only thing on screen while a page is fetched,
/// so a call that blocked would take the screen reader and the Escape key with it for the length of a web
/// request. So paging reports rather than asks, and the answer comes back through a callback on the UI
/// thread.
/// </remarks>
internal interface IYouTubeResultsFeed
{
    /// <summary>The user has moved to a row.</summary>
    void Selected(int index);

    /// <summary>Puts the address of a row on the clipboard, saying so.</summary>
    void CopyLink(int index);

    /// <summary>Shows a row in the web browser.</summary>
    void OpenInBrowser(int index);

    /// <summary>Shows the channel that published a row, refusing aloud when it named none.</summary>
    void OpenChannel(int index);

    /// <summary>Saves a row to a folder on this computer, asking which folder first.</summary>
    void Download(int index);

    /// <summary>Asks for the next page. Returns at once; <paramref name="appended"/> runs later on the UI
    /// thread, and is given an empty list when there is nothing more to come.</summary>
    void RequestMore(Action<IReadOnlyList<YouTubeResult>> appended);

    /// <summary>The window has gone, so a page still in flight should be dropped rather than delivered.
    /// </summary>
    void Close();
}

internal sealed record YouTubeResultsPrompt(
    string Title,
    string Label,
    IReadOnlyList<YouTubeResult> Results,
    int SelectedIndex,
    IYouTubeResultsFeed Feed);
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
    /// <summary>Fetches the programs a YouTube download needs. Nothing else on the settings page uses
    /// them, so this is the only way in.</summary>
    /// <summary>Fetches the programs yt-dlp needs, from the release line the page currently shows.
    /// Reports for itself, behind its own window, so there is nothing for the page to say afterwards.
    /// </summary>
    /// <remarks>
    /// The channel is passed in rather than read from the settings, because the settings still hold the
    /// old one: the page edits a copy and applies it when the window is accepted. Reading it there would
    /// fetch from whichever line was chosen last time, not the one on screen.
    /// </remarks>
    Action<YtDlpChannel> DownloadYouTubeComponents,
    /// <summary>Makes sure the programs the yt-dlp resolver needs are present, offering to fetch them if
    /// not. False means the setting that asked for them should be put back.</summary>
    /// <summary>Makes sure those programs are there, offering to fetch them from the release line the
    /// page currently shows. False means the page should put its tick box back - the user declined - rather
    /// than that the programs are absent, which is also true while they are on their way.</summary>
    Func<YtDlpChannel, bool> EnsureYouTubeComponents,
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

    /// <summary>Moves a bar that has no figure behind it. Called on every tick of a job that cannot say how
    /// far through it is, so the window still shows that something is happening.</summary>
    void Pulse();

    /// <summary>Whether the user has pressed Cancel. A plain flag rather than something reported back out
    /// of Update, so a job with nothing new to report can still be cancelled.</summary>
    bool Cancelled { get; }
}

internal interface IMainView : IDisposable
{
    event Action<ActionId>? ActionRequested;
    event Action? CloseRequested;

    /// <summary>Asked when Escape is pressed on the main window with no modifier. Returning true means it
    /// was dealt with, and the key goes no further.</summary>
    event Func<bool>? EscapePressed;

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
    void SetVideoOptionsEnabled(bool enabled);

    /// <summary>Puts the recording menu into the state the recorder is in.</summary>
    void SetRecordingState(LunaPlayer.Recording.RecordingState state);
    FileSelection? ChooseFile(string initialDirectory);
    /// <param name="message">What the window asks for. Empty leaves the system's own wording, which is
    /// what the callers that are choosing "a folder" and nothing more particular want.</param>
    string? ChooseFolder(string initialDirectory, string message = "");
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
    /// <param name="detailed">Whether the job reports several lines at a time rather than one. A
    /// detailed window shows them in a read-only text area; the rest get a label.</param>
    IProgressView BeginProgress(string title, string message, bool proportional, bool detailed);
    void ShowTextInfo(string title, string text);
    /// <summary>Asks which half of a link naming a video and a playlist the user meant. Null when they
    /// backed out.</summary>
    YouTubeLinkKind? ChooseYouTubeLinkKind();
    FavoriteRequest? ManageFavorites(IReadOnlyList<FavoriteListItem> favorites, string selectedId);
    FavoriteDraft? EditFavorite(string caption, FavoriteDraft value);
    /// <summary>Opens the list of results and returns the row the user chose to play, or null when they
    /// closed it without playing anything. Everything else the window offers it does for itself, through
    /// <see cref="IYouTubeResultsFeed"/>, without closing.</summary>
    int? ShowYouTubeResults(YouTubeResultsPrompt prompt);
    /// <summary>Offers to fetch the programs a YouTube download needs. True when the user accepted.</summary>
    /// <param name="doNotAskAgain">Whether they asked not to be offered again, whatever they answered.</param>
    bool OfferYouTubeComponents(out bool doNotAskAgain);
    /// <summary>Opens the window where recording is set up and run. It is modal, but closing it does not
    /// end a recording: the sources and the recorder outlive it.</summary>
    void ShowRecording(
        LunaPlayer.Recording.AudioCatalog catalog, LunaPlayer.Recording.RecordingSources sources, LunaPlayer.Recording.RecordingEngine engine);
    PlayerSettings? EditPreferences(PlayerSettings settings, PrefsOps operations, Action<string> speakHelp);
    void ApplyShortcuts(ShortcutManager shortcuts);
    /// <summary>Starts watching for the system-wide shortcuts in <paramref name="shortcuts"/>, replacing any
    /// set already being watched. Returns false when the system would not let the player watch the keyboard at
    /// all, which is worth telling the user about: no global shortcut will work.</summary>
    bool ApplyGlobalShortcuts(ShortcutManager shortcuts);
}
