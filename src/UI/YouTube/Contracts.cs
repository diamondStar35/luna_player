using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Equalizer;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI.Equalizer;
using LunaPlayer.YouTube;

namespace LunaPlayer.UI.YouTube;

/// <summary>Which half of a link naming a video and a playlist at once the user meant.</summary>
internal enum YouTubeLinkKind { Video, Playlist }

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
internal enum FavoriteAction { Open, Add, Edit, Remove }

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


/// <summary>What the user asked to search YouTube for, and which kind of result they filtered to.</summary>
/// <param name="Query">The words to search for, already trimmed.</param>
/// <param name="Filter">The row chosen in the search dialog's filter, 0-5: no filter, live, upload date,
/// view count, playlist, channels.</param>
internal readonly record struct YouTubeSearchRequest(string Query, int Filter);

internal readonly record struct FavoriteListItem(string Id, string Name, string Type, string Link);
internal readonly record struct FavoriteRequest(LunaPlayer.UI.YouTube.FavoriteAction Action, string Id);

/// <summary>What the user typed into the favourite editor, before anything has checked it.</summary>
internal readonly record struct FavoriteDraft(string Name, FavoriteKind Kind, string Link);

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
