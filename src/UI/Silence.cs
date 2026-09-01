using System.Globalization;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class SilencePreferences : Preferences
{
    // Translators: Spoken description of the box for the shortest silence that will be trimmed, on the silence removal settings page.
    private static readonly string MinimumHint = Tr("Minimum silence duration in seconds. Silence shorter than this is kept. Default is 0.5.");
    // Translators: Spoken description of the box for the loudness under which sound counts as silence, on the silence removal settings page.
    private static readonly string ThresholdHint = Tr("Silence level threshold. Audio below this level is treated as silence. Enter only a number, for example -30.");
    // Translators: Spoken description of the list of ways silence can be measured, on the silence removal settings page. Peak and RMS are the names of the two methods.
    private static readonly string DetectionHint = Tr("How silence is detected. Peak reacts faster to speech transients (default). RMS is smoother and less sensitive to short spikes.");

    private static readonly (string Key, string Label, string Hint)[] AdvancedFields =
    [
        // Translators: Label of an advanced silence removal setting: how many silent parts to cut from the start of the file.
        ("start_periods", Tr("Leading silent parts to trim"),
            // Translators: Spoken description of the advanced silence removal setting for how many silent parts to cut from the start of the file.
            Tr("How many silent chunks to remove from the beginning.")),
        // Translators: Label of an advanced silence removal setting: how long silence at the start must be before it is cut.
        ("start_duration", Tr("Minimum leading silence length (seconds)"),
            // Translators: Spoken description of the advanced silence removal setting for how long silence at the start must be before it is cut.
            Tr("Only trim leading silence chunks that are at least this long (default: 0.2).")),
        // Translators: Label of an advanced silence removal setting: how many silent parts to cut once the sound has begun.
        ("stop_periods", Tr("Silent parts to trim after audio starts"),
            // Translators: Spoken description of the advanced silence removal setting for how many silent parts to cut once the sound has begun. Minus one means every one of them.
            Tr("How many silence chunks to trim after audio has started (-1 means all).")),
        // Translators: Label of an advanced silence removal setting: how long silence in the middle or at the end must be before it is cut.
        ("stop_duration", Tr("Minimum inner silence length (seconds)"),
            // Translators: Spoken description of the advanced silence removal setting for how long silence in the middle or at the end must be before it is cut.
            Tr("Only trim middle/end silence chunks that are at least this long.")),
        // Translators: Label of an advanced silence removal setting: how much of a pause to leave where silence was cut.
        ("stop_silence", Tr("Pause to keep after trimmed silence (seconds)"),
            // Translators: Spoken description of the advanced silence removal setting for how much of a pause to leave where silence was cut.
            Tr("Leaves a short pause before the next word (default: 0.2).")),
        // Translators: Label of an advanced silence removal setting: the length of sound looked at at once when deciding whether it is silent.
        ("window", Tr("Detection window size (seconds)"),
            // Translators: Spoken description of the advanced silence removal setting for the length of sound looked at at once when deciding whether it is silent.
            Tr("Smoothing window used by silence detection (default: 0.02).")),
    ];

    private readonly SilenceSettings _settings;
    private readonly StaticBoxSizer _group;
    private readonly FlexGridSizer _advancedGrid;
    private readonly TextCtrl _minimum;
    private readonly TextCtrl _threshold;
    private readonly CheckBox _advanced;
    private readonly Dictionary<string, TextCtrl> _advancedControls = [];
    private readonly Choice _detection;

    internal SilencePreferences(Window parent, SilenceSettings settings)
        : base(new ScrolledWindow(parent),
            // Translators: Spoken description of the whole silence removal settings page, read when the page is opened.
            Tr("Silence removal settings. Minimum silence duration and threshold are always shown. " +
            "Enable advanced settings to configure all remaining filter options."))
    {
        _settings = settings;
        var page = (ScrolledWindow)Window;
        page.SetScrollRate(8, 8);
        // Translators: Title of the group holding the silence removal settings.
        // "FFmpeg silenceremove" is the name of the filter doing the work and is not translated.
        var box = new StaticBox(page, Tr("Silence removal (FFmpeg silenceremove)"));
        _group = new StaticBoxSizer(box);

        var basic = Grid();
        // Translators: Label of the box for the shortest silence that will be trimmed, on the silence removal settings page.
        _minimum = Field(box, basic, Tr("Minimum silence duration (seconds)"), MinimumHint, settings.StopDuration);
        // Translators: Label of the box for the loudness under which sound counts as silence, on the silence removal settings page.
        _threshold = Field(box, basic, Tr("Silence threshold"), ThresholdHint, settings.Threshold);
        _group.Add(basic, flags: SizerFlags.All | SizerFlags.Expand, border: 8);

        // Translators: Label of the tick box that shows the rest of the silence removal settings.
        _advanced = new CheckBox(box, label: Tr("Show advanced settings")) { Checked = settings.Advanced };
        _group.Add(_advanced, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);

        // The advanced rows live in this page's own group rather than in a nested panel, so turning them
        // on and off hides the rows in place instead of drawing a second box inside the first one.
        _advancedGrid = Grid();
        foreach (var (key, label, hint) in AdvancedFields)
        {
            var control = Field(box, _advancedGrid, label, hint, ValueOf(settings, key));
            _advancedControls[key] = control;
            Help(control, HelpFor(key));
        }

        // Translators: Label of the list of ways silence can be measured, on the silence removal settings page.
        var detectionLabel = new StaticText(box, label: Tr("Detection mode"));
        detectionLabel.ToolTip = DetectionHint;
        _detection = new Choice(box);
        // Translators: One of the two ways silence can be measured: by the loudest moment. This one answers quickly to speech.
        _detection.Add(Tr("Peak (fast reaction)"));
        // Translators: One of the two ways silence can be measured: by the average loudness. This one is steadier. RMS is the usual name for it and can be left as it is.
        _detection.Add(Tr("RMS (smoother)"));
        _detection.SelectedIndex = (int)settings.Detection;
        _detection.ToolTip = DetectionHint;
        AddRow(_advancedGrid, detectionLabel, _detection);
        _group.Add(_advancedGrid, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);

        var root = new BoxSizer(Orientation.Vertical);
        root.Add(_group, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        page.SetSizer(root);

        // Measure while the advanced rows are still visible and keep that as the page's minimum, so the
        // dialog reserves room for them even when the page starts collapsed. Without this the group box
        // would be clipped the moment the user ticks "Show advanced settings".
        page.MinSize = root.MinSize;

        _advanced.Toggled += (_, _) => SetAdvanced(_advanced.Checked);
        SetAdvanced(settings.Advanced);

        Help(_minimum,
            // Translators: Help text for the box holding the shortest silence that will be trimmed, spoken when the user asks for help on it.
            Tr("Minimum silence duration in seconds. Silence shorter than this value is kept. " +
            "Increase it to preserve short pauses. Decrease it to trim more aggressively."));
        Help(_threshold,
            // Translators: Help text for the box holding the loudness under which sound counts as silence, spoken when the user asks for help on it.
            Tr("Silence level threshold. Audio quieter than this level is treated as silence. " +
            "Enter only the number, for example -20, -30, or -40."));
        Help(_advanced,
            // Translators: Help text for the tick box that shows the rest of the silence removal settings.
            // "silenceremove" is the name of the filter doing the work and is not translated.
            Tr("Show advanced settings. When enabled, extra silenceremove parameters are displayed. " +
            "When disabled, only minimum duration and threshold are used."));
        Help(_detection,
            // Translators: Help text for the list of ways silence can be measured. Peak and RMS are the names of the two methods.
            Tr("Detection mode for silence analysis. Peak reacts quickly to speech transients. RMS is smoother."));
    }

    public override string? Validate()
    {
        if (_minimum.Value.Trim().Length > 0 && !NonNegative(_minimum.Value))
        {
            _minimum.Focus();
            // Translators: Error message shown when the shortest silence to trim was typed as something other than a number, or as a negative one.
            return Tr("Minimum silence duration must be a non-negative number.");
        }
        if (_threshold.Value.Trim().Length > 0 && !Number(_threshold.Value))
        {
            _threshold.Focus();
            // Translators: Error message shown when the silence loudness threshold was typed as something other than a number.
            return Tr("Silence threshold must be a valid number.");
        }
        if (!_advanced.Checked) return null;
        foreach (var (key, label, _) in AdvancedFields)
        {
            var control = _advancedControls[key];
            var text = control.Value.Trim();
            if (text.Length == 0) continue;
            if (key is "start_periods" or "stop_periods")
            {
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                {
                    control.Focus();
                    // Translators: Error message shown when an advanced silence removal setting needs a whole number.
                    // {field} is the name of the setting, such as "Leading silent parts to trim".
                    return TrFormat("{field} must be an integer value.", label);
                }
                if (key == "start_periods" && integer < 0)
                {
                    control.Focus();
                    // Translators: Error message shown when an advanced silence removal setting cannot be negative.
                    // {field} is the name of the setting, such as "Leading silent parts to trim".
                    return TrFormat("{field} must be zero or greater.", label);
                }
                if (key == "stop_periods" && integer < -1)
                {
                    control.Focus();
                    // Translators: Error message shown when an advanced silence removal setting accepts -1 but nothing lower.
                    // {field} is the name of the setting, such as "Silent parts to trim after audio starts".
                    return TrFormat("{field} must be -1 or greater.", label);
                }
            }
            else if (!NonNegative(text))
            {
                control.Focus();
                // Translators: Error message shown when an advanced silence removal setting needs a number that is not negative.
                // {field} is the name of the setting, such as "Minimum leading silence length (seconds)".
                return TrFormat("{field} must be a non-negative number.", label);
            }
        }
        return null;
    }

    public override void Apply()
    {
        var minimum = ValueOr(_minimum.Value, _settings.StopDuration);
        _settings.Threshold = ValueOr(_threshold.Value, -30);
        _settings.Advanced = _advanced.Checked;
        if (!_advanced.Checked)
        {
            // Hidden advanced values stay as configured; only the basic fields are updated.
            _settings.StopDuration = minimum;
            return;
        }
        _settings.StartPeriods = IntValueOr(_advancedControls["start_periods"].Value, _settings.StartPeriods);
        _settings.StartDuration = ValueOr(_advancedControls["start_duration"].Value, _settings.StartDuration);
        _settings.StopPeriods = IntValueOr(_advancedControls["stop_periods"].Value, _settings.StopPeriods);
        _settings.StopDuration = ValueOr(_advancedControls["stop_duration"].Value, minimum);
        _settings.StopSilence = ValueOr(_advancedControls["stop_silence"].Value, _settings.StopSilence);
        _settings.Window = ValueOr(_advancedControls["window"].Value, _settings.Window);
        _settings.Detection = (SilenceDetection)Math.Max(0, _detection.SelectedIndex);
    }

    public override void Refresh()
    {
        _minimum.Value = Format(_settings.StopDuration);
        _threshold.Value = Format(_settings.Threshold);
        foreach (var (key, _, _) in AdvancedFields)
            _advancedControls[key].Value = Format(ValueOf(_settings, key));
        _detection.SelectedIndex = (int)_settings.Detection;
        _advanced.Checked = _settings.Advanced;
        SetAdvanced(_settings.Advanced);
    }

    private void SetAdvanced(bool visible)
    {
        _advancedGrid.ShowItems(visible);
        _group.Show(_advancedGrid, visible);
        Window.Layout();
        Window.FitInside();
    }

    private static double ValueOf(SilenceSettings settings, string key) => key switch
    {
        "start_periods" => settings.StartPeriods,
        "start_duration" => settings.StartDuration,
        "stop_periods" => settings.StopPeriods,
        "stop_duration" => settings.StopDuration,
        "stop_silence" => settings.StopSilence,
        "window" => settings.Window,
        _ => 0,
    };

    private static FlexGridSizer Grid()
    {
        var grid = new FlexGridSizer(0, 2, 6, 8);
        grid.AddGrowableColumn(1, 1);
        return grid;
    }

    private static TextCtrl Field(Window parent, FlexGridSizer grid, string label, string hint, double value)
    {
        var title = new StaticText(parent, label: label);
        title.ToolTip = hint;
        var field = new TextCtrl(parent, value: Format(value));
        field.ToolTip = hint;
        AddRow(grid, title, field);
        return field;
    }

    private static void AddRow(FlexGridSizer grid, StaticText label, Window control)
    {
        grid.Add(label, flags: SizerFlags.AlignCenterVertical);
        grid.Add(control, flags: SizerFlags.Expand);
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static double ValueOr(string value, double fallback)
        => double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : fallback;
    private static int IntValueOr(string value, int fallback)
        => int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : fallback;
    private static bool Number(string value) => double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    private static bool NonNegative(string value) => double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number >= 0;

    private static string HelpFor(string key) => key switch
    {
        // Translators: Help text for the advanced setting holding how many silent parts to cut from the start of the file.
        "start_periods" => Tr("Leading silent parts to trim. This controls how many silent chunks are removed from the beginning. Typical value is 1."),
        // Translators: Help text for the advanced setting holding how long silence at the start must be before it is cut.
        "start_duration" => Tr("Minimum leading silence length in seconds. Only leading silence at least this long is removed. Default is 0.2."),
        // Translators: Help text for the advanced setting holding how many silent parts to cut once the sound has begun.
        "stop_periods" => Tr("Silent parts to trim after audio starts. Use -1 to trim all matching silent parts."),
        // Translators: Help text for the advanced setting holding how long silence in the middle or at the end must be before it is cut.
        "stop_duration" => Tr("Minimum inner silence length in seconds. Only silence in the middle or end at least this long is removed."),
        // Translators: Help text for the advanced setting holding how much of a pause to leave where silence was cut.
        "stop_silence" => Tr("Pause to keep after trimmed silence, in seconds. Default is 0.2."),
        // Translators: Help text for the advanced setting holding the length of sound looked at at once when deciding whether it is silent.
        "window" => Tr("Detection window size in seconds. Default is 0.02."),
        // Translators: Help text used for any silence removal setting with no help text of its own.
        _ => Tr("Silence removal setting. Enter a value and press OK to save."),
    };
}
