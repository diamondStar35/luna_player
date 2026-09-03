using LunaPlayer.Accessibility;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.YouTube;

/// <summary>What came of asking for the video after this one.</summary>
///
/// <remarks>
/// Four states rather than the Python player's true/false/None, which cannot tell "the next video is on
/// its way, do not stop" from "there is nothing after this one, please stop". Its end-of-playback handler
/// answers both with false and stops in either case, so a video whose successor is still resolving ends
/// the session a moment before the successor arrives.
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
/// This is the part of the Python player's <c>youtube/flow.py</c> that could not live on
/// <see cref="Backend"/>: opening a video needs the player and the results window needs the session, and
/// <see cref="Backend"/> knows about neither. Everything here runs on the UI thread except the work handed
/// to <see cref="ResolveCache"/>, which is the only thing that touches the network.
/// </remarks>
internal sealed class YouTubeSessions : IDisposable
{
    /// <summary>How many videos to resolve ahead when a list of results is first shown.</summary>
    private const int OpeningPrefetch = 5;

    /// <summary>How many to resolve ahead when the user moves to a row.</summary>
    private const int BrowsingPrefetch = 2;

    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly ExplodeClient _client;
    private readonly Backend _backend;
    private readonly ResolveCache _cache;
    private readonly Action<string> _download;
    private readonly Action<string> _copy;
    private readonly Action<string> _browse;
    private YouTubeSession? _session;
    private PendingNext? _pending;

    /// <param name="download">What to do when a row is downloaded. Saving a video is the action handler's
    /// job - it owns the folder chooser and the progress window - so it is passed in rather than repeated
    /// here.</param>
    internal YouTubeSessions(
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        ISpeechOutput speech,
        IApplicationDispatcher dispatcher,
        ExplodeClient client,
        Backend backend,
        ResolveCache cache,
        Action<string> download,
        Action<string> copy,
        Action<string> browse)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _speech = speech;
        _dispatcher = dispatcher;
        _client = client;
        _backend = backend;
        _cache = cache;
        _download = download;
        _copy = copy;
        _browse = browse;
    }

    /// <summary>The address of the video playing now, or null when what is playing is not one.</summary>
    internal string? CurrentWatchUrl => _player.CurrentSource;

    /// <summary>Searches YouTube and shows what it finds.</summary>
    ///
    /// <remarks>
    /// The first video is resolved before the window opens, which is what the Python player does and worth
    /// keeping: it means the row the list opens on plays the instant it is chosen, and it puts the first
    /// sign of a rate limit or a broken network in the progress window the user is already looking at
    /// rather than in a message box after they have picked something.
    /// </remarks>
    internal void Search(string query)
    {
        var audioOnly = _settings.YouTube.AudioOnly;
        var quality = _settings.YouTube.Quality;
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
            (report, token) => RunSearch(query, count, audioOnly, quality, fetching, report, token),
            found =>
            {
                if (found.Failure is ResolveFailure.Cancelled)
                    return;
                if (found.Failure is not ResolveFailure.None)
                {
                    ShowError(Describe(found.Failure, found.Detail,
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
                Show(new YouTubeSession(
                    SessionKind.Search,
                    found.Items,
                    audioOnly,
                    quality,
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
        var audioOnly = _settings.YouTube.AudioOnly;
        var quality = _settings.YouTube.Quality;
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
                    ShowError(Describe(found.Failure, found.Detail,
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
                Show(new YouTubeSession(SessionKind.Playlist, found.Items, audioOnly, quality));
            });
    }

    /// <summary>Plays one video, named by a link rather than chosen from a list.</summary>
    /// <remarks>There is no session: nothing follows a single video, so there is no next and Escape has
    /// nothing to go back to. Any session already open is closed, as the Python player closes it.</remarks>
    internal void PlayLink(string link)
    {
        Clear();
        var watchUrl = ExplodeClient.Canonical(link);
        if (watchUrl is null)
        {
            ShowError(
                // Translators: Shown when an address looked like a YouTube link but names no video.
                Tr("This link does not include a YouTube video."));
            return;
        }
        Resolve(watchUrl, YouTubeResult.None, _settings.YouTube.AudioOnly, _settings.YouTube.Quality,
            CancellationToken.None,
            outcome =>
            {
                if (outcome.Value is Resolved ready)
                    OpenAlone(ready);
                else if (outcome.Failure is not ResolveFailure.Cancelled)
                    ShowError(Describe(outcome.Failure, outcome.Detail, StreamFailed));
            });
    }

    /// <summary>Moves to the video after the one playing, resolving it first if it is not ready.</summary>
    internal NextOutcome TryNext()
    {
        if (_session is not YouTubeSession session || CurrentWatchUrl is not string playing)
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
    /// from here, so both would be a step behind. The Python player syncs at the same two points.
    /// </remarks>
    internal void SyncSelection()
    {
        if (_session is not YouTubeSession session || CurrentWatchUrl is not string playing)
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
        if (_session is not YouTubeSession session || CurrentWatchUrl is not string playing)
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
    /// The session is installed before the window opens, not when something is first played. The Python
    /// player installs it on first play, which leaves a first search with no session - so the page of
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
    private void Show(YouTubeSession session)
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
            var chosen = _view.ShowYouTubeResults(new YouTubeResultsPrompt(
                // Translators: Title of the window listing the videos a search or a playlist turned up.
                Tr("Videos"), session.Label, session.Items, session.Selected, feed));
            // Another list opened over this one while it was up, and closing it took this session with
            // it. Whatever was chosen here belongs to a session that has gone.
            if (!ReferenceEquals(_session, session))
                return;
            if (chosen is not int index || index >= session.Items.Count)
            {
                Clear();
                return;
            }
            session.Selected = index;
            var item = session.Items[index];
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
    private void PlayAt(YouTubeSession session, int index, YouTubeResult item)
    {
        if (!ReferenceEquals(_session, session))
            return;
        Resolve(item.Url, item, session.AudioOnly, session.Quality, session.Token, outcome =>
        {
            if (!ReferenceEquals(_session, session))
                return;
            if (outcome.Value is Resolved ready && Start(session, index, ready))
                return;
            if (outcome.Failure is not (ResolveFailure.None or ResolveFailure.Cancelled))
                ShowError(Describe(outcome.Failure, outcome.Detail, StreamFailed));
            Show(session);
        });
    }

    private void Resolve(
        string watchUrl,
        YouTubeResult item,
        bool audioOnly,
        YouTubeQuality quality,
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

    // ---- playing ----

    /// <summary>Starts a video from a session and makes it the session's current one.</summary>
    private bool Start(YouTubeSession session, int index, Resolved ready)
    {
        // Refusing rather than installing it: a session that is no longer the current one has been
        // disposed, and putting it back would leave the player driving a list nothing can add to.
        if (!ReferenceEquals(_session, session))
            return false;
        if (!Open(ready))
            return false;
        session.Selected = index;
        Prefetch(session, index + 1, 1);
        return true;
    }

    /// <summary>Starts a video from a list, in front of the playlist the user opened.</summary>
    /// <remarks>
    /// So that playlist keeps its files, its order and its place. Nothing here has to turn shuffle off
    /// either - the session's list starts without it, and the one the user set stays set on the playlist it
    /// belongs to.
    /// </remarks>
    private bool Open(Resolved ready)
        => Report(_player.PlaySessionStream(
            ready.Url, ready.Item.Title, ready.Item.Url, ready.AudioUrl));

    /// <summary>Starts a video that came from a link rather than from a list.</summary>
    /// <remarks>
    /// Into the playlist the user is working in, which is where the Python player puts it and where a
    /// single video belongs: there is no list behind it to move through, nothing for Escape to go back to,
    /// and nothing a stage of its own would keep separate. A plain network stream is opened the same way.
    /// </remarks>
    private bool OpenAlone(Resolved ready)
        => Report(_player.OpenStream(ready.Url, ready.Item.Title, ready.Item.Url, ready.AudioUrl));

    private bool Report(bool opened)
    {
        if (opened)
            return true;
        ShowError(
            // Translators: Shown when a video was found but the player could not start playing it.
            Tr("Could not open YouTube stream."));
        return false;
    }

    /// <summary>Moves playback to a video that is ready, adding it to the playlist if it is not there yet.
    /// </summary>
    private bool Advance(YouTubeSession session, int from, int to, Resolved ready)
    {
        // Asked before anything is added to the playlist, not after. Between asking for the next video and
        // its arriving the user may have opened something else entirely - and an entry appended on the way
        // to a move that is then refused stays in the playlist for good, named after a video that is not
        // playing and pointing at an address that expires within the hour.
        if (!ReferenceEquals(_session, session)
            || CurrentWatchUrl is not string playing || session.IndexOf(playing) != from)
            return false;
        if (_player.IndexOfSource(ready.Item.Url) < 0
            && !_player.QueueSessionStream(ready.Url, ready.Item.Title, ready.Item.Url, ready.AudioUrl))
            return false;
        if (!_player.Next(wrap: false))
            return false;
        session.Selected = to;
        Prefetch(session, to + 1, 1);
        return true;
    }

    /// <summary>Resolves the next video in the background and moves to it when it arrives.</summary>
    private void LoadNext(YouTubeSession session, int from, int to, YouTubeResult item)
    {
        var pending = new PendingNext(session, from, to, item.Url);
        _pending = pending;
        _speech.Speak(
            // Translators: Spoken when the video after this one is being fetched before it can be played.
            Tr("Loading next video..."),
            // Translators: The short wording spoken while the video after this one is being fetched.
            Tr("Loading next video..."));
        var task = _cache.Start(
            _cache.Key(item.Url, session.AudioOnly, session.Quality),
            item.Url, item, session.AudioOnly, session.Quality, session.Token);
        _ = task.ContinueWith(
            finished => _dispatcher.Post(() => NextReady(pending, finished.Result)),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
    }

    /// <remarks>
    /// Announcing the new title is not in the Python player, which advances silently and leaves the user to
    /// work out what is playing. Nor is saying anything when the video could not be fetched, which there
    /// leaves "Loading next video..." as the last thing said and nothing following it.
    /// </remarks>
    private void NextReady(PendingNext pending, ResolveOutcome outcome)
    {
        if (!ReferenceEquals(_pending, pending) || !ReferenceEquals(_session, pending.Session))
            return;
        _pending = null;
        if (outcome.Value is not Resolved ready)
        {
            if (outcome.Failure is ResolveFailure.Cancelled)
                return;
            _speech.Speak(
                // Translators: Spoken when the video after this one could not be fetched, so playback stops.
                Tr("Could not load the next video."),
                // Translators: The short wording spoken when the next video could not be fetched.
                Tr("Next video failed."));
            return;
        }
        if (!Advance(pending.Session, pending.From, pending.To, ready))
            return;
        if (_settings.General.SpeakFileOnNavigation)
            _speech.Speak(ready.Item.Title, ready.Item.Title);
    }

    // ---- prefetching ----

    /// <summary>Starts resolving <paramref name="count"/> videos from <paramref name="start"/>, so they
    /// play without a wait when they are reached.</summary>
    private void Prefetch(YouTubeSession session, int start, int count)
    {
        if (session.IsCancelled)
            return;
        var end = Math.Min(session.Items.Count, Math.Max(0, start) + count);
        for (var index = Math.Max(0, start); index < end; index++)
        {
            var item = session.Items[index];
            _cache.Prefetch(item.Url, item, session.AudioOnly, session.Quality, session.Token);
        }
    }

    private Resolved? Ready(YouTubeSession session, YouTubeResult item)
        => _cache.TryTake(_cache.Key(item.Url, session.AudioOnly, session.Quality));

    // ---- the background jobs ----

    private SearchResults RunSearch(
        string query,
        int count,
        bool audioOnly,
        YouTubeQuality quality,
        string fetching,
        Action<ProgressUpdate> report,
        CancellationToken token)
    {
        try
        {
            var (items, page) = _client.Search(query, count, token);
            if (items.Count == 0)
                return new SearchResults([], null, ResolveFailure.None, string.Empty);
            report(new ProgressUpdate(0, 0, fetching));
            // Waited for, not merely started: the list opens on its first row, and the point of doing this
            // before the window appears is that choosing that row plays at once. A video that will not
            // resolve is not an error - the user still gets their results - so the outcome is dropped.
            _ = _cache.Wait(items[0].Url, items[0], audioOnly, quality, token, token);
            return new SearchResults(items, page, ResolveFailure.None, string.Empty);
        }
        catch (Exception failure)
        {
            var explained = ExplodeClient.Explain(failure, token);
            return new SearchResults([], null, explained.Failure, explained.Detail);
        }
    }

    private PlaylistResults RunPlaylist(string link, CancellationToken token)
    {
        var (title, items, failure, detail) = _backend.Playlist(link, token);
        return new PlaylistResults(title, items, failure, detail);
    }

    // ---- wording ----

    /// <summary>Turns a failure a worker reported into the sentence the user reads.</summary>
    /// <remarks>
    /// Here rather than at the point of failure because <c>Tr</c> may only be called on the UI thread, and
    /// the workers are not on it. The raw detail follows the sentence, as the Python player's yt-dlp
    /// diagnostics follow its own wording.
    /// </remarks>
    /// <param name="fallback">What to say when nothing more precise is known. Each job has its own
    /// wording for it - a search that failed and a video that failed are not the same news - which is why
    /// it is passed in rather than fixed here.</param>
    internal static string Describe(ResolveFailure failure, string detail, string fallback = "")
    {
        var message = failure switch
        {
            // Translators: Shown when a video is private, deleted, or never existed.
            ResolveFailure.Unavailable => Tr("This video is not available."),
            // Translators: Shown when YouTube has the video but will not serve it - age restricted, blocked
            // in this country, or paid for.
            ResolveFailure.Unplayable => Tr("YouTube will not play this video here."),
            // Translators: Shown when a video exists but offers nothing the player can play.
            ResolveFailure.NoStream => Tr("Could not resolve a playable stream."),
            // Translators: Shown when the yt-dlp resolver is turned on and the programs it needs are not
            // installed. "yt-dlp" is a program name and is not translated.
            ResolveFailure.MissingComponents => Tr("YouTube components are missing. Download them in the YouTube settings, or turn off the yt-dlp resolver there."),
            // Translators: Shown when YouTube is refusing requests from this computer for the time being.
            // "HTTP 429" is the numbered error it answers with and is not translated.
            ResolveFailure.RateLimited => Tr("YouTube returned HTTP 429 (Too Many Requests). Your IP may be temporarily rate-limited."),
            // Translators: Shown when the request never reached YouTube or its answer never arrived.
            ResolveFailure.Network => Tr("Could not reach YouTube. Check the network connection."),
            _ => fallback.Length > 0
                ? fallback
                // Translators: Shown when something went wrong that the player cannot explain more precisely.
                : Tr("Could not read this video."),
        };
        return detail.Length == 0
            ? message
            // Translators: Adds the technical reason under a message about YouTube. {message} is that
            // message and {details} is the reason, which is not translated.
            : TrFormat("{message}\nDetails: {details}", message, Short(detail));
    }

    /// <summary>The first line of a diagnostic, cut short. A stack trace in a message box helps nobody.
    /// </summary>
    private static string Short(string detail)
    {
        var line = detail.Split('\n')[0].Trim();
        return line.Length <= 220 ? line : string.Concat(line.AsSpan(0, 220).TrimEnd(), "...");
    }

    /// <summary>What a video that would not resolve is called, when nothing more precise is known.
    /// </summary>
    private static string StreamFailed =>
        // Translators: Shown when a video could not be turned into something playable and nothing said why.
        Tr("Could not resolve YouTube stream.");

    private void ShowError(string message) =>
        // Translators: Title of the messages the player shows about YouTube.
        _view.ShowError(message, Tr("YouTube"));

    private readonly record struct SearchResults(
        IReadOnlyList<YouTubeResult> Items, SearchPage? Page, ResolveFailure Failure, string Detail);

    private readonly record struct PlaylistResults(
        string Title, IReadOnlyList<YouTubeResult> Items, ResolveFailure Failure, string Detail);

    /// <summary>The move to the next video that is waiting on a resolve.</summary>
    private sealed record PendingNext(YouTubeSession Session, int From, int To, string WatchUrl);

    /// <summary>The results window's way of talking back: it reports where the user is, asks for more rows
    /// and says when it has gone.</summary>
    ///
    /// <remarks>
    /// One of these per opening of the window rather than one per session, so closing it cannot silence
    /// the next one. Every method runs on the UI thread - the page arrives through
    /// <see cref="IApplicationDispatcher.Post"/> - which is why one plain field is guard enough and no
    /// lock appears here.
    /// </remarks>
    private sealed class Feed : IYouTubeResultsFeed, IDisposable
    {
        private readonly YouTubeSessions _owner;
        private readonly YouTubeSession _session;
        private bool _closed;
        private bool _loading;
        private bool _exhausted;

        internal Feed(YouTubeSessions owner, YouTubeSession session)
        {
            _owner = owner;
            _session = session;
        }

        public void Selected(int index) => _owner.Prefetch(_session, index, BrowsingPrefetch);

        public void CopyLink(int index) => On(index, item => _owner._copy(item.Url));

        public void OpenInBrowser(int index) => On(index, item => _owner._browse(item.Url));

        public void OpenChannel(int index) => On(index, item =>
        {
            if (item.ChannelUrl.Length > 0)
            {
                _owner._browse(item.ChannelUrl);
                return;
            }
            _owner._speech.Speak(
                // Translators: Spoken when the chosen video does not say which channel published it.
                Tr("Channel link is not available."),
                // Translators: The short wording spoken when the chosen video does not name its channel.
                Tr("No channel link."));
        });

        public void Download(int index) => On(index, item => _owner._download(item.Url));

        /// <summary>Runs something on one row, so long as the row is still there.</summary>
        /// <remarks>
        /// The list can be longer than it was when the window opened - a page may have arrived while the
        /// user was reading - but never shorter, so this only has to guard the bounds rather than re-read
        /// what the row holds.
        /// </remarks>
        private void On(int index, Action<YouTubeResult> action)
        {
            if (_closed || index < 0 || index >= _session.Items.Count)
                return;
            _session.Selected = index;
            action(_session.Items[index]);
        }

        public void RequestMore(Action<IReadOnlyList<YouTubeResult>> appended)
        {
            if (_closed || _loading || _exhausted || _session.IsCancelled)
                return;
            if (_session.Page is not SearchPage page || !page.HasMore)
            {
                _exhausted = true;
                appended([]);
                return;
            }
            _loading = true;
            _owner._speech.Speak(
                // Translators: Spoken when the user reaches the end of the results list and more are being fetched.
                Tr("Loading more videos..."),
                // Translators: The short wording spoken while more search results are being fetched.
                Tr("Loading more videos..."));
            var count = _owner._settings.YouTube.SearchResultCount;
            var token = _session.Token;
            _ = Task.Run(() => page.Take(count, token), token).ContinueWith(
                finished => _owner._dispatcher.Post(() => Arrived(finished, appended)),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }

        public void Close() => _closed = true;

        public void Dispose() => Close();

        private void Arrived(Task<IReadOnlyList<YouTubeResult>> finished, Action<IReadOnlyList<YouTubeResult>> appended)
        {
            _loading = false;
            if (_session.IsCancelled || !ReferenceEquals(_owner._session, _session))
                return;
            // A page that failed is not the end of the results, only the end of this attempt: leaving
            // _exhausted alone lets the user try again by arrowing off the last row and back onto it.
            if (!finished.IsCompletedSuccessfully)
                return;
            var page = finished.Result;
            if (page.Count == 0)
            {
                _exhausted = true;
                if (!_closed)
                    appended([]);
                return;
            }
            // Kept on the session before anything else, and kept even when the window has gone. Taking a
            // page consumes the search's continuation, so a page dropped here could never be asked for
            // again; this way closing the list while one is in flight only defers it to the next opening.
            var start = _session.Items.Count;
            _session.Append(page);
            if (!_closed)
                appended(page);
            _owner.Prefetch(_session, start, OpeningPrefetch);
        }
    }
}
