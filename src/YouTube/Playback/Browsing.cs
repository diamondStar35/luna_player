using LunaPlayer.Accessibility;
using LunaPlayer.Application;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Media;
using LunaPlayer.UI;

namespace LunaPlayer.YouTube.Playback;

/// <summary>Finds and browses YouTube searches, playlists, and channels. Playback sessions receive the
/// resulting video lists but do not own the services used to discover them.</summary>
internal sealed class Browsing
{
    private readonly IMainView _view;
    private readonly Settings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly Metadata.Client _client;
    private readonly Backend _backend;
    private readonly ResolveCache _cache;
    private readonly FavoriteStore _favorites;
    private readonly Components.Service _components;
    private readonly Action<Session> _show;

    internal Browsing(
        IMainView view,
        Settings settings,
        ISpeechOutput speech,
        IApplicationDispatcher dispatcher,
        Metadata.Client client,
        Backend backend,
        ResolveCache cache,
        FavoriteStore favorites,
        Components.Service components,
        Action<Session> show)
    {
        _view = view;
        _settings = settings;
        _speech = speech;
        _dispatcher = dispatcher;
        _client = client;
        _backend = backend;
        _cache = cache;
        _favorites = favorites;
        _components = components;
        _show = show;
    }

    internal void Search(string query, int filter)
    {
        var videoQuality = (int)_settings.YouTube.VideoQuality;
        var audioQuality = (int)_settings.YouTube.AudioQuality;
        var count = _settings.YouTube.SearchResultCount;
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
                _show(new Session(
                    SessionKind.Search, found.Items, videoQuality, audioQuality, found.Page));
            });
    }

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
                _show(new Session(SessionKind.Playlist, found.Items, videoQuality, audioQuality));
            });
    }

    internal void OpenChannel(string channelUrl, string title)
        => OpenChannel(new YouTubeResult(string.Empty, title, title, null, channelUrl, channelUrl));

    internal void OpenChannel(YouTubeResult channel)
    {
        if (!Backend.HasComponents
            && _components.Ensure(_settings.YouTube.Channel, () => OpenChannel(channel))
                is not Components.Service.ComponentsState.Ready)
            return;
        var videoQuality = (int)_settings.YouTube.VideoQuality;
        var audioQuality = (int)_settings.YouTube.AudioQuality;
        var channelBase = Client.Tabs.Normalise(channel.Url.Length > 0 ? channel.Url : channel.ChannelUrl);
        var tabIndex = Client.Tabs.DefaultIndex;
        var title = channel.Title;
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while a YouTube channel is being opened.
            Tr("Loading channel"),
            // Translators: First message shown while a YouTube channel is being opened.
            Tr("Loading channel videos..."),
            update => update.Name) { Proportional = false };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (_, token) => _backend.OpenChannelTab(channelBase, Client.Tabs.Keys[tabIndex], token),
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
                _show(new Session(
                    SessionKind.Channel, found.Items, videoQuality, audioQuality,
                    found.Page, channelBase, tabIndex, title));
            });
    }

    internal bool IsFavorite(YouTubeResult item) => _favorites.FindByLink(item.Url) is not null;

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

    internal Backend.ChannelTabResult OpenChannelTab(
        string channelBase,
        string tabKey,
        CancellationToken token)
        => _backend.OpenChannelTab(channelBase, tabKey, token);

    private SearchResults RunSearch(
        string query,
        int filter,
        int count,
        int videoQuality,
        int audioQuality,
        string fetching,
        Action<ProgressUpdate> report,
        CancellationToken token)
    {
        try
        {
            var (items, page) = _client.Search(query, filter, count, token);
            if (items.Count == 0)
                return new SearchResults([], null, ResolveFailure.None, string.Empty);
            if (items[0].ItemType is YouTubeItemType.Video)
            {
                report(new ProgressUpdate(0, 0, fetching));
                _cache.Prefetch(items[0].Url, items[0], true, audioQuality, token);
                _ = _cache.Wait(items[0].Url, items[0], false, videoQuality, token, token);
            }
            return new SearchResults(items, page, ResolveFailure.None, string.Empty);
        }
        catch (Exception failure)
        {
            var explained = FailureMapping.Explain(failure, token);
            return new SearchResults([], null, explained.Failure, explained.Detail);
        }
    }

    private PlaylistResults RunPlaylist(string link, CancellationToken token)
    {
        var (title, items, failure, detail) = _backend.Playlist(link, token);
        return new PlaylistResults(title, items, failure, detail);
    }

    private void ShowError(string message) =>
        // Translators: Title of the messages the player shows about YouTube.
        _view.Dialogs.ShowError(message);

    private readonly record struct SearchResults(
        IReadOnlyList<YouTubeResult> Items, Metadata.Page? Page, ResolveFailure Failure, string Detail);

    private readonly record struct PlaylistResults(
        string Title, IReadOnlyList<YouTubeResult> Items, ResolveFailure Failure, string Detail);
}
