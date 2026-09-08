using System.Globalization;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class AudioPreferences : Preferences
{
    private readonly AudioSettings _settings;
    private readonly TextCtrl _customSeek;
    private readonly TextCtrl _speedStep;
    private readonly TextCtrl _pitchStep;
    private readonly SpinCtrl _volumeStep;
    private readonly SpinCtrl _panStep;
    private readonly Choice _endBehavior;
    private readonly CheckBox _wrap;
    private readonly CheckBox _savePositions;
    private readonly CheckBox _normalize;
    private readonly CheckBox _mono;

    internal AudioPreferences(Window parent, AudioSettings settings)
        // Translators: Spoken description of the Audio settings page, read when the page is opened.
        : base(new ScrolledWindow(parent), Tr("Move through this page with Tab or Shift+Tab. Press F1 while a control is focused to hear what it changes."))
    {
        _settings = settings;
        var panel = (ScrolledWindow)Window;
        panel.SetScrollRate(8, 8);
        // Translators: Label of the box holding the seek amount used by the "Custom value" seek step, in seconds.
        var customSeekLabel = new StaticText(panel, label: Tr("Custom seek value (seconds)"));
        _customSeek = new TextCtrl(panel, value: settings.CustomSeekStep.ToString(CultureInfo.InvariantCulture));
        // Translators: Label of the box holding how much faster or slower one press of the speed keys makes the file play.
        var speedStepLabel = new StaticText(panel, label: Tr("Speed step"));
        _speedStep = new TextCtrl(panel, value: settings.SpeedStep.ToString(CultureInfo.InvariantCulture));
        // Translators: Label of the box holding how many semitones one press of a pitch key changes the sound.
        var pitchStepLabel = new StaticText(panel, label: Tr("Pitch step (semitones)"));
        _pitchStep = new TextCtrl(panel, value: settings.PitchStep.ToString(CultureInfo.InvariantCulture));
        // Translators: Label of the box holding how much louder or quieter one press of the volume keys makes the sound.
        var volumeStepLabel = new StaticText(panel, label: Tr("Volume step"));
        _volumeStep = new SpinCtrl(panel, settings.VolumeStep, 1, 20);
        // Translators: Label of the box holding how far one press moves the sound left or right.
        var panStepLabel = new StaticText(panel, label: Tr("Pan step (percent)"));
        _panStep = new SpinCtrl(panel, settings.PanStep, 1, 100);
        // Translators: Label of the list that chooses what the player does when it reaches the end of a file.
        var endBehaviorLabel = new StaticText(panel, label: Tr("What happens after a file ends?"));
        _endBehavior = Choice(panel, [
            // Translators: One of the things the player can do at the end of a file: play the next one.
            Tr("Advance to the next file"),
            // Translators: One of the things the player can do at the end of a file: play the same one again.
            Tr("Loop the file"),
            // Translators: One of the things the player can do at the end of a file: stop there.
            Tr("Do nothing")], (int)settings.EndBehavior);
        // Translators: Tick box on the Audio settings page: after the last file, carry on from the first one again.
        _wrap = new CheckBox(panel, label: Tr("Wrap to top for multiple files")) { Checked = settings.WrapPlaylist };
        // Translators: Tick box on the Audio settings page: remember where each file was stopped and start there again.
        _savePositions = new CheckBox(panel, label: Tr("Save current position for each file")) { Checked = settings.SaveFilePositions };
        // Translators: Tick box on the Audio settings page: even out the loudness and hold back the loudest peaks.
        _normalize = new CheckBox(panel, label: Tr("Enable dynamic normalize and limiter")) { Checked = settings.NormalizeAudio };
        // Translators: Tick box on the Audio settings page: play the left and right channels mixed together.
        _mono = new CheckBox(panel, label: Tr("Play audio as Mono")) { Checked = settings.MonoAudio };
        var sizer = new BoxSizer(Orientation.Vertical);
        AddField(sizer, customSeekLabel, _customSeek);
        AddField(sizer, speedStepLabel, _speedStep);
        AddField(sizer, pitchStepLabel, _pitchStep);
        AddField(sizer, volumeStepLabel, _volumeStep);
        AddField(sizer, panStepLabel, _panStep);
        AddField(sizer, endBehaviorLabel, _endBehavior);
        sizer.Add(_wrap, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_savePositions, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_normalize, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_mono, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        panel.SetSizer(sizer);
        Help(_customSeek,
            // Translators: Help text for the box holding the seek amount used by the "Custom value" seek step.
            Tr("Enter the number of seconds skipped by the seek shortcuts when Custom value is selected. " +
            "Positive whole numbers and decimals are accepted; for example, 2.5."));
        Help(_speedStep,
            // Translators: Help text for the box holding how much one press of the speed keys changes the playing speed.
            Tr("Sets how much each Speed Up or Speed Down command changes the playback rate. For example, 0.025 changes 1x speed to 1.025x."));
        Help(_pitchStep,
            // Translators: Help text for the box holding how much one press of the pitch keys raises or lowers a sound.
            Tr("Sets how far each Raise Pitch or Lower Pitch command moves. One semitone is the distance between adjacent piano keys; " +
                "0.1 semitone is 10 cents. Enter a value from 0.001 to 12."));
        Help(_volumeStep,
            // Translators: Help text for the box holding how much one press of the volume keys changes the loudness.
            Tr("Sets the amount added or subtracted by each Volume Up or Volume Down command. Enter a whole number from 1 to 20."));
        Help(_panStep,
            // Translators: Help text for the box holding how far one press moves the sound left or right.
            Tr("Sets how many percentage points each Pan Left or Pan Right command moves the sound. Enter a whole number from 1 to 100."));
        Help(_endBehavior,
            // Translators: Help text for the list that chooses what the player does at the end of a file.
            Tr("Choose whether reaching the end starts the next playlist item, repeats the current item, or leaves playback stopped at the end."));
        Help(_wrap,
            // Translators: Help text for the tick box that carries on from the first file after the last one.
            Tr("With at least two items loaded, Next on the final item returns to the first and Previous on the first returns to the final item. " +
            "Automatic advance follows the same rule."));
        Help(_savePositions,
            // Translators: Help text for the tick box that remembers where each file was stopped.
            Tr("Keeps a separate resume time for every item and returns to that time when the item is opened again during navigation."));
        Help(_normalize,
            // Translators: Help text for the tick box that evens out the loudness and holds back the loudest peaks.
            Tr("Evens out changing loudness and restrains the loudest peaks to reduce clipping. Clear this option to hear the source without those filters."));
        Help(_mono,
            // Translators: Help text for the tick box that plays the left and right channels mixed together.
            Tr("Combines the left and right channels into one mono output. Clear this option to preserve the source channel layout."));
    }

    public override string? Validate()
    {
        if (!double.TryParse(_speedStep.Value.Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var speedStep)
            || !double.IsFinite(speedStep) || speedStep <= 0)
        {
            // Translators: Error message shown when the speed step was typed as something other than a number above zero.
            return Tr("Speed step must be a positive number.");
        }
        if (!double.TryParse(_pitchStep.Value.Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var pitchStep)
            || !double.IsFinite(pitchStep)
            || pitchStep < AudioSettings.MinimumPitchStep
            || pitchStep > AudioSettings.MaximumPitchStep)
        {
            // Translators: Error shown when the pitch step is not a number from 0.001 through 12.
            return Tr("Pitch step must be a number from 0.001 to 12.");
        }
        return null;
    }

    public override void Apply()
    {
        if (double.TryParse(_customSeek.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seek) && seek > 0)
            _settings.CustomSeekStep = seek;
        _settings.SpeedStep = double.Parse(_speedStep.Value.Trim(), CultureInfo.InvariantCulture);
        _settings.PitchStep = double.Parse(_pitchStep.Value.Trim(), CultureInfo.InvariantCulture);
        _settings.VolumeStep = _volumeStep.Value;
        _settings.PanStep = _panStep.Value;
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
        _pitchStep.Value = _settings.PitchStep.ToString(CultureInfo.InvariantCulture);
        _volumeStep.Value = _settings.VolumeStep;
        _panStep.Value = _settings.PanStep;
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
