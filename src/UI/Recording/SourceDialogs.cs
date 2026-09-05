using LunaPlayer.Application;
using LunaPlayer.Recording;
using WxSharp;

namespace LunaPlayer.UI.Recording;

/// <summary>The window for adding or changing one recording source.</summary>
///
/// <remarks>
/// One class for all three kinds, because they differ by two rows: a device source names a device, a
/// program source names a program and can be turned inside out. Three near-identical windows would drift
/// apart, and the user meets them one at a time anyway - what they see is "Add input device" or "Add
/// program", which is the caption rather than the class.
///
/// The list of devices or programs is fetched on a worker thread. Opening every audio endpoint to ask its
/// name takes long enough to be felt, and it is not worth holding the window shut for: the box says it is
/// loading and fills itself in when the answer arrives.
/// </remarks>
internal sealed class SourceDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly AudioCatalog _catalog;
    private readonly RecordingSource _source;
    private readonly TextCtrl _name;
    private readonly Choice _target;
    private readonly CheckBox? _others;
    private readonly CustomSlider _volume;
    private IReadOnlyList<AudioDeviceChoice> _devices = [];
    private IReadOnlyList<ProcessChoice> _processes = [];
    private bool _closed;

    /// <param name="caption">"Add input device", "Edit program", and so on - which is the only thing that
    /// tells the user whether they are adding or changing.</param>
    /// <param name="source">The source being edited. A copy, so backing out changes nothing.</param>
    internal SourceDialog(
        Window parent,
        IApplicationDispatcher dispatcher,
        AudioCatalog catalog,
        string caption,
        RecordingSource source)
    {
        _dispatcher = dispatcher;
        _catalog = catalog;
        _source = source;
        _dialog = new Dialog(parent, title: caption, style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Label of the box holding what the user calls a recording source.
        var nameLabel = new StaticText(_dialog, label: Tr("Name"));
        _name = new TextCtrl(_dialog, value: source.Name);
        var targetLabel = new StaticText(_dialog, label: source.Kind is RecordingSourceKind.Process
            // Translators: Label of the list that chooses which program a recording source captures.
            ? Tr("Program")
            // Translators: Label of the list that chooses which device a recording source captures.
            : Tr("Device"));
        _target = new Choice(_dialog);
        // Something has to be in the list before the real answer arrives, or a screen reader lands on an
        // empty box with nothing to say about it.
        // Translators: Shown in a list while what belongs in it is still being looked up.
        _target.Add(Tr("Loading..."));
        _target.SelectedIndex = 0;
        _target.Enabled = false;

        // Translators: Label of the slider that sets how loud one recording source is in the mix.
        var volumeLabel = new StaticText(_dialog, label: Tr("Volume"));
        // No label beside it showing the figure: the slider reports its own value, so a label would only
        // say the same thing again and be read out twice.
        _volume = new CustomSlider(_dialog, value: source.Volume, minValue: 0, maxValue: 100);

        var form = new FlexGridSizer(0, 2, 8, 8);
        form.AddGrowableColumn(1, 1);
        Add(form, nameLabel, _name);
        Add(form, targetLabel, _target);
        if (source.Kind is RecordingSourceKind.Process)
        {
            // Worded for what Windows actually does. There is no mode that captures a program without its
            // children, so a box about child processes would promise something unavailable; what the two
            // modes really are is "this program and anything it started" or "everything but that".
            _others = new CheckBox(_dialog,
                // Translators: Tick box on the recording source window. Ticked, the source records every
                // program except the chosen one rather than the chosen one itself.
                label: Tr("Capture everything except this application")) { Checked = source.CaptureOthers };
            form.AddSpacer(0);
            form.Add(_others, flags: SizerFlags.Expand);
        }
        Add(form, volumeLabel, _volume);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(form, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        var buttons = _dialog.CreateButtonSizer(ButtonSizerFlags.Ok | ButtonSizerFlags.Cancel);
        if (buttons is not null)
        {
            sizer.Add(buttons,
                flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand,
                border: 10);
        }
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(560, source.Kind is RecordingSourceKind.Process ? 280 : 240);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.ButtonClicked, OnAccept, StandardId.Ok);
        _name.Focus();
        Load();
    }

    /// <summary>The edited source, or null when the user backed out.</summary>
    internal RecordingSource? Show()
        => _dialog.ShowModal() == StandardId.Ok ? _source : null;

    public void Dispose()
    {
        // Set before the window goes, so a list that arrives late is dropped rather than written into a
        // control that is no longer there.
        _closed = true;
        _dialog.Dispose();
    }

    /// <summary>Fetches whatever belongs in the list, off the UI thread, and fills it in when it arrives.
    /// </summary>
    private void Load()
    {
        var kind = _source.Kind;
        // Translators: The entry in a device list that means whichever microphone Windows is currently set
        // to use, rather than one particular microphone.
        var defaultInput = Tr("Default input device");
        // Translators: The entry in a device list that means whichever speakers Windows is currently set to
        // use, rather than one particular set.
        var defaultOutput = Tr("Default output device");
        _ = Task.Run(() =>
        {
            if (kind is RecordingSourceKind.Process)
            {
                var processes = _catalog.Processes();
                _dispatcher.Post(() => FillProcesses(processes));
                return;
            }
            var devices = kind is RecordingSourceKind.OutputLoopback
                ? _catalog.OutputDevices(defaultOutput)
                : _catalog.InputDevices(defaultInput);
            _dispatcher.Post(() => FillDevices(devices));
        });
    }

    private void FillDevices(IReadOnlyList<AudioDeviceChoice> devices)
    {
        if (_closed)
            return;
        _devices = devices;
        _target.Clear();
        foreach (var device in devices)
            _target.Add(device.Name);
        // The device this source already names, or the default entry when it names none or names one that
        // has since been unplugged.
        var index = _source.DeviceId is null
            ? 0
            : devices.ToList().FindIndex(device => device.Id == _source.DeviceId);
        _target.SelectedIndex = index < 0 ? 0 : index;
        _target.Enabled = true;
    }

    private void FillProcesses(IReadOnlyList<ProcessChoice> processes)
    {
        if (_closed)
            return;
        _processes = processes;
        _target.Clear();
        foreach (var process in processes)
            _target.Add(process.Label);
        if (processes.Count == 0)
        {
            // Translators: Shown in the list of programs when no program has an audio session to capture.
            _target.Add(Tr("No programs are using sound"));
            _target.SelectedIndex = 0;
            return;
        }
        var index = processes.ToList().FindIndex(process => process.ProcessId == _source.ProcessId);
        _target.SelectedIndex = index < 0 ? 0 : index;
        _target.Enabled = true;
    }

    /// <summary>Reads the controls back into the source and closes, unless something is missing.</summary>
    private void OnAccept(object? sender, CommandEventArgs args)
    {
        _source.Name = _name.Value.Trim();
        _source.Volume = _volume.Value;
        if (_others is not null)
            _source.CaptureOthers = _others.Checked;
        var selected = _target.SelectedIndex;
        if (_source.Kind is RecordingSourceKind.Process)
        {
            if (selected >= 0 && selected < _processes.Count)
            {
                _source.ProcessId = _processes[selected].ProcessId;
                _source.ProcessName = _processes[selected].Name;
            }
        }
        else if (selected >= 0 && selected < _devices.Count)
        {
            _source.DeviceId = _devices[selected].Id;
        }
        if (!RecordingSources.Validate(_source, out var error))
        {
            // Kept open, so nothing typed is lost to a missing name.
            Wx.MessageBox(error, Title, MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            return;
        }
        _dialog.EndModal(StandardId.Ok);
    }

    private static void Add(FlexGridSizer form, StaticText label, Window control)
    {
        form.Add(label, flags: SizerFlags.AlignCenterVertical);
        form.Add(control, proportion: 1, flags: SizerFlags.Expand);
    }

    private static string Title =>
        // Translators: Title of the messages shown about a recording source.
        Tr("Recording source");
}
