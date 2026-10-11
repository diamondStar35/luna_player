using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.YouTube;
using LunaPlayer.YouTube.Playback;
using WxSharp;

namespace LunaPlayer.Application.Commands.YouTube;

/// <summary>The commands that play, describe and save videos from YouTube.</summary>
///
/// <remarks>
/// Only the commands are here. Anything that outlives one keypress - the list a search returned, what
/// comes after the video playing, what Escape goes back to - belongs to <see cref="Sessions"/>,
/// which this hands to and otherwise leaves alone.
///
/// The three commands that act on the video playing take its address from
/// <see cref="MediaPlayer.CurrentSource"/> rather than from its path. The path is a signed stream address
/// that expires and names no video a person could look at; the source is the watch page it came from.
/// </remarks>
internal sealed partial class YouTube
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IClipboardService _clipboard;
    private readonly Backend _backend;
    private readonly Sessions _sessions;
    private readonly LunaPlayer.YouTube.Components.Service _components;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly MediaGuard _guard;

    internal YouTube(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        ISpeechOutput speech,
        IClipboardService clipboard,
        Backend backend,
        Sessions sessions,
        LunaPlayer.YouTube.Components.Service components,
        IApplicationDispatcher dispatcher)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _speech = speech;
        _clipboard = clipboard;
        _backend = backend;
        _sessions = sessions;
        _components = components;
        _dispatcher = dispatcher;
        _guard = new MediaGuard(player, speech);
        router.Register(ActionId.OpenYouTubeLink, OpenLink);
        router.Register(ActionId.SearchYouTube, Search);
        router.Register(ActionId.VideoDownload, Download);
        router.Register(ActionId.VideoDescription, ShowDescription);
        router.Register(ActionId.VideoCopyLink, CopyLink);
        router.Register(ActionId.VideoOpenInBrowser, OpenCurrentInBrowser);
        router.Register(ActionId.VideoOpenChannelInBrowser, OpenCurrentChannelInBrowser);
        router.Register(ActionId.VideoGoToChannel, GoToCurrentChannel);
        router.Register(ActionId.UpdateYouTubeComponents, components.Update);
    }

    private void OpenLink()
    {
        var typed = _view.PromptText(
            // Translators: Asks the user for the address of the YouTube video or playlist they want to play.
            Tr("Enter a YouTube video or playlist link."),
            // Translators: Title of the window that asks for a YouTube address.
            Tr("Open YouTube Link"));
        if (typed is null)
            return;
        var link = typed.Trim();
        var info = LinkValidator.Parse(link);
        if (!info.IsHttp)
        {
            // Translators: Title of the message shown when what was typed is not a YouTube address.
            ShowError(Tr("The link must start with http or https."), Tr("Invalid link"));
            return;
        }
        if (!info.IsYouTube)
        {
            ShowError(
                // Translators: Shown when the address typed into Open YouTube Link is a web address but not a YouTube one.
                Tr("This is not a valid YouTube link."),
                // Translators: Title of the message shown when a YouTube address was expected and something else was given.
                Tr("Invalid YouTube link"));
            return;
        }
        if (info.Kind == LinkKind.Channel)
        {
            ShowError(
                // Translators: Shown when the address names a whole YouTube channel, which this window cannot open.
                Tr("Channel links are not supported here."), Tr("Invalid YouTube link"));
            return;
        }

        var kind = ChooseKind(info);
        if (kind is LinkKind.Playlist)
            _sessions.OpenPlaylist(link);
        else if (kind is LinkKind.Video)
            _sessions.PlayLink(link);
    }

    /// <summary>Which half of the link to use, when it names a video and a playlist at once.</summary>
    /// <remarks>
    /// Null means the user backed out. An address naming only one of the two answers for itself, and the
    /// setting can answer for the rest, so the window is opened only when there is really a question.
    /// </remarks>
    private LinkKind? ChooseKind(LinkInfo info)
    {
        // A YouTube address that names neither - the bare domain, a /watch with nothing after it - is
        // treated as a video, so it is refused in the words that name what was expected of it. Calling it
        // a playlist would refuse it in the wrong ones.
        if (!info.HasVideo && !info.HasPlaylist)
            return LinkKind.Video;
        if (!info.HasVideo)
            return LinkKind.Playlist;
        if (!info.HasPlaylist)
            return LinkKind.Video;
        return _settings.YouTube.MixedLink switch
        {
            MixedLinkBehavior.Video => LinkKind.Video,
            MixedLinkBehavior.Playlist => LinkKind.Playlist,
            _ => _view.ChooseYouTubeLinkKind() switch
            {
                YouTubeLinkKind.Video => LinkKind.Video,
                YouTubeLinkKind.Playlist => LinkKind.Playlist,
                _ => null,
            },
        };
    }

    private void Search()
    {
        var prompt = new YouTubeSearchPrompt(
            InitialQuery: string.Empty,
            SuggestionsEnabled: _settings.YouTube.SearchSuggestions,
            FetchSuggestions: _backend.Suggestions,
            AnnounceSuggestions: AnnounceSuggestions);
        if (_view.SearchYouTube(prompt) is not { } asked || asked.Query.Length == 0)
            return;
        RunSearch(asked.Query, asked.Filter);
    }

    /// <remarks>
    /// The programs are ensured here, not at the dialog: a search prefetches and plays its first result,
    /// which needs yt-dlp. When they are missing the offer is made and the search picked up again once they
    /// arrive; when it is declined nothing happens, which is the whole of "I would rather not install them".
    /// </remarks>
    private void RunSearch(string query, int filter)
    {
        if (!Backend.HasComponents
            && _components.Ensure(_settings.YouTube.Channel, () => RunSearch(query, filter))
                is not LunaPlayer.YouTube.Components.Service.ComponentsState.Ready)
            return;
        _sessions.Search(query, filter);
    }

    /// <summary>Speaks that suggestions have appeared under the search box, on the UI thread.</summary>
    /// <remarks>Without interrupting, so it does not talk over the word the user is still typing.</remarks>
    private void AnnounceSuggestions() =>
        _speech.Speak(
            // Translators: Spoken when the list of search suggestions appears under the YouTube search box.
            Tr("Search suggestions shown"),
            // Translators: The short wording spoken when search suggestions appear.
            Tr("Suggestions shown"), interrupt: false);

    /// <summary>Whether what is playing came from YouTube, refusing aloud when it did not.</summary>
    /// <remarks>
    /// The three video commands are on a menu that is disabled unless this holds, so reaching them any
    /// other way means a shortcut was pressed. <see cref="MediaGuard"/> handles the case where nothing is
    /// loaded at all, so only the second half is worded here.
    /// </remarks>
    private bool RequireVideo(out string watchUrl)
    {
        watchUrl = string.Empty;
        if (!_guard.RequireFile(out _))
            return false;
        if (_player.CurrentSource is string source && LinkValidator.IsYouTubeUrl(source))
        {
            watchUrl = source;
            return true;
        }
        _speech.Speak(
            // Translators: Spoken when a command that only works on a YouTube video is used on something else.
            Tr("No YouTube video is active."),
            // Translators: The short wording spoken when a YouTube command is used on something that is not one.
            Tr("No YouTube video."));
        return false;
    }

    private void Report(YouTubeOutcome outcome)
    {
        if (!outcome.Success)
            ShowError(outcome.Error, Tr("YouTube"));
    }

    private void ShowError(string message, string caption) => _view.ShowError(message, caption);
}
