using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>The YouTube page of the Preferences window.</summary>
///
/// <remarks>
/// Most of these settings always apply. The last three govern yt-dlp, which the player fetches the first
/// time a YouTube search is run and which every YouTube feature then leans on; they are grouped together
/// below the settings that shape a search and its results.
/// </remarks>
internal sealed class YouTubePreferences : UI.PreferencesPage
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
    private readonly TextCtrl _downloadFolder;
    private readonly CheckBox _splitByType;
    private readonly RadioBox _downloadType;
    private readonly Choice _downloadVideoQuality;
    private readonly Choice _downloadContainer;
    private readonly Choice _downloadAudioFormat;
    private readonly Choice _downloadAudioQuality;
    private readonly StaticText _downloadVideoQualityLabel;
    private readonly StaticText _downloadContainerLabel;
    private readonly StaticText _downloadAudioFormatLabel;
    private readonly StaticText _downloadAudioQualityLabel;
    private readonly SpinCtrl _concurrentFragments;
    private readonly SpinCtrl _simultaneousDownloads;
    private readonly CheckBox _embedMetadata;
    private readonly CheckBox _embedThumbnail;
    private readonly CheckBox _embedChapters;
    private readonly CheckBox _embedSubtitles;
    private readonly CheckBox _sponsorBlock;

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

    /// <summary>The video containers offered, in menu order.</summary>
    private static readonly VideoContainer[] Containers = [VideoContainer.Mp4, VideoContainer.Mkv];

    /// <summary>The audio file formats offered, in menu order.</summary>
    private static readonly AudioFileFormat[] AudioFormats =
        [AudioFileFormat.Mp3, AudioFileFormat.M4a, AudioFileFormat.Opus, AudioFileFormat.Flac, AudioFileFormat.Wav];

    internal YouTubePreferences(Window parent, YouTubeSettings settings, PrefsOps operations)
        // Translators: Spoken description of the YouTube settings page, read when the page is opened.
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

        // "Download settings" group.
        var downloadDefaults = settings.Download;
        // Translators: Heading of the group of settings that control how YouTube videos are downloaded.
        var downloadBox = new StaticBoxSizer(new StaticBox(panel, Tr("Download settings")), Orientation.Vertical);
        // Translators: Label of the box holding the folder downloads are saved into.
        var downloadFolderLabel = new StaticText(downloadBox.Box, label: Tr("Download folder"));
        _downloadFolder = new TextCtrl(downloadBox.Box, value: downloadDefaults.Folder);
        // Translators: Button that opens a folder picker to choose where downloads are saved.
        var browseFolder = new Button(downloadBox.Box, label: Tr("Browse..."));
        browseFolder.Click += (_, _) => BrowseForDownloadFolder();
        // Translators: Tick box: sort downloads into 'videos' and 'audios' subfolders of the download folder.
        _splitByType = new CheckBox(downloadBox.Box, label: Tr("Sort downloads by type")) { Checked = downloadDefaults.SplitByType };
        // Translators: Heading of the radio group that chooses whether a download saves the video or its sound alone by default.
        _downloadType = new RadioBox(downloadBox.Box, Tr("Default download type"), [
            // Translators: One of the download types: save the picture, with its sound, as a video file.
            Tr("Video"),
            // Translators: One of the download types: save the sound alone as an audio file.
            Tr("Audio")]) { SelectedIndex = (int)downloadDefaults.Kind };
        _downloadType.SelectionChanged += (_, _) => SyncDownloadType();
        // Translators: Label of the list that chooses the picture quality a downloaded video is saved at.
        _downloadVideoQualityLabel = new StaticText(downloadBox.Box, label: Tr("Video quality"));
        _downloadVideoQuality = Choice(
            downloadBox.Box,
            Array.ConvertAll(VideoQualities, quality => QualityLabels.VideoLabel((int)quality)),
            Math.Max(0, Array.IndexOf(VideoQualities, downloadDefaults.VideoQuality)));
        // Translators: Label of the list that chooses the file type a downloaded video is saved as.
        _downloadContainerLabel = new StaticText(downloadBox.Box, label: Tr("Video format"));
        // Translators: The two video container formats. "MP4" and "MKV" are format names and are not translated.
        _downloadContainer = Choice(downloadBox.Box, [Tr("MP4"), Tr("MKV")],
            Math.Max(0, Array.IndexOf(Containers, downloadDefaults.Container)));
        // Translators: Label of the list that chooses the file type a downloaded audio track is saved as.
        _downloadAudioFormatLabel = new StaticText(downloadBox.Box, label: Tr("Audio format"));
        // The audio formats are their own names, not translated.
        _downloadAudioFormat = Choice(downloadBox.Box, ["MP3", "M4A", "Opus", "FLAC", "WAV"],
            Math.Max(0, Array.IndexOf(AudioFormats, downloadDefaults.AudioFormat)));
        // Translators: Label of the list that chooses the bitrate a downloaded audio track is saved at.
        _downloadAudioQualityLabel = new StaticText(downloadBox.Box, label: Tr("Audio quality"));
        _downloadAudioQuality = Choice(
            downloadBox.Box,
            Array.ConvertAll(AudioQualities, quality => QualityLabels.AudioLabel((int)quality)),
            Math.Max(0, Array.IndexOf(AudioQualities, downloadDefaults.AudioQuality)));
        AddBoxField(downloadBox, downloadFolderLabel, _downloadFolder);
        downloadBox.Add(browseFolder, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        downloadBox.Add(_splitByType, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        downloadBox.Add(_downloadType, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop | SizerFlags.Expand, border: 8);
        AddBoxField(downloadBox, _downloadVideoQualityLabel, _downloadVideoQuality);
        AddBoxField(downloadBox, _downloadContainerLabel, _downloadContainer);
        AddBoxField(downloadBox, _downloadAudioFormatLabel, _downloadAudioFormat);
        AddBoxField(downloadBox, _downloadAudioQualityLabel, _downloadAudioQuality);

        // "Advanced options" group.
        // Translators: Heading of the group of less-used download options (embedding, fragments, SponsorBlock).
        var advancedBox = new StaticBoxSizer(new StaticBox(panel, Tr("Advanced options")), Orientation.Vertical);
        // Translators: Label of the box holding how many pieces of a file are fetched at once. "yt-dlp" calls
        // these fragments.
        var fragmentsLabel = new StaticText(panel, label: Tr("Concurrent fragments"));
        _concurrentFragments = new SpinCtrl(panel, downloadDefaults.ConcurrentFragments, 1, 16);
        // Translators: Label of the box holding how many videos the download queue may fetch at once.
        var simultaneousDownloadsLabel = new StaticText(panel, label: Tr("Simultaneous downloads"));
        _simultaneousDownloads = new SpinCtrl(panel, downloadDefaults.SimultaneousDownloads, 1, 16);
        // Translators: Tick box: write the title, uploader and other tags into the downloaded file.
        _embedMetadata = new CheckBox(panel, label: Tr("Embed metadata")) { Checked = downloadDefaults.EmbedMetadata };
        // Translators: Tick box: embed the video's thumbnail as cover art in the downloaded file.
        _embedThumbnail = new CheckBox(panel, label: Tr("Embed thumbnail")) { Checked = downloadDefaults.EmbedThumbnail };
        // Translators: Tick box: write the video's chapter marks into the downloaded file.
        _embedChapters = new CheckBox(panel, label: Tr("Embed chapters")) { Checked = downloadDefaults.EmbedChapters };
        // Translators: Tick box: embed available subtitles into the downloaded file.
        _embedSubtitles = new CheckBox(panel, label: Tr("Embed subtitles")) { Checked = downloadDefaults.EmbedSubtitles };
        // Translators: Tick box: cut out SponsorBlock segments (sponsor reads, intros) from the download.
        // "SponsorBlock" is a service name and is not translated.
        _sponsorBlock = new CheckBox(panel, label: Tr("Remove SponsorBlock segments")) { Checked = downloadDefaults.SponsorBlockRemove };
        AddBoxField(advancedBox, fragmentsLabel, _concurrentFragments);
        AddBoxField(advancedBox, simultaneousDownloadsLabel, _simultaneousDownloads);
        foreach (var tick in new[] { _embedMetadata, _embedThumbnail, _embedChapters, _embedSubtitles, _sponsorBlock })
            advancedBox.Add(tick, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);

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
        sizer.Add(downloadBox, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        sizer.Add(advancedBox, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
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
        Help(_downloadFolder,
            // Translators: Help text for the box holding the folder downloads are saved into.
            Tr("The base folder downloads are saved into. When empty, a Luna Player folder in your Downloads is used."));
        Help(_splitByType,
            // Translators: Help text for the tick box that sorts downloads into video and audio subfolders.
            Tr("When checked, downloads are sorted into 'videos' and 'audios' subfolders. When unchecked, they are saved straight into the download folder."));
        Help(browseFolder,
            // Translators: Help text for the button that picks the download folder.
            Tr("Opens a folder picker to choose where downloads are saved."));
        Help(_downloadType,
            // Translators: Help text for the list that chooses whether a download saves video or audio.
            Tr("Whether a direct download saves the video or its sound alone. The normal download dialog opens on this choice and lets you change it per download."));
        Help(_downloadVideoQuality,
            // Translators: Help text for the list that chooses the picture quality a downloaded video is saved at.
            Tr("The picture height a video download is capped at. yt-dlp saves the best quality at or below it."));
        Help(_downloadContainer,
            // Translators: Help text for the list that chooses a downloaded video's file type. "MP4"/"MKV" are not translated.
            Tr("The file type a downloaded video is saved as. MP4 plays almost everywhere; MKV holds more but is less widely supported."));
        Help(_downloadAudioFormat,
            // Translators: Help text for the list that chooses a downloaded audio track's file type.
            Tr("The file type a downloaded audio track is converted to. MP3 plays almost everywhere; FLAC and WAV are lossless and larger."));
        Help(_downloadAudioQuality,
            // Translators: Help text for the list that chooses a downloaded audio track's bitrate.
            Tr("The bitrate a downloaded audio track is converted at. It has no effect on the lossless formats."));
        Help(_concurrentFragments,
            // Translators: Help text for the box holding how many file pieces are fetched at once.
            Tr("How many pieces of a file yt-dlp fetches at once, from 1 through 16. More can be faster on a fast connection; one is gentlest."));
        Help(_simultaneousDownloads,
            // Translators: Help text for the box holding how many videos the download queue may fetch at once.
            Tr("How many videos the download queue may download at the same time, from 1 through 16."));
        Help(_embedMetadata,
            // Translators: Help text for the tick box that writes tags into the downloaded file.
            Tr("When checked, the title, uploader and other details are written into the downloaded file."));
        Help(_embedThumbnail,
            // Translators: Help text for the tick box that embeds the thumbnail as cover art.
            Tr("When checked, the video's thumbnail is embedded as cover art in the downloaded file."));
        Help(_embedChapters,
            // Translators: Help text for the tick box that writes chapter marks into the downloaded file.
            Tr("When checked, the video's chapter marks are written into the downloaded file, where the format supports them."));
        Help(_embedSubtitles,
            // Translators: Help text for the tick box that embeds subtitles into the downloaded file.
            Tr("When checked, the video's available subtitles are embedded into the downloaded file."));
        Help(_sponsorBlock,
            // Translators: Help text for the tick box that removes SponsorBlock segments. "SponsorBlock" is a service name and is not translated.
            Tr("When checked, SponsorBlock data is used to cut sponsor reads, intros and similar segments out of the download."));
        UpdateCookieStatus();
        SyncDownloadType();
    }

    /// <summary>Opens the file picker and, if a file is chosen, uses it and leaves the Firefox mode.</summary>
    private void BrowseForCookies()
    {
        using var dialog = new FileDialog(
            Window,
            message: "",
            wildcard:
                // Translators: The two names in the cookie-file picker's type list. The patterns beside them are
                // literal and must not be translated.
                $"{Tr("Cookie files")} (*.txt)|*.txt|{Tr("All Files")} (*.*)|*.*",
            style: FileDialogStyle.DefaultOpen);
        if (dialog.ShowModal() != StandardId.Ok)
            return;
        _cookiesFromFirefox = false;
        // ChangeValue rather than Value so the programmatic set does not fire the TextChanged handler that
        // would otherwise run before the flag is settled; the state is then refreshed by hand.
        _cookiesPath.ChangeValue(dialog.Path);
        UpdateCookieStatus();
    }

    /// <summary>Opens the folder picker and, if a folder is chosen, puts it in the download-folder box.</summary>
    private void BrowseForDownloadFolder()
    {
        using var dialog = new DirDialog(
            Window,
            // Translators: Title of the window that picks the folder downloads are saved into.
            message: Tr("Select the download folder"),
            defaultPath: _downloadFolder.Value,
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() == StandardId.Ok)
            _downloadFolder.ChangeValue(dialog.Path);
    }

    /// <summary>Shows the video options or the audio options, following the chosen default download type, so
    /// only the ones that apply are on screen and in the tab order.</summary>
    private void SyncDownloadType()
    {
        var video = _downloadType.SelectedIndex == (int)DownloadKind.Video;
        _downloadVideoQualityLabel.Show(video);
        _downloadVideoQuality.Show(video);
        _downloadContainerLabel.Show(video);
        _downloadContainer.Show(video);
        _downloadAudioFormatLabel.Show(!video);
        _downloadAudioFormat.Show(!video);
        _downloadAudioQualityLabel.Show(!video);
        _downloadAudioQuality.Show(!video);
        Window.Layout();
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

        var download = _settings.Download;
        download.Folder = _downloadFolder.Value.Trim();
        download.SplitByType = _splitByType.Checked;
        download.Kind = (DownloadKind)Math.Max(0, _downloadType.SelectedIndex);
        download.VideoQuality = VideoQualities[Math.Clamp(_downloadVideoQuality.SelectedIndex, 0, VideoQualities.Length - 1)];
        download.Container = Containers[Math.Clamp(_downloadContainer.SelectedIndex, 0, Containers.Length - 1)];
        download.AudioFormat = AudioFormats[Math.Clamp(_downloadAudioFormat.SelectedIndex, 0, AudioFormats.Length - 1)];
        download.AudioQuality = AudioQualities[Math.Clamp(_downloadAudioQuality.SelectedIndex, 0, AudioQualities.Length - 1)];
        download.ConcurrentFragments = _concurrentFragments.Value;
        download.SimultaneousDownloads = _simultaneousDownloads.Value;
        download.EmbedMetadata = _embedMetadata.Checked;
        download.EmbedThumbnail = _embedThumbnail.Checked;
        download.EmbedChapters = _embedChapters.Checked;
        download.EmbedSubtitles = _embedSubtitles.Checked;
        download.SponsorBlockRemove = _sponsorBlock.Checked;
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

        var download = _settings.Download;
        _downloadFolder.ChangeValue(download.Folder);
        _splitByType.Checked = download.SplitByType;
        _downloadType.SelectedIndex = (int)download.Kind;
        _downloadVideoQuality.SelectedIndex = Math.Max(0, Array.IndexOf(VideoQualities, download.VideoQuality));
        _downloadContainer.SelectedIndex = Math.Max(0, Array.IndexOf(Containers, download.Container));
        _downloadAudioFormat.SelectedIndex = Math.Max(0, Array.IndexOf(AudioFormats, download.AudioFormat));
        _downloadAudioQuality.SelectedIndex = Math.Max(0, Array.IndexOf(AudioQualities, download.AudioQuality));
        _concurrentFragments.Value = download.ConcurrentFragments;
        _simultaneousDownloads.Value = download.SimultaneousDownloads;
        _embedMetadata.Checked = download.EmbedMetadata;
        _embedThumbnail.Checked = download.EmbedThumbnail;
        _embedChapters.Checked = download.EmbedChapters;
        _embedSubtitles.Checked = download.EmbedSubtitles;
        _sponsorBlock.Checked = download.SponsorBlockRemove;
        SyncDownloadType();
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

    /// <summary>Label-then-control pair, added to a StaticBoxSizer group.</summary>
    private static void AddBoxField(Sizer sizer, StaticText label, Window control)
    {
        sizer.Add(label,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop,
            border: 8);
        sizer.Add(control,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand,
            border: 8);
    }
}
