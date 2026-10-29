using LunaPlayer.UI.Services;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer.UI.Playback;

/// <summary>What the sleep-timer dialog was asked to set.</summary>

/// <summary>The window for setting a sleep timer: how it ends (after a length of time, or at the end of the
/// current track), how long, what it then does (pause or stop), and whether it fades the volume down first.
/// </summary>
///
/// <remarks>
/// The length rows - the presets and the hours and minutes - are shown only in the timed mode and hidden for
/// the end-of-track mode, and the fade-length row only when fading is ticked: the same show/hide approach the
/// IPTV source window uses, so a screen reader never lands on a field the chosen mode has no use for. Each label
/// is created immediately before its control so Windows infers the accessible name from native child order.
/// </remarks>
internal sealed class SleepTimerDialog : IDisposable
{
    private static readonly int[] Presets = [15, 30, 45, 60, 90, 120];

    private readonly Dialog _dialog;
    private readonly FlexGridSizer _form;
    private readonly Choice _mode;
    private readonly StaticText _presetLabel;
    private readonly Choice _preset;
    private readonly StaticText _hoursLabel;
    private readonly SpinCtrl _hours;
    private readonly StaticText _minutesLabel;
    private readonly SpinCtrl _minutes;
    private readonly Choice _action;
    private readonly StaticText _fadeLabel;
    private readonly CheckBox _fade;
    private readonly StaticText _fadeSecondsLabel;
    private readonly SpinCtrl _fadeSeconds;
    private bool _turnOff;
    private bool _updating;

    internal SleepTimerDialog(Window parent, SleepTimerRequest initial, bool armed, string? statusText)
    {
        // Translators: Title of the window used to configure the sleep timer.
        _dialog = new Dialog(parent, title: Tr("Sleep timer"), style: DialogStyle.Default | DialogStyle.ResizeBorder);

        StaticText? status = null;
        if (statusText is { Length: > 0 })
            status = new StaticText(_dialog, label: statusText);

        // Translators: Label of the list choosing whether the sleep timer ends after a length of time or when
        // the current track finishes.
        var modeLabel = new StaticText(_dialog, label: Tr("End the timer"));
        _mode = new Choice(_dialog);
        // Translators: The sleep-timer option that fires after a chosen length of time.
        _mode.Add(Tr("After a length of time"));
        // Translators: The sleep-timer option that fires when the current track finishes.
        _mode.Add(Tr("At the end of the current track"));
        _mode.SelectedIndex = initial.Mode == SleepTimerMode.EndOfTrack ? 1 : 0;

        // Translators: Label of the list of ready-made timer lengths.
        _presetLabel = new StaticText(_dialog, label: Tr("Length"));
        _preset = new Choice(_dialog);
        foreach (var minutes in Presets)
            // Translators: One ready-made timer length. {minutes} is a number of minutes.
            _preset.Add(TrFormat("{minutes} minutes", minutes));
        // Translators: The entry in the length list that lets the user type their own hours and minutes.
        _preset.Add(Tr("Custom"));

        // Translators: Label of the box holding the hours part of a custom timer length.
        _hoursLabel = new StaticText(_dialog, label: Tr("Hours"));
        _hours = new SpinCtrl(_dialog, minimum: 0, maximum: 23);
        // Translators: Label of the box holding the minutes part of a custom timer length.
        _minutesLabel = new StaticText(_dialog, label: Tr("Minutes"));
        _minutes = new SpinCtrl(_dialog, minimum: 0, maximum: 59);

        // Translators: Label of the list choosing what happens when the timer fires.
        var actionLabel = new StaticText(_dialog, label: Tr("When the timer fires"));
        _action = new Choice(_dialog);
        // Translators: The timer action that pauses playback.
        _action.Add(Tr("Pause"));
        // Translators: The timer action that stops playback.
        _action.Add(Tr("Stop"));
        _action.SelectedIndex = initial.Action == SleepTimerAction.Stop ? 1 : 0;

        _fadeLabel = new StaticText(_dialog, label: string.Empty);
        // Translators: Tick box that fades the volume down over the last seconds before the timer fires.
        _fade = new CheckBox(_dialog, label: Tr("Fade the volume out before firing")) { Checked = initial.Fade };
        // Translators: Label of the box holding how many seconds the fade lasts.
        _fadeSecondsLabel = new StaticText(_dialog, label: Tr("Fade length (seconds)"));
        _fadeSeconds = new SpinCtrl(_dialog, value: initial.FadeSeconds, minimum: 1, maximum: 120);

        // Set the length controls from the remembered minutes: a matching preset if there is one, otherwise
        // Custom with the hours and minutes filled in.
        var total = Math.Clamp(initial.DurationMinutes, 1, 23 * 60 + 59);
        _hours.Value = total / 60;
        _minutes.Value = total % 60;
        var presetIndex = Array.IndexOf(Presets, total);
        _preset.SelectedIndex = presetIndex >= 0 ? presetIndex : Presets.Length;

        _form = new FlexGridSizer(0, 2, 8, 8);
        _form.AddGrowableColumn(1, 1);
        AddField(modeLabel, _mode);
        AddField(_presetLabel, _preset);
        AddField(_hoursLabel, _hours);
        AddField(_minutesLabel, _minutes);
        AddField(actionLabel, _action);
        AddField(_fadeLabel, _fade);
        AddField(_fadeSecondsLabel, _fadeSeconds);

        var root = new BoxSizer(Orientation.Vertical);
        if (status is not null)
            root.Add(status, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        root.Add(_form, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        root.Add(BuildButtons(armed),
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand,
            border: 10);
        _dialog.SetSizer(root);

        _mode.SelectionChanged += (_, _) => Refresh();
        _preset.SelectionChanged += (_, _) => SyncSpinnersToPreset();
        _fade.Toggled += (_, _) => Refresh();
        foreach (var spin in new[] { _hours, _minutes })
        {
            spin.ValueChanged += OnDurationSpun;
            spin.TextChanged += OnDurationTyped;
        }
        Refresh();

        _dialog.Fit();
        _dialog.MinSize = new Size(420, 300);
        _dialog.Center(onParent: true);
        _mode.Focus();
    }

    internal SleepTimerDialogResult? Show()
        => _dialog.ShowModal() == StandardId.Ok
            ? new SleepTimerDialogResult(_turnOff, Current())
            : null;

    public void Dispose() => _dialog.Dispose();

    private SleepTimerRequest Current() => new(
        _mode.SelectedIndex == 1 ? SleepTimerMode.EndOfTrack : SleepTimerMode.Duration,
        Math.Max(1, _hours.Value * 60 + _minutes.Value),
        _action.SelectedIndex == 1 ? SleepTimerAction.Stop : SleepTimerAction.Pause,
        _fade.Checked,
        _fadeSeconds.Value);

    private BoxSizer BuildButtons(bool armed)
    {
        var buttons = new BoxSizer(Orientation.Horizontal);
        if (armed)
        {
            // Translators: Button in the sleep-timer window that turns off a timer already running.
            var turnOff = new Button(_dialog, label: Tr("Turn off timer"));
            turnOff.Click += (_, _) => { _turnOff = true; _dialog.EndModal(StandardId.Ok); };
            buttons.Add(turnOff, flags: SizerFlags.BorderRight, border: 6);
        }
        buttons.AddStretchSpacer();
        // Translators: The button that accepts a window and carries the change out.
        var ok = new Button(_dialog, StandardId.Ok, Tr("OK"));
        ok.SetDefault();
        ok.Click += OnAccept;
        buttons.Add(ok, flags: SizerFlags.BorderRight, border: 6);
        // Translators: The button that closes a window and leaves everything as it was.
        buttons.Add(new Button(_dialog, StandardId.Cancel, Tr("Cancel")));
        return buttons;
    }

    /// <summary>Shows the length rows only in the timed mode and the fade-length row only when fading is on,
    /// then lays the form out again.</summary>
    private void Refresh()
    {
        var timed = _mode.SelectedIndex != 1;
        ShowField(_presetLabel, _preset, timed);
        ShowField(_hoursLabel, _hours, timed);
        ShowField(_minutesLabel, _minutes, timed);
        ShowField(_fadeSecondsLabel, _fadeSeconds, _fade.Checked);
        _dialog.Layout();
        _dialog.Fit();
    }

    // Choosing a ready-made length fills the hours and minutes to match; the guard stops that write from
    // looking like a hand edit and flipping the list straight back to Custom.
    private void SyncSpinnersToPreset()
    {
        if (_updating)
            return;
        var index = _preset.SelectedIndex;
        if (index < 0 || index >= Presets.Length)
            return;
        _updating = true;
        try
        {
            _hours.Value = Presets[index] / 60;
            _minutes.Value = Presets[index] % 60;
        }
        finally
        {
            _updating = false;
        }
    }

    private void OnDurationSpun(object? sender, SpinEventArgs args) => SyncPresetToSpinners();
    private void OnDurationTyped(object? sender, CommandEventArgs args) => SyncPresetToSpinners();

    // A hand-set length that no longer matches a preset shows as Custom.
    private void SyncPresetToSpinners()
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            var total = _hours.Value * 60 + _minutes.Value;
            var presetIndex = Array.IndexOf(Presets, total);
            _preset.SelectedIndex = presetIndex >= 0 ? presetIndex : Presets.Length;
        }
        finally
        {
            _updating = false;
        }
    }

    private void OnAccept(object? sender, CommandEventArgs args)
    {
        _turnOff = false;
        if (_mode.SelectedIndex != 1 && _hours.Value * 60 + _minutes.Value < 1)
        {
            Messages.Show(
                // Translators: Shown when a timed sleep timer is set to a length of zero.
                Tr("Choose a length of at least one minute."),
                MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            return;
        }
        _dialog.EndModal(StandardId.Ok);
    }

    /// <summary>One row of the form: the label centred against its control, the control taking the width.
    /// </summary>
    private void AddField(StaticText label, Window control)
    {
        _form.Add(label, flags: SizerFlags.AlignCenterVertical);
        _form.Add(control, proportion: 1, flags: SizerFlags.Expand);
    }

    /// <summary>Shows or hides a whole row so the sizer gives it no space when it does not apply.</summary>
    private void ShowField(StaticText label, Window control, bool show)
    {
        _form.Show(label, show);
        _form.Show(control, show);
    }
}
