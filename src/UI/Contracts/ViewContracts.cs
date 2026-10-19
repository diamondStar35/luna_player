using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Equalizer;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI.Equalizer;
using LunaPlayer.YouTube;

namespace LunaPlayer.UI;
internal readonly record struct FileSelection(string Path, string Directory);
internal readonly record struct BookmarkListItem(string Id, string Name, string Position);
internal enum BookmarkManagementAction { Jump, Rename, Delete }
internal readonly record struct BookmarkManagementRequest(BookmarkManagementAction Action, string Id);
internal enum OpenedFilesAction { Jump, Information }
internal readonly record struct OpenedFilesRequest(OpenedFilesAction Action, int SelectedIndex);
internal readonly record struct UiOperation(bool Success, string Error = "");
internal readonly record struct AppUpdatePrompt(string CurrentVersion, string AvailableVersion, string Changes);

/// <summary>One subtitle track as the Subtitles menu shows it: the mpv track id and the label already worded
/// for the user. The "Off" entry is not one of these - the menu always carries it itself.</summary>
internal readonly record struct SubtitleMenuEntry(int Id, string Label);

/// <summary>One entry in a Recents submenu: the path to reopen and the numbered label already worded for the
/// user (such as "1. song.mp3 — C:\Music").</summary>
internal readonly record struct RecentMenuEntry(string Path, string Label);

/// <summary>The three kinds of recent paths as they are displayed in the File menu.</summary>
internal readonly record struct RecentMenuState(
    IReadOnlyList<RecentMenuEntry> Files,
    IReadOnlyList<RecentMenuEntry> Folders,
    IReadOnlyList<RecentMenuEntry> Playlists);

/// <summary>Whether the user picked a recent item to open, or its kind's Clear command.</summary>
internal enum RecentAction { Open, Clear }

/// <summary>What the user chose in a Recents submenu: open <see cref="Path"/>, or clear the whole
/// <see cref="Kind"/> (then <see cref="Path"/> is empty).</summary>
internal readonly record struct RecentCommand(RecentAction Action, RecentKind Kind, string Path);

/// <summary>Which half of a link naming a video and a playlist at once the user meant.</summary>
internal enum YouTubeLinkKind { Video, Playlist }

/// <summary>What the user asked to search YouTube for, and which kind of result they filtered to.</summary>
/// <param name="Query">The words to search for, already trimmed.</param>
/// <param name="Filter">The row chosen in the search dialog's filter, 0-5: no filter, live, upload date,
/// view count, playlist, channels.</param>
internal readonly record struct YouTubeSearchRequest(string Query, int Filter);

/// <summary>Everything the search window needs beyond the widgets it builds for itself.</summary>
///
/// <remarks>
/// The window owns the debounce and the thread the suggestions are fetched on - that is presentation, and
/// keeps the network work off the UI thread without the action handler having to know how the box is laid
/// out - but it is handed the fetch itself and the words to announce rather than reaching for the client
/// or the screen reader, neither of which the UI layer knows about.
/// </remarks>
/// <param name="InitialQuery">What to put in the box before it opens; usually empty.</param>
/// <param name="SuggestionsEnabled">Whether the box offers live suggestions as the user types.</param>
/// <param name="FetchSuggestions">Fetches the words YouTube offers to finish a query. Called on a
/// background thread, so it may block; an empty list is a fine answer.</param>
/// <param name="AnnounceSuggestions">Speaks that suggestions have appeared, on the UI thread.</param>
internal sealed record YouTubeSearchPrompt(
    string InitialQuery,
    bool SuggestionsEnabled,
    Func<string, CancellationToken, IReadOnlyList<string>> FetchSuggestions,
    Action AnnounceSuggestions);

internal readonly record struct FavoriteListItem(string Id, string Name, string Type, string Link);
internal enum FavoriteAction { Open, Add, Edit, Remove }
internal readonly record struct FavoriteRequest(FavoriteAction Action, string Id);

/// <summary>What the user typed into the favourite editor, before anything has checked it.</summary>
internal readonly record struct FavoriteDraft(string Name, FavoriteKind Kind, string Link);

/// <summary>One row in the IPTV source manager: a saved source shown by name and kind.</summary>
/// <param name="Id">The store's id, carried back on the request so the handler knows which source was
/// chosen without the window knowing anything about the store.</param>
/// <param name="Name">What the source is called.</param>
/// <param name="Type">The kind of source, worded for the user ("Xtream Codes", "M3U web address"...).</param>
/// <param name="Detail">A second column giving the server or file the source points at.</param>
internal readonly record struct IptvSourceListItem(string Id, string Name, string Type, string Detail);
internal enum IptvSourceAction { Open, Add, Edit, Remove }
internal readonly record struct IptvSourceRequest(IptvSourceAction Action, string Id);

/// <summary>Everything the channel browser needs to open: the source's loaded channels and categories, and
/// a way to speak its own help. The browser filters the channels itself and answers with the index of the
/// one the user chose to play, or null when they closed it without playing.</summary>
/// <param name="Title">The window title, naming the source being browsed.</param>
/// <param name="Categories">The groups the channels fall into, for the category filter.</param>
/// <param name="Channels">Every channel the source holds, in load order.</param>
/// <param name="SpeakHelp">Speaks a line of help, for the F1 key, on the UI thread.</param>
/// <param name="Guide">The programme guide, when one was loaded, for the "Now" column and the on-demand
/// now-and-next announcement. Null when the source carried no guide.</param>
internal readonly record struct ChannelBrowserPrompt(
    string Title,
    IReadOnlyList<IptvCategory> Categories,
    IReadOnlyList<IptvChannel> Channels,
    Action<string> SpeakHelp,
    EpgGuide? Guide = null);

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

    /// <summary>Opens the channel that published a row in the web browser, refusing aloud when it named
    /// none.</summary>
    void OpenChannelInBrowser(int index);

    /// <summary>Saves a row straight to disk with the saved download settings.</summary>
    void DirectDownload(int index);

    /// <summary>Opens the download window on a row, to choose how it is saved before downloading.</summary>
    void Download(int index);

    /// <summary>Adds the row to the download queue with the saved default settings.</summary>
    void AddToDownloadQueue(int index);

    /// <summary>Whether the row is already in the favourites, so the window can offer to remove rather than
    /// add it.</summary>
    bool IsFavorite(int index);

    /// <summary>Adds the row to the favourites, or removes it when it is already there, saying which way it
    /// went.</summary>
    void ToggleFavorite(int index);

    /// <summary>Switches a channel browser to another tab. Returns at once; <paramref name="replaced"/>
    /// runs later on the UI thread with the new tab's rows, or with null when the switch failed or was
    /// refused so the window can restore its tab selector.</summary>
    void SwitchTab(int tabIndex, Action<IReadOnlyList<YouTubeResult>?> replaced);

    /// <summary>Asks for the next page. Returns at once; <paramref name="appended"/> runs later on the UI
    /// thread, and is given an empty list when there is nothing more to come.</summary>
    void RequestMore(Action<IReadOnlyList<YouTubeResult>> appended);

    /// <summary>The window has gone, so a page still in flight should be dropped rather than delivered.
    /// </summary>
    void Close();
}

/// <summary>What the user chose from the results list: which row, and whether to play its picture or its
/// sound alone - or, when <see cref="Channel"/> is set, to browse into that row's channel instead of
/// playing it.</summary>
/// <remarks>
/// The mode is the whole point of returning a record rather than a bare index: Enter asks for the video and
/// Ctrl+Enter for the audio, and the session needs to know which before it resolves the row.
/// </remarks>
internal readonly record struct ResultChoice(int Index, PlayMode Mode)
{
    /// <summary>Set when the user asked to go to the row's channel rather than play the row. The window
    /// closes the same way, and the session opens the channel instead of resolving a stream.</summary>
    internal bool Channel { get; init; }
}

internal sealed record YouTubeResultsPrompt(
    string Title,
    string Label,
    IReadOnlyList<YouTubeResult> Results,
    int SelectedIndex,
    IYouTubeResultsFeed Feed,
    /// <summary>The channel browser's tab names, or null for a search or playlist, which has no tabs.</summary>
    IReadOnlyList<string>? Tabs = null,
    /// <summary>Which tab is showing when the window opens.</summary>
    int SelectedTab = 0);
internal sealed record PrefsOps(
    string SettingsPath,
    string BookmarksPath,
    string SettingsFolder,
    Func<string, bool> ExportSettings,
    Func<string, Settings?> ImportSettings,
    Func<Settings?> ResetSettings,
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
    Action<Settings> ApplyImmediate);

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
