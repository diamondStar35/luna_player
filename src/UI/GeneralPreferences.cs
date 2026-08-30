using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class GeneralPreferences : Preferences
{
    private readonly GeneralSettings _settings;
    private readonly CheckBox _speakNavigation;
    private readonly CheckBox _checkUpdates;
    private readonly CheckBox _rememberPosition;
    private readonly CheckBox _saveOnClose;
    private readonly Choice _verbosity;
    private readonly Choice _openMode;

    internal GeneralPreferences(Window parent, GeneralSettings settings, PrefsOps operations)
        : base(new Panel(parent), "General settings. Use Tab to move between controls. Press F1 on a specific control to hear detailed help.")
    {
        _settings = settings;
        var panel = (Panel)Window;
        _rememberPosition = new CheckBox(panel, label: "Remember last file position") { Checked = settings.RememberLastPosition };
        _speakNavigation = new CheckBox(panel, label: "Speak file name when navigating (Previous/Next)") { Checked = settings.SpeakFileOnNavigation };
        _checkUpdates = new CheckBox(panel, label: "Check for app updates on startup") { Checked = settings.CheckUpdatesOnStartup };
        _saveOnClose = new CheckBox(panel, label: "Save settings on close") { Checked = settings.SaveOnClose };
        var verbosityLabel = new StaticText(panel, label: "Verbosity");
        _verbosity = Choice(panel, ["Beginner", "Advanced"], (int)settings.Verbosity);
        var openModeLabel = new StaticText(panel, label: "What would you like to open with files?");
        _openMode = Choice(panel,
            ["Open the file only", "Open the file and the main folder files", "Open the file with the main and subfolder files"],
            (int)settings.OpenFilesMode);
        var register = new Button(panel, label: "Register file extensions");
        var unregister = new Button(panel, label: "Unregister file extensions");
        register.Click += (_, _) => ShowAssociationResult(panel, operations.RegisterFiles(),
            "Registration", "File extensions registered successfully.",
            "Registration Error", "Could not register file extensions.");
        unregister.Click += (_, _) => ShowAssociationResult(panel, operations.UnregisterFiles(),
            "Unregister", "File extensions unregistered successfully.",
            "Unregister Error", "Could not unregister file extensions.");

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_rememberPosition, flags: SizerFlags.All, border: 8);
        sizer.Add(_speakNavigation, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_checkUpdates, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_saveOnClose, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        AddChoice(sizer, verbosityLabel, _verbosity);
        AddChoice(sizer, openModeLabel, _openMode);
        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(register, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(unregister);
        sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        panel.SetSizer(sizer);

        Help(_rememberPosition,
            "Remember last file position. When enabled, the player saves the current file and playback time on exit, " +
            "then restores that position next time. When disabled, the player starts fresh each launch.");
        Help(_speakNavigation,
            "Speak file name when navigating. When enabled, the player will announce the name of the new file when you move to the previous or next track. " +
            "This is helpful for identifying files without manually requesting file information.");
        Help(_checkUpdates,
            "Check for app updates on startup. When enabled, the app checks online update metadata at launch and prompts when a newer version is available.");
        Help(_saveOnClose,
            "Save settings on close. When enabled, current settings are written when the app closes. " +
            "When disabled, closing the app does not save session changes such as volume, speed, or other setting updates.");
        Help(_verbosity,
            "Verbosity controls speech detail. Beginner gives clearer full messages. Advanced gives shorter, compact announcements. " +
            "Possible values: Beginner or Advanced.");
        Help(_openMode,
            "Open with files behavior controls what happens when you open a single file. " +
            "Open the file only loads just that file. Open the file and the main folder files loads all supported files in the same folder. " +
            "Open the file with the main and subfolder files scans the folder recursively and loads files from subfolders too.");
        Help(register,
            "Register file extensions writes Windows registry entries for supported media types. " +
            "This lets files open with this app from Explorer and Open With. " +
            "On modern Windows, default app choice can still require user confirmation in system Default apps settings.");
        Help(unregister,
            "Unregister file extensions removes registry entries created by this app for media associations. " +
            "This does not delete your media files. Windows may still keep separate user default selections managed by system settings.");
    }

    public override void Apply()
    {
        _settings.RememberLastPosition = _rememberPosition.Checked;
        _settings.SpeakFileOnNavigation = _speakNavigation.Checked;
        _settings.CheckUpdatesOnStartup = _checkUpdates.Checked;
        _settings.SaveOnClose = _saveOnClose.Checked;
        _settings.Verbosity = (SpeechVerbosity)Math.Max(0, _verbosity.SelectedIndex);
        _settings.OpenFilesMode = (OpenFilesMode)Math.Max(0, _openMode.SelectedIndex);
    }

    public override void Refresh()
    {
        _rememberPosition.Checked = _settings.RememberLastPosition;
        _speakNavigation.Checked = _settings.SpeakFileOnNavigation;
        _checkUpdates.Checked = _settings.CheckUpdatesOnStartup;
        _saveOnClose.Checked = _settings.SaveOnClose;
        _verbosity.SelectedIndex = (int)_settings.Verbosity;
        _openMode.SelectedIndex = (int)_settings.OpenFilesMode;
    }

    private static void ShowAssociationResult(Window parent, UiOperation result,
        string successCaption, string successMessage, string errorCaption, string errorMessage)
    {
        if (result.Success)
            Wx.MessageBox(successMessage, successCaption, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, parent);
        else
            Wx.MessageBox($"{errorMessage}\n{result.Error}", errorCaption, MessageBoxStyle.Ok | MessageBoxStyle.IconError, parent);
    }

    private static void AddChoice(BoxSizer parent, StaticText label, Choice choice)
    {
        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(label, flags: SizerFlags.BorderBottom, border: 4);
        sizer.Add(choice, flags: SizerFlags.Expand);
        parent.Add(sizer,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand,
            border: 8);
    }

    private static Choice Choice(Window parent, IEnumerable<string> values, int selected)
    { var choice = new Choice(parent); foreach (var value in values) choice.Add(value); choice.SelectedIndex = selected; return choice; }
}
