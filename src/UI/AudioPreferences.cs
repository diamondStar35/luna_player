using System.Globalization;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class AudioPreferences : Preferences
{
    private readonly AudioSettings _settings;
    private readonly TextCtrl _customSeek;
    private readonly TextCtrl _speedStep;
    private readonly SpinCtrl _volumeStep;
    private readonly Choice _endBehavior;
    private readonly CheckBox _wrap;
    private readonly CheckBox _savePositions;
    private readonly CheckBox _normalize;
    private readonly CheckBox _mono;

    internal AudioPreferences(Window parent, AudioSettings settings)
        : base(new ScrolledWindow(parent), "Audio settings. Use Tab to move between controls. Press F1 on a specific control to hear detailed help.")
    {
        _settings = settings;
        var panel = (ScrolledWindow)Window;
        panel.SetScrollRate(8, 8);
        var customSeekLabel = new StaticText(panel, label: "Custom seek value (seconds)");
        _customSeek = new TextCtrl(panel, value: settings.CustomSeekStep.ToString(CultureInfo.InvariantCulture));
        var speedStepLabel = new StaticText(panel, label: "Speed step");
        _speedStep = new TextCtrl(panel, value: settings.SpeedStep.ToString(CultureInfo.InvariantCulture));
        var volumeStepLabel = new StaticText(panel, label: "Volume step");
        _volumeStep = new SpinCtrl(panel, settings.VolumeStep, 1, 20);
        var endBehaviorLabel = new StaticText(panel, label: "What happens after a file ends?");
        _endBehavior = Choice(panel, ["Advance to the next file", "Loop the file", "Do nothing"], (int)settings.EndBehavior);
        _wrap = new CheckBox(panel, label: "Wrap to top for multiple files") { Checked = settings.WrapPlaylist };
        _savePositions = new CheckBox(panel, label: "Save current position for each file") { Checked = settings.SaveFilePositions };
        _normalize = new CheckBox(panel, label: "Enable dynamic normalize and limiter") { Checked = settings.NormalizeAudio };
        _mono = new CheckBox(panel, label: "Play audio as Mono") { Checked = settings.MonoAudio };
        var sizer = new BoxSizer(Orientation.Vertical);
        AddField(sizer, customSeekLabel, _customSeek);
        AddField(sizer, speedStepLabel, _speedStep);
        AddField(sizer, volumeStepLabel, _volumeStep);
        AddField(sizer, endBehaviorLabel, _endBehavior);
        sizer.Add(_wrap, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_savePositions, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_normalize, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_mono, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        panel.SetSizer(sizer);
        Help(_customSeek,
            "Custom seek value in seconds. This value is used when the custom seek step is selected. " +
            "Enter a positive number like 5, 10, or 30. Decimals are allowed, for example 2.5 seconds.");
        Help(_speedStep,
            "Speed step used when increasing or decreasing playback speed. Enter a positive decimal value like 0.025 or 0.1.");
        Help(_volumeStep,
            "Volume step used when pressing volume up or down. Allowed range is from 1 to 20.");
        Help(_endBehavior,
            "What happens after a file ends. Advance to the next file moves to the next item in the playlist. " +
            "Loop the file repeats the same file automatically. Do nothing keeps playback stopped at the end. " +
            "Possible values: Advance to the next file, Loop the file, or Do nothing.");
        Help(_wrap,
            "Wrap to top for multiple files. When enabled and the playlist has more than one file, moving next from the last file goes to the first file, " +
            "and moving previous from the first file goes to the last file. Advance-at-end uses the same wrapping behavior.");
        Help(_savePositions,
            "Save current position for each file. When enabled, the app stores the current position of files and restores that position when navigating between files.");
        Help(_normalize,
            "Enable dynamic normalize and limiter audio filter. When enabled, audio uses dynaudnorm followed by alimiter to reduce clipping at high volume boosts. " +
            "Disable it to use raw output without this processing.");
        Help(_mono,
            "Play audio as Mono. When enabled, a mono downmix filter is applied so left and right channels are combined. " +
            "Disable it to keep the original channel layout.");
    }

    public override string? Validate()
        => double.TryParse(_speedStep.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? null : "Speed step must be a positive number.";

    public override void Apply()
    {
        if (double.TryParse(_customSeek.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seek) && seek > 0)
            _settings.CustomSeekStep = seek;
        _settings.SpeedStep = double.Parse(_speedStep.Value.Trim(), CultureInfo.InvariantCulture);
        _settings.VolumeStep = _volumeStep.Value;
        _settings.EndBehavior = (EndBehavior)Math.Max(0, _endBehavior.SelectedIndex);
        _settings.WrapPlaylist = _wrap.Checked;
        _settings.SaveFilePositions = _savePositions.Checked;
        _settings.NormalizeAudio = _normalize.Checked;
        _settings.MonoAudio = _mono.Checked;
    }

    public override void Refresh()
    {
        _customSeek.Value = _settings.CustomSeekStep.ToString(CultureInfo.InvariantCulture);
        _speedStep.Value = _settings.SpeedStep.ToString(CultureInfo.InvariantCulture);
        _volumeStep.Value = _settings.VolumeStep;
        _endBehavior.SelectedIndex = (int)_settings.EndBehavior;
        _wrap.Checked = _settings.WrapPlaylist;
        _savePositions.Checked = _settings.SaveFilePositions;
        _normalize.Checked = _settings.NormalizeAudio;
        _mono.Checked = _settings.MonoAudio;
    }

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
