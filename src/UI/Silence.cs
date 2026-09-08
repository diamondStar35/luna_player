using System.Globalization;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class SilencePreferences : Preferences
{
    // Translators: Spoken description of the box for the shortest silence that will be trimmed, on the silence removal settings page.
    private static readonly string MinimumHint = Tr("Quiet sections shorter than this many seconds remain unchanged. The default is 0.5 seconds.");
    // Translators: Spoken description of the box for the loudness under which sound counts as silence, on the silence removal settings page.
    private static readonly string ThresholdHint = Tr("Samples quieter than this decibel level count as silence. Enter a number such as -30.");
    // Translators: Spoken description of the list of ways silence can be measured, on the silence removal settings page. Peak and RMS are the names of the two methods.
    private static readonly string DetectionHint = Tr("Peak follows the loudest sample in each analysis window and reacts to brief sounds. RMS averages the window for steadier detection.");

    private static readonly (string Key, string Label, string Hint)[] AdvancedFields =
    [
        // Translators: Label of an advanced silence removal setting: which section of sound should end trimming at the start of the file.
        ("start_periods", Tr("Leading silent parts to trim"),
            // Translators: Spoken description of the advanced setting that chooses which section of sound ends trimming at the start of the file.
            Tr("Use 0 to leave the beginning untouched or 1 to remove its initial silence. Higher values continue trimming through additional sections of sound.")),
        // Translators: Label of an advanced silence removal setting: how long sound must continue before trimming at the start stops.
        ("start_duration", Tr("Sound required before leading trim stops (seconds)"),
            // Translators: Spoken description of the advanced setting for how long sound must continue before trimming at the start stops.
            Tr("Sound must stay above the threshold for this long before leading-silence trimming stops. The default is 0.2 seconds.")),
        // Translators: Label of an advanced silence removal setting: how many silent parts to cut once the sound has begun.
        ("stop_periods", Tr("Silent parts to trim after audio starts"),
            // Translators: Spoken description of the advanced silence removal setting for how many silent parts to cut once the sound has begun. Minus one means every one of them.
            Tr("Use -1 to shorten every qualifying pause after sound begins, or 0 to leave later pauses untouched.")),
        // Translators: Label of an advanced silence removal setting: how long silence in the middle or at the end must be before it is cut.
        ("stop_duration", Tr("Minimum inner silence length (seconds)"),
            // Translators: Spoken description of the advanced silence removal setting for how long silence in the middle or at the end must be before it is cut.
            Tr("A quiet section after sound begins must last at least this long before it is shortened.")),
        // Translators: Label of an advanced silence removal setting: how much of a pause to leave where silence was cut.
        ("stop_silence", Tr("Pause to keep after trimmed silence (seconds)"),
            // Translators: Spoken description of the advanced silence removal setting for how much of a pause to leave where silence was cut.
            Tr("Keeps up to this much of a pause instead of removing it completely. The default is 0.2 seconds.")),
        // Translators: Label of an advanced silence removal setting: the length of sound looked at at once when deciding whether it is silent.
        ("window", Tr("Detection window size (seconds)"),
            // Translators: Spoken description of the advanced silence removal setting for the length of sound looked at at once when deciding whether it is silent.
            Tr("Sets how many seconds of samples are measured together. Larger windows are steadier; smaller windows react faster. The default is 0.02.")),
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
            Tr("The basic controls decide how long and how quiet a pause must be before Luna shortens it. " +
            "Advanced controls treat silence at the beginning and after sound starts separately."))
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
            Tr("Raise this value to preserve more short pauses, or lower it to remove briefer pauses. " +
            "A quiet section is changed only after it lasts for the entered number of seconds."));
        Help(_threshold,
            // Translators: Help text for the box holding the loudness under which sound counts as silence, spoken when the user asks for help on it.
            Tr("Enter a decibel value such as -30. A less negative value treats louder audio as silence and removes more; " +
            "a more negative value limits removal to very quiet audio."));
        Help(_advanced,
            // Translators: Help text for the tick box that shows the rest of the silence removal settings.
            Tr("Reveals separate controls for leading and later silence, the amount of each pause to retain, " +
            "and the method used to measure loudness."));
        Help(_detection,
            // Translators: Help text for the list of ways silence can be measured. Peak and RMS are the names of the two methods.
            Tr("Peak uses the loudest sample in each analysis window and responds to brief sounds. " +
            "RMS uses average energy, producing a steadier result that is less affected by short spikes."));
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
                // {field} is the name of the setting, such as "Sound required before leading trim stops (seconds)".
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
        // Translators: Help text for the advanced setting that chooses which section of sound ends trimming at the start of the file.
        "start_periods" => Tr("Use 0 to preserve the beginning or 1 to remove initial silence until sustained sound is found. " +
            "Values above 1 continue discarding audio through additional non-silent sections."),
        // Translators: Help text for the advanced setting holding how long sound must continue before trimming at the start stops.
        "start_duration" => Tr("After initial trimming, sound must remain above the threshold for this many seconds before Luna starts keeping it. " +
            "The default is 0.2."),
        // Translators: Help text for the advanced setting holding how many silent parts to cut once the sound has begun.
        "stop_periods" => Tr("Use -1 to shorten every qualifying pause after sound begins. Use 0 to preserve later silence; positive values limit how many sections are removed."),
        // Translators: Help text for the advanced setting holding how long silence in the middle or at the end must be before it is cut.
        "stop_duration" => Tr("A pause after sound begins must remain below the threshold for this many seconds before Luna shortens it."),
        // Translators: Help text for the advanced setting holding how much of a pause to leave where silence was cut.
        "stop_silence" => Tr("Enter how many seconds of a removed pause should remain, so words do not run together. The default is 0.2."),
        // Translators: Help text for the advanced setting holding the length of sound looked at at once when deciding whether it is silent.
        "window" => Tr("Sets the span of audio used for each loudness measurement. Larger values smooth sudden changes; smaller values react faster. " +
            "The default is 0.02 seconds."),
        // Translators: Help text used for any silence removal setting with no help text of its own.
        _ => Tr("Enter a numeric value, then press OK to apply it."),
    };
}
