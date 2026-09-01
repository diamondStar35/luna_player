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
        // Translators: Spoken description of the Audio settings page, read when the page is opened.
        : base(new ScrolledWindow(parent), Tr("Audio settings. Use Tab to move between controls. Press F1 on a specific control to hear detailed help."))
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
        // Translators: Label of the box holding how much louder or quieter one press of the volume keys makes the sound.
        var volumeStepLabel = new StaticText(panel, label: Tr("Volume step"));
        _volumeStep = new SpinCtrl(panel, settings.VolumeStep, 1, 20);
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
        AddField(sizer, volumeStepLabel, _volumeStep);
        AddField(sizer, endBehaviorLabel, _endBehavior);
        sizer.Add(_wrap, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_savePositions, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_normalize, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_mono, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        panel.SetSizer(sizer);
        Help(_customSeek,
            // Translators: Help text for the box holding the seek amount used by the "Custom value" seek step.
            Tr("Custom seek value in seconds. This value is used when the custom seek step is selected. " +
            "Enter a positive number like 5, 10, or 30. Decimals are allowed, for example 2.5 seconds."));
        Help(_speedStep,
            // Translators: Help text for the box holding how much one press of the speed keys changes the playing speed.
            Tr("Speed step used when increasing or decreasing playback speed. Enter a positive decimal value like 0.025 or 0.1."));
        Help(_volumeStep,
            // Translators: Help text for the box holding how much one press of the volume keys changes the loudness.
            Tr("Volume step used when pressing volume up or down. Allowed range is from 1 to 20."));
        Help(_endBehavior,
            // Translators: Help text for the list that chooses what the player does at the end of a file. It names the
            // three entries in that list, which should read the same here as they do there.
            Tr("What happens after a file ends. Advance to the next file moves to the next item in the playlist. " +
            "Loop the file repeats the same file automatically. Do nothing keeps playback stopped at the end. " +
            "Possible values: Advance to the next file, Loop the file, or Do nothing."));
        Help(_wrap,
            // Translators: Help text for the tick box that carries on from the first file after the last one.
            Tr("Wrap to top for multiple files. When enabled and the playlist has more than one file, moving next from the last file goes to the first file, " +
            "and moving previous from the first file goes to the last file. Advance-at-end uses the same wrapping behavior."));
        Help(_savePositions,
            // Translators: Help text for the tick box that remembers where each file was stopped.
            Tr("Save current position for each file. When enabled, the app stores the current position of files and restores that position when navigating between files."));
        Help(_normalize,
            // Translators: Help text for the tick box that evens out the loudness and holds back the loudest peaks.
            // "dynaudnorm" and "alimiter" are the names of the filters doing the work and are not translated.
            Tr("Enable dynamic normalize and limiter audio filter. When enabled, audio uses dynaudnorm followed by alimiter to reduce clipping at high volume boosts. " +
            "Disable it to use raw output without this processing."));
        Help(_mono,
            // Translators: Help text for the tick box that plays the left and right channels mixed together.
            Tr("Play audio as Mono. When enabled, a mono downmix filter is applied so left and right channels are combined. " +
            "Disable it to keep the original channel layout."));
    }

    public override string? Validate()
        => double.TryParse(_speedStep.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            // Translators: Error message shown when the speed step was typed as something other than a number above zero.
            ? null : Tr("Speed step must be a positive number.");

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
