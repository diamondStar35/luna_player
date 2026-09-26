using LunaPlayer.Media;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>The window where files are converted to another audio format.</summary>
///
/// <remarks>
/// The window gathers the work and hands it back; it does not run it. <see cref="Show"/> returns a
/// <see cref="ConversionRequest"/> - the files, the format and options, and where each is to be written -
/// which the caller passes to <see cref="MediaConverter"/> behind a progress window. Keeping the two apart
/// is what lets the conversion outlive the window: the moment Convert is pressed the window closes, and the
/// files are turned into audio behind a progress window of their own, the way a download is.
///
/// The source list is virtual, the way <see cref="OpenedFilesDialog"/> is: a folder sent here from Windows
/// Explorer can hold a hundred thousand files, and a list handed that many strings up front would freeze
/// the window. It is asked for a row's text only as that row is drawn.
///
/// Output options that a container cannot honour are hidden rather than greyed, so a screen reader moving
/// through the window meets only the controls that mean something for the chosen format: no quality for a
/// lossless one, no sample rate for Opus, the Opus mode and profile only for Opus.
/// </remarks>
internal sealed class MediaConverterDialog : IDisposable
{
    // The formats a file can be written to: audio only, video containers left out on purpose. The label is
    // a codec name and an extension, neither of which is translated - they are the names of the things
    // themselves. Sorted by extension, which is how they are shown.
    private static readonly (string Extension, string Label)[] Formats =
    [
        (".aac", "AAC (.aac)"),
        (".ac3", "AC-3 (.ac3)"),
        (".aiff", "AIFF (.aiff)"),
        (".flac", "FLAC (.flac)"),
        (".m4a", "AAC in MP4 (.m4a)"),
        (".mka", "Matroska audio (.mka)"),
        (".mp3", "MP3 (.mp3)"),
        (".oga", "Ogg audio (.oga)"),
        (".ogg", "Ogg Vorbis (.ogg)"),
        (".opus", "Opus (.opus)"),
        (".wav", "WAV (.wav)"),
        (".wma", "Windows Media Audio (.wma)"),
    ];

    // Lowest to highest, the order they are offered in.
    private static readonly int[] SampleRates =
        [8000, 11025, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000];

    // The bitrates offered for the lossy formats that take a plain target rate.
    private static readonly int[] Bitrates = [64, 96, 128, 160, 192, 224, 256, 320];

    // The formats written without a lossy encoder, so there is no quality to choose for them.
    private static readonly HashSet<string> Lossless =
        new(StringComparer.OrdinalIgnoreCase) { ".flac", ".wav", ".aiff" };

    private readonly Dialog _dialog;
    private readonly List<string> _files = [];
    private readonly SourceList _list;
    private readonly Button _addFile;
    private readonly Button _addFolder;
    private readonly Button _remove;
    private readonly Choice _format;
    private readonly StaticText _sampleRateLabel;
    private readonly Choice _sampleRate;
    private readonly Choice _channels;
    private readonly StaticText _qualityLabel;
    private readonly Choice _quality;
    private readonly StaticText _opusModeLabel;
    private readonly Choice _opusMode;
    private readonly StaticText _opusProfileLabel;
    private readonly Choice _opusProfile;
    private readonly SpinCtrl _threads;
    private readonly CheckBox _preserveLocations;
    private readonly StaticText _destinationLabel;
    private readonly TextCtrl _destination;
    private readonly Button _browse;
    private readonly CheckBox _deleteOriginals;
    private readonly Button _convert;

    // What the window was filled in with, built when Convert is pressed and read by Show once the window
    // closes. Null until then, and left null when the window is closed any other way.
    private ConversionRequest? _request;

    // Set once the user picks a format themselves, so adding more files no longer moves the choice for them.
    private bool _formatUserSet;
    // Set while a default format is being chosen in code, so that choice is not mistaken for the user's own
    // and does not itself set _formatUserSet.
    private bool _choosingFormat;

    internal MediaConverterDialog(Window parent, IReadOnlyList<string>? initialFiles = null)
    {
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window where files are converted to another audio format.
            title: Tr("Media converter"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Each label is created immediately before the control it names: Windows reads an unnamed input's
        // accessible name from the child created next to it, so building the inputs first would leave the
        // lists and boxes here unlabelled however the sizer later arranges them.

        // Translators: Title of the group holding the list of files to be converted and its buttons.
        var sourcesBox = new StaticBoxSizer(new StaticBox(_dialog, Tr("Sources")), Orientation.Vertical);
        _list = new SourceList(_dialog, RowText);
        // One unnamed column filling the width, which is what a virtual report list needs.
        _list.InsertColumn(0, string.Empty, 420);
        _list.SetItemCount(0);
        // Translators: Button on the media converter that adds one or more files to the list to convert.
        _addFile = new Button(_dialog, label: Tr("Add file..."));
        // Translators: Button on the media converter that adds every supported file in a folder to the list.
        _addFolder = new Button(_dialog, label: Tr("Add folder..."));
        // Translators: Button on the media converter that takes the selected files back out of the list.
        _remove = new Button(_dialog, label: Tr("Remove"));

        // Translators: Title of the group holding the format the files are converted to and its options.
        var outputBox = new StaticBoxSizer(new StaticBox(_dialog, Tr("Output")), Orientation.Vertical);
        // Translators: Label of the list that chooses the format the files are converted to.
        var formatLabel = new StaticText(_dialog, label: Tr("Convert to"));
        _format = new Choice(_dialog);
        foreach (var (_, label) in Formats)
            _format.Add(label);
        // Translators: Label of the list that chooses the sample rate the converted files are written at.
        _sampleRateLabel = new StaticText(_dialog, label: Tr("Sample rate"));
        _sampleRate = new Choice(_dialog);
        // Translators: First item in a converter list, meaning the file's own value is kept unchanged.
        _sampleRate.Add(Tr("Preserve original"));
        foreach (var rate in SampleRates)
            // Translators: One sample rate in the converter's sample-rate list. {rate} is a number in hertz.
            _sampleRate.Add(TrFormat("{rate} Hz", rate));
        // Translators: Label of the list that chooses how many channels the converted files have.
        var channelsLabel = new StaticText(_dialog, label: Tr("Channels"));
        _channels = new Choice(_dialog);
        _channels.Add(Tr("Preserve original"));
        // Translators: The one-channel choice in the converter's channels list.
        _channels.Add(Tr("Mono"));
        // Translators: The two-channel choice in the converter's channels list.
        _channels.Add(Tr("Stereo"));
        // Translators: Label of the list that chooses the audio quality (bitrate) of the converted files.
        _qualityLabel = new StaticText(_dialog, label: Tr("Quality"));
        _quality = new Choice(_dialog);
        _quality.Add(Tr("Preserve original"));
        foreach (var bitrate in Bitrates)
            // Translators: One bitrate in the converter's quality list. {rate} is a number in kilobits per second.
            _quality.Add(TrFormat("{rate} kbps", bitrate));
        // Translators: Label of the list that chooses whether Opus uses a constant or variable bitrate.
        _opusModeLabel = new StaticText(_dialog, label: Tr("Bitrate mode"));
        _opusMode = new Choice(_dialog);
        // Translators: The Opus bitrate mode that holds one steady rate. Written in full rather than as "CBR".
        _opusMode.Add(Tr("Constant bitrate"));
        // Translators: The Opus bitrate mode that varies the rate with the sound. Written in full rather than as "VBR".
        _opusMode.Add(Tr("Variable bitrate"));
        // Translators: Label of the list that chooses what Opus tunes its variable bitrate for.
        _opusProfileLabel = new StaticText(_dialog, label: Tr("Optimize for"));
        _opusProfile = new Choice(_dialog);
        // Translators: The Opus profile for music and general audio.
        _opusProfile.Add(Tr("Music"));
        // Translators: The Opus profile for speech.
        _opusProfile.Add(Tr("Speech"));
        // Translators: The Opus profile that keeps the delay as short as possible.
        _opusProfile.Add(Tr("Low delay"));
        // Translators: Label of the box that chooses how many files are converted at the same time.
        var threadsLabel = new StaticText(_dialog, label: Tr("Maximum simultaneous conversions"));
        _threads = new SpinCtrl(_dialog, value: 1, minimum: 1, maximum: 32);

        // Translators: Title of the group holding where the converted files are written and what happens to
        // the originals.
        var locationBox = new StaticBoxSizer(new StaticBox(_dialog, Tr("Location")), Orientation.Vertical);
        // Translators: Tick box on the converter. When on, each converted file is written beside the file it
        // came from; when off, they all go to one chosen folder instead.
        _preserveLocations = new CheckBox(_dialog, label: Tr("Preserve original file locations"));
        _preserveLocations.Checked = true;
        // Translators: Label of the box holding the folder the converted files are written to.
        _destinationLabel = new StaticText(_dialog, label: Tr("Destination folder"));
        _destination = new TextCtrl(_dialog, value: DefaultDestination(), style: TextCtrlStyle.MultiLine);
        // Translators: Button on the converter that opens a folder picker for the destination folder.
        _browse = new Button(_dialog, label: Tr("Browse..."));
        // Translators: Tick box on the converter that removes each source file once it has been converted.
        _deleteOriginals = new CheckBox(_dialog, label: Tr("Delete original files after conversion"));

        // Sources group: the list fills the width and grows with the window; its three buttons sit in a row
        // beneath it.
        sourcesBox.Add(_list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        var sourceButtons = new BoxSizer(Orientation.Horizontal);
        sourceButtons.Add(_addFile, flags: SizerFlags.BorderRight, border: 6);
        sourceButtons.Add(_addFolder, flags: SizerFlags.BorderRight, border: 6);
        sourceButtons.Add(_remove);
        sourcesBox.Add(sourceButtons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);

        // Output group: a two-column grid of label then control, the way the recording window lays its form
        // out. The optional rows are added here and shown or hidden by SyncFormatOptions.
        var outputForm = new FlexGridSizer(0, 2, 8, 8);
        outputForm.AddGrowableColumn(1, 1);
        AddRow(outputForm, formatLabel, _format);
        AddRow(outputForm, _sampleRateLabel, _sampleRate);
        AddRow(outputForm, channelsLabel, _channels);
        AddRow(outputForm, _qualityLabel, _quality);
        AddRow(outputForm, _opusModeLabel, _opusMode);
        AddRow(outputForm, _opusProfileLabel, _opusProfile);
        AddRow(outputForm, threadsLabel, _threads);
        outputBox.Add(outputForm, flags: SizerFlags.All | SizerFlags.Expand, border: 8);

        // Location group: the preserve tick box, then the destination label, box and Browse button, then the
        // delete tick box. The middle three are enabled or hidden together with the tick box.
        locationBox.Add(_preserveLocations, flags: SizerFlags.All, border: 8);
        locationBox.Add(_destinationLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        var destinationRow = new BoxSizer(Orientation.Horizontal);
        destinationRow.Add(_destination, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderRight, border: 6);
        destinationRow.Add(_browse, flags: SizerFlags.AlignCenterVertical);
        locationBox.Add(destinationRow, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        locationBox.Add(_deleteOriginals, flags: SizerFlags.All, border: 8);

        // Convert and Close along the bottom. Convert is the default; for now it only closes, because the
        // conversion behind it is not written yet.
        // Translators: Button that starts the conversion and closes the window.
        var convert = new Button(_dialog, label: Tr("Convert"));
        _convert = convert;
        // Translators: Button that closes the media converter without converting anything.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));
        convert.SetDefault();
        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        buttons.Add(convert, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(close);

        var root = new BoxSizer(Orientation.Vertical);
        root.Add(sourcesBox, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        root.Add(outputBox, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.Expand, border: 8);
        root.Add(locationBox, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        root.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        _dialog.SetSizer(root);

        _addFile.Click += (_, _) => AddFiles();
        _addFolder.Click += (_, _) => AddFolder();
        _remove.Click += (_, _) => RemoveSelected();
        // The two together cover every way the selection changes, so the buttons that act on it keep pace.
        _list.ItemSelected += (_, _) => SyncButtons();
        _list.ItemDeselected += (_, _) => SyncButtons();
        _format.SelectionChanged += (_, _) => OnFormatChanged();
        _preserveLocations.Toggled += (_, _) => SyncDestination();
        _browse.Click += (_, _) => BrowseDestination();
        convert.Click += (_, _) => Convert();

        _sampleRate.SelectedIndex = 0;
        _channels.SelectedIndex = 0;
        _quality.SelectedIndex = 0;
        _opusMode.SelectedIndex = 1;
        _opusProfile.SelectedIndex = 0;
        _format.SelectedIndex = 0;
        SyncFormatOptions();
        SyncDestination();
        SyncButtons();

        _dialog.Fit();
        _dialog.MinSize = new Size(560, 620);
        _dialog.Center(onParent: true);
        // Files sent here from Windows Explorer's "Convert with Luna" verb, added after everything is built
        // so the list, the buttons and the default format all fall into step through the ordinary path.
        if (initialFiles is { Count: > 0 })
            AddPaths(initialFiles);
        _list.Focus();
    }
    /// <summary>Shows the window and, once it closes, gives back what was filled in, or null when the user
    /// closed it without starting a conversion. The caller runs the conversion; the window only gathers it.
    /// </summary>
    internal ConversionRequest? Show() => _dialog.ShowModal() == StandardId.Ok ? _request : null;

    public void Dispose() => _dialog.Dispose();

    // The text shown for one row of the source list, asked for only as the row is drawn. The full path is
    // held; only the file name is shown, which is what a screen reader should read out for a list of files.
    private string RowText(int index)
        => index >= 0 && index < _files.Count ? MediaLibrary.DisplayName(_files[index]) : string.Empty;

    // Where converted files go until the user picks somewhere else: a "converted" folder under the player's
    // own folder in the user's Documents. Not created here - nothing is written until a conversion runs.
    private static string DefaultDestination()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Luna Player", "converted");

    private static void AddRow(FlexGridSizer form, StaticText label, Window control)
    {
        form.Add(label, flags: SizerFlags.AlignCenterVertical);
        form.Add(control, proportion: 1, flags: SizerFlags.Expand);
    }

    private void OnFormatChanged()
    {
        if (!_choosingFormat)
            _formatUserSet = true;
        SyncFormatOptions();
    }
    // Opens a file picker that takes several files at once and adds every one chosen. The picker is filtered
    // to the media types the player knows, so nothing that cannot be converted reaches the list.
    private void AddFiles()
    {
        using var dialog = new FileDialog(
            _dialog,
            // Translators: Title of the picker the converter's "Add file" button opens.
            message: Tr("Add files to convert"),
            wildcard: MediaLibrary.DialogWildcard,
            style: FileDialogStyle.Open | FileDialogStyle.Multiple | FileDialogStyle.FileMustExist);
        if (dialog.ShowModal() != StandardId.Ok)
            return;
        AddPaths(dialog.GetPaths());
    }

    // Opens a folder picker and adds every supported file found under it, subfolders included. A folder can
    // hold far more than a file picker ever would, which is the whole reason the source list is virtual.
    private void AddFolder()
    {
        using var dialog = new DirDialog(
            _dialog,
            // Translators: Title of the picker the converter's "Add folder" button opens.
            message: Tr("Add a folder of files to convert"),
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() != StandardId.Ok)
            return;
        AddPaths(MediaLibrary.CollectFiles(dialog.Path, recursive: true));
    }
    // Adds paths not already listed, keeping the order they arrived in. The first file added to an empty list
    // decides the default output format, unless the user has since chosen one for themselves.
    private void AddPaths(IReadOnlyList<string> paths)
    {
        var wasEmpty = _files.Count == 0;
        var seen = new HashSet<string>(_files, StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
            if (seen.Add(path))
                _files.Add(path);
        if (wasEmpty && _files.Count > 0)
            ApplyDefaultFormat(_files[0]);
        RefreshList();
    }

    // Takes the selected files back out. Removing from the highest index down leaves the lower indices, which
    // have not yet been touched, still pointing at the same files.
    private void RemoveSelected()
    {
        var indices = _list.GetSelectedIndices();
        Array.Sort(indices);
        for (var i = indices.Length - 1; i >= 0; i--)
            _files.RemoveAt((int)indices[i]);
        RefreshList();
    }

    // Tells the virtual list how many rows it now has - which is enough to redraw it, since it asks for each
    // visible row's text itself - and brings the buttons back into step with the new count.
    private void RefreshList()
    {
        _list.SetItemCount(_files.Count);
        SyncButtons();
    }
    // Moves the format choice to the one that fits the first file added: its own extension when that is an
    // audio format we can write, MP3 otherwise - which is the case for a video file, whose audio we keep. Does
    // nothing once the user has picked a format, so their choice is never overwritten by adding more files.
    private void ApplyDefaultFormat(string firstAddedPath)
    {
        if (_formatUserSet)
            return;
        var extension = Path.GetExtension(firstAddedPath);
        var index = Array.FindIndex(Formats, f => f.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            index = Array.FindIndex(Formats, f => f.Extension == ".mp3");
        // Guarded so the change made here is not taken for the user's own and left stuck for good.
        _choosingFormat = true;
        _format.SelectedIndex = index;
        _choosingFormat = false;
        // A programmatic selection does not always raise the changed event, so the options are synced by hand.
        SyncFormatOptions();
    }

    // Shows only the options the chosen format can honour, and hides the rest so a screen reader moving through
    // the window never meets a control that means nothing: no quality for a lossless format, no sample rate for
    // Opus - which is fixed at 48 kHz and offers no way to change it - and the Opus-only bitrate mode and
    // profile for Opus alone.
    private void SyncFormatOptions()
    {
        var extension = Formats[_format.SelectedIndex].Extension;
        var lossless = Lossless.Contains(extension);
        var opus = extension.Equals(".opus", StringComparison.OrdinalIgnoreCase);
        _sampleRateLabel.Show(!opus);
        _sampleRate.Show(!opus);
        _qualityLabel.Show(!lossless);
        _quality.Show(!lossless);
        _opusModeLabel.Show(opus);
        _opusMode.Show(opus);
        _opusProfileLabel.Show(opus);
        _opusProfile.Show(opus);
        _dialog.Layout();
    }
    // Shows the destination folder box and its Browse button only when the files are not being written beside
    // the ones they came from. With the box hidden, a screen reader skips straight past a control that would
    // have no bearing on where anything ends up.
    private void SyncDestination()
    {
        var custom = !_preserveLocations.Checked;
        _destinationLabel.Show(custom);
        _destination.Show(custom);
        _browse.Show(custom);
        _dialog.Layout();
    }

    // Remove acts on the selection, Convert on the list as a whole, so each is offered only when it has
    // something to work on.
    private void SyncButtons()
    {
        _remove.Enabled = _list.SelectedCount > 0;
        _convert.Enabled = _files.Count > 0;
    }

    // Opens a folder picker starting at whatever the box already holds, and writes the chosen folder back into
    // it. Only the box is touched; nothing is created until a conversion runs.
    private void BrowseDestination()
    {
        using var dialog = new DirDialog(
            _dialog,
            // Translators: Title of the picker the converter's "Browse" button opens for the destination folder.
            message: Tr("Choose where converted files are written"),
            defaultPath: _destination.Value,
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() == StandardId.Ok)
            _destination.Value = dialog.Path;
    }

    // Builds what the window was filled in with and closes it, handing the work to whoever opened it. Nothing
    // is converted here: gathering the request and running it are kept apart, so the window is free the moment
    // Convert is pressed and the conversion runs behind its own progress window, the way a download does.
    private void Convert()
    {
        var extension = Formats[_format.SelectedIndex].Extension;
        var preserve = _preserveLocations.Checked;
        var folder = _destination.Value.Trim();
        // A destination is needed only when the files are not written beside their sources. It is created now
        // rather than during the conversion so a folder that cannot be made is reported over this window,
        // where the user can still put it right, rather than failing every file once the window has gone.
        if (!preserve)
        {
            if (folder.Length == 0)
            {
                Warn(
                    // Translators: Shown when Convert is pressed with no destination folder chosen and files
                    // are not being written beside their sources.
                    Tr("Choose a folder to write the converted files to, or turn on \"Preserve original file locations\"."));
                return;
            }
            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException
                or ArgumentException or NotSupportedException or System.Security.SecurityException)
            {
                Warn(TrFormat(
                    // Translators: Shown when the chosen destination folder could not be created. {reason} is
                    // what went wrong, in the language of the system rather than the player.
                    "{message}\n{reason}", Tr("The destination folder could not be created."), failure.Message));
                return;
            }
        }

        var jobs = new List<ConversionJob>(_files.Count);
        foreach (var source in _files)
        {
            var directory = preserve ? Path.GetDirectoryName(source) ?? folder : folder;
            var name = Path.GetFileNameWithoutExtension(source) + extension;
            jobs.Add(new ConversionJob(source, Path.Combine(directory, name)));
        }
        _request = new ConversionRequest(
            jobs, BuildSettings(extension), _threads.Value, _deleteOriginals.Checked,
            Formats[_format.SelectedIndex].Label);
        _dialog.EndModal(StandardId.Ok);
    }

    // The encoder settings for the chosen format: which encoder writes it, and the sample rate, channel count
    // and bitrate to write it at - each null when the file's own value is kept. Lossless formats carry no
    // bitrate, and Opus carries no sample rate, matching the controls SyncFormatOptions leaves on screen.
    private ConversionSettings BuildSettings(string extension)
    {
        var lossless = Lossless.Contains(extension);
        var opus = extension.Equals(".opus", StringComparison.OrdinalIgnoreCase);
        int? sampleRate = opus || _sampleRate.SelectedIndex == 0 ? null : SampleRates[_sampleRate.SelectedIndex - 1];
        int? channels = _channels.SelectedIndex switch { 1 => 1, 2 => 2, _ => null };
        int? bitrate = lossless || _quality.SelectedIndex == 0 ? null : Bitrates[_quality.SelectedIndex - 1];
        var opusSettings = opus
            ? new OpusSettings(
                VariableBitrate: _opusMode.SelectedIndex == 1,
                // Music tunes for general audio, speech for voice, low delay for the shortest latency; these
                // are the three yt-dlp-independent applications libopus takes.
                Application: _opusProfile.SelectedIndex switch { 1 => "voip", 2 => "lowdelay", _ => "audio" })
            : (OpusSettings?)null;
        return new ConversionSettings(CodecFor(extension), sampleRate, channels, bitrate, opusSettings);
    }

    // The ffmpeg encoder that writes each format. Named rather than left to ffmpeg's own default for the
    // container, so the same choice writes the same thing however the file is named, and so the bundled build's
    // libmp3lame, libvorbis and libopus are used rather than any weaker encoder that shares their container.
    private static string CodecFor(string extension) => extension switch
    {
        ".mp3" => "libmp3lame",
        ".aac" or ".m4a" => "aac",
        ".ac3" => "ac3",
        ".aiff" => "pcm_s16be",
        ".wav" => "pcm_s16le",
        ".flac" => "flac",
        ".ogg" or ".oga" => "libvorbis",
        ".opus" => "libopus",
        ".wma" => "wmav2",
        ".mka" => "libvorbis",
        _ => "libmp3lame",
    };

    private void Warn(string message) => Wx.MessageBox(
        message,
        // Translators: Title of the messages the media converter shows.
        Tr("Media converter"),
        MessageBoxStyle.Ok | MessageBoxStyle.IconWarning,
        _dialog);

    /// <summary>The list of files waiting to be converted, which holds none of them.</summary>
    ///
    /// <remarks>
    /// Virtual, the way <see cref="OpenedFilesDialog"/>'s list is: a folder sent here can hold a hundred
    /// thousand files, and a list handed that many strings up front would freeze the window as it opened. It
    /// is asked for a row's text only as that row is drawn, so only what is on screen costs anything.
    /// </remarks>
    private sealed class SourceList(Window parent, Func<int, string> rowText) : ListCtrl(parent,
        style: ListCtrlStyle.Report | ListCtrlStyle.Virtual | ListCtrlStyle.NoHeader)
    {
        // Called while the control is painting, so it does no more than look the text up.
        protected override string OnGetItemText(long item, int column) => rowText((int)item);
    }
}





