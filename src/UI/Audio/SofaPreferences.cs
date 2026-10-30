using System.Globalization;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using WxSharp;

namespace LunaPlayer.UI.Audio;

internal sealed class SofaPreferences : LunaPlayer.UI.Preferences.PreferencesPage
{
    private readonly AudioSettings _settings;
    private readonly CheckBox _enabled;
    private readonly TextCtrl _file;
    private readonly TextCtrl _gain;
    private readonly SpinCtrl _rotation;
    private readonly SpinCtrl _elevation;
    private readonly SpinCtrl _rotationStep;
    private readonly SpinCtrl _elevationStep;
    private readonly List<Window> _dependentControls = [];

    internal SofaPreferences(Window parent, AudioSettings settings)
        : base(new ScrolledWindow(parent),
            // Translators: F1 help for the SOFA headphone spatialization page.
            Tr("Use a SOFA HRTF file to make audio sound like speakers around you when listening with headphones. Move between controls with Tab or Shift+Tab and press F1 for help."))
    {
        _settings = settings;
        var panel = (ScrolledWindow)Window;
        panel.SetScrollRate(8, 8);
        // Translators: Checkbox enabling SOFA-based binaural audio processing for headphones.
        _enabled = new CheckBox(panel, label: Tr("Enable headphone spatialization"));
        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_enabled, flags: SizerFlags.All, border: 8);
        // Translators: Label for the path to the user's SOFA HRTF file. Keep SOFA unchanged.
        _file = Field(panel, sizer, Tr("SOFA file"));
        // Translators: Button opening the file picker for a SOFA HRTF file.
        var browse = new Button(panel, label: Tr("Browse..."));
        browse.Click += Browse;
        _dependentControls.Add(browse);
        sizer.Add(browse, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        // Translators: Output gain applied by the SOFA filter, in decibels.
        _gain = Field(panel, sizer, Tr("Gain (dB)"));
        // Translators: Horizontal rotation of the virtual speakers around the listener.
        _rotation = SpinField(panel, sizer, Tr("Rotation (degrees)"), -360, 360);
        // Translators: Vertical angle of the virtual speakers relative to the listener.
        _elevation = SpinField(panel, sizer, Tr("Elevation (degrees)"),
            -(int)AudioSettings.MaximumSofaElevation, (int)AudioSettings.MaximumSofaElevation);
        // Translators: How far each left/right spatialization command rotates the virtual speakers.
        _rotationStep = SpinField(panel, sizer, Tr("Rotation step (degrees)"),
            (int)AudioSettings.MinimumSofaStep, (int)AudioSettings.MaximumSofaStep);
        // Translators: How far each raise/lower spatialization command changes the vertical angle.
        _elevationStep = SpinField(panel, sizer, Tr("Elevation step (degrees)"),
            (int)AudioSettings.MinimumSofaStep, (int)AudioSettings.MaximumSofaStep);
        panel.SetSizer(sizer);
        _enabled.Toggled += (_, _) => ShowSettings();
        Help(_enabled,
            // Translators: F1 help for the headphone spatialization enable checkbox.
            Tr("Enable binaural rendering for headphones and show its settings. Leave this off when listening through speakers or to audio that is already binaural. The Player menu can toggle spatialization for this session without changing this default."));
        // Translators: F1 help for the SOFA file path and browse button.
        var fileHelp = Tr("Choose a local .sofa file containing head-related impulse responses (HRTFs). Luna uses FFmpeg's sofalizer filter with libmysofa. The file must remain available at this path; it is not copied into settings backups.");
        Help(_file, fileHelp);
        Help(browse, fileHelp);
        Help(_gain,
            // Translators: F1 help for SOFA output gain.
            Tr("Enter a gain from -20 to 40 dB. The default is 0. Lower the gain if the spatialized sound clips or is too loud."));
        Help(_rotation,
            // Translators: F1 help for the horizontal rotation angle.
            Tr("Set the default rotation of the virtual speakers around your head in whole degrees, from -360 to 360. Positive values turn right and negative values turn left. The default is 0. Player menu and shortcut changes are not saved."));
        Help(_elevation,
            // Translators: F1 help for the vertical angle.
            Tr("Set the default elevation of the virtual speakers above or below you in whole degrees, from -89 to 89. The default is 0. Player menu and shortcut changes are not saved."));
        Help(_rotationStep,
            // Translators: F1 help for the step used by the spatialization rotation commands.
            Tr("Set how many whole degrees each Rotate Left or Rotate Right command changes rotation, from 1 to 90. The default is 5. Rotation wraps around after a full turn."));
        Help(_elevationStep,
            // Translators: F1 help for the step used by the spatialization elevation commands.
            Tr("Set how many whole degrees each Raise Elevation or Lower Elevation command changes elevation, from 1 to 90. The default is 5. Elevation stops at -89 and 89 degrees."));
        Refresh();
    }

    public override string? Validate()
    {
        // Hidden fields must not prevent disabling the effect or receive validation focus.
        if (!_enabled.Checked)
            return null;
        if (!AudioFilters.IsSofaFile(_file.Value.Trim()))
        {
            _file.Focus();
            // Translators: Validation error when enabling SOFA processing without an existing .sofa file.
            return Tr("Choose an existing .sofa HRTF file before enabling headphone spatialization.");
        }
        if (!NumberInRange(_gain.Value, -20, 40))
        {
            _gain.Focus();
            // Translators: Validation error for SOFA output gain.
            return Tr("Gain must be a number from -20 to 40 dB.");
        }
        return null;
    }

    public override void Apply()
    {
        _settings.SofaEnabled = _enabled.Checked;
        var path = _file.Value.Trim();
        _settings.SofaFile = File.Exists(path) ? Path.GetFullPath(path) : path;
        // A hidden invalid gain draft must not prevent disabling the effect or overwrite its default.
        _settings.SofaGain = NumberOrDefault(_gain.Value, -20, 40, _settings.SofaGain);
        _settings.SofaRotation = _rotation.Value;
        _settings.SofaElevation = _elevation.Value;
        _settings.SofaRotationStep = _rotationStep.Value;
        _settings.SofaElevationStep = _elevationStep.Value;
    }

    public override void Refresh()
    {
        _enabled.Checked = _settings.SofaEnabled;
        _file.Value = _settings.SofaFile;
        _gain.Value = _settings.SofaGain.ToString(CultureInfo.InvariantCulture);
        _rotation.Value = WholeDegrees(_settings.SofaRotation);
        _elevation.Value = WholeDegrees(_settings.SofaElevation);
        _rotationStep.Value = WholeDegrees(_settings.SofaRotationStep);
        _elevationStep.Value = WholeDegrees(_settings.SofaElevationStep);
        ShowSettings();
    }

    private void Browse(object? sender, CommandEventArgs args)
    {
        using var dialog = new FileDialog(Window,
            // Translators: Title of the file picker for headphone HRTF data.
            Tr("Choose a SOFA file"), wildcard:
            // Translators: File picker filter. Translate only "SOFA files"; keep the extension and separators unchanged.
            Tr("SOFA files (*.sofa)|*.sofa"), style: FileDialogStyle.DefaultOpen);
        if (dialog.ShowModal() != StandardId.Ok)
            return;
        _file.Value = dialog.Path;
        _file.Focus();
    }

    private void ShowSettings()
    {
        var visible = _enabled.Checked;
        if (!visible)
        {
            // Also handle programmatic refresh while a child (including a native subcontrol) has focus.
            for (var focused = Window.FindFocus(); focused is not null; focused = focused.Parent)
                if (_dependentControls.Contains(focused))
                {
                    _enabled.Focus();
                    break;
                }
        }
        foreach (var control in _dependentControls)
            control.Show(visible);
        Window.Layout();
        Window.FitInside();
    }

    private TextCtrl Field(Window parent, BoxSizer sizer, string label)
    {
        var title = new StaticText(parent, label: label);
        var field = new TextCtrl(parent);
        AddField(sizer, title, field);
        return field;
    }

    private SpinCtrl SpinField(Window parent, BoxSizer sizer, string label, int minimum, int maximum)
    {
        var title = new StaticText(parent, label: label);
        var field = new SpinCtrl(parent, Math.Clamp(0, minimum, maximum), minimum, maximum);
        AddField(sizer, title, field);
        return field;
    }

    private void AddField(BoxSizer sizer, StaticText title, Window field)
    {
        _dependentControls.Add(title);
        _dependentControls.Add(field);
        sizer.Add(title, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(field, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
    }

    // Retain compatibility with saved decimal defaults while presenting integer spin controls.
    private static int WholeDegrees(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static double NumberOrDefault(string text, double minimum, double maximum, double previous)
        => NumberInRange(text, minimum, maximum) ? double.Parse(text.Trim(), CultureInfo.InvariantCulture) : previous;

    private static bool NumberInRange(string text, double minimum, double maximum)
        => double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && double.IsFinite(value) && value >= minimum && value <= maximum;
}
