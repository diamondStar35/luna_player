using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
using LunaPlayer.Iptv;
using LunaPlayer.UI.Equalizer;
using LunaPlayer.YouTube;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI;

/// <summary>Everything that opens a window or a prompt and returns what the user chose. Implemented by the
/// <see cref="DialogService"/>, which is the only part of the UI coupled to the concrete dialog classes.</summary>
internal interface IDialogService
{
    /// <summary>Opens the band editor on one preset, giving back what was saved or null if it was not.
    /// </summary>
    EqualizerEditResult? EditEqualizerPreset(EqualizerEditContext context);

    /// <summary>Opens the window listing every preset, giving back whether anything changed in it.
    /// </summary>
    bool ManageEqualizerPresets(Library library);

    /// <summary>Asks for a subtitle file to load, or null when the user cancels.</summary>
    string? ChooseSubtitleFile();

    /// <summary>Asks for a subtitle's web address to load, or null when the user cancels or leaves it blank.</summary>
    string? PromptSubtitleUrl();

    FileSelection? ChooseFile(string initialDirectory);

    /// <param name="message">What the window asks for. Empty leaves the system's own wording, which is
    /// what the callers that are choosing "a folder" and nothing more particular want.</param>
    string? ChooseFolder(string initialDirectory, string message = "");
    string? PromptText(string message, string caption, string value = "");
    bool Confirm(string message, string caption);
    /// <summary>Warns that closing will cancel unfinished queue jobs.</summary>
    bool ConfirmCloseWithDownloads();
    void ShowInfo(string message, string caption);
    /// <summary>Shows an information box owned by the main window, even if another modeless window is active.</summary>
    void ShowMainInfo(string message, string caption);
    void ShowWarning(string message, string caption);
    void ShowError(string message, string caption);
    /// <summary>Shows an error box owned by the main window, even if another modeless window is active.</summary>
    void ShowMainError(string message, string caption);
    /// <summary>Shows a non-modal operating-system notification associated with the main window.</summary>
    void ShowNotification(string title, string message);
    void ShowAbout();

    /// <summary>Offers a newer Luna Player release and shows its change list. True means update now.</summary>
    bool OfferAppUpdate(AppUpdatePrompt prompt);
    double? ChooseTime(double duration, double elapsed);
    double? ChooseSelectionTime(string title, string prompt, double duration, double elapsed);
    string? ChooseSelectionDestination(string source, bool video);

    /// <summary>Opens the sleep-timer window, prefilled from the last-used choices and the current armed state.
    /// Null means the window was cancelled.</summary>
    SleepTimerDialogResult? ChooseSleepTimer(SleepTimerRequest initial, bool armed, string? statusText);
    int? ChooseAudioDevice(IReadOnlyList<string> descriptions, int selectedIndex);

    /// <summary>Shows the file's audio tracks to switch between, each already worded for the user. Null means
    /// the window was cancelled.</summary>
    int? ChooseAudioTrack(IReadOnlyList<string> labels, int selectedIndex);
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

    /// <summary>Shows the text an uploader wrote under a video, in the purpose-built window that can open its
    /// links and save it. <paramref name="title"/> is the video's title, shown above the text.</summary>
    void ShowVideoDescription(string title, string description);

    /// <summary>Asks which half of a link naming a video and a playlist the user meant. Null when they
    /// backed out.</summary>
    YouTubeLinkKind? ChooseYouTubeLinkKind();

    /// <summary>Opens the search window and returns what the user asked to search for, or null when they
    /// closed it without searching.</summary>
    YouTubeSearchRequest? SearchYouTube(YouTubeSearchPrompt prompt);
    FavoriteRequest? ManageFavorites(IReadOnlyList<FavoriteListItem> favorites, string selectedId);
    FavoriteDraft? EditFavorite(string caption, FavoriteDraft value);

    /// <summary>Opens the IPTV source manager and returns what the user asked to do with which source, or
    /// null when they closed it. The manager reopens after add, edit and remove, like the favourites one.
    /// </summary>
    IptvSourceRequest? ManageIptvSources(IReadOnlyList<IptvSourceListItem> sources, string selectedId);

    /// <summary>Opens the add-or-edit form for an IPTV source, giving back what was typed or null if the
    /// user backed out. The form shows only the fields the chosen kind needs.</summary>
    IptvSourceDraft? EditIptvSource(string caption, IptvSourceDraft value);

    /// <summary>Opens the channel browser on a loaded source and returns the index into
    /// <see cref="ChannelBrowserPrompt.Channels"/> of the channel the user chose to play, or null when they
    /// closed it without playing anything.</summary>
    int? BrowseChannels(ChannelBrowserPrompt prompt);

    /// <summary>Opens the list of results and returns the row the user chose to play and whether they asked
    /// for its picture or its sound alone, or null when they closed it without playing anything. Everything
    /// else the window offers it does for itself, through <see cref="IYouTubeResultsFeed"/>, without closing.
    /// </summary>
    ResultChoice? ShowYouTubeResults(YouTubeResultsPrompt prompt);

    /// <summary>Offers to fetch the programs a YouTube download needs. True when the user accepted.</summary>
    bool OfferYouTubeComponents();

    /// <summary>Opens the download dialog prefilled from <paramref name="defaults"/>; null if cancelled.</summary>
    Downloads.OptionsResult? ShowDownloadOptions(Downloads.Request defaults);

    /// <summary>Opens the download queue until the user closes it. Downloads continue after it closes.</summary>
    void ShowDownloadQueue(
        Downloads.DownloadQueue queue,
        Func<IReadOnlyList<string>, string?> addLinks,
        Action start);

    /// <summary>Shows a modeless progress window for a download already owned by the queue.</summary>
    void ShowDownloadProgress(
        Downloads.DownloadQueue queue,
        Guid itemId,
        Action<Downloads.Outcome?> completed);

    /// <summary>Opens the window where recording is set up and run. It is modal, but closing it does not
    /// end a recording: the sources and the recorder outlive it.</summary>
    void ShowRecording(
        LunaPlayer.Recording.AudioCatalog catalog, LunaPlayer.Recording.RecordingSources sources, LunaPlayer.Recording.RecordingEngine engine);

    /// <summary>Opens the window where files are converted to another audio format and returns what the user
    /// chose to convert, or null if they closed it without starting.</summary>
    /// <param name="initialFiles">Files to put in the list before the window opens, for the "Convert with
    /// Luna" verb, or null to open it empty.</param>
    LunaPlayer.Media.ConversionRequest? ShowMediaConverter(IReadOnlyList<string>? initialFiles = null);

    /// <summary>Runs a conversion behind the converter's progress window, which does not return until the
    /// batch has finished or the user stopped it. Gives back what the batch produced, or null when it was
    /// stopped.</summary>
    /// <param name="work">The batch. It is handed something to report progress to and a token set when the
    /// user stops it, and returns what it converted.</param>
    LunaPlayer.Media.ConversionOutcome? RunConversion(
        Func<Action<LunaPlayer.Media.ConversionProgress>, CancellationToken, LunaPlayer.Media.ConversionOutcome> work);

    /// <summary>Shows a read-only report of a batch that mostly worked: a summary line, then a scrollable list
    /// of the files that failed with their reasons.</summary>
    void ShowConversionReport(string title, string message, string details);
    Settings? EditPreferences(Settings settings, PrefsOps operations, Action<string> speakHelp);
}
