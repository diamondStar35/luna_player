using LunaPlayer.Application;
using LunaPlayer.Recording;
using WxSharp;

namespace LunaPlayer.UI.Recording;

/// <summary>The window where recording is set up and run.</summary>
///
/// <remarks>
/// Two groups: what to record from, and how to write it. Closing the window does not end a recording -
/// the sources and the recorder belong to the application, not to this - so somebody can set a recording
/// going, close this, and carry on using the player.
///
/// Every row of the source list is a whole sentence rather than a set of columns. A screen reader reads a
/// row; after reading one the user should know what that source is and how loud it is without having to
/// go and find the rest of it somewhere else on the window.
/// </remarks>
internal sealed partial class MainDialog : IDisposable
{
    private readonly EncoderChoices _choices = new();
    private readonly SessionController _session;
    private readonly Dialog _dialog;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly AudioCatalog _catalog;
    private readonly RecordingSources _sources;
    private readonly RecordingEngine _engine;
    private readonly ListBox _list;
    private readonly Button _add;
    private readonly Button _edit;
    private readonly Button _remove;
    private readonly CustomSlider _volume;
    private readonly Choice _format;
    private readonly Choice _rate;
    private readonly Choice _channels;
    private readonly StaticText _bitrateLabel;
    private readonly Choice _bitrate;
    private readonly TextCtrl _folder;
    private readonly Button _browse;
    /// <summary>The one button that starts and stops. See <see cref="SyncButtons"/>.</summary>
    private readonly Button _record;
    private readonly Button _pause;
    private readonly int _inputId = IdManager.NewId();
    private readonly int _outputId = IdManager.NewId();
    private readonly int _processId = IdManager.NewId();
    private readonly Menu _addMenu;
    private IReadOnlyList<int> _bitrates { get => _choices.Bitrates; set => _choices.Bitrates = value; }

    /// <summary>The rates and channel counts the chosen format can actually be written at.</summary>
    /// <remarks>
    /// Held rather than asked again on every change: the lists are rebuilt whenever the sample rate moves,
    /// and interrogating an encoder is not something to do from a combo box's event.
    /// </remarks>
    private FormatSupport _support { get => _choices.Support; set => _choices.Support = value; }
    private IReadOnlyList<int> _rates { get => _choices.Rates; set => _choices.Rates = value; }
    private IReadOnlyList<int> _channelCounts { get => _choices.ChannelCounts; set => _choices.ChannelCounts = value; }

    /// <summary>Set while the rate and channel lists are being refilled, so the changes that refilling
    /// them raises do not each start the work again.</summary>
    private bool _filling;
    private bool _syncing;
    private bool _closed;

    /// <summary>Set while a start or a stop is still running on a worker.</summary>
    /// <remarks>
    /// A flag rather than a disabled button. Disabling the control the user is standing on makes the
    /// toolkit move the keyboard to the next one in the tab order - which is how pressing Start used to
    /// land the user on Close - so the button stays enabled and simply ignores a second press.
    /// </remarks>
    private bool _busy;

    internal MainDialog(
        Window parent,
        IApplicationDispatcher dispatcher,
        AudioCatalog catalog,
        RecordingSources sources,
        RecordingEngine engine)
    {
        _dispatcher = dispatcher;
        _catalog = catalog;
        _sources = sources;
        _engine = engine;
        _session = new SessionController(engine, dispatcher);
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window where recording is set up and started.
            title: Tr("Recording"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // ---- sources ----
        // Translators: Title of the group of the recording window holding what to record from.
        var sourcesBox = new StaticBoxSizer(new StaticBox(_dialog, Tr("Sources")), Orientation.Vertical);
        _list = new ListBox(_dialog);
        // Translators: Button that adds a recording source. It opens a menu of the kinds that can be added.
        _add = new Button(_dialog, label: Tr("Add"));
        // Translators: Button that changes the recording source chosen in the list.
        _edit = new Button(_dialog, label: Tr("Edit..."));
        // Translators: Button that removes the recording source chosen in the list. It asks first.
        _remove = new Button(_dialog, label: Tr("Remove..."));
        _add.Click += (_, _) => ShowAddMenu();
        _edit.Click += (_, _) => EditSelected();
        _remove.Click += (_, _) => RemoveSelected();
        _list.ItemActivated += (_, _) => EditSelected();
        _list.SelectionChanged += (_, _) => SyncSelection();

        var sourceButtons = new BoxSizer(Orientation.Horizontal);
        sourceButtons.Add(_add, flags: SizerFlags.BorderRight, border: 6);
        sourceButtons.Add(_edit, flags: SizerFlags.BorderRight, border: 6);
        sourceButtons.Add(_remove);

        // Translators: Label of the slider that sets how loud the chosen recording source is in the mix.
        var volumeLabel = new StaticText(_dialog, label: Tr("Volume for selected source"));
        // No label beside it showing the figure: the slider reports its own value, so a label would only
        // say the same thing again and be read out twice. The row in the list carries the figure for the
        // sources that are not selected.
        _volume = new CustomSlider(_dialog, value: 100, minValue: 0, maxValue: 100);
        _volume.ValueChanged += (_, _) => ApplyVolume();

        sourcesBox.Add(_list, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        sourcesBox.Add(sourceButtons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sourcesBox.Add(volumeLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        sourcesBox.Add(_volume, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);

        _addMenu = BuildAddMenu();
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => AddSource(RecordingSourceKind.InputDevice), _inputId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => AddSource(RecordingSourceKind.OutputLoopback), _outputId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => AddSource(RecordingSourceKind.Process), _processId);

        // ---- settings ----
        // Translators: Title of the group of the recording window holding how a recording is written.
        var settingsBox = new StaticBoxSizer(new StaticBox(_dialog, Tr("Recording settings")), Orientation.Vertical);
        // Translators: Label of the list that chooses what a recording is saved as.
        var formatLabel = new StaticText(_dialog, label: Tr("Format"));
        _format = Choice(["WAV", "MP3", "AAC", "FLAC"], (int)_sources.Options.Format);
        // Translators: Label of the list that chooses how many times a second a recording is sampled.
        var rateLabel = new StaticText(_dialog, label: Tr("Sample rate"));
        _rate = Choice([.. _rates.Select(Hertz)], RateIndex(_rates, _sources.Options.SampleRate));
        // Translators: Label of the list that chooses whether a recording is mono or stereo.
        var channelsLabel = new StaticText(_dialog, label: Tr("Channels"));
        _channels = Choice([
            // Translators: One of the channel counts a recording can be made in: one channel.
            Tr("Mono"),
            // Translators: One of the channel counts a recording can be made in: two channels.
            Tr("Stereo")], _sources.Options.Channels == 1 ? 0 : 1);
        // Translators: Label of the list that chooses how much room a second of compressed audio may take.
        _bitrateLabel = new StaticText(_dialog, label: Tr("Bitrate"));
        _bitrate = new Choice(_dialog);
        // Translators: Label of the box holding the folder recordings are saved into.
        var folderLabel = new StaticText(_dialog, label: Tr("Folder"));
        _folder = new TextCtrl(_dialog, value: _sources.Options.Folder);
        // Translators: Button that opens a window for choosing the folder recordings are saved into.
        _browse = new Button(_dialog, label: Tr("Browse..."));
        _browse.Click += (_, _) => Browse();
        _format.SelectionChanged += (_, _) => OnFormatChanged();
        _rate.SelectionChanged += (_, _) => OnRateChanged();
        _channels.SelectionChanged += (_, _) => LoadBitrates();

        var form = new FlexGridSizer(0, 2, 8, 8);
        form.AddGrowableColumn(1, 1);
        Add(form, formatLabel, _format);
        Add(form, rateLabel, _rate);
        Add(form, channelsLabel, _channels);
        Add(form, _bitrateLabel, _bitrate);
        Add(form, folderLabel, _folder);
        form.AddSpacer(0);
        form.Add(_browse, flags: SizerFlags.AlignLeft);
        settingsBox.Add(form, flags: SizerFlags.All | SizerFlags.Expand, border: 8);

        // ---- the buttons that run it ----
        // Translators: Button that begins recording, and ends it when pressed a second time.
        _record = new Button(_dialog, label: Tr("Start"));
        // Translators: Button that holds a recording where it is, and starts it again when pressed a second time.
        _pause = new Button(_dialog, label: Tr("Pause"));
        // Translators: The button that closes a window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));
        _record.Click += (_, _) => Record();
        _pause.Click += (_, _) => TogglePause();

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(_record, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_pause);
        buttons.AddStretchSpacer();
        buttons.Add(close);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(sourcesBox, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        sizer.Add(settingsBox, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.Expand, border: 8);
        sizer.Add(buttons, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(680, 560);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);

        Rebuild();
        // The lists are built from the full offered set above; this narrows them to what the chosen
        // format can actually be written as, and fills the bitrates once it has.
        LoadSupport();
        SyncButtons();
        _list.Focus();
    }

    internal void Show() => _dialog.ShowModal();

    /// <remarks>
    /// The ids the add menu uses are not released. Putting one on a menu hands the reservation to a
    /// reference counter inside wxWidgets, and destroying the menu gives it back - so releasing it here as
    /// well would be a second free, which a debug build asserts on.
    /// </remarks>
    public void Dispose()
    {
        _closed = true;
        _addMenu.Dispose();
        _dialog.Dispose();
    }

    private static string Title =>
        // Translators: Title of the messages shown about recording.
        Tr("Recording");
}
