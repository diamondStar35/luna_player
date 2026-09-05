using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Recording;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>The Recording page of the Preferences window.</summary>
///
/// <remarks>
/// These are the defaults, not the settings of a particular recording. They are what a recording shortcut
/// uses when no sources have been set up - which is the whole of recording for somebody who never opens
/// the recording window - and what that window starts from each time the player is launched. Changing the
/// format in the window for one afternoon does not come back here.
/// </remarks>
internal sealed class RecordingPreferences : Preferences
{
    private readonly RecordingSettings _settings;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly AudioCatalog _catalog;
    private readonly Choice _format;
    private readonly Choice _rate;
    private readonly Choice _channels;
    private readonly StaticText _bitrateLabel;
    private readonly Choice _bitrate;
    private readonly TextCtrl _folder;
    private IReadOnlyList<int> _bitrates = [];

    /// <summary>The rates and channel counts the chosen format can actually be written at. See
    /// <see cref="AudioCatalog.Support"/>: the lists differ per format and are asked for, not assumed.
    /// </summary>
    private FormatSupport _support = new([.. AudioCatalog.SampleRates], [.. AudioCatalog.SampleRates.Select(_ => (IReadOnlyList<int>)new[] { 1, 2 })]);
    private IReadOnlyList<int> _rates = [.. AudioCatalog.SampleRates];
    private IReadOnlyList<int> _channelCounts = [1, 2];

    /// <summary>Set while the two lists are being refilled, so refilling them does not start the work
    /// again through their own change events.</summary>
    private bool _filling;

    internal RecordingPreferences(
        Window parent, RecordingSettings settings, IApplicationDispatcher dispatcher, AudioCatalog catalog)
        // Translators: Spoken description of the Recording settings page, read when the page is opened.
        : base(new ScrolledWindow(parent), Tr("Recording settings. Use Tab to move between controls. Press F1 on a specific control to hear detailed help."))
    {
        _settings = settings;
        _dispatcher = dispatcher;
        _catalog = catalog;
        var panel = (ScrolledWindow)Window;
        panel.SetScrollRate(8, 8);

        // Translators: Label of the list that chooses what a recording is saved as.
        var formatLabel = new StaticText(panel, label: Tr("Audio format"));
        _format = Choice(panel, ["WAV", "MP3", "AAC", "FLAC"], (int)settings.Format);
        // Translators: Label of the list that chooses how many times a second a recording is sampled.
        var rateLabel = new StaticText(panel, label: Tr("Sample rate"));
        _rate = Choice(panel, [.. _rates.Select(Hertz)], RateIndex(_rates, settings.SampleRate));
        // Translators: Label of the list that chooses whether a recording is mono or stereo.
        var channelsLabel = new StaticText(panel, label: Tr("Channels"));
        _channels = Choice(panel, [
            // Translators: One of the channel counts a recording can be made in: one channel.
            Tr("Mono"),
            // Translators: One of the channel counts a recording can be made in: two channels.
            Tr("Stereo")], settings.Channels == 1 ? 0 : 1);
        // Translators: Label of the list that chooses how much room a second of compressed audio may take.
        _bitrateLabel = new StaticText(panel, label: Tr("Audio quality"));
        _bitrate = new Choice(panel);
        // Translators: Label of the box holding the folder recordings are saved into.
        var folderLabel = new StaticText(panel, label: Tr("Recordings folder"));
        _folder = new TextCtrl(panel, value: settings.Folder);
        // Translators: Button that opens a window for choosing the folder recordings are saved into.
        var browse = new Button(panel, label: Tr("Browse..."));
        browse.Click += (_, _) => Browse();
        _format.SelectionChanged += (_, _) => LoadSupport();
        _rate.SelectionChanged += (_, _) => OnRateChanged();
        _channels.SelectionChanged += (_, _) => LoadBitrates();

        var sizer = new BoxSizer(Orientation.Vertical);
        AddField(sizer, formatLabel, _format);
        AddField(sizer, rateLabel, _rate);
        AddField(sizer, channelsLabel, _channels);
        AddField(sizer, _bitrateLabel, _bitrate);
        AddField(sizer, folderLabel, _folder);
        sizer.Add(browse, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        panel.SetSizer(sizer);

        Help(_format,
            // Translators: Help text for the list that chooses what a recording is saved as. It names the
            // four entries in that list, which should read the same here as they do there.
            Tr("What a recording is saved as. WAV keeps everything and takes the most room. FLAC also keeps everything but takes about half as much. MP3 and AAC throw some away to take far less. " +
            "Possible values: WAV, MP3, AAC, or FLAC."));
        Help(_rate,
            // Translators: Help text for the list that chooses how many times a second a recording is sampled.
            Tr("How many times a second the sound is measured. 44100 is what a compact disc uses and is the usual choice. Higher costs room and is rarely audible; lower is for speech where size matters more than quality."));
        Help(_channels,
            // Translators: Help text for the list that chooses whether a recording is mono or stereo. It
            // names the two entries in that list, which should read the same here as they do there.
            Tr("Whether a recording has one channel or two. Mono halves the size and suits a single microphone. Stereo keeps left and right apart, which matters when recording music or anything the computer plays. " +
            "Possible values: Mono or Stereo."));
        Help(_bitrate,
            // Translators: Help text for the list that chooses how much room a second of compressed audio may take.
            Tr("How much room a second of sound may take, for the formats that compress. Higher sounds better and makes a larger file. This does nothing for WAV and FLAC, which keep everything and are not offered a choice."));
        Help(_folder,
            // Translators: Help text for the box holding the folder recordings are saved into.
            Tr("Where recordings are saved. The folder is made if it is not there. Each recording is named after the moment it started, so one can never overwrite another."));
        Help(browse,
            // Translators: Help text for the button that opens a window for choosing the recordings folder.
            Tr("Choose the folder recordings are saved into, rather than typing the path."));

        LoadBitrates();
    }

    public override void Apply()
    {
        _settings.Format = Format();
        _settings.SampleRate = Rate();
        _settings.Channels = Channels();
        if (_bitrates.Count > 0 && _bitrate.SelectedIndex >= 0 && _bitrate.SelectedIndex < _bitrates.Count)
            _settings.Bitrate = _bitrates[_bitrate.SelectedIndex];
        _settings.Folder = _folder.Value.Trim();
    }

    public override void Refresh()
    {
        _format.SelectedIndex = (int)_settings.Format;
        _folder.Value = _settings.Folder;
        LoadSupport();
    }

    public override string? Validate()
    {
        if (_folder.Value.Trim().Length != 0)
            return null;
        _folder.Focus();
        // Translators: Shown when the Recording settings page was left with no folder to save into.
        return Tr("Choose a folder for recordings.");
    }

    /// <summary>Asks Windows what bitrates the chosen format will take, off the UI thread.</summary>
    /// <remarks>
    /// The answer depends on the sample rate and the channel count as well as the format, so it is asked
    /// again whenever any of the three changes rather than being listed once and left.
    /// </remarks>
    private int Rate() => _rates.Count == 0
        ? _settings.SampleRate
        : _rates[Math.Clamp(_rate.SelectedIndex, 0, _rates.Count - 1)];

    private int Channels() => _channelCounts.Count == 0
        ? _settings.Channels
        : _channelCounts[Math.Clamp(_channels.SelectedIndex, 0, _channelCounts.Count - 1)];

    /// <summary>Asks what the chosen format can be written as, and rebuilds the rate list from it.
    /// </summary>
    /// <remarks>
    /// The offered rates are not the same for every format - MP3 stops at 48 kHz, FLAC starts at 44.1,
    /// AAC does 96 kHz in stereo only - so the list follows the format rather than being fixed.
    /// </remarks>
    private void LoadSupport()
    {
        var format = Format();
        var wantedRate = Rate();
        var wantedChannels = Channels();
        _ = Task.Run(() =>
        {
            var support = _catalog.Support(format);
            _dispatcher.Post(() => ApplySupport(support, wantedRate, wantedChannels));
        });
    }

    private void ApplySupport(FormatSupport support, int wantedRate, int wantedChannels)
    {
        _support = support;
        _rates = support.Rates;
        _filling = true;
        _rate.Clear();
        foreach (var rate in _rates)
            _rate.Add(Hertz(rate));
        _rate.SelectedIndex = RateIndex(_rates, wantedRate);
        _filling = false;
        FillChannels(wantedChannels);
    }

    private void OnRateChanged()
    {
        if (_filling)
            return;
        FillChannels(Channels());
    }

    private void FillChannels(int wanted)
    {
        var index = Math.Clamp(_rate.SelectedIndex, 0, Math.Max(0, _support.Rates.Count - 1));
        _channelCounts = _support.Rates.Count == 0 ? [] : _support.Channels[index];
        _filling = true;
        _channels.Clear();
        foreach (var count in _channelCounts)
            _channels.Add(count == 1
                // Translators: One of the channel counts a recording can be made in: one channel.
                ? Tr("Mono")
                // Translators: One of the channel counts a recording can be made in: two channels.
                : Tr("Stereo"));
        var chosen = _channelCounts.ToList().IndexOf(wanted);
        _channels.SelectedIndex = chosen < 0 ? _channelCounts.Count - 1 : chosen;
        _filling = false;
        LoadBitrates();
    }

    private void LoadBitrates()
    {
        if (_filling)
            return;
        var format = Format();
        if (!AudioCatalog.HasBitrate(format))
        {
            _bitrate.Clear();
            _bitrates = [];
            _bitrateLabel.Enabled = false;
            _bitrate.Enabled = false;
            return;
        }
        var rate = Rate();
        var channels = Channels();
        var wanted = _settings.Bitrate;
        _bitrate.Enabled = false;
        _bitrateLabel.Enabled = true;
        _ = Task.Run(() =>
        {
            var rates = _catalog.Bitrates(format, rate, channels);
            _dispatcher.Post(() => FillBitrates(rates, wanted));
        });
    }

    private void FillBitrates(IReadOnlyList<int> rates, int wanted)
    {
        _bitrates = rates;
        _bitrate.Clear();
        foreach (var rate in rates)
            _bitrate.Add(Kilobits(rate));
        if (rates.Count == 0)
            return;
        // The nearest figure to the one already saved, so changing the sample rate does not throw away a
        // bitrate the user chose merely because that exact number is no longer offered.
        var best = 0;
        for (var index = 1; index < rates.Count; index++)
        {
            if (Math.Abs(rates[index] - wanted) < Math.Abs(rates[best] - wanted))
                best = index;
        }
        _bitrate.SelectedIndex = best;
        _bitrate.Enabled = true;
    }

    private void Browse()
    {
        using var dialog = new DirDialog(
            Window,
            // Translators: Title of the window that asks where recordings should be saved.
            message: Tr("Select the folder for recordings"),
            defaultPath: _folder.Value,
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() == StandardId.Ok)
            _folder.Value = dialog.Path;
    }

    private RecordingFormat Format() => (RecordingFormat)Math.Max(0, _format.SelectedIndex);

    /// <summary>Where a rate sits in a list, falling back to the nearest rather than the first: a format
    /// that cannot do 96000 should leave the user on 48000, not on 22050.</summary>
    private static int RateIndex(IReadOnlyList<int> rates, int rate)
    {
        if (rates.Count == 0)
            return -1;
        var best = 0;
        for (var index = 1; index < rates.Count; index++)
        {
            if (Math.Abs(rates[index] - rate) < Math.Abs(rates[best] - rate))
                best = index;
        }
        return best;
    }

    // Translators: A sample rate, as shown in a list. {value} is a number such as 44100.
    private static string Hertz(int value) => TrFormat("{value} Hz", value);

    // Translators: A bitrate, as shown in a list. {value} is a number such as 192.
    private static string Kilobits(int value) => TrFormat("{value} kbps", value / 1000);

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
