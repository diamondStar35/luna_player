using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>The YouTube page of the Preferences window.</summary>
///
/// <remarks>
/// Two of these settings govern the player's own resolver and two govern yt-dlp, which is optional:
/// nothing here needs it until <see cref="YouTubeSettings.UseYtDlp"/> is turned on or a download is
/// asked for. The yt-dlp controls are therefore grouped below the switch that brings them into play,
/// rather than sitting among the settings that always apply.
/// </remarks>
internal sealed class Preferences : UI.Preferences
{
    private readonly YouTubeSettings _settings;
    private readonly CheckBox _audioOnly;
    private readonly Choice _quality;
    private readonly SpinCtrl _resultCount;
    private readonly Choice _mixedLink;
    private readonly CheckBox _useYtDlp;
    private readonly Choice _channel;
    private readonly CheckBox _checkUpdates;

    internal Preferences(Window parent, YouTubeSettings settings, PrefsOps operations)
        // Translators: Spoken description of the YouTube settings page, read when the page is opened.
        : base(new ScrolledWindow(parent), Tr("YouTube settings. Use Tab to move between controls. Press F1 on a specific control to hear detailed help."))
    {
        _settings = settings;
        var panel = (ScrolledWindow)Window;
        panel.SetScrollRate(8, 8);

        // Translators: Tick box on the YouTube settings page: play only the sound of a video, not the picture.
        _audioOnly = new CheckBox(panel, label: Tr("Play videos as audio only")) { Checked = settings.AudioOnly };
        // Translators: Label of the list that chooses how good the picture and sound of a video should be.
        var qualityLabel = new StaticText(panel, label: Tr("Video quality"));
        _quality = Choice(panel, [
            // Translators: One of the video quality settings: the smallest and fastest.
            Tr("Low"),
            // Translators: One of the video quality settings: the middle one.
            Tr("Medium"),
            // Translators: One of the video quality settings: the best the video offers.
            Tr("Best")], (int)settings.Quality);
        // Translators: Label of the box holding how many videos a search should look for.
        var resultCountLabel = new StaticText(panel, label: Tr("Number of search results"));
        _resultCount = new SpinCtrl(panel, settings.SearchResultCount, 5, 100);
        // Translators: Label of the list that chooses what to do with a link naming a video and a playlist at once.
        var mixedLinkLabel = new StaticText(panel, label: Tr("Video+playlist link behavior"));
        _mixedLink = Choice(panel, [
            // Translators: One of the choices for a link naming both: ask which was meant, every time.
            Tr("Ask every time"),
            // Translators: One of the choices for a link naming both: always play the single video.
            Tr("Play the video"),
            // Translators: One of the choices for a link naming both: always open the whole playlist.
            Tr("Open the playlist")], (int)settings.MixedLink);

        // Translators: Tick box on the YouTube settings page: use the separate yt-dlp program to find streams
        // instead of the player's own way of finding them. "yt-dlp" is the name of that program and is not translated.
        _useYtDlp = new CheckBox(panel, label: Tr("Use yt-dlp to resolve streams")) { Checked = settings.UseYtDlp };
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
        // Asked as soon as the box is ticked rather than when a stream is next resolved, so the answer
        // arrives while the user is still looking at the setting that caused the question.
        _useYtDlp.Toggled += (_, _) =>
        {
            if (_useYtDlp.Checked && !operations.EnsureYouTubeComponents(SelectedChannel))
                _useYtDlp.Checked = false;
        };
        // Translators: Tick box on the YouTube settings page: look for a newer yt-dlp each time the player starts.
        // "yt-dlp" is a program name and is not translated.
        _checkUpdates = new CheckBox(panel, label: Tr("Check for yt-dlp updates on startup")) { Checked = settings.CheckComponentUpdates };
        // Translators: Button on the YouTube settings page that fetches the extra programs YouTube downloads need.
        var download = new Button(panel, label: Tr("Download YouTube components"));
        download.Click += (_, _) => operations.DownloadYouTubeComponents(SelectedChannel);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_audioOnly, flags: SizerFlags.All, border: 8);
        AddField(sizer, qualityLabel, _quality);
        AddField(sizer, resultCountLabel, _resultCount);
        AddField(sizer, mixedLinkLabel, _mixedLink);
        sizer.Add(_useYtDlp, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        AddField(sizer, channelLabel, _channel);
        sizer.Add(_checkUpdates, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(download, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        panel.SetSizer(sizer);

        Help(_audioOnly,
            // Translators: Help text for the tick box that plays only the sound of a video.
            Tr("Play videos as audio only. When enabled, only the sound of a video is played, which uses less of the network " +
            "and starts sooner. Disable it to play the picture as well."));
        Help(_quality,
            // Translators: Help text for the list that chooses how good the picture and sound should be. It names the
            // three entries in that list, which should read the same here as they do there.
            Tr("How good the video should be. Low uses the least of the network and starts soonest. Medium is a balance. " +
            "Best uses the finest the video offers, which may take longer to start. " +
            "Possible values: Low, Medium, or Best."));
        Help(_resultCount,
            // Translators: Help text for the box holding how many videos a search should look for. The two numbers are
            // the smallest and largest it accepts.
            Tr("How many videos a search looks for. More takes longer to search and gives a longer list. Reaching the end " +
            "of that list fetches more, whatever this is set to. " +
            "Possible values: 5 to 100."));
        Help(_mixedLink,
            // Translators: Help text for the list that chooses what to do with a link naming a video and a playlist at
            // once. It names the three entries in that list, which should read the same here as they do there.
            Tr("What to do with a link that names a video and a playlist at the same time. Ask every time opens a window " +
            "asking which you meant. Play the video plays the one video. Open the playlist lists every video in it. " +
            "Possible values: Ask every time, Play the video, or Open the playlist."));
        Help(_useYtDlp,
            // Translators: Help text for the tick box that hands stream finding to yt-dlp. "yt-dlp" is a program name
            // and is not translated.
            Tr("Use yt-dlp to resolve streams. When enabled, the separate yt-dlp program finds the sound and picture of a " +
            "video instead of the player doing it itself. yt-dlp has to be downloaded first, which the player offers to do. " +
            "Leave it off unless a video the player cannot play works in yt-dlp."));
        Help(_channel,
            // Translators: Help text for the list that chooses which line of yt-dlp releases to follow. It names the
            // three entries in that list, which should read the same here as they do there. "yt-dlp" is a program name
            // and is not translated.
            Tr("Which line of yt-dlp releases to follow. Stable is tested and changes least often. Nightly is rebuilt each " +
            "night. Master is rebuilt from the latest source and may not work. " +
            "Possible values: Stable, Nightly, or Master."));
        Help(_checkUpdates,
            // Translators: Help text for the tick box that looks for a newer yt-dlp at startup. "yt-dlp" is a program
            // name and is not translated.
            Tr("Check for yt-dlp updates on startup. When enabled, the player looks for a newer yt-dlp each time it starts " +
            "and offers to fetch it. This needs the network, so turn it off to start without one."));
        Help(download,
            // Translators: Help text for the button that fetches the extra programs YouTube downloads need. "yt-dlp" is
            // a program name and is not translated.
            Tr("Download YouTube components. Fetches yt-dlp, which downloading a video needs and which resolving streams " +
            "with yt-dlp needs. Nothing else on this page requires it."));
    }

    /// <summary>The release line the page is showing, which is not the one in the settings until the
    /// window has been accepted.</summary>
    private YtDlpChannel SelectedChannel => (YtDlpChannel)Math.Max(0, _channel.SelectedIndex);

    public override void Apply()
    {
        _settings.AudioOnly = _audioOnly.Checked;
        _settings.Quality = (YouTubeQuality)Math.Max(0, _quality.SelectedIndex);
        _settings.SearchResultCount = _resultCount.Value;
        _settings.MixedLink = (MixedLinkBehavior)Math.Max(0, _mixedLink.SelectedIndex);
        _settings.UseYtDlp = _useYtDlp.Checked;
        _settings.Channel = SelectedChannel;
        _settings.CheckComponentUpdates = _checkUpdates.Checked;
    }

    public override void Refresh()
    {
        _audioOnly.Checked = _settings.AudioOnly;
        _quality.SelectedIndex = (int)_settings.Quality;
        _resultCount.Value = _settings.SearchResultCount;
        _mixedLink.SelectedIndex = (int)_settings.MixedLink;
        _useYtDlp.Checked = _settings.UseYtDlp;
        _channel.SelectedIndex = (int)_settings.Channel;
        _checkUpdates.Checked = _settings.CheckComponentUpdates;
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
