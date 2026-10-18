using LunaPlayer.Accessibility;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.YouTube.Playback;

internal sealed partial class Sessions
{
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
        private readonly Sessions _owner;
        private readonly Session _session;
        private bool _closed;
        private bool _loading;

        internal Feed(Sessions owner, Session session)
        {
            _owner = owner;
            _session = session;
        }

        public void Selected(int index) => _owner.Prefetch(_session, index, BrowsingPrefetch);

        public void CopyLink(int index) => On(index, item => _owner._copy(item.Url));

        public void OpenInBrowser(int index) => On(index, item => _owner._browse(item.Url));

        public void OpenChannelInBrowser(int index) => On(index, item => WithChannel(item, _owner._browse));

        /// <summary>Runs <paramref name="action"/> on the row's channel address, or says so when the listing
        /// named no channel. Shared by the browser and in-player ways of opening a channel.</summary>
        private void WithChannel(YouTubeResult item, Action<string> action)
        {
            if (item.ChannelUrl.Length > 0)
            {
                action(item.ChannelUrl);
                return;
            }
            _owner._speech.Speak(
                // Translators: Spoken when the chosen video does not say which channel published it.
                Tr("Channel link is not available."),
                // Translators: The short wording spoken when the chosen video does not name its channel.
                Tr("No channel link."));
        }

        public void DirectDownload(int index) => On(index, item => _owner._directDownload(item.Url, item.Title));

        public void Download(int index) => On(index, item => _owner._download(item.Url, item.Title));

        public void AddToDownloadQueue(int index) => On(index, item =>
        {
            if (item.ItemType is YouTubeItemType.Video)
            {
                _owner._queueDownload(item.Url, item.Title);
                return;
            }
            _owner._speech.Speak(
                // Translators: Spoken when a playlist or channel row is used with a video-only queue command.
                Tr("Only videos can be added to the download queue."),
                // Translators: Short wording for a non-video row used with the download queue.
                Tr("Not a video."));
        });

        public bool IsFavorite(int index) =>
            Peek(index) is YouTubeResult item && _owner._favorites.FindByLink(item.Url) is not null;

        public void ToggleFavorite(int index) => On(index, item => _owner.ToggleFavorite(item));

        /// <summary>Reads one row without making it the current selection - a query the context menu runs
        /// before it opens, which must not move the caret the way acting on a row does.</summary>
        private YouTubeResult? Peek(int index)
            => _closed || index < 0 || index >= _session.Items.Count ? null : _session.Items[index];

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
            if (_closed || _loading || _session.Exhausted || _session.IsCancelled)
                return;
            if (_session.Page is not IResultPage page || !page.HasMore)
            {
                _session.Exhausted = true;
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

        /// <summary>Loads another tab of the channel and swaps the list to it.</summary>
        /// <remarks>
        /// A tab already visited is switched to at once, with no fetch: the session kept its rows and its
        /// paging the first time, so returning to it is free. A tab not yet seen is fetched off the UI
        /// thread the way <see cref="RequestMore"/> pages, and its result posted back. <paramref
        /// name="replaced"/> is handed the rows on success, or null when the switch failed, was refused, or
        /// the window closed under it - so a deferred answer that arrives after the dialog has gone touches
        /// nothing that has been disposed. Only a channel session has tabs; anything else answers null at
        /// once.
        /// </remarks>
        public void SwitchTab(int tabIndex, Action<IReadOnlyList<YouTubeResult>?> replaced)
        {
            if (_closed || _session.IsCancelled
                || _session.Kind is not SessionKind.Channel
                || tabIndex < 0 || tabIndex >= Client.Tabs.Keys.Length)
            {
                replaced(null);
                return;
            }
            // Seen before: make it current and hand back its rows without a fetch.
            if (_session.HasTab(tabIndex))
            {
                _session.ActivateTab(tabIndex);
                replaced(_session.Items);
                _owner.Prefetch(_session, 0, OpeningPrefetch);
                return;
            }
            if (_loading)
            {
                replaced(null);
                return;
            }
            _loading = true;
            _owner._speech.Speak(
                // Translators: Spoken while another tab of a YouTube channel is being loaded.
                Tr("Loading..."),
                // Translators: The short wording spoken while a channel tab is being loaded.
                Tr("Loading..."));
            var channelBase = _session.ChannelBase;
            var tabKey = Client.Tabs.Keys[tabIndex];
            var token = _session.Token;
            _ = Task.Run(() => _owner._backend.OpenChannelTab(channelBase, tabKey, token), token).ContinueWith(
                finished => _owner._dispatcher.Post(() => Switched(tabIndex, finished, replaced)),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }

        private void Switched(
            int tabIndex,
            Task<Backend.ChannelTabResult> finished,
            Action<IReadOnlyList<YouTubeResult>?> replaced)
        {
            _loading = false;
            // The window closed, or another list replaced this session, while the tab was on its way. The
            // page that came back has no home now, so it is closed here rather than leaked, and the caller
            // is not told - its callback belongs to a dialog that has gone.
            if (_closed || _session.IsCancelled || !ReferenceEquals(_owner._session, _session))
            {
                if (finished.IsCompletedSuccessfully && finished.Result.Page is IAsyncDisposable page)
                    _ = page.DisposeAsync().AsTask();
                return;
            }
            if (!finished.IsCompletedSuccessfully || finished.Result.Failure is not ResolveFailure.None)
            {
                replaced(null);
                return;
            }
            var result = finished.Result;
            _session.CacheTab(tabIndex, result.Items, result.Page);
            replaced(result.Items);
            _owner.Prefetch(_session, 0, OpeningPrefetch);
        }

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
                _session.Exhausted = true;
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
