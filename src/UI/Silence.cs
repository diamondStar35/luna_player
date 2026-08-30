using System.Globalization;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class SilencePreferences : Preferences
{
    private const string MinimumHint = "Minimum silence duration in seconds. Silence shorter than this is kept. Default is 0.5.";
    private const string ThresholdHint = "Silence level threshold. Audio below this level is treated as silence. Enter only a number, for example -30.";
    private const string DetectionHint = "How silence is detected. Peak reacts faster to speech transients (default). RMS is smoother and less sensitive to short spikes.";

    private static readonly (string Key, string Label, string Hint)[] AdvancedFields =
    [
        ("start_periods", "Leading silent parts to trim",
            "How many silent chunks to remove from the beginning."),
        ("start_duration", "Minimum leading silence length (seconds)",
            "Only trim leading silence chunks that are at least this long (default: 0.2)."),
        ("stop_periods", "Silent parts to trim after audio starts",
            "How many silence chunks to trim after audio has started (-1 means all)."),
        ("stop_duration", "Minimum inner silence length (seconds)",
            "Only trim middle/end silence chunks that are at least this long."),
        ("stop_silence", "Pause to keep after trimmed silence (seconds)",
            "Leaves a short pause before the next word (default: 0.2)."),
        ("window", "Detection window size (seconds)",
            "Smoothing window used by silence detection (default: 0.02)."),
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
            "Silence removal settings. Minimum silence duration and threshold are always shown. " +
            "Enable advanced settings to configure all remaining filter options.")
    {
        _settings = settings;
        var page = (ScrolledWindow)Window;
        page.SetScrollRate(8, 8);
        var box = new StaticBox(page, "Silence removal (FFmpeg silenceremove)");
        _group = new StaticBoxSizer(box);

        var basic = Grid();
        _minimum = Field(box, basic, "Minimum silence duration (seconds)", MinimumHint, settings.StopDuration);
        _threshold = Field(box, basic, "Silence threshold", ThresholdHint, settings.Threshold);
        _group.Add(basic, flags: SizerFlags.All | SizerFlags.Expand, border: 8);

        _advanced = new CheckBox(box, label: "Show advanced settings") { Checked = settings.Advanced };
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

        var detectionLabel = new StaticText(box, label: "Detection mode");
        detectionLabel.ToolTip = DetectionHint;
        _detection = new Choice(box);
        _detection.Add("Peak (fast reaction)");
        _detection.Add("RMS (smoother)");
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
            "Minimum silence duration in seconds. Silence shorter than this value is kept. " +
            "Increase it to preserve short pauses. Decrease it to trim more aggressively.");
        Help(_threshold,
            "Silence level threshold. Audio quieter than this level is treated as silence. " +
            "Enter only the number, for example -20, -30, or -40.");
        Help(_advanced,
            "Show advanced settings. When enabled, extra silenceremove parameters are displayed. " +
            "When disabled, only minimum duration and threshold are used.");
        Help(_detection,
            "Detection mode for silence analysis. Peak reacts quickly to speech transients. RMS is smoother.");
    }

    public override string? Validate()
    {
        if (_minimum.Value.Trim().Length > 0 && !NonNegative(_minimum.Value))
        {
            _minimum.Focus();
            return "Minimum silence duration must be a non-negative number.";
        }
        if (_threshold.Value.Trim().Length > 0 && !Number(_threshold.Value))
        {
            _threshold.Focus();
            return "Silence threshold must be a valid number.";
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
                    return $"{label} must be an integer value.";
                }
                if (key == "start_periods" && integer < 0)
                {
                    control.Focus();
                    return $"{label} must be zero or greater.";
                }
                if (key == "stop_periods" && integer < -1)
                {
                    control.Focus();
                    return $"{label} must be -1 or greater.";
                }
            }
            else if (!NonNegative(text))
            {
                control.Focus();
                return $"{label} must be a non-negative number.";
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
        "start_periods" => "Leading silent parts to trim. This controls how many silent chunks are removed from the beginning. Typical value is 1.",
        "start_duration" => "Minimum leading silence length in seconds. Only leading silence at least this long is removed. Default is 0.2.",
        "stop_periods" => "Silent parts to trim after audio starts. Use -1 to trim all matching silent parts.",
        "stop_duration" => "Minimum inner silence length in seconds. Only silence in the middle or end at least this long is removed.",
        "stop_silence" => "Pause to keep after trimmed silence, in seconds. Default is 0.2.",
        "window" => "Detection window size in seconds. Default is 0.02.",
        _ => "Silence removal setting. Enter a value and press OK to save.",
    };
}
