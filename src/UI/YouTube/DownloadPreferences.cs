using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>The download page of the YouTube settings category.</summary>
internal sealed class DownloadPreferences : UI.PreferencesPage
{
    private readonly DownloadSettings _settings;
    private readonly TextCtrl _folder;
    private readonly CheckBox _splitByType;
    private readonly RadioBox _type;
    private readonly Choice _videoQuality;
    private readonly Choice _container;
    private readonly Choice _audioFormat;
    private readonly Choice _audioQuality;
    private readonly StaticText _videoQualityLabel;
    private readonly StaticText _containerLabel;
    private readonly StaticText _audioFormatLabel;
    private readonly StaticText _audioQualityLabel;
    private readonly SpinCtrl _concurrentFragments;
    private readonly SpinCtrl _simultaneousDownloads;
    private readonly CheckBox _embedMetadata;
    private readonly CheckBox _embedThumbnail;
    private readonly CheckBox _embedChapters;
    private readonly CheckBox _embedSubtitles;
    private readonly CheckBox _sponsorBlock;

    /// <summary>The picture heights offered, worst to best, including choices without a fixed cap.</summary>
    private static readonly VideoQuality[] VideoQualities =
    [
        VideoQuality.Worst,
        VideoQuality.P144, VideoQuality.P240, VideoQuality.P360, VideoQuality.P480,
        VideoQuality.P720, VideoQuality.P1080, VideoQuality.P1440, VideoQuality.P2160,
        VideoQuality.Best,
    ];

    /// <summary>The audio bitrates offered, worst to best, including choices without a fixed cap.</summary>
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

    internal DownloadPreferences(Window parent, DownloadSettings settings)
        // Translators: Spoken description of the YouTube download settings page, read when the page is open.
        : base(new ScrolledWindow(parent), Tr("Move through this page with Tab or Shift+Tab. Press F1 while a control is focused to hear what it changes."))
    {
        _settings = settings;
        var panel = (ScrolledWindow)Window;
        panel.SetScrollRate(8, 8);

        // Translators: Label of the box holding the folder YouTube downloads are saved into.
        var folderLabel = new StaticText(panel, label: Tr("Download folder"));
        _folder = new TextCtrl(panel, value: settings.Folder);
        // Translators: Button on the YouTube download settings page that opens a folder picker.
        var browse = new Button(panel, label: Tr("Browse..."));
        browse.Click += (_, _) => BrowseForFolder();
        // Translators: Tick box on the YouTube download settings page: sort files into video and audio subfolders.
        _splitByType = new CheckBox(panel, label: Tr("Sort downloads by type")) { Checked = settings.SplitByType };
        // Translators: Heading of the radio group that chooses whether a YouTube download saves video or audio by default.
        _type = new RadioBox(panel, Tr("Default download type"), [
            // Translators: One of the YouTube download types: save the picture and sound as a video file.
            Tr("Video"),
            // Translators: One of the YouTube download types: save the sound alone as an audio file.
            Tr("Audio")]) { SelectedIndex = (int)settings.Kind };
        _type.SelectionChanged += (_, _) => SyncType();

        // Translators: Label of the list that chooses the picture quality for downloaded YouTube videos.
        _videoQualityLabel = new StaticText(panel, label: Tr("Video quality"));
        _videoQuality = Choice(
            panel,
            Array.ConvertAll(VideoQualities, quality => QualityLabels.VideoLabel((int)quality)),
            Math.Max(0, Array.IndexOf(VideoQualities, settings.VideoQuality)));
        // Translators: Label of the list that chooses the file type for downloaded YouTube videos.
        _containerLabel = new StaticText(panel, label: Tr("Video format"));
        _container = Choice(panel, [
            // Translators: Video format offered on the YouTube download settings page. "MP4" is a format name.
            Tr("MP4"),
            // Translators: Video format offered on the YouTube download settings page. "MKV" is a format name.
            Tr("MKV")],
            Math.Max(0, Array.IndexOf(Containers, settings.Container)));
        // Translators: Label of the list that chooses the file type for downloaded YouTube audio.
        _audioFormatLabel = new StaticText(panel, label: Tr("Audio format"));
        _audioFormat = Choice(panel, [
            // Translators: Audio format offered on the YouTube download settings page. "MP3" is a format name.
            Tr("MP3"),
            // Translators: Audio format offered on the YouTube download settings page. "M4A" is a format name.
            Tr("M4A"),
            // Translators: Audio format offered on the YouTube download settings page. "Opus" is a format name.
            Tr("Opus"),
            // Translators: Audio format offered on the YouTube download settings page. "FLAC" is a format name.
            Tr("FLAC"),
            // Translators: Audio format offered on the YouTube download settings page. "WAV" is a format name.
            Tr("WAV")],
            Math.Max(0, Array.IndexOf(AudioFormats, settings.AudioFormat)));
        // Translators: Label of the list that chooses the bitrate for downloaded YouTube audio.
        _audioQualityLabel = new StaticText(panel, label: Tr("Audio quality"));
        _audioQuality = Choice(
            panel,
            Array.ConvertAll(AudioQualities, quality => QualityLabels.AudioLabel((int)quality)),
            Math.Max(0, Array.IndexOf(AudioQualities, settings.AudioQuality)));

        // Less frequently changed download controls remain grouped within the dedicated download page.
        // Translators: Heading of the group of less-used YouTube download options.
        var advancedBox = new StaticBoxSizer(new StaticBox(panel, Tr("Advanced options")), Orientation.Vertical);
        // Translators: Label of the box holding how many pieces of a YouTube file are fetched at once. "yt-dlp" calls these fragments.
        var fragmentsLabel = new StaticText(advancedBox.Box, label: Tr("Concurrent fragments"));
        _concurrentFragments = new SpinCtrl(advancedBox.Box, settings.ConcurrentFragments, 1, 16);
        // Translators: Label of the box holding how many YouTube videos the download queue may fetch at once.
        var simultaneousDownloadsLabel = new StaticText(advancedBox.Box, label: Tr("Simultaneous downloads"));
        _simultaneousDownloads = new SpinCtrl(advancedBox.Box, settings.SimultaneousDownloads, 1, 16);
        // Translators: Tick box on the YouTube download settings page: write details into the downloaded file.
        _embedMetadata = new CheckBox(advancedBox.Box, label: Tr("Embed metadata")) { Checked = settings.EmbedMetadata };
        // Translators: Tick box on the YouTube download settings page: embed the thumbnail as cover art.
        _embedThumbnail = new CheckBox(advancedBox.Box, label: Tr("Embed thumbnail")) { Checked = settings.EmbedThumbnail };
        // Translators: Tick box on the YouTube download settings page: write chapter marks into the downloaded file.
        _embedChapters = new CheckBox(advancedBox.Box, label: Tr("Embed chapters")) { Checked = settings.EmbedChapters };
        // Translators: Tick box on the YouTube download settings page: embed available subtitles.
        _embedSubtitles = new CheckBox(advancedBox.Box, label: Tr("Embed subtitles")) { Checked = settings.EmbedSubtitles };
        // Translators: Tick box on the YouTube download settings page: remove segments identified by SponsorBlock. "SponsorBlock" is a service name.
        _sponsorBlock = new CheckBox(advancedBox.Box, label: Tr("Remove SponsorBlock segments")) { Checked = settings.SponsorBlockRemove };

        AddField(advancedBox, fragmentsLabel, _concurrentFragments);
        AddField(advancedBox, simultaneousDownloadsLabel, _simultaneousDownloads);
        foreach (var tick in new[] { _embedMetadata, _embedThumbnail, _embedChapters, _embedSubtitles, _sponsorBlock })
            advancedBox.Add(tick,
                flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
                border: 8);

        var sizer = new BoxSizer(Orientation.Vertical);
        AddField(sizer, folderLabel, _folder);
        sizer.Add(browse,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
            border: 8);
        sizer.Add(_splitByType,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
            border: 8);
        sizer.Add(_type,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop | SizerFlags.Expand,
            border: 8);
        AddField(sizer, _videoQualityLabel, _videoQuality);
        AddField(sizer, _containerLabel, _container);
        AddField(sizer, _audioFormatLabel, _audioFormat);
        AddField(sizer, _audioQualityLabel, _audioQuality);
        sizer.Add(advancedBox,
            flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop,
            border: 8);
        panel.SetSizer(sizer);

        Help(_folder,
            // Translators: Help text for the box holding the folder YouTube downloads are saved into.
            Tr("The base folder downloads are saved into. When empty, a Luna Player folder in your Downloads is used."));
        Help(browse,
            // Translators: Help text for the button that picks the YouTube download folder.
            Tr("Opens a folder picker to choose where downloads are saved."));
        Help(_splitByType,
            // Translators: Help text for the tick box that sorts YouTube downloads into video and audio subfolders.
            Tr("When checked, downloads are sorted into 'videos' and 'audios' subfolders. When unchecked, they are saved straight into the download folder."));
        Help(_type,
            // Translators: Help text for the control that chooses whether a YouTube download saves video or audio.
            Tr("Whether a direct download saves the video or its sound alone. The normal download dialog opens on this choice and lets you change it per download."));
        Help(_videoQuality,
            // Translators: Help text for the list that chooses the picture quality of downloaded YouTube videos.
            Tr("The picture height a video download is capped at. yt-dlp saves the best quality at or below it."));
        Help(_container,
            // Translators: Help text for the list that chooses a downloaded YouTube video's file type. "MP4" and "MKV" are format names.
            Tr("The file type a downloaded video is saved as. MP4 plays almost everywhere; MKV holds more but is less widely supported."));
        Help(_audioFormat,
            // Translators: Help text for the list that chooses a downloaded YouTube audio track's file type.
            Tr("The file type a downloaded audio track is converted to. MP3 plays almost everywhere; FLAC and WAV are lossless and larger."));
        Help(_audioQuality,
            // Translators: Help text for the list that chooses a downloaded YouTube audio track's bitrate.
            Tr("The bitrate a downloaded audio track is converted at. It has no effect on the lossless formats."));
        Help(_concurrentFragments,
            // Translators: Help text for the box holding how many pieces of a YouTube file are fetched at once.
            Tr("How many pieces of a file yt-dlp fetches at once, from 1 through 16. More can be faster on a fast connection; one is gentlest."));
        Help(_simultaneousDownloads,
            // Translators: Help text for the box holding how many YouTube videos the download queue may fetch at once.
            Tr("How many videos the download queue may download at the same time, from 1 through 16."));
        Help(_embedMetadata,
            // Translators: Help text for the tick box that writes details into a downloaded YouTube file.
            Tr("When checked, the title, uploader and other details are written into the downloaded file."));
        Help(_embedThumbnail,
            // Translators: Help text for the tick box that embeds a YouTube thumbnail as cover art.
            Tr("When checked, the video's thumbnail is embedded as cover art in the downloaded file."));
        Help(_embedChapters,
            // Translators: Help text for the tick box that writes chapter marks into a downloaded YouTube file.
            Tr("When checked, the video's chapter marks are written into the downloaded file, where the format supports them."));
        Help(_embedSubtitles,
            // Translators: Help text for the tick box that embeds subtitles into a downloaded YouTube file.
            Tr("When checked, the video's available subtitles are embedded into the downloaded file."));
        Help(_sponsorBlock,
            // Translators: Help text for the tick box that removes SponsorBlock segments from a YouTube download. "SponsorBlock" is a service name.
            Tr("When checked, SponsorBlock data is used to cut sponsor reads, intros and similar segments out of the download."));
        SyncType();
    }

    /// <summary>Opens the folder picker and puts the chosen folder into the page.</summary>
    private void BrowseForFolder()
    {
        using var dialog = new DirDialog(
            Window,
            // Translators: Title of the window that picks the folder YouTube downloads are saved into.
            message: Tr("Select the download folder"),
            defaultPath: _folder.Value,
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() == StandardId.Ok)
            _folder.ChangeValue(dialog.Path);
    }

    /// <summary>Shows only the format controls that apply to the selected default download type.</summary>
    private void SyncType()
    {
        var video = _type.SelectedIndex == (int)DownloadKind.Video;
        _videoQualityLabel.Show(video);
        _videoQuality.Show(video);
        _containerLabel.Show(video);
        _container.Show(video);
        _audioFormatLabel.Show(!video);
        _audioFormat.Show(!video);
        _audioQualityLabel.Show(!video);
        _audioQuality.Show(!video);
        Window.Layout();
    }

    public override void Apply()
    {
        _settings.Folder = _folder.Value.Trim();
        _settings.SplitByType = _splitByType.Checked;
        _settings.Kind = (DownloadKind)Math.Max(0, _type.SelectedIndex);
        _settings.VideoQuality = VideoQualities[Math.Clamp(_videoQuality.SelectedIndex, 0, VideoQualities.Length - 1)];
        _settings.Container = Containers[Math.Clamp(_container.SelectedIndex, 0, Containers.Length - 1)];
        _settings.AudioFormat = AudioFormats[Math.Clamp(_audioFormat.SelectedIndex, 0, AudioFormats.Length - 1)];
        _settings.AudioQuality = AudioQualities[Math.Clamp(_audioQuality.SelectedIndex, 0, AudioQualities.Length - 1)];
        _settings.ConcurrentFragments = _concurrentFragments.Value;
        _settings.SimultaneousDownloads = _simultaneousDownloads.Value;
        _settings.EmbedMetadata = _embedMetadata.Checked;
        _settings.EmbedThumbnail = _embedThumbnail.Checked;
        _settings.EmbedChapters = _embedChapters.Checked;
        _settings.EmbedSubtitles = _embedSubtitles.Checked;
        _settings.SponsorBlockRemove = _sponsorBlock.Checked;
    }

    public override void Refresh()
    {
        _folder.ChangeValue(_settings.Folder);
        _splitByType.Checked = _settings.SplitByType;
        _type.SelectedIndex = (int)_settings.Kind;
        _videoQuality.SelectedIndex = Math.Max(0, Array.IndexOf(VideoQualities, _settings.VideoQuality));
        _container.SelectedIndex = Math.Max(0, Array.IndexOf(Containers, _settings.Container));
        _audioFormat.SelectedIndex = Math.Max(0, Array.IndexOf(AudioFormats, _settings.AudioFormat));
        _audioQuality.SelectedIndex = Math.Max(0, Array.IndexOf(AudioQualities, _settings.AudioQuality));
        _concurrentFragments.Value = _settings.ConcurrentFragments;
        _simultaneousDownloads.Value = _settings.SimultaneousDownloads;
        _embedMetadata.Checked = _settings.EmbedMetadata;
        _embedThumbnail.Checked = _settings.EmbedThumbnail;
        _embedChapters.Checked = _settings.EmbedChapters;
        _embedSubtitles.Checked = _settings.EmbedSubtitles;
        _sponsorBlock.Checked = _settings.SponsorBlockRemove;
        SyncType();
    }

    private static Choice Choice(Window parent, IEnumerable<string> values, int selected)
    {
        var choice = new Choice(parent);
        foreach (var value in values)
            choice.Add(value);
        choice.SelectedIndex = selected;
        return choice;
    }

    private static void AddField(Sizer sizer, StaticText label, Window control)
    {
        sizer.Add(label,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop,
            border: 8);
        sizer.Add(control,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand,
            border: 8);
    }
}
