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
    private readonly TextCtrl _rotation;
    private readonly TextCtrl _elevation;
    private readonly TextCtrl _rotationStep;
    private readonly TextCtrl _elevationStep;
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
        var browse = new Button(panel, label: Tr("Browse for SOFA file..."));
        browse.Click += Browse;
        _dependentControls.Add(browse);
        sizer.Add(browse, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        // Translators: Output gain applied by the SOFA filter, in decibels.
        _gain = Field(panel, sizer, Tr("Gain (dB)"));
        // Translators: Horizontal rotation of the virtual speakers around the listener.
        _rotation = Field(panel, sizer, Tr("Rotation (degrees)"));
        // Translators: Vertical angle of the virtual speakers relative to the listener.
        _elevation = Field(panel, sizer, Tr("Elevation (degrees)"));
        // Translators: How far each left/right spatialization command rotates the virtual speakers.
        _rotationStep = Field(panel, sizer, Tr("Rotation step (degrees)"));
        // Translators: How far each raise/lower spatialization command changes the vertical angle.
        _elevationStep = Field(panel, sizer, Tr("Elevation step (degrees)"));
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
            Tr("Set the default rotation of the virtual speakers around your head, from -360 to 360 degrees. The default is 0. Player menu and shortcut changes are not saved."));
        Help(_elevation,
            // Translators: F1 help for the vertical angle.
            Tr("Set the default elevation of the virtual speakers above or below you, from -90 to 90 degrees. The default is 0. Player menu and shortcut changes are not saved."));
        Help(_rotationStep,
            // Translators: F1 help for the step used by the spatialization rotation commands.
            Tr("Enter how many degrees each Rotate Left or Rotate Right command changes rotation, from 1 to 90. Decimals are accepted. The default is 5. Rotation wraps around after a full turn."));
        Help(_elevationStep,
            // Translators: F1 help for the step used by the spatialization elevation commands.
            Tr("Enter how many degrees each Raise Elevation or Lower Elevation command changes elevation, from 1 to 90. Decimals are accepted. The default is 5. Elevation stops at -90 and 90 degrees."));
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
        if (!NumberInRange(_rotation.Value, -360, 360))
        {
            _rotation.Focus();
            // Translators: Validation error for the SOFA horizontal rotation.
            return Tr("Rotation must be a number from -360 to 360 degrees.");
        }
        if (!NumberInRange(_elevation.Value, -90, 90))
        {
            _elevation.Focus();
            // Translators: Validation error for the SOFA vertical angle.
            return Tr("Elevation must be a number from -90 to 90 degrees.");
        }
        if (!NumberInRange(_rotationStep.Value, AudioSettings.MinimumSofaStep, AudioSettings.MaximumSofaStep))
        {
            _rotationStep.Focus();
            // Translators: Validation error for the number of degrees changed by a rotation command.
            return Tr("Rotation step must be a number from 1 to 90 degrees.");
        }
        if (!NumberInRange(_elevationStep.Value, AudioSettings.MinimumSofaStep, AudioSettings.MaximumSofaStep))
        {
            _elevationStep.Focus();
            // Translators: Validation error for the number of degrees changed by an elevation command.
            return Tr("Elevation step must be a number from 1 to 90 degrees.");
        }
        return null;
    }

    public override void Apply()
    {
        _settings.SofaEnabled = _enabled.Checked;
        var path = _file.Value.Trim();
        _settings.SofaFile = File.Exists(path) ? Path.GetFullPath(path) : path;
        // Valid edits are retained even if the user clears Enable before saving. Invalid hidden
        // drafts leave the previous default intact instead of preventing the effect from being disabled.
        _settings.SofaGain = NumberOrDefault(_gain.Value, -20, 40, _settings.SofaGain);
        _settings.SofaRotation = NumberOrDefault(_rotation.Value, -360, 360, _settings.SofaRotation);
        _settings.SofaElevation = NumberOrDefault(_elevation.Value, -90, 90, _settings.SofaElevation);
        _settings.SofaRotationStep = NumberOrDefault(_rotationStep.Value,
            AudioSettings.MinimumSofaStep, AudioSettings.MaximumSofaStep, _settings.SofaRotationStep);
        _settings.SofaElevationStep = NumberOrDefault(_elevationStep.Value,
            AudioSettings.MinimumSofaStep, AudioSettings.MaximumSofaStep, _settings.SofaElevationStep);
    }

    public override void Refresh()
    {
        _enabled.Checked = _settings.SofaEnabled;
        _file.Value = _settings.SofaFile;
        _gain.Value = _settings.SofaGain.ToString(CultureInfo.InvariantCulture);
        _rotation.Value = _settings.SofaRotation.ToString(CultureInfo.InvariantCulture);
        _elevation.Value = _settings.SofaElevation.ToString(CultureInfo.InvariantCulture);
        _rotationStep.Value = _settings.SofaRotationStep.ToString(CultureInfo.InvariantCulture);
        _elevationStep.Value = _settings.SofaElevationStep.ToString(CultureInfo.InvariantCulture);
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
        _dependentControls.Add(title);
        _dependentControls.Add(field);
        sizer.Add(title, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(field, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        return field;
    }

    private static double NumberOrDefault(string text, double minimum, double maximum, double previous)
        => NumberInRange(text, minimum, maximum) ? double.Parse(text.Trim(), CultureInfo.InvariantCulture) : previous;

    private static bool NumberInRange(string text, double minimum, double maximum)
        => double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && double.IsFinite(value) && value >= minimum && value <= maximum;
}
