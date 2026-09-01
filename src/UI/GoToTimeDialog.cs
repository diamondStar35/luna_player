using WxSharp;

namespace LunaPlayer.UI;

internal sealed class GoToTimeDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly int _duration;
    private readonly bool _hasHours;
    private readonly bool _hasMinutes;
    private readonly SpinCtrl _hours;
    private readonly SpinCtrl _minutes;
    private readonly SpinCtrl _seconds;
    private bool _updating;

    internal GoToTimeDialog(Window parent, double duration, double elapsed)
    {
        _duration = Math.Max(0, (int)duration);
        _hasHours = _duration >= 3600;
        _hasMinutes = _duration >= 60;
        _dialog = new Dialog(parent,
            title: Tr("Go to time"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _hours = new SpinCtrl(_dialog, maximum: Math.Max(0, _duration / 3600));
        _minutes = new SpinCtrl(_dialog, maximum: 59);
        _seconds = new SpinCtrl(_dialog, maximum: 59);

        var total = Math.Clamp((int)elapsed, 0, _duration);
        _hours.Value = total / 3600;
        total %= 3600;
        _minutes.Value = total / 60;
        _seconds.Value = total % 60;
        // The Python player binds EVT_SPINCTRL and EVT_TEXT on each spinner; wxEVT_SPINCTRL covers the
        // arrows and wxEVT_TEXT covers typing, so both are needed for a typed value to be re-clamped.
        foreach (var spin in new[] { _hours, _minutes, _seconds })
        {
            spin.ValueChanged += OnValueChanged;
            spin.TextChanged += OnTextChanged;
        }

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        // Translators: The button that accepts a window and carries the change out.
        var ok = new Button(_dialog, StandardId.Ok, Tr("OK"));
        ok.SetDefault();
        ok.Click += OnAccept;
        buttons.Add(ok, flags: SizerFlags.BorderRight, border: 6);
        // Translators: The button that closes a window and leaves everything as it was.
        buttons.Add(new Button(_dialog, StandardId.Cancel, Tr("Cancel")));

        var root = new BoxSizer(Orientation.Vertical);
        // Translators: Label above the boxes where the user types the point in the file to move to.
        root.Add(new StaticText(_dialog, label: Tr("Choose time position:")), flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        if (_hasHours)
        {
            // Translators: Label of the box holding the hours part of the point in the file to move to.
            root.Add(new StaticText(_dialog, label: Tr("Hours")), flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
            root.Add(_hours, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        }
        if (_hasMinutes)
        {
            // Translators: Label of the box holding the minutes part of the point in the file to move to.
            root.Add(new StaticText(_dialog, label: Tr("Minutes")), flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
            root.Add(_minutes, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        }
        // Translators: Label of the box holding the seconds part of the point in the file to move to.
        root.Add(new StaticText(_dialog, label: Tr("Seconds")), flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        root.Add(_seconds, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        root.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        _dialog.SetSizer(root);
        _dialog.Fit();
        _dialog.MinSize = new Size(320, 220);
        _dialog.Center(onParent: true);
        UpdateLimits();
    }

    internal double? Show() => _dialog.ShowModal() == StandardId.Ok ? SelectedSeconds() : null;

    public void Dispose() => _dialog.Dispose();

    private int SelectedSeconds()
        => (_hasHours ? _hours.Value * 3600 : 0)
            + (_hasMinutes ? _minutes.Value * 60 : 0)
            + _seconds.Value;

    // A value typed into a spin control does not raise wxEVT_SPINCTRL, so it never goes through
    // UpdateLimits and can still be out of range by the time OK is pressed.
    private void OnAccept(object? sender, CommandEventArgs args)
    {
        var target = SelectedSeconds();
        if (target < 0 || target > _duration)
        {
            Wx.MessageBox(
                // Translators: Shown when the point in the file the user typed lies past the end of the file.
                Tr("The selected time exceeds the file duration."), Tr("Go to time"),
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
            return;
        }
        _dialog.EndModal(StandardId.Ok);
    }

    private void OnValueChanged(object? sender, SpinEventArgs args) => UpdateLimits();
    private void OnTextChanged(object? sender, CommandEventArgs args) => UpdateLimits();

    private void UpdateLimits()
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            var maximumHours = _hasHours ? _duration / 3600 : 0;
            _hours.SetRange(0, maximumHours);
            if (!_hasHours)
                _hours.Value = 0;
            var hours = Math.Clamp(_hours.Value, 0, maximumHours);
            _hours.Value = hours;

            int maximumMinutes;
            if (!_hasMinutes)
                maximumMinutes = 0;
            else if (_hasHours && hours == maximumHours)
                maximumMinutes = Math.Min(59, Math.Max(0, (_duration - hours * 3600) / 60));
            else if (_hasHours)
                maximumMinutes = 59;
            else
                maximumMinutes = Math.Max(0, _duration / 60);
            _minutes.SetRange(0, maximumMinutes);
            var minutes = Math.Clamp(_minutes.Value, 0, maximumMinutes);
            _minutes.Value = minutes;

            var remaining = Math.Max(0, _duration - hours * 3600 - minutes * 60);
            var maximumSeconds = _hasMinutes ? Math.Min(59, remaining) : remaining;
            _seconds.SetRange(0, maximumSeconds);
            _seconds.Value = Math.Clamp(_seconds.Value, 0, maximumSeconds);
        }
        finally
        {
            _updating = false;
        }
    }
}
