using WxSharp;

namespace LunaPlayer.UI.Selections;

/// <summary>Asks for a media position down to one millisecond, showing only the time units the file needs.</summary>
internal sealed class TimeDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly long _durationMilliseconds;
    private readonly bool _hasHours;
    private readonly bool _hasMinutes;
    private readonly SpinCtrl _hours;
    private readonly SpinCtrl _minutes;
    private readonly SpinCtrl _seconds;
    private readonly SpinCtrl _milliseconds;

    internal TimeDialog(Window parent, string title, string prompt, double duration, double elapsed)
    {
        _durationMilliseconds = Math.Max(0, (long)Math.Round(duration * 1000, MidpointRounding.AwayFromZero));
        _hasHours = _durationMilliseconds >= 3_600_000;
        _hasMinutes = _durationMilliseconds >= 60_000;
        _dialog = new Dialog(parent, title: title, style: DialogStyle.Default | DialogStyle.ResizeBorder);

        var promptLabel = new StaticText(_dialog, label: prompt);
        // Translators: Label of the hours box in a selection time dialog.
        var hoursLabel = new StaticText(_dialog, label: Tr("Hours"));
        _hours = new SpinCtrl(_dialog, maximum: Math.Max(0, (int)(_durationMilliseconds / 3_600_000)));
        // Translators: Label of the minutes box in a selection time dialog.
        var minutesLabel = new StaticText(_dialog, label: Tr("Minutes"));
        _minutes = new SpinCtrl(_dialog,
            maximum: _hasHours ? 59 : Math.Max(0, (int)(_durationMilliseconds / 60_000)));
        // Translators: Label of the seconds box in a selection time dialog.
        var secondsLabel = new StaticText(_dialog, label: Tr("Seconds"));
        _seconds = new SpinCtrl(_dialog,
            maximum: _hasMinutes ? 59 : Math.Max(0, (int)(_durationMilliseconds / 1000)));
        // Translators: Label of the milliseconds box in a selection time dialog.
        var millisecondsLabel = new StaticText(_dialog, label: Tr("Milliseconds"));
        _milliseconds = new SpinCtrl(_dialog, maximum: 999);

        hoursLabel.Show(_hasHours);
        _hours.Show(_hasHours);
        minutesLabel.Show(_hasMinutes);
        _minutes.Show(_hasMinutes);

        var total = Math.Clamp((long)Math.Round(elapsed * 1000, MidpointRounding.AwayFromZero), 0, _durationMilliseconds);
        _hours.Value = (int)(total / 3_600_000);
        total %= 3_600_000;
        _minutes.Value = (int)(total / 60_000);
        total %= 60_000;
        _seconds.Value = (int)(total / 1000);
        _milliseconds.Value = (int)(total % 1000);

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        // Translators: Button that accepts the time entered in a selection time dialog.
        var ok = new Button(_dialog, StandardId.Ok, Tr("OK"));
        ok.SetDefault();
        ok.Click += OnAccept;
        buttons.Add(ok, flags: SizerFlags.BorderRight, border: 6);
        // Translators: Button that closes a selection time dialog without changing anything.
        buttons.Add(new Button(_dialog, StandardId.Cancel, Tr("Cancel")));

        var root = new BoxSizer(Orientation.Vertical);
        root.Add(promptLabel, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        if (_hasHours) AddField(root, hoursLabel, _hours);
        if (_hasMinutes) AddField(root, minutesLabel, _minutes);
        AddField(root, secondsLabel, _seconds);
        AddField(root, millisecondsLabel, _milliseconds);
        root.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        _dialog.SetSizer(root);
        _dialog.Fit();
        _dialog.MinSize = new Size(320, 260);
        _dialog.Center(onParent: true);
    }

    internal double? Show() => _dialog.ShowModal() == StandardId.Ok ? SelectedMilliseconds() / 1000.0 : null;

    public void Dispose() => _dialog.Dispose();

    private long SelectedMilliseconds()
        => (_hasHours ? (long)_hours.Value * 3_600_000 : 0)
            + (_hasMinutes ? (long)_minutes.Value * 60_000 : 0)
            + (long)_seconds.Value * 1000
            + _milliseconds.Value;

    private void OnAccept(object? sender, CommandEventArgs args)
    {
        if (SelectedMilliseconds() > _durationMilliseconds)
        {
            Wx.MessageBox(
                // Translators: Shown when a manually entered selection marker lies beyond the file's end.
                Tr("The selected time exceeds the file duration."),
                // Translators: Title of the error for a selection time entered beyond the file's end.
                Tr("Selection time"),
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
            return;
        }
        _dialog.EndModal(StandardId.Ok);
    }

    private static void AddField(BoxSizer root, StaticText label, Window control)
    {
        root.Add(label, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        root.Add(control, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
    }
}
