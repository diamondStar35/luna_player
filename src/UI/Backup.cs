using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class BackupPreferences : Preferences
{
    private readonly PrefsOps _operations;
    private readonly Action<PlayerSettings> _replaceSettings;

    internal BackupPreferences(Window parent, PrefsOps operations, Action<PlayerSettings> replaceSettings)
        : base(new Panel(parent),
            // Translators: Spoken description of the backup and restore settings page, read when the page is opened.
            Tr("Backup and restore settings page. Use Export settings, Import settings, or Open user settings folder."))
    {
        _operations = operations;
        _replaceSettings = replaceSettings;
        var panel = (Panel)Window;
        // Translators: Button on the backup and restore settings page that saves a copy of the settings to a file the user chooses.
        var exportSettings = Button(panel, Tr("Export settings"), ExportSettings);
        // Translators: Button on the backup and restore settings page that loads settings back from a file the user chooses.
        var importSettings = Button(panel, Tr("Import settings"), ImportSettings);
        // Translators: Button on the backup and restore settings page that saves a copy of the bookmarks to a file the user chooses.
        var exportBookmarks = Button(panel, Tr("Export bookmarks"), ExportBookmarks);
        // Translators: Button on the backup and restore settings page that loads bookmarks back from a file the user chooses.
        var importBookmarks = Button(panel, Tr("Import bookmarks"), ImportBookmarks);
        // Translators: Button on the backup and restore settings page that puts every setting back the way it started.
        var reset = Button(panel, Tr("Reset settings"), ResetSettings);
        // Translators: Button on the backup and restore settings page that opens the folder where the player keeps its settings.
        var openFolder = Button(panel, Tr("Open user settings folder"), OpenFolder);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(exportSettings, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        foreach (var button in new[] { importSettings, exportBookmarks, importBookmarks, reset, openFolder })
            sizer.Add(button, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        panel.SetSizer(sizer);

        // Translators: Help text for the button that saves a copy of the settings, spoken when the user asks for help on it.
        Help(exportSettings, Tr("Export settings creates a copy of the current settings file."));
        // Translators: Help text for the button that loads settings back from a file, spoken when the user asks for help on it.
        Help(importSettings, Tr("Import settings replaces current settings with a selected settings file."));
        // Translators: Help text for the button that saves a copy of the bookmarks, spoken when the user asks for help on it. JSON is the file format and is not translated.
        Help(exportBookmarks, Tr("Export bookmarks creates a copy of the bookmarks JSON file."));
        // Translators: Help text for the button that loads bookmarks back from a file, spoken when the user asks for help on it.
        Help(importBookmarks, Tr("Import bookmarks replaces current bookmarks with a selected bookmarks file."));
        // Translators: Help text for the button that puts every setting back the way it started, spoken when the user asks for help on it.
        Help(reset, Tr("Reset settings restores all preferences to default values."));
        // Translators: Help text for the button that opens the folder where the player keeps its settings, spoken when the user asks for help on it.
        Help(openFolder, Tr("Open user settings folder opens the folder where this app stores its configuration."));
    }

    public override void Apply() { }

    private void ExportSettings(object? sender, CommandEventArgs args)
    {
        var path = SavePath(Tr("Export settings"), _operations.SettingsPath);
        if (path is null) return;
        ShowResult(_operations.ExportSettings(path),
            // Translators: Message shown once the settings have been saved to the chosen file.
            Tr("Settings exported successfully."), Tr("Export settings"),
            // Translators: Message shown when the settings could not be saved to the chosen file.
            Tr("Could not export settings."),
            // Translators: Title of the message shown when the settings could not be saved to the chosen file.
            Tr("Export error"));
    }

    private void ImportSettings(object? sender, CommandEventArgs args)
    {
        var path = OpenPath(Tr("Import settings"), _operations.SettingsPath);
        if (path is null) return;
        var settings = _operations.ImportSettings(path);
        if (settings is null)
            ShowResult(false, "", "",
                // Translators: Message shown when the chosen file does not hold settings the player can read.
                Tr("The selected settings file is invalid or could not be imported."),
                // Translators: Title of the message shown when settings could not be loaded from the chosen file.
                Tr("Import error"));
        else
        {
            _replaceSettings(settings);
            // Translators: Message shown once settings have been loaded from the chosen file.
            ShowResult(true, Tr("Settings imported successfully."), Tr("Import settings"), "", Tr("Import error"));
        }
    }

    private void ExportBookmarks(object? sender, CommandEventArgs args)
    {
        var path = SavePath(Tr("Export bookmarks"), _operations.BookmarksPath);
        if (path is null) return;
        ShowResult(_operations.ExportBookmarks(path),
            // Translators: Message shown once the bookmarks have been saved to the chosen file.
            Tr("Bookmarks exported successfully."), Tr("Export bookmarks"),
            // Translators: Message shown when the bookmarks could not be saved to the chosen file.
            Tr("Could not export bookmarks."), Tr("Export error"));
    }

    private void ImportBookmarks(object? sender, CommandEventArgs args)
    {
        var path = OpenPath(Tr("Import bookmarks"), _operations.BookmarksPath);
        if (path is null) return;
        ShowResult(_operations.ImportBookmarks(path),
            // Translators: Message shown once bookmarks have been loaded from the chosen file.
            Tr("Bookmarks imported successfully."), Tr("Import bookmarks"),
            // Translators: Message shown when the chosen file does not hold bookmarks the player can read.
            Tr("The selected bookmarks file is invalid or could not be imported."), Tr("Import error"));
    }

    private void ResetSettings(object? sender, CommandEventArgs args)
    {
        // Translators: Question asked before every setting is put back the way it started.
        if (Wx.MessageBox(Tr("Are you sure you want to reset all settings to defaults?\nThis action cannot be undone."),
            Tr("Reset settings"), MessageBoxStyle.YesNo | MessageBoxStyle.NoDefault | MessageBoxStyle.IconWarning, Window) != MessageBoxStyle.Yes) return;
        var settings = _operations.ResetSettings();
        if (settings is null)
            ShowResult(false, "", "",
                // Translators: Message shown when the settings could not be put back the way they started.
                Tr("Could not reset settings."),
                // Translators: Title of the message shown when the settings could not be put back the way they started.
                Tr("Reset error"));
        else
        {
            _replaceSettings(settings);
            // Translators: Message shown once every setting has been put back the way it started.
            ShowResult(true, Tr("Settings were reset to defaults."), Tr("Reset settings"), "", Tr("Reset error"));
        }
    }

    private void OpenFolder(object? sender, CommandEventArgs args)
        => ShowResult(_operations.OpenSettingsFolder(), "", "",
            // Translators: Message shown when the folder holding the player's settings could not be opened.
            Tr("Could not open the settings folder."),
            // Translators: Title of the message shown when the folder holding the player's settings could not be opened.
            Tr("Open folder error"));

    private string? SavePath(string title, string source)
    {
        using var dialog = new FileDialog(Window, title, Path.GetDirectoryName(source) ?? "", Path.GetFileName(source),
            // Translators: The file types offered when saving a copy of the settings or bookmarks. Translate only
            // the two names, "JSON files" and "All files"; leave everything else, including the vertical bars, as it is.
            Tr("JSON files (*.json)|*.json|All files (*.*)|*.*"), FileDialogStyle.DefaultSave);
        return dialog.ShowModal() == StandardId.Ok ? dialog.Path : null;
    }

    private string? OpenPath(string title, string source)
    {
        using var dialog = new FileDialog(Window, title, Path.GetDirectoryName(source) ?? "", Path.GetFileName(source),
            Tr("JSON files (*.json)|*.json|All files (*.*)|*.*"), FileDialogStyle.DefaultOpen);
        return dialog.ShowModal() == StandardId.Ok ? dialog.Path : null;
    }

    private void ShowResult(bool success, string successMessage, string successCaption,
        string failureMessage, string failureCaption)
    {
        if (success && successMessage.Length == 0) return;
        Wx.MessageBox(success ? successMessage : failureMessage, success ? successCaption : failureCaption,
            MessageBoxStyle.Ok | (success ? MessageBoxStyle.IconInformation : MessageBoxStyle.IconError), Window);
    }

    private static Button Button(Window parent, string label, EventHandler<CommandEventArgs> action)
    {
        var button = new Button(parent, label: label);
        button.Click += action;
        return button;
    }
}
