using LunaPlayer.Accessibility;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.YouTube.Playback;

/// <summary>What came of asking for the video after this one.</summary>
///
/// <remarks>
/// Four states rather than three. A plain true/false/None cannot tell "the next video is on its way, do
/// not stop" from "there is nothing after this one, please stop"; answering both with false and stopping
/// in either case would end the session a moment before a still-resolving successor arrives.
/// </remarks>
internal enum NextOutcome
{
    /// <summary>What is playing is not part of a session, so the ordinary playlist rules apply.</summary>
    NotOurs,
    /// <summary>Playback has moved on already.</summary>
    Advanced,
    /// <summary>The next video is being resolved and will start itself. Do not stop.</summary>
    Pending,
    /// <summary>This was the last video.</summary>
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
    private readonly MediaPlayer _player;
    private readonly Settings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly Metadata.Client _client;
    private readonly Backend _backend;
    private readonly ResolveCache _cache;
    private readonly Action<string, string> _directDownload;
    private readonly Action<string, string> _download;
    private readonly Action<string, string> _queueDownload;
    private readonly Action<string> _copy;
    private readonly Action<string> _browse;
    private readonly FavoriteStore _favorites;
    private readonly Components.Service _components;
    private Session? _session;
    private PendingNext? _pending;

    /// <param name="directDownload">Downloads a row straight to disk with the saved settings.</param>
    /// <param name="favorites">The shared store, so a row can be saved to the favourites straight from the
    /// results list. The same store the favourites manager writes to, so an addition shows up there.</param>
    /// <param name="components">The programs the channel browser needs, offered when a channel is opened and
    /// they are not yet installed.</param>
    internal Sessions(
        IMainView view,
        MediaPlayer player,
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
        _client = client;
        _backend = backend;
        _cache = cache;
        _directDownload = directDownload;
        _download = download;
        _queueDownload = queueDownload;
        _copy = copy;
        _browse = browse;
        _favorites = favorites;
        _components = components;
    }

    /// <summary>The address of the video playing now, or null when what is playing is not one.</summary>
    internal string? CurrentWatchUrl => _player.CurrentSource;

    private string _activeChannelUrl = string.Empty;

    /// <summary>The channel address of the YouTube video playing now, or empty when it is not known - a
    /// video opened from a bare link carries no channel. Used by the video commands that open the channel.</summary>
    internal string ActiveChannelUrl => _activeChannelUrl;

    /// <summary>Searches YouTube and shows what it finds.</summary>
    ///
    /// <remarks>
    /// The first video is resolved before the window opens, which is worth doing: it means the row the list
    /// opens on plays the instant it is chosen, and it puts the first
    /// sign of a rate limit or a broken network in the progress window the user is already looking at
    /// rather than in a message box after they have picked something.
    /// </remarks>
    /// <param name="filter">The kind of result the user asked for in the search dialog, 0-5. It decides
    /// which YouTube search is run and whether the rows are videos, playlists, or channels.</param>
    internal void Search(string query, int filter)
    {
        var videoQuality = (int)_settings.YouTube.VideoQuality;
        var audioQuality = (int)_settings.YouTube.AudioQuality;
        var count = _settings.YouTube.SearchResultCount;
        // Read here and carried into the job: Tr may only be called on the thread that owns the windows,
        // and the job does not run on it.
        // Translators: Message shown while the first video of a search is being made ready to play.
        var fetching = Tr("Fetching first stream...");
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while a YouTube search is running.
            Tr("Searching YouTube"),
            // Translators: First message shown while a YouTube search is running.
            Tr("Searching videos..."),
            update => update.Name) { Proportional = false };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (report, token) => RunSearch(query, filter, count, videoQuality, audioQuality, fetching, report, token),
            found =>
            {
                if (found.Failure is ResolveFailure.Cancelled)
                    return;
                if (found.Failure is not ResolveFailure.None)
                {
                    ShowError(Utils.Describe(found.Failure, found.Detail,
                        // Translators: Shown when a YouTube search failed and nothing said why.
                        Tr("Could not complete YouTube search.")));
                    return;
                }
                if (found.Items.Count == 0)
                {
                    ShowError(
                        // Translators: Shown when a YouTube search found nothing at all.
                        Tr("No videos were found for this search."));
                    return;
                }
                Show(new Session(
                    SessionKind.Search,
                    found.Items,
                    videoQuality,
                    audioQuality,
                    found.Page));
            });
    }

    /// <summary>Opens every video in a playlist and shows the list.</summary>
    internal void OpenPlaylist(string link)
    {
        if (!LinkValidator.Parse(link).HasPlaylist)
        {
            ShowError(
                // Translators: Shown when an address looked like a YouTube link but names no playlist.
                Tr("This link does not include a YouTube playlist."));
            return;
        }
        var videoQuality = (int)_settings.YouTube.VideoQuality;
        var audioQuality = (int)_settings.YouTube.AudioQuality;
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while a YouTube playlist is being read.
            Tr("Loading YouTube link"),
            // Translators: First message shown while the videos in a YouTube playlist are being listed.
            Tr("Loading playlist items..."),
            update => update.Name) { Proportional = false };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (_, token) => RunPlaylist(link, token),
            found =>
            {
                if (found.Failure is ResolveFailure.Cancelled)
                    return;
                if (found.Failure is not ResolveFailure.None)
                {
                    ShowError(Utils.Describe(found.Failure, found.Detail,
                        // Translators: Shown when a YouTube address could not be read and nothing said why.
                        Tr("Could not read YouTube link data.")));
                    return;
                }
                if (found.Items.Count == 0)
                {
                    ShowError(
                        // Translators: Shown when a YouTube playlist address opened nothing playable.
                        Tr("No videos were found in this playlist."));
                    return;
                }
                Show(new Session(SessionKind.Playlist, found.Items, videoQuality, audioQuality));
            });
    }

    /// <summary>Opens a channel in the tabbed browser: its videos, shorts, streams, playlists and the rest,
    /// each paged as the user scrolls.</summary>
    /// <remarks>
    /// The browser is backed by yt-dlp, not PyYt, so it is gated on the programs being installed the same
    /// way a download is: when they are missing the offer is made and the channel opened again once they
    /// arrive. It opens on the videos tab; switching tabs, playing a video with next and previous, drilling
    /// into a playlist row and saving a row all flow through the same session machinery a search uses.
    /// </remarks>
    private void OpenChannel(YouTubeResult channel)
    {
        if (!Backend.HasComponents
            && _components.Ensure(_settings.YouTube.Channel, () => OpenChannel(channel))
                is not Components.Service.ComponentsState.Ready)
            return;
        var videoQuality = (int)_settings.YouTube.VideoQuality;
        var audioQuality = (int)_settings.YouTube.AudioQuality;
        var channelBase = Client.Tabs.Normalise(channel.Url.Length > 0 ? channel.Url : channel.ChannelUrl);
        var tabIndex = Client.Tabs.DefaultIndex;
        var tabKey = Client.Tabs.Keys[tabIndex];
        var title = channel.Title;
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while a YouTube channel is being opened.
            Tr("Loading channel"),
            // Translators: First message shown while a YouTube channel is being opened.
            Tr("Loading channel videos..."),
            update => update.Name) { Proportional = false };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (_, token) => _backend.OpenChannelTab(channelBase, tabKey, token),
            found =>
            {
                if (found.Failure is ResolveFailure.Cancelled)
                    return;
                if (found.Failure is not ResolveFailure.None)
                {
                    ShowError(Utils.Describe(found.Failure, found.Detail,
                        // Translators: Shown when a YouTube channel could not be read.
                        Tr("Could not read this YouTube channel.")));
                    return;
                }
                // A channel whose videos tab is empty is still worth opening: the user can switch to a tab
                // that has something. Only a real failure stops it.
                Show(new Session(
                    SessionKind.Channel, found.Items, videoQuality, audioQuality,
                    found.Page, channelBase, tabIndex, title));
            });
    }

    /// <summary>Adds a results row to the favourites, or removes it when it is already saved, saying which
    /// way it went.</summary>
    /// <remarks>
    /// The shared store, so it turns up in the favourites manager and survives a restart. A playlist row is
    /// saved as a playlist; everything else as a video, which is what a channel row's contents would be
    /// played as anyway. A row already saved - matched by its address - is taken back out, so the same key
    /// adds and removes. Success and failure are spoken rather than shown, because the results window is
    /// still up and a message box over it would take the caret off the row.
    /// </remarks>
    internal void ToggleFavorite(YouTubeResult item)
    {
        if (_favorites.FindByLink(item.Url) is Favorite existing)
        {
            if (_favorites.Delete(existing.Id))
            {
                _speech.Speak(
                    // Translators: Spoken once a video has been taken back out of the favourites from the results list.
                    Tr("Removed from favorites."),
                    // Translators: The short wording spoken once a video has been removed from the favourites.
                    Tr("Removed from favorites."));
                return;
            }
            var removeError = _favorites.LastError.Length > 0
                ? _favorites.LastError
                // Translators: Spoken when a video could not be removed from the favourites.
                : Tr("Could not remove from favorites.");
            _speech.Speak(removeError, removeError);
            return;
        }
        var kind = item.ItemType is YouTubeItemType.Playlist ? FavoriteKind.Playlist : FavoriteKind.Video;
        if (_favorites.Add(item.Title, kind, item.Url) is not null)
        {
            _speech.Speak(
                // Translators: Spoken once a video has been added to the favourites from the results list.
                Tr("Added to favorites."),
                // Translators: The short wording spoken once a video has been added to the favourites.
                Tr("Added to favorites."));
            return;
        }
        var message = _favorites.LastError.Length > 0
            ? _favorites.LastError
            // Translators: Spoken when a video could not be added to the favourites.
            : Tr("Could not add to favorites.");
        _speech.Speak(message, message);
    }

    /// <summary>Opens a video's channel in the player's own channel browser, from the channel address a row
    /// carried. The in-player counterpart of opening the channel in the web browser.</summary>
    internal void GoToChannel(string channelUrl, string title)
        => OpenChannel(new YouTubeResult(string.Empty, title, title, null, channelUrl, channelUrl));

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

    /// <summary>Moves to the video after the one playing, resolving it first if it is not ready.</summary>
    internal NextOutcome TryNext()
    {
        if (_session is not Session session || CurrentWatchUrl is not string playing)
            return NextOutcome.NotOurs;
        var current = session.IndexOf(playing);
        if (current < 0)
            return NextOutcome.NotOurs;
        session.Selected = current;
        var next = current + 1;
        if (next >= session.Items.Count)
        {
            _pending = null;
            return NextOutcome.Exhausted;
        }
        var item = session.Items[next];
        if (Ready(session, item) is Resolved ready)
        {
            _pending = null;
            return Advance(session, current, next, ready) ? NextOutcome.Advanced : NextOutcome.Exhausted;
        }
        LoadNext(session, current, next, item);
        return NextOutcome.Pending;
    }

    /// <summary>Brings the session's idea of where the user is back in line with what is playing.
    /// </summary>
    ///
    /// <remarks>
    /// Moving through the playlist by the ordinary means can land on a session video without going through
    /// <see cref="TryNext"/> - pressing Previous does exactly that. Nothing about playback goes wrong when
    /// it does, but the row Escape returns to and the video counted as "the one after this" are both taken
    /// from here, so both would be a step behind.
    /// </remarks>
    internal void SyncSelection()
    {
        if (_session is not Session session || CurrentWatchUrl is not string playing)
            return;
        var index = session.IndexOf(playing);
        if (index >= 0)
            session.Selected = index;
    }

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
                OpenChannel(item);
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

}
