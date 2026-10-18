using LunaPlayer.Configuration.Models;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.YouTube;

/// <summary>The download options dialog. Download or Add to queue returns the filled-in choices.</summary>
internal sealed class DownloadDialog : IDisposable
{
    private static readonly VideoQuality[] VideoQualities =
    [
        VideoQuality.Worst, VideoQuality.P144, VideoQuality.P240, VideoQuality.P360, VideoQuality.P480,
        VideoQuality.P720, VideoQuality.P1080, VideoQuality.P1440, VideoQuality.P2160, VideoQuality.Best,
    ];

    private static readonly AudioQuality[] AudioQualities =
    [
        AudioQuality.Worst, AudioQuality.Kbps64, AudioQuality.Kbps96, AudioQuality.Kbps128, AudioQuality.Kbps160,
        AudioQuality.Kbps192, AudioQuality.Kbps256, AudioQuality.Kbps320, AudioQuality.Best,
    ];

    private static readonly VideoContainer[] Containers = [VideoContainer.Mp4, VideoContainer.Mkv];

    private static readonly AudioFileFormat[] AudioFormats =
        [AudioFileFormat.Mp3, AudioFileFormat.M4a, AudioFileFormat.Opus, AudioFileFormat.Flac, AudioFileFormat.Wav];

    private readonly Dialog _dialog;
    private readonly string _url;
    private readonly bool _splitByType;
    private readonly BoxSizer _form;
    private readonly TextCtrl _fileName;
    private readonly TextCtrl _folder;
    private readonly RadioBox _type;
    private readonly Choice _videoQuality;
    private readonly StaticText _videoQualityLabel;
    private readonly Choice _container;
    private readonly StaticText _containerLabel;
    private readonly Choice _audioFormat;
    private readonly StaticText _audioFormatLabel;
    private readonly Choice _audioQuality;
    private readonly StaticText _audioQualityLabel;
    private readonly SpinCtrl _fragments;
    private readonly CheckBox _embedMetadata;
    private readonly CheckBox _embedThumbnail;
    private readonly CheckBox _embedChapters;
    private readonly CheckBox _embedSubtitles;
    private readonly CheckBox _sponsorBlock;
    private Downloads.OptionsResult? _result;

    internal DownloadDialog(Window parent, Downloads.Request defaults)
    {
        _url = defaults.Url;
        _splitByType = defaults.SplitByType;
        // Translators: Title of the window where a download's name, type, format and quality are chosen.
        _dialog = new Dialog(parent, title: Tr("Download"), style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Label of the box holding the name a download is saved under, without an extension.
        var fileNameLabel = new StaticText(_dialog, label: Tr("File name"));
        _fileName = new TextCtrl(_dialog, value: defaults.FileName);
        // Translators: Heading of the radio group that chooses whether a download saves the video or its sound alone.
        _type = new RadioBox(_dialog, Tr("Download type"), [
            // Translators: One of the download types: save the picture, with its sound, as a video file.
            Tr("Video"),
            // Translators: One of the download types: save the sound alone as an audio file.
            Tr("Audio")]) { SelectedIndex = (int)defaults.Kind };
        _type.SelectionChanged += (_, _) => SyncOptions();
        // Translators: Label of the list that chooses the picture quality a downloaded video is saved at.
        _videoQualityLabel = new StaticText(_dialog, label: Tr("Video quality"));
        _videoQuality = Choice(_dialog,
            Array.ConvertAll(VideoQualities, quality => QualityLabels.VideoLabel((int)quality)),
            Math.Max(0, Array.IndexOf(VideoQualities, defaults.VideoQuality)));
        // Translators: Label of the list that chooses the file type a downloaded video is saved as.
        _containerLabel = new StaticText(_dialog, label: Tr("Video format"));
        // Translators: The two video container formats. "MP4" and "MKV" are format names and are not translated.
        _container = Choice(_dialog, [Tr("MP4"), Tr("MKV")], Math.Max(0, Array.IndexOf(Containers, defaults.Container)));
        // Translators: Label of the list that chooses the file type a downloaded audio track is saved as.
        _audioFormatLabel = new StaticText(_dialog, label: Tr("Audio format"));
        // The audio formats are their own names, not translated.
        _audioFormat = Choice(_dialog, ["MP3", "M4A", "Opus", "FLAC", "WAV"],
            Math.Max(0, Array.IndexOf(AudioFormats, defaults.AudioFormat)));
        // Translators: Label of the list that chooses the bitrate a downloaded audio track is saved at.
        _audioQualityLabel = new StaticText(_dialog, label: Tr("Audio quality"));
        _audioQuality = Choice(_dialog,
            Array.ConvertAll(AudioQualities, quality => QualityLabels.AudioLabel((int)quality)),
            Math.Max(0, Array.IndexOf(AudioQualities, defaults.AudioQuality)));
        // Translators: Label of the box holding the folder a download is saved into.
        var folderLabel = new StaticText(_dialog, label: Tr("Download folder"));
        _folder = new TextCtrl(_dialog, value: defaults.Folder);
        // Translators: Button that opens a folder picker to choose where the download is saved.
        var browse = new Button(_dialog, label: Tr("Browse..."));
        browse.Click += (_, _) => Browse();
        // Translators: Label of the box holding how many pieces of the file are fetched at once.
        var fragmentsLabel = new StaticText(_dialog, label: Tr("Concurrent fragments"));
        _fragments = new SpinCtrl(_dialog, defaults.ConcurrentFragments, 1, 16);
        // Translators: Tick box: write the title, uploader and other tags into the downloaded file.
        _embedMetadata = new CheckBox(_dialog, label: Tr("Embed metadata")) { Checked = defaults.EmbedMetadata };
        // Translators: Tick box: embed the video's thumbnail as cover art in the downloaded file.
        _embedThumbnail = new CheckBox(_dialog, label: Tr("Embed thumbnail")) { Checked = defaults.EmbedThumbnail };
        // Translators: Tick box: write the video's chapter marks into the downloaded file.
        _embedChapters = new CheckBox(_dialog, label: Tr("Embed chapters")) { Checked = defaults.EmbedChapters };
        // Translators: Tick box: embed available subtitles into the downloaded file.
        _embedSubtitles = new CheckBox(_dialog, label: Tr("Embed subtitles")) { Checked = defaults.EmbedSubtitles };
        // Translators: Tick box: cut out SponsorBlock segments. "SponsorBlock" is a service name and is not translated.
        _sponsorBlock = new CheckBox(_dialog, label: Tr("Remove SponsorBlock segments")) { Checked = defaults.SponsorBlockRemove };

        _form = new BoxSizer(Orientation.Vertical);
        AddField(fileNameLabel, _fileName);
        _form.Add(_type, flags: SizerFlags.Expand | SizerFlags.BorderTop, border: 6);
        AddField(_videoQualityLabel, _videoQuality);
        AddField(_containerLabel, _container);
        AddField(_audioFormatLabel, _audioFormat);
        AddField(_audioQualityLabel, _audioQuality);
        AddField(folderLabel, _folder);
        _form.Add(browse, flags: SizerFlags.BorderBottom, border: 6);
        AddField(fragmentsLabel, _fragments);
        foreach (var tick in new[] { _embedMetadata, _embedThumbnail, _embedChapters, _embedSubtitles, _sponsorBlock })
            _form.Add(tick, flags: SizerFlags.BorderTop, border: 4);

        // Translators: The button that starts this download immediately.
        var download = new Button(_dialog, StandardId.Ok, Tr("Download"));
        download.SetDefault();
        download.Click += (_, _) => Accept(addToQueue: false);
        // Translators: The button that saves these choices as an item in the download queue.
        var addToQueue = new Button(_dialog, label: Tr("Add to queue"));
        addToQueue.Click += (_, _) => Accept(addToQueue: true);
        // Translators: The button that closes a window and leaves everything as it was.
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        _dialog.SetEscapeId(StandardId.Cancel);
        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        buttons.Add(download, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(addToQueue, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(cancel);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_form, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 10);
        sizer.Add(buttons, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 10);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(460, 520);
        _dialog.Center(onParent: true);
        _fileName.Focus();
        SyncOptions();
    }

    /// <summary>The request and whether it should be queued, or null when the user cancelled.</summary>
    internal Downloads.OptionsResult? Show()
    {
        _result = null;
        _dialog.ShowModal();
        return _result;
    }

    public void Dispose() => _dialog.Dispose();

    private void Accept(bool addToQueue)
    {
        var folder = _folder.Value.Trim();
        if (folder.Length == 0)
        {
            Wx.MessageBox(
                // Translators: Shown when OK is pressed in the download window with no folder chosen.
                Tr("Choose a folder to download into."),
                // Translators: Title of the warning shown when the download window has no folder chosen.
                Tr("Warning"), MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            _folder.Focus();
            return;
        }
        var request = new Downloads.Request
        {
            Url = _url,
            Kind = (DownloadKind)Math.Max(0, _type.SelectedIndex),
            Folder = folder,
            SplitByType = _splitByType,
            FileName = _fileName.Value.Trim(),
            VideoQuality = VideoQualities[Math.Clamp(_videoQuality.SelectedIndex, 0, VideoQualities.Length - 1)],
            Container = Containers[Math.Clamp(_container.SelectedIndex, 0, Containers.Length - 1)],
            AudioFormat = AudioFormats[Math.Clamp(_audioFormat.SelectedIndex, 0, AudioFormats.Length - 1)],
            AudioQuality = AudioQualities[Math.Clamp(_audioQuality.SelectedIndex, 0, AudioQualities.Length - 1)],
            ConcurrentFragments = _fragments.Value,
            EmbedMetadata = _embedMetadata.Checked,
            EmbedThumbnail = _embedThumbnail.Checked,
            EmbedChapters = _embedChapters.Checked,
            EmbedSubtitles = _embedSubtitles.Checked,
            SponsorBlockRemove = _sponsorBlock.Checked,
        };
        _result = new Downloads.OptionsResult(request, addToQueue);
        _dialog.EndModal(StandardId.Ok);
    }

    private void Browse()
    {
        using var dialog = new DirDialog(
            _dialog,
            // Translators: Title of the window that picks the folder a download is saved into.
            message: Tr("Select the download folder"),
            defaultPath: _folder.Value,
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() == StandardId.Ok)
            _folder.ChangeValue(dialog.Path);
    }

    /// <summary>Shows only the options for the chosen type.</summary>
    private void SyncOptions()
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
        _dialog.Layout();
    }

    private void AddField(StaticText label, Window control)
    {
        _form.Add(label, flags: SizerFlags.BorderTop, border: 6);
        _form.Add(control, flags: SizerFlags.Expand);
    }

    private static Choice Choice(Window parent, IEnumerable<string> values, int selected)
    {
        var choice = new Choice(parent);
        foreach (var value in values)
            choice.Add(value);
        choice.SelectedIndex = selected;
        return choice;
    }
}


