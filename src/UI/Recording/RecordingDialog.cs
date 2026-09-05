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
internal sealed class RecordingDialog : IDisposable
{
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
    /// <summary>The one button that starts and stops. See <see cref="SyncButtons"/>.</summary>
    private readonly Button _record;
    private readonly Button _pause;
    private readonly int _inputId = IdManager.NewId();
    private readonly int _outputId = IdManager.NewId();
    private readonly int _processId = IdManager.NewId();
    private readonly Menu _addMenu;
    private IReadOnlyList<int> _bitrates = [];

    /// <summary>The rates and channel counts the chosen format can actually be written at.</summary>
    /// <remarks>
    /// Held rather than asked again on every change: the lists are rebuilt whenever the sample rate moves,
    /// and interrogating an encoder is not something to do from a combo box's event.
    /// </remarks>
    private FormatSupport _support = new([.. AudioCatalog.SampleRates], [.. AudioCatalog.SampleRates.Select(_ => (IReadOnlyList<int>)new[] { 1, 2 })]);
    private IReadOnlyList<int> _rates = [.. AudioCatalog.SampleRates];
    private IReadOnlyList<int> _channelCounts = [1, 2];

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

    internal RecordingDialog(
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
        var browse = new Button(_dialog, label: Tr("Browse..."));
        browse.Click += (_, _) => Browse();
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
        form.Add(browse, flags: SizerFlags.AlignLeft);
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

    // ---- sources ----

    private void Rebuild(string? select = null)
    {
        var selected = select ?? SelectedId();
        // Replaced in one go rather than cleared and refilled. A list being built item by item is read out
        // as it grows, which is noise.
        _list.Set([.. _sources.All.Select(RecordingSources.Describe)]);
        if (_sources.Count == 0)
        {
            SyncSelection();
            SyncButtons();
            return;
        }
        var index = selected is null
            ? 0
            : _sources.All.ToList().FindIndex(source => source.Id == selected);
        _list.SelectedIndex = index < 0 ? 0 : index;
        SyncSelection();
        SyncButtons();
    }

    private string? SelectedId()
        => _list.SelectedIndex >= 0 && _list.SelectedIndex < _sources.Count
            ? _sources.All[_list.SelectedIndex].Id
            : null;

    private RecordingSource? Selected()
        => SelectedId() is string id ? _sources.Find(id) : null;

    /// <summary>Brings the volume slider on to the row the user is on.</summary>
    /// <remarks>
    /// Guarded, because the custom slider raises its change event even when the value is set in code -
    /// which is what lets a screen reader hear it, and would otherwise mean moving through the list wrote
    /// each row's volume on to the row before it.
    /// </remarks>
    private void SyncSelection()
    {
        _syncing = true;
        try
        {
            _volume.Value = Selected()?.Volume ?? 100;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void ApplyVolume()
    {
        if (_syncing)
            return;
        if (Selected() is not RecordingSource source)
            return;
        source.Volume = _volume.Value;
        var index = _list.SelectedIndex;
        _list.SetString(index, RecordingSources.Describe(source));
    }

    private Menu BuildAddMenu()
    {
        var menu = new Menu();
        // Translators: Menu item on the recording window that adds a microphone or line-in as a source.
        menu.Append(_inputId, Tr("Input device..."));
        // Translators: Menu item on the recording window that adds everything a speaker plays as a source.
        menu.Append(_outputId, Tr("System output..."));
        // Translators: Menu item on the recording window that adds one program's sound as a source.
        menu.Append(_processId, Tr("Program..."));
        // Left there but greyed on a Windows too old for it, so the option is visible and its absence
        // explainable rather than simply missing.
        menu.Enable(_processId, AudioCatalog.SupportsProcessCapture);
        return menu;
    }

    private void ShowAddMenu()
    {
        var at = _add.Position;
        _dialog.PopupMenu(_addMenu, new Point(at.X, at.Y + _add.Size.Height));
    }

    private void AddSource(RecordingSourceKind kind)
    {
        if (kind is RecordingSourceKind.Process && !AudioCatalog.SupportsProcessCapture)
        {
            Wx.MessageBox(
                // Translators: Shown when capturing one program's sound is asked for on a version of
                // Windows that cannot do it. Capturing a device still works.
                Tr("This version of Windows cannot capture a single program. Windows 10 version 2004 or later is needed for that; recording from a device still works."),
                Title, MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            return;
        }
        var source = new RecordingSource { Kind = kind, Name = DefaultName(kind) };
        using var dialog = new SourceDialog(_dialog, _dispatcher, _catalog, AddCaption(kind), source);
        if (dialog.Show() is not RecordingSource added)
            return;
        _sources.Add(added);
        Rebuild(added.Id);
    }

    private void EditSelected()
    {
        if (Selected() is not RecordingSource source)
            return;
        // A copy, so backing out of the window leaves the source as it was.
        using var dialog = new SourceDialog(
            _dialog, _dispatcher, _catalog, EditCaption(source.Kind), source.Copy());
        if (dialog.Show() is not RecordingSource edited)
            return;
        _sources.Update(edited);
        Rebuild(edited.Id);
    }

    private void RemoveSelected()
    {
        if (Selected() is not RecordingSource source)
            return;
        if (Wx.MessageBox(
            // Translators: Asks the user to confirm removing a recording source. {name} is what they called it.
            TrFormat("Remove the source '{name}'?", source.Name),
            // Translators: Title of the window that asks the user to confirm removing a recording source.
            Tr("Confirm remove"),
            MessageBoxStyle.YesNo | MessageBoxStyle.IconQuestion, _dialog) != MessageBoxStyle.Yes)
            return;
        _sources.Remove(source.Id);
        Rebuild();
    }

    // ---- settings ----

    private void OnFormatChanged()
    {
        var compressed = AudioCatalog.HasBitrate(Format());
        _bitrateLabel.Enabled = compressed;
        _bitrate.Enabled = compressed;
        LoadSupport();
    }

    /// <summary>Asks what the chosen format can be written as, and rebuilds the rate list from it.
    /// </summary>
    /// <remarks>
    /// The lists are not the same for every format: MP3 stops at 48 kHz, FLAC starts at 44.1, and AAC
    /// does 96 kHz in stereo only. Offering the rest and failing later is what this replaces.
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
        if (_closed)
            return;
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

    /// <summary>Rebuilds the channel list for whichever rate is chosen, then the bitrates for both.
    /// </summary>
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

    /// <summary>Asks Windows what bitrates the chosen format will take, off the UI thread.</summary>
    /// <remarks>
    /// The answer depends on the rate and the channel count as well as the format, so it is asked again
    /// whenever any of the three changes rather than listed once.
    /// </remarks>
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
        var wanted = _sources.Options.Bitrate;
        _bitrate.Enabled = false;
        _ = Task.Run(() =>
        {
            var rates = _catalog.Bitrates(format, rate, channels);
            _dispatcher.Post(() => FillBitrates(rates, wanted));
        });
    }

    private void FillBitrates(IReadOnlyList<int> rates, int wanted)
    {
        if (_closed)
            return;
        _bitrates = rates;
        _bitrate.Clear();
        foreach (var rate in rates)
            _bitrate.Add(Kilobits(rate));
        if (rates.Count == 0)
            return;
        // The nearest offered rate to the one already chosen, so changing the sample rate does not throw
        // away a bitrate the user picked just because that exact figure is no longer on the list.
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
            _dialog,
            // Translators: Title of the window that asks where recordings should be saved.
            message: Tr("Select the folder for recordings"),
            defaultPath: _folder.Value,
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() == StandardId.Ok)
            _folder.Value = dialog.Path;
    }

    private RecordingOptions Options() => new(
        Format(), Rate(), Channels(),
        _bitrates.Count > 0 && _bitrate.SelectedIndex >= 0 && _bitrate.SelectedIndex < _bitrates.Count
            ? _bitrates[_bitrate.SelectedIndex]
            : _sources.Options.Bitrate,
        _folder.Value.Trim());

    private RecordingFormat Format() => (RecordingFormat)Math.Max(0, _format.SelectedIndex);

    private int Rate() => _rates.Count == 0
        ? _sources.Options.SampleRate
        : _rates[Math.Clamp(_rate.SelectedIndex, 0, _rates.Count - 1)];

    private int Channels() => _channelCounts.Count == 0
        ? _sources.Options.Channels
        : _channelCounts[Math.Clamp(_channels.SelectedIndex, 0, _channelCounts.Count - 1)];

    // ---- running ----

    /// <summary>Starts the recording, or ends the one that is running.</summary>
    /// <remarks>
    /// One button and one handler rather than two of each, so pressing it never moves the keyboard
    /// somewhere else: the button the user is on stays the button they are on, and only what it says
    /// changes. A screen reader announces the new name, which is the whole of what happened.
    /// </remarks>
    private void Record()
    {
        // Ignored rather than prevented. See _busy: the button cannot be disabled without taking the
        // keyboard away from it.
        if (_busy)
            return;
        if (_engine.State is not RecordingState.Idle)
        {
            Stop();
            return;
        }
        if (_sources.Count == 0)
        {
            Wx.MessageBox(
                // Translators: Shown when recording was started with nothing set up to record from.
                Tr("Add at least one source before recording."),
                Title, MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            return;
        }
        // Kept, so the settings are still here the next time the window is opened in this session.
        _sources.Options = Options();
        var options = _sources.Options;
        var sources = _sources.All.Select(source => source.Copy()).ToList();
        _busy = true;
        _ = Task.Run(() =>
        {
            try
            {
                var outcome = _engine.Start(options, sources, out var failures);
                _dispatcher.Post(() => Started(outcome, failures));
            }
            catch (Exception failure)
            {
                // Reported, not dropped: a faulted task nobody awaits is collected in silence.
                _dispatcher.Post(() => Started(
                    RecordingOutcome.Failed(RecordingFailure.Failed, failure.Message), []));
            }
        });
    }

    private void Started(RecordingOutcome outcome, IReadOnlyList<string> failures)
    {
        _busy = false;
        if (_closed)
            return;
        SyncButtons();
        if (!outcome.Success)
        {
            Wx.MessageBox(RecordingSources.Describe(outcome.Failure, outcome.Detail), Title,
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
            return;
        }
        if (failures.Count > 0)
        {
            Wx.MessageBox(
                // Translators: Shown when recording started but some sources would not open. {names} is a
                // list of what the user called them, separated by commas.
                TrFormat("Recording started, but these sources could not be opened: {names}",
                    string.Join(", ", failures)),
                Title, MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
        }
    }

    private void TogglePause()
    {
        if (_engine.State is RecordingState.Paused)
            _engine.Resume();
        else
            _engine.Pause();
        SyncButtons();
    }

    private void Stop()
    {
        _busy = true;
        _ = Task.Run(() =>
        {
            try
            {
                var outcome = _engine.Stop();
                _dispatcher.Post(() => Stopped(outcome));
            }
            catch (Exception failure)
            {
                _dispatcher.Post(() => Stopped(
                    RecordingOutcome.Failed(RecordingFailure.Failed, failure.Message)));
            }
        });
    }

    private void Stopped(RecordingOutcome outcome)
    {
        _busy = false;
        if (_closed)
            return;
        SyncButtons();
        if (!outcome.Success)
            Wx.MessageBox(RecordingSources.Describe(outcome.Failure, outcome.Detail), Title,
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
    }

    /// <summary>Puts every control into the state the recorder is in.</summary>
    /// <remarks>
    /// Everything about what is being recorded is fixed once recording starts. The mixer a recording is
    /// built on has no way to take an input away again, so a source cannot be added or removed part way
    /// through, and saying so by greying the buttons is better than refusing afterwards.
    /// </remarks>
    private void SyncButtons()
    {
        var idle = _engine.State is RecordingState.Idle;
        var has = _sources.Count > 0 && _list.SelectedIndex >= 0;
        // The label carries the state, not a second button. Enabled either way: with nothing set up
        // there is nothing to start, but a recording that is running can always be stopped.
        _record.Enabled = idle ? _sources.Count > 0 : true;
        _record.Label = idle
            // Translators: Button that begins recording.
            ? Tr("Start")
            // Translators: Button that ends a recording and closes the file.
            : Tr("Stop");
        _pause.Enabled = !idle;
        _pause.Label = _engine.State is RecordingState.Paused
            // Translators: Button that starts a paused recording again.
            ? Tr("Resume")
            // Translators: Button that holds a recording where it is.
            : Tr("Pause");
        _add.Enabled = idle;
        _edit.Enabled = idle && has;
        _remove.Enabled = idle && has;
        _volume.Enabled = idle && has;
        _format.Enabled = idle;
        _rate.Enabled = idle;
        _channels.Enabled = idle;
        _bitrate.Enabled = idle && AudioCatalog.HasBitrate(Format()) && _bitrates.Count > 0;
        _folder.Enabled = idle;
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
            _dialog.EndModal(StandardId.Cancel);
        else
            args.Skip();
    }

    // ---- wording ----

    private Choice Choice(IEnumerable<string> values, int selected)
    {
        var choice = new Choice(_dialog);
        foreach (var value in values)
            choice.Add(value);
        choice.SelectedIndex = selected;
        return choice;
    }

    private static void Add(FlexGridSizer form, StaticText label, Window control)
    {
        form.Add(label, flags: SizerFlags.AlignCenterVertical);
        form.Add(control, proportion: 1, flags: SizerFlags.Expand);
    }

    /// <summary>Where a rate sits in a list, falling back to the nearest one rather than the first: a
    /// format that cannot do 96000 should land the user on 48000, not on 22050.</summary>
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

    private static string DefaultName(RecordingSourceKind kind) => kind switch
    {
        // Translators: The name a newly added microphone source is given until the user changes it.
        RecordingSourceKind.InputDevice => Tr("Microphone"),
        // Translators: The name a newly added speaker-output source is given until the user changes it.
        RecordingSourceKind.OutputLoopback => Tr("System output"),
        // Translators: The name a newly added program source is given until the user changes it.
        _ => Tr("Program"),
    };

    private static string AddCaption(RecordingSourceKind kind) => kind switch
    {
        // Translators: Title of the window for adding a microphone or line-in as a recording source.
        RecordingSourceKind.InputDevice => Tr("Add input device"),
        // Translators: Title of the window for adding a speaker's output as a recording source.
        RecordingSourceKind.OutputLoopback => Tr("Add system output"),
        // Translators: Title of the window for adding one program's sound as a recording source.
        _ => Tr("Add program"),
    };

    private static string EditCaption(RecordingSourceKind kind) => kind switch
    {
        // Translators: Title of the window for changing a microphone or line-in recording source.
        RecordingSourceKind.InputDevice => Tr("Edit input device"),
        // Translators: Title of the window for changing a speaker-output recording source.
        RecordingSourceKind.OutputLoopback => Tr("Edit system output"),
        // Translators: Title of the window for changing a program recording source.
        _ => Tr("Edit program"),
    };

    // Translators: A sample rate, as shown in a list. {value} is a number such as 44100.
    private static string Hertz(int value) => TrFormat("{value} Hz", value);

    // Translators: A bitrate, as shown in a list. {value} is a number such as 192.
    private static string Kilobits(int value) => TrFormat("{value} kbps", value / 1000);

    private static string Title =>
        // Translators: Title of the messages shown about recording.
        Tr("Recording");
}
