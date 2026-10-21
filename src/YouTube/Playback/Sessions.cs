using LunaPlayer.Accessibility;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.YouTube.Playback;

/// <summary>What came of asking for another video in the current results list.</summary>
///
/// <remarks>
/// Four states rather than three. A plain true/false/None cannot tell "the requested video is on its way"
/// from "there is nothing in that direction"; answering both with false could stop playback a moment
/// before a still-resolving video arrives.
/// </remarks>
internal enum NavigationOutcome
{
    /// <summary>What is playing is not part of a session, so the ordinary playlist rules apply.</summary>
    NotOurs,
    /// <summary>Playback has moved on already.</summary>
    Advanced,
    /// <summary>The requested video is being resolved and will start itself. Do not stop.</summary>
    Pending,
    /// <summary>There is no video in the requested direction.</summary>
    Exhausted,
}

/// <summary>Runs a list of YouTube videos: shows it, plays from it, moves through it and takes it away
/// again.</summary>
///
/// <remarks>
/// This is the part of the YouTube flow that could not live on
/// <see cref="Backend"/>: opening a video needs the player and the results window needs the session, and
/// <see cref="Backend"/> knows about neither. Everything here runs on the UI thread except the work handed
/// to <see cref="ResolveCache"/>, which is the only thing that touches the network.
/// </remarks>
internal sealed partial class Sessions : IDisposable
{
    /// <summary>How many videos to resolve ahead when a list of results is first shown.</summary>
    private const int OpeningPrefetch = 5;

    /// <summary>How many to resolve ahead when the user moves to a row.</summary>
    private const int BrowsingPrefetch = 2;

    private readonly IMainView _view;
    private readonly Player _player;
    private readonly Settings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly ResolveCache _cache;
    private readonly Action<string, string> _directDownload;
    private readonly Action<string, string> _download;
    private readonly Action<string, string> _queueDownload;
    private readonly Action<string> _copy;
    private readonly Action<string> _browse;
    private readonly Browsing _browsing;
    private Session? _session;
    private PendingMove? _pending;

    /// <param name="directDownload">Downloads a row straight to disk with the saved settings.</param>
    /// <param name="favorites">The shared store, so a row can be saved to the favourites straight from the
    /// results list. The same store the favourites manager writes to, so an addition shows up there.</param>
    /// <param name="components">The programs the channel browser needs, offered when a channel is opened and
    /// they are not yet installed.</param>
    internal Sessions(
        IMainView view,
        Player player,
        Settings settings,
        ISpeechOutput speech,
        IApplicationDispatcher dispatcher,
        Metadata.Client client,
        Backend backend,
        ResolveCache cache,
        Action<string, string> directDownload,
        Action<string, string> download,
        Action<string, string> queueDownload,
        Action<string> copy,
        Action<string> browse,
        FavoriteStore favorites,
        Components.Service components)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _speech = speech;
        _dispatcher = dispatcher;
        _cache = cache;
        _directDownload = directDownload;
        _download = download;
        _queueDownload = queueDownload;
        _copy = copy;
        _browse = browse;
        _browsing = new Browsing(
            view, settings, speech, dispatcher, client, backend, cache, favorites, components, Show);
    }

    /// <summary>The address of the video playing now, or null when what is playing is not one.</summary>
    internal string? CurrentWatchUrl => _player.CurrentSource;

    /// <summary>Whether the current results row has a predecessor.</summary>
    internal bool CanMovePrevious => CurrentSessionIndex() > 0;

    /// <summary>Whether the current results row has a successor.</summary>
    internal bool CanMoveNext
    {
        get
        {
            var index = CurrentSessionIndex();
            return index >= 0 && _session is Session session && index < session.Items.Count - 1;
        }
    }

    private string _activeChannelUrl = string.Empty;

    /// <summary>The channel address of the YouTube video playing now, or empty when it is not known - a
    /// video opened from a bare link carries no channel. Used by the video commands that open the channel.</summary>
    internal string ActiveChannelUrl => _activeChannelUrl;

    internal void Search(string query, int filter) => _browsing.Search(query, filter);

    internal void OpenPlaylist(string link) => _browsing.OpenPlaylist(link);

    internal void GoToChannel(string channelUrl, string title) => _browsing.OpenChannel(channelUrl, title);

    /// <summary>Plays one video, named by a link rather than chosen from a list.</summary>
    /// <remarks>There is no session: nothing follows a single video, so there is no next and Escape has
    /// nothing to go back to. Any session already open is closed.</remarks>
    internal void PlayLink(string link)
    {
        Clear();
        var watchUrl = Utils.Canonical(link);
        if (watchUrl is null)
        {
            ShowError(
                // Translators: Shown when an address looked like a YouTube link but names no video.
                Tr("This link does not include a YouTube video."));
            return;
        }
        // A link is played as its picture, at the video quality: it is what Enter would do to a row, and a
        // lone link carries no key to say otherwise.
        Resolve(watchUrl, YouTubeResult.None, false, (int)_settings.YouTube.VideoQuality,
            CancellationToken.None,
            outcome =>
            {
                if (outcome.Value is Resolved ready)
                    OpenAlone(ready);
                else if (outcome.Failure is not ResolveFailure.Cancelled)
                    ShowError(Utils.Describe(outcome.Failure, outcome.Detail, Utils.StreamFailed));
            });
    }

    internal NavigationOutcome TryNext() => TryMove(1);

    internal NavigationOutcome TryPrevious() => TryMove(-1);

    /// <summary>Moves through the session's result list, resolving the requested video when necessary.
    /// The result list remains authoritative even when only the current stream is loaded in mpv.</summary>
    private NavigationOutcome TryMove(int direction)
    {
        if (_session is not Session session || CurrentWatchUrl is not string playing)
            return NavigationOutcome.NotOurs;
        var current = session.IndexOf(playing);
        if (current < 0)
            return NavigationOutcome.NotOurs;
        session.Selected = current;
        var target = current + Math.Sign(direction);
        if (target < 0 || target >= session.Items.Count)
        {
            _pending = null;
            return NavigationOutcome.Exhausted;
        }
        var item = session.Items[target];
        if (Ready(session, item) is Resolved ready)
        {
            _pending = null;
            return MoveTo(session, current, target, ready)
                ? NavigationOutcome.Advanced
                : NavigationOutcome.Exhausted;
        }
        LoadMove(session, current, target, item, direction);
        return NavigationOutcome.Pending;
    }

    private int CurrentSessionIndex()
        => _session is Session session && CurrentWatchUrl is string playing
            ? session.IndexOf(playing)
            : -1;

    /// <summary>Answers Escape: stops a session video, takes the session's entries out of the playlist and
    /// puts the results list back.</summary>
    /// <returns>False when Escape means nothing here, so the key falls through to whatever else wants it.
    /// </returns>
    internal bool HandleEscape()
    {
        if (_session is not Session session || CurrentWatchUrl is not string playing)
            return false;
        if (session.IndexOf(playing) < 0)
            return false;
        _pending = null;
        // The whole stage goes, list and all, and the playlist the user opened comes back exactly as it
        // was - paused where it was paused. There is nothing to unpick, because nothing was mixed in.
        _player.LeaveSession();
        Show(session);
        return true;
    }

    /// <summary>Forgets the current session and abandons everything it had running.</summary>
    internal void Clear()
    {
        _pending = null;
        _activeChannelUrl = string.Empty;
        var session = _session;
        _session = null;
        session?.Dispose();
        // Whatever the session was playing goes with it. Opening a file or a plain stream does this for
        // itself, so by the time a new one starts there is usually nothing left to take away.
        _player.LeaveSession();
    }

    public void Dispose() => Clear();

    // ---- showing the list ----

    /// <remarks>
    /// The session is installed before the window opens, not when something is first played. Installing it
    /// on first play would leave a first search with no session - so the page of
    /// results its "load more" fetches is dropped by the guard that checks the session is still the current
    /// one, after the continuation has already been consumed. Paging a fresh search therefore does nothing
    /// there and cannot be made to by trying again.
    /// </remarks>
    /// <remarks>
    /// Re-entrant, and has to be. The progress window is modeless, so the main window keeps taking
    /// commands while a search runs: the user can press Escape, land back in a list, and have the search
    /// they started arrive and open a second list over the top of it. When that happens the outer loop is
    /// left holding a session that is no longer the current one, and every iteration below re-checks that
    /// rather than carrying on with it - a session that has been replaced has already been disposed, and
    /// anything done to it from here would be done to something dead.
    /// </remarks>
    private void Show(Session session)
    {
        if (!ReferenceEquals(_session, session))
        {
            Clear();
            _session = session;
        }
        while (ReferenceEquals(_session, session))
        {
            Prefetch(session, 0, OpeningPrefetch);
            using var feed = new Feed(this, session);
            // A channel session carries a tab bar; a search or playlist does not.
            var tabs = session.Kind is SessionKind.Channel ? Client.Tabs.DisplayNames() : null;
            var chosen = _view.Dialogs.ShowYouTubeResults(new YouTubeResultsPrompt(
                // Translators: Title of the window listing the videos a search or a playlist turned up.
                Tr("Videos"), session.Label, session.Items, session.Selected, feed, tabs, session.CurrentTab));
            // Another list opened over this one while it was up, and closing it took this session with
            // it. Whatever was chosen here belongs to a session that has gone.
            if (!ReferenceEquals(_session, session))
                return;
            if (chosen is not ResultChoice choice || choice.Index >= session.Items.Count)
            {
                Clear();
                return;
            }
            var index = choice.Index;
            // Which stream the user asked for - Enter for the picture, Ctrl+Enter for the sound - decides
            // what this play and every next after it resolves, so it is set on the session before anything
            // looks for a resolved address.
            session.Mode = choice.Mode;
            session.Selected = index;
            var item = session.Items[index];
            // The user asked to browse into this row's channel rather than play it. Handled like choosing a
            // channel row: the window has already closed, so opening the channel replaces this list cleanly.
            if (choice.Channel)
            {
                if (item.ChannelUrl.Length > 0)
                {
                    GoToChannel(item.ChannelUrl, item.Author);
                    return;
                }
                _speech.Speak(
                    // Translators: Spoken when the chosen video does not say which channel published it.
                    Tr("Channel link is not available."),
                    // Translators: The short wording spoken when the chosen video does not name its channel.
                    Tr("No channel link."));
                continue;
            }
            // A playlist or channel row is not played; choosing it browses one level in, replacing this
            // list with the videos of the playlist or the playlists of the channel, opening a fresh dialog
            // rather than trying to play the thing itself.
            if (item.ItemType is YouTubeItemType.Playlist)
            {
                OpenPlaylist(item.Url);
                return;
            }
            if (item.ItemType is YouTubeItemType.Channel)
            {
                _browsing.OpenChannel(item);
                return;
            }
            // A video already resolved starts here and now, with no window in between. That is what the
            // prefetching is for, and it is the difference the user actually notices.
            if (Ready(session, item) is Resolved ready)
            {
                if (Start(session, index, ready))
                    return;
                // It would not open. Round the loop, which puts the list back on the same row.
                continue;
            }
            PlayAt(session, index, item);
            return;
        }
    }

    /// <summary>Resolves a video behind a progress window, then plays it. On any failure the results list
    /// comes back, so the user is never left with nothing open.</summary>
    private void PlayAt(Session session, int index, YouTubeResult item)
    {
        if (!ReferenceEquals(_session, session))
            return;
        Resolve(item.Url, item, Session.AudioFor(session.Mode), session.QualityFor(session.Mode),
            session.Token, outcome =>
        {
            if (!ReferenceEquals(_session, session))
                return;
            if (outcome.Value is Resolved ready && Start(session, index, ready))
                return;
            if (outcome.Failure is not (ResolveFailure.None or ResolveFailure.Cancelled))
                ShowError(Utils.Describe(outcome.Failure, outcome.Detail, Utils.StreamFailed));
            Show(session);
        });
    }

    private void Resolve(
        string watchUrl,
        YouTubeResult item,
        bool audioOnly,
        int quality,
        CancellationToken token,
        Action<ResolveOutcome> completed)
    {
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while the address of a video is being looked up.
            Tr("Loading YouTube stream"),
            // Translators: First message shown while the address of a video is being looked up.
            Tr("Fetching stream URL..."),
            update => update.Name) { Proportional = false };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (_, waitToken) => _cache.Wait(watchUrl, item, audioOnly, quality, token, waitToken),
            completed);
    }

    private void ShowError(string message) =>
        // Translators: Title of an error shown while resolving or playing a YouTube video.
        _view.Dialogs.ShowError(message, Tr("YouTube"));

}
