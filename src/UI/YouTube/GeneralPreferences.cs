using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>The general page of the YouTube settings category.</summary>
internal sealed class GeneralPreferences : UI.PreferencesPage
{
    private readonly YouTubeSettings _settings;
    private readonly Choice _videoQuality;
    private readonly Choice _audioQuality;
    private readonly SpinCtrl _resultCount;
    private readonly CheckBox _searchSuggestions;
    private readonly Choice _mixedLink;
    private readonly Choice _channel;
    private readonly CheckBox _checkUpdates;
    private readonly TextCtrl _cookiesPath;
    private readonly TextCtrl _cookiesStatus;

    /// <summary>Whether Luna is set to read cookies live from Firefox rather than from a file. Held apart
    /// from the settings while the page is open, and mutually exclusive with a chosen file: turning one on
    /// turns the other off.</summary>
    private bool _cookiesFromFirefox;

    /// <summary>The picture heights offered, worst to best (with the two prefer-best/worst choices at the
    /// ends), so the list reads the way the download picker's does and the chosen index maps straight back to
    /// a quality.</summary>
    private static readonly VideoQuality[] VideoQualities =
    [
        VideoQuality.Worst,
        VideoQuality.P144, VideoQuality.P240, VideoQuality.P360, VideoQuality.P480,
        VideoQuality.P720, VideoQuality.P1080, VideoQuality.P1440, VideoQuality.P2160,
        VideoQuality.Best,
    ];

    /// <summary>The audio bitrates offered, worst to best, with the two prefer-best/worst choices at the ends.</summary>
    private static readonly AudioQuality[] AudioQualities =
    [
        AudioQuality.Worst,
        AudioQuality.Kbps64, AudioQuality.Kbps96, AudioQuality.Kbps128, AudioQuality.Kbps160,
        AudioQuality.Kbps192, AudioQuality.Kbps256, AudioQuality.Kbps320,
        AudioQuality.Best,
    ];

    internal GeneralPreferences(Window parent, YouTubeSettings settings, PrefsOps operations)
        // Translators: Spoken description of the general YouTube settings page, read when the page is open.
        : base(new ScrolledWindow(parent), Tr("Move through this page with Tab or Shift+Tab. Press F1 while a control is focused to hear what it changes."))
    {
        _settings = settings;
        var panel = (ScrolledWindow)Window;
        panel.SetScrollRate(8, 8);

        // Translators: Label of the list that chooses the picture quality a video is played at.
        var videoQualityLabel = new StaticText(panel, label: Tr("Video quality"));
        _videoQuality = Choice(
            panel,
            Array.ConvertAll(VideoQualities, quality => QualityLabels.VideoLabel((int)quality)),
            Math.Max(0, Array.IndexOf(VideoQualities, settings.VideoQuality)));
        // Translators: Label of the list that chooses the sound quality a video's audio is played at.
        var audioQualityLabel = new StaticText(panel, label: Tr("Audio quality"));
        _audioQuality = Choice(
            panel,
            Array.ConvertAll(AudioQualities, quality => QualityLabels.AudioLabel((int)quality)),
            Math.Max(0, Array.IndexOf(AudioQualities, settings.AudioQuality)));
        // Translators: Label of the box holding how many videos a search should look for.
        var resultCountLabel = new StaticText(panel, label: Tr("Number of search results"));
        _resultCount = new SpinCtrl(panel, settings.SearchResultCount, 5, 100);
        // Translators: Tick box on the YouTube settings page: offer search suggestions as the user types.
        _searchSuggestions = new CheckBox(panel, label: Tr("Show search suggestions while typing")) { Checked = settings.SearchSuggestions };
        // Translators: Label of the list that chooses what to do with a link naming a video and a playlist at once.
        var mixedLinkLabel = new StaticText(panel, label: Tr("Video+playlist link behavior"));
        _mixedLink = Choice(panel, [
            // Translators: One of the choices for a link naming both: ask which was meant, every time.
            Tr("Ask every time"),
            // Translators: One of the choices for a link naming both: always play the single video.
            Tr("Play the video"),
            // Translators: One of the choices for a link naming both: always open the whole playlist.
            Tr("Open the playlist")], (int)settings.MixedLink);

        // The "Cookies" group. yt-dlp can be handed cookies so age-restricted or members-only videos, and
        // videos behind a "confirm you are not a robot" wall, still play and download. The two ways in are
        // mutually exclusive: a file gives a fixed snapshot, Firefox gives whatever the browser holds now.
        // Translators: Heading of the group of settings that give YouTube the browser cookies it may need.
        var cookiesBox = new StaticBoxSizer(new StaticBox(panel, Tr("Cookies")), Orientation.Vertical);
        // Translators: Label of the box showing the cookie file yt-dlp will use, if one was chosen.
        var cookiesPathLabel = new StaticText(panel, label: Tr("Cookie file"));
        _cookiesPath = new TextCtrl(panel, value: settings.CookiesPath);
        // Translators: Button that opens a file picker to choose a cookie file by hand.
        var browse = new Button(panel, label: Tr("Browse..."));
        browse.Click += (_, _) => BrowseForCookies();
        // Translators: Button that switches Luna to reading cookies straight from Firefox each time, with no file.
        // "Firefox" is a program name and is not translated.
        var fromFirefox = new Button(panel, label: Tr("Import from Firefox"));
        fromFirefox.Click += (_, _) => UseFirefoxCookies();
        // Read-only rather than a plain label so a screen reader can read the current state on demand: there
        // is no way to announce a change, so the state has to be somewhere the user can move focus to.
        // Translators: Label of the read-only box that says which cookie source is in use.
        var cookiesStatusLabel = new StaticText(panel, label: Tr("Cookie status"));
        _cookiesStatus = new TextCtrl(panel, style: TextCtrlStyle.ReadOnly);
        _cookiesFromFirefox = settings.CookiesFromFirefox;
        // Typing or pasting a path is choosing a file, so it cannot also be the Firefox mode.
        _cookiesPath.TextChanged += (_, _) =>
        {
            if (_cookiesPath.Value.Length > 0)
                _cookiesFromFirefox = false;
            UpdateCookieStatus();
        };
        cookiesBox.Add(cookiesPathLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        cookiesBox.Add(_cookiesPath, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.Expand, border: 8);
        var cookieButtons = new BoxSizer(Orientation.Horizontal);
        cookieButtons.Add(browse, flags: SizerFlags.BorderRight, border: 6);
        cookieButtons.Add(fromFirefox);
        cookiesBox.Add(cookieButtons, flags: SizerFlags.All, border: 8);
        cookiesBox.Add(cookiesStatusLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        cookiesBox.Add(_cookiesStatus, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);

        // Translators: Label of the list that chooses which line of yt-dlp releases to follow. "yt-dlp" is a
        // program name and is not translated.
        var channelLabel = new StaticText(panel, label: Tr("yt-dlp update channel"));
        _channel = Choice(panel, [
            // Translators: One of the yt-dlp release lines: the tested one.
            Tr("Stable"),
            // Translators: One of the yt-dlp release lines: rebuilt every night.
            Tr("Nightly"),
            // Translators: One of the yt-dlp release lines: rebuilt from the latest source.
            Tr("Master")], (int)settings.Channel);
        // Translators: Tick box on the YouTube settings page: look for a newer yt-dlp each time the player starts.
        // "yt-dlp" is a program name and is not translated.
        _checkUpdates = new CheckBox(panel, label: Tr("Check for yt-dlp updates on startup")) { Checked = settings.CheckComponentUpdates };
        // Translators: Button on the YouTube settings page that fetches the extra programs YouTube downloads need.
        var download = new Button(panel, label: Tr("Download YouTube components"));
        download.Click += (_, _) => operations.DownloadYouTubeComponents(SelectedChannel);

        var sizer = new BoxSizer(Orientation.Vertical);
        AddField(sizer, videoQualityLabel, _videoQuality);
        AddField(sizer, audioQualityLabel, _audioQuality);
        AddField(sizer, resultCountLabel, _resultCount);
        sizer.Add(_searchSuggestions, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        AddField(sizer, mixedLinkLabel, _mixedLink);
        sizer.Add(cookiesBox, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        AddField(sizer, channelLabel, _channel);
        sizer.Add(_checkUpdates, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(download, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        panel.SetSizer(sizer);

        Help(_videoQuality,
            // Translators: Help text for the list that chooses the picture quality a video is played at.
            Tr("Sets the picture height Luna asks for when you play a video with Enter. A lower height starts sooner and uses less network; Luna falls back to the closest the video offers."));
        Help(_audioQuality,
            // Translators: Help text for the list that chooses the sound quality a video's audio is played at.
            Tr("Sets the bitrate Luna asks for when you play a video's sound alone with Ctrl+Enter. Luna falls back to the closest the video offers."));
        Help(_resultCount,
            // Translators: Help text for the box holding how many videos a search should look for. The two numbers are
            // the smallest and largest it accepts.
            Tr("Sets the size of the first result page, from 5 through 100 videos. Larger pages take longer to fetch; reaching the end still loads more results."));
        Help(_searchSuggestions,
            // Translators: Help text for the tick box that offers search suggestions while typing.
            Tr("When checked, a list of completions is offered as you type, fetched from YouTube. When unchecked, no completions are fetched and typing makes no background network requests."));
        Help(_mixedLink,
            // Translators: Help text for the list that chooses what to do with a link naming a video and a playlist at once.
            Tr("For an address containing both a video and a playlist, Luna can ask each time, open only that video, or list the complete playlist."));
        Help(_channel,
            // Translators: Help text for the list that chooses which line of yt-dlp releases to follow. "yt-dlp" is a program name and is not translated.
            Tr("Stable changes least often and is intended for normal use. Nightly is rebuilt each night. Master follows the newest source and may be unreliable."));
        Help(_checkUpdates,
            // Translators: Help text for the tick box that looks for a newer yt-dlp at startup. "yt-dlp" is a program
            // name and is not translated.
            Tr("When checked, Luna looks for a newer yt-dlp when it starts and offers to download it. When unchecked, no update check runs at startup."));
        Help(download,
            // Translators: Help text for the button that fetches the extra programs YouTube downloads need. "yt-dlp" is
            // a program name and is not translated.
            Tr("Fetches yt-dlp and the JavaScript runtime it needs. YouTube playback and downloads do not work until these are installed."));
        Help(_cookiesPath,
            // Translators: Help text for the box holding the cookie file yt-dlp will use.
            Tr("The cookie file, in Netscape format, that YouTube requests are sent with. Use it for age-restricted or members-only videos. Choosing a file turns off reading cookies from Firefox."));
        Help(browse,
            // Translators: Help text for the button that picks a cookie file by hand.
            Tr("Opens a file picker to choose a cookie file saved from your browser. This turns off reading cookies from Firefox."));
        Help(fromFirefox,
            // Translators: Help text for the button that switches to reading cookies from Firefox. "Firefox" is a
            // program name and is not translated.
            Tr("Reads cookies straight from Firefox each time, always up to date, with no file to keep. This clears any cookie file you chose. Firefox must be installed and signed in to YouTube."));
        Help(_cookiesStatus,
            // Translators: Help text for the read-only box that reports which cookie source is in use.
            Tr("Reports whether YouTube requests currently use Firefox cookies, a cookie file, or no cookies at all."));
        UpdateCookieStatus();
    }

    /// <summary>Opens the file picker and, if a file is chosen, uses it and leaves the Firefox mode.</summary>
    private void BrowseForCookies()
    {
        // Translators: File type name in the cookie-file picker on the general YouTube settings page.
        var cookieFiles = Tr("Cookie files");
        // Translators: File type name for showing every file in the cookie-file picker on the general YouTube settings page.
        var allFiles = Tr("All Files");
        using var dialog = new FileDialog(
            Window,
            message: "",
            wildcard: $"{cookieFiles} (*.txt)|*.txt|{allFiles} (*.*)|*.*",
            style: FileDialogStyle.DefaultOpen);
        if (dialog.ShowModal() != StandardId.Ok)
            return;
        _cookiesFromFirefox = false;
        // ChangeValue rather than Value so the programmatic set does not fire the TextChanged handler that
        // would otherwise run before the flag is settled; the state is then refreshed by hand.
        _cookiesPath.ChangeValue(dialog.Path);
        UpdateCookieStatus();
    }

    /// <summary>Switches to reading cookies from Firefox, dropping any file that was chosen.</summary>
    private void UseFirefoxCookies()
    {
        _cookiesFromFirefox = true;
        _cookiesPath.ChangeValue(string.Empty);
        UpdateCookieStatus();
    }

    /// <summary>Puts the current cookie source into words in the read-only status box.</summary>
    private void UpdateCookieStatus()
    {
        var path = _cookiesPath.Value.Trim();
        _cookiesStatus.ChangeValue(_cookiesFromFirefox
            // Translators: Cookie status: Luna is reading cookies straight from Firefox. "Firefox" is a program
            // name and is not translated.
            ? Tr("Using Firefox cookies")
            : path.Length > 0
                // Translators: Cookie status naming the file in use. {path} is a file path.
                ? TrFormat("Using cookie file: {path}", path)
                // Translators: Cookie status: no cookies are sent with YouTube requests.
                : Tr("No cookies"));
    }

    /// <summary>The release line the page is showing, which is not the one in the settings until the
    /// window has been accepted.</summary>
    private YtDlpChannel SelectedChannel => (YtDlpChannel)Math.Max(0, _channel.SelectedIndex);

    public override void Apply()
    {
        _settings.VideoQuality = VideoQualities[Math.Clamp(_videoQuality.SelectedIndex, 0, VideoQualities.Length - 1)];
        _settings.AudioQuality = AudioQualities[Math.Clamp(_audioQuality.SelectedIndex, 0, AudioQualities.Length - 1)];
        _settings.SearchResultCount = _resultCount.Value;
        _settings.SearchSuggestions = _searchSuggestions.Checked;
        _settings.MixedLink = (MixedLinkBehavior)Math.Max(0, _mixedLink.SelectedIndex);
        _settings.Channel = SelectedChannel;
        _settings.CheckComponentUpdates = _checkUpdates.Checked;
        // Mutually exclusive: the Firefox mode ignores any file, so a file is only saved when that mode is off.
        _settings.CookiesFromFirefox = _cookiesFromFirefox;
        _settings.CookiesPath = _cookiesFromFirefox ? string.Empty : _cookiesPath.Value.Trim();
    }

    public override void Refresh()
    {
        _videoQuality.SelectedIndex = Math.Max(0, Array.IndexOf(VideoQualities, _settings.VideoQuality));
        _audioQuality.SelectedIndex = Math.Max(0, Array.IndexOf(AudioQualities, _settings.AudioQuality));
        _resultCount.Value = _settings.SearchResultCount;
        _searchSuggestions.Checked = _settings.SearchSuggestions;
        _mixedLink.SelectedIndex = (int)_settings.MixedLink;
        _channel.SelectedIndex = (int)_settings.Channel;
        _checkUpdates.Checked = _settings.CheckComponentUpdates;
        _cookiesFromFirefox = _settings.CookiesFromFirefox;
        _cookiesPath.ChangeValue(_settings.CookiesFromFirefox ? string.Empty : _settings.CookiesPath);
        UpdateCookieStatus();
    }

    private static Choice Choice(Window parent, IEnumerable<string> values, int selected)
    { var choice = new Choice(parent); foreach (var value in values) choice.Add(value); choice.SelectedIndex = selected; return choice; }

    private static void AddField(BoxSizer sizer, StaticText label, Window control)
    {
        sizer.Add(label,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop,
            border: 8);
        sizer.Add(control,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand,
            border: 8);
    }
}
