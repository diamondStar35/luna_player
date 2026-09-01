using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class GeneralPreferences : Preferences
{
    private readonly GeneralSettings _settings;
    private readonly Choice _language;
    private readonly string[] _languages;
    private readonly CheckBox _speakNavigation;
    private readonly CheckBox _checkUpdates;
    private readonly CheckBox _rememberPosition;
    private readonly CheckBox _saveOnClose;
    private readonly Choice _verbosity;
    private readonly Choice _openMode;

    internal GeneralPreferences(Window parent, GeneralSettings settings, PrefsOps operations)
        // Translators: Spoken description of the General settings page, read when the page is opened.
        : base(new Panel(parent), Tr("General settings. Use Tab to move between controls. Press F1 on a specific control to hear detailed help."))
    {
        _settings = settings;
        var panel = (Panel)Window;
        // Translators: Label of the list for choosing the language the player speaks and shows its windows in.
        var languageLabel = new StaticText(panel, label: Tr("Language"));
        // What the player can be switched to is whatever it ships a catalogue for, so the list is built from
        // those rather than from a fixed table that would drift as translations arrive.
        var available = Localization.AvailableLanguages()
            .Select(code => (Code: code, Name: Localization.LanguageName(code)))
            .OrderBy(language => language.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        _languages = [Localization.SystemLanguage, .. available.Select(language => language.Code)];
        // Translators: First entry in the language list: use whatever language Windows itself is set to.
        _language = Choice(panel, [Tr("System default"), .. available.Select(language => language.Name)],
            LanguageIndex(settings.Language));
        // Translators: Tick box on the General settings page: start again where playing stopped last time.
        _rememberPosition = new CheckBox(panel, label: Tr("Remember last file position")) { Checked = settings.RememberLastPosition };
        // Translators: Tick box on the General settings page: say the name of each file as the user moves through the list.
        _speakNavigation = new CheckBox(panel, label: Tr("Speak file name when navigating (Previous/Next)")) { Checked = settings.SpeakFileOnNavigation };
        // Translators: Tick box on the General settings page: look for a newer version of the player when it starts.
        _checkUpdates = new CheckBox(panel, label: Tr("Check for app updates on startup")) { Checked = settings.CheckUpdatesOnStartup };
        // Translators: Tick box on the General settings page: keep changes such as volume and speed when the player closes.
        _saveOnClose = new CheckBox(panel, label: Tr("Save settings on close")) { Checked = settings.SaveOnClose };
        // Translators: Label of the list that chooses how much detail the player speaks.
        var verbosityLabel = new StaticText(panel, label: Tr("Verbosity"));
        _verbosity = Choice(panel, [
            // Translators: One of the two amounts of spoken detail: whole, clearly worded messages.
            Tr("Beginner"),
            // Translators: One of the two amounts of spoken detail: short messages for users who know the player well.
            Tr("Advanced")], (int)settings.Verbosity);
        // Translators: Label of the list that chooses how much of a folder is loaded when one file is opened.
        var openModeLabel = new StaticText(panel, label: Tr("What would you like to open with files?"));
        _openMode = Choice(panel,
            [
                // Translators: One way of opening a file: load nothing but that one file.
                Tr("Open the file only"),
                // Translators: One way of opening a file: load every media file sitting in the same folder as well.
                Tr("Open the file and the main folder files"),
                // Translators: One way of opening a file: load the media files in its folder and in the folders inside it too.
                Tr("Open the file with the main and subfolder files")],
            (int)settings.OpenFilesMode);
        // Translators: Button on the General settings page that tells Windows this player can open media files.
        var register = new Button(panel, label: Tr("Register file extensions"));
        // Translators: Button on the General settings page that undoes telling Windows this player can open media files.
        var unregister = new Button(panel, label: Tr("Unregister file extensions"));
        register.Click += (_, _) => ShowAssociationResult(panel, operations.RegisterFiles(),
            // Translators: Title of the message shown once Windows has been told this player can open media files.
            Tr("Registration"),
            // Translators: Message shown once Windows has been told this player can open media files.
            Tr("File extensions registered successfully."),
            // Translators: Title of the message shown when Windows could not be told this player can open media files.
            Tr("Registration Error"),
            // Translators: Message shown when Windows could not be told this player can open media files.
            Tr("Could not register file extensions."));
        unregister.Click += (_, _) => ShowAssociationResult(panel, operations.UnregisterFiles(),
            // Translators: Title of the message shown once this player has stopped being offered for media files.
            Tr("Unregister"),
            // Translators: Message shown once this player has stopped being offered for media files.
            Tr("File extensions unregistered successfully."),
            // Translators: Title of the message shown when this player could not stop being offered for media files.
            Tr("Unregister Error"),
            // Translators: Message shown when this player could not stop being offered for media files.
            Tr("Could not unregister file extensions."));

        var sizer = new BoxSizer(Orientation.Vertical);
        AddChoice(sizer, languageLabel, _language);
        sizer.Add(_rememberPosition,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
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

        Help(_language,
            // Translators: Help text for the language list, spoken when the user asks for help on it.
            Tr("Application language. " +
            "System default follows your OS language. " +
            "Choosing a specific language applies it on next app start."));
        Help(_rememberPosition,
            // Translators: Help text for the tick box that starts again where playing stopped last time.
            Tr("Remember last file position. When enabled, the player saves the current file and playback time on exit, " +
            "then restores that position next time. When disabled, the player starts fresh each launch."));
        Help(_speakNavigation,
            // Translators: Help text for the tick box that says the name of each file as the user moves through the list.
            Tr("Speak file name when navigating. When enabled, the player will announce the name of the new file when you move to the previous or next track. " +
            "This is helpful for identifying files without manually requesting file information."));
        Help(_checkUpdates,
            // Translators: Help text for the tick box that looks for a newer version of the player when it starts.
            Tr("Check for app updates on startup. When enabled, the app checks online update metadata at launch and prompts when a newer version is available."));
        Help(_saveOnClose,
            // Translators: Help text for the tick box that keeps changes such as volume and speed when the player closes.
            Tr("Save settings on close. When enabled, current settings are written when the app closes. " +
            "When disabled, closing the app does not save session changes such as volume, speed, or other setting updates."));
        Help(_verbosity,
            // Translators: Help text for the list that chooses how much detail the player speaks. Beginner and Advanced are the two entries in that list and should read the same here as they do there.
            Tr("Verbosity controls speech detail. Beginner gives clearer full messages. Advanced gives shorter, compact announcements. " +
            "Possible values: Beginner or Advanced."));
        Help(_openMode,
            // Translators: Help text for the list that chooses how much of a folder is loaded when one file is opened. It names the three entries in that list, which should read the same here as they do there.
            Tr("Open with files behavior controls what happens when you open a single file. " +
            "Open the file only loads just that file. Open the file and the main folder files loads all supported files in the same folder. " +
            "Open the file with the main and subfolder files scans the folder recursively and loads files from subfolders too."));
        Help(register,
            // Translators: Help text for the button that tells Windows this player can open media files.
            Tr("Register file extensions writes Windows registry entries for supported media types. " +
            "This lets files open with this app from Explorer and Open With. " +
            "On modern Windows, default app choice can still require user confirmation in system Default apps settings."));
        Help(unregister,
            // Translators: Help text for the button that undoes telling Windows this player can open media files.
            Tr("Unregister file extensions removes registry entries created by this app for media associations. " +
            "This does not delete your media files. Windows may still keep separate user default selections managed by system settings."));
    }

    public override void Apply()
    {
        // Read only at startup: every window and menu has already been built in the old language, which is
        // what the control's own help tells the user.
        _settings.Language = _languages[Math.Clamp(_language.SelectedIndex, 0, _languages.Length - 1)];
        _settings.RememberLastPosition = _rememberPosition.Checked;
        _settings.SpeakFileOnNavigation = _speakNavigation.Checked;
        _settings.CheckUpdatesOnStartup = _checkUpdates.Checked;
        _settings.SaveOnClose = _saveOnClose.Checked;
        _settings.Verbosity = (SpeechVerbosity)Math.Max(0, _verbosity.SelectedIndex);
        _settings.OpenFilesMode = (OpenFilesMode)Math.Max(0, _openMode.SelectedIndex);
    }

    public override void Refresh()
    {
        _language.SelectedIndex = LanguageIndex(_settings.Language);
        _rememberPosition.Checked = _settings.RememberLastPosition;
        _speakNavigation.Checked = _settings.SpeakFileOnNavigation;
        _checkUpdates.Checked = _settings.CheckUpdatesOnStartup;
        _saveOnClose.Checked = _settings.SaveOnClose;
        _verbosity.SelectedIndex = (int)_settings.Verbosity;
        _openMode.SelectedIndex = (int)_settings.OpenFilesMode;
    }

    private int LanguageIndex(string? code)
    {
        var index = Array.FindIndex(_languages,
            language => language.Equals((code ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
        return index < 0 ? 0 : index;
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
