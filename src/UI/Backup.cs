using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class BackupPreferences : Preferences
{
    private readonly PrefsOps _operations;
    private readonly Action<PlayerSettings> _replaceSettings;

    internal BackupPreferences(Window parent, PrefsOps operations, Action<PlayerSettings> replaceSettings)
        : base(new Panel(parent),
            "Backup and restore settings page. Use Export settings, Import settings, or Open user settings folder.")
    {
        _operations = operations;
        _replaceSettings = replaceSettings;
        var panel = (Panel)Window;
        var exportSettings = Button(panel, "Export settings", ExportSettings);
        var importSettings = Button(panel, "Import settings", ImportSettings);
        var exportBookmarks = Button(panel, "Export bookmarks", ExportBookmarks);
        var importBookmarks = Button(panel, "Import bookmarks", ImportBookmarks);
        var reset = Button(panel, "Reset settings", ResetSettings);
        var openFolder = Button(panel, "Open user settings folder", OpenFolder);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(exportSettings, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        foreach (var button in new[] { importSettings, exportBookmarks, importBookmarks, reset, openFolder })
            sizer.Add(button, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        panel.SetSizer(sizer);

        Help(exportSettings, "Export settings creates a copy of the current settings file.");
        Help(importSettings, "Import settings replaces current settings with a selected settings file.");
        Help(exportBookmarks, "Export bookmarks creates a copy of the bookmarks JSON file.");
        Help(importBookmarks, "Import bookmarks replaces current bookmarks with a selected bookmarks file.");
        Help(reset, "Reset settings restores all preferences to default values.");
        Help(openFolder, "Open user settings folder opens the folder where this app stores its configuration.");
    }

    public override void Apply() { }

    private void ExportSettings(object? sender, CommandEventArgs args)
    {
        var path = SavePath("Export settings", _operations.SettingsPath);
        if (path is not null) ShowResult(_operations.ExportSettings(path), "Settings exported successfully.", "Export settings", "Could not export settings.", "Export error");
    }

    private void ImportSettings(object? sender, CommandEventArgs args)
    {
        var path = OpenPath("Import settings", _operations.SettingsPath);
        if (path is null) return;
        var settings = _operations.ImportSettings(path);
        if (settings is null) ShowResult(false, "", "", "The selected settings file is invalid or could not be imported.", "Import error");
        else
        {
            _replaceSettings(settings);
            ShowResult(true, "Settings imported successfully.", "Import settings", "", "Import error");
        }
    }

    private void ExportBookmarks(object? sender, CommandEventArgs args)
    {
        var path = SavePath("Export bookmarks", _operations.BookmarksPath);
        if (path is not null) ShowResult(_operations.ExportBookmarks(path), "Bookmarks exported successfully.", "Export bookmarks", "Could not export bookmarks.", "Export error");
    }

    private void ImportBookmarks(object? sender, CommandEventArgs args)
    {
        var path = OpenPath("Import bookmarks", _operations.BookmarksPath);
        if (path is not null) ShowResult(_operations.ImportBookmarks(path), "Bookmarks imported successfully.", "Import bookmarks", "The selected bookmarks file is invalid or could not be imported.", "Import error");
    }

    private void ResetSettings(object? sender, CommandEventArgs args)
    {
        if (Wx.MessageBox("Are you sure you want to reset all settings to defaults?\nThis action cannot be undone.",
            "Reset settings", MessageBoxStyle.YesNo | MessageBoxStyle.NoDefault | MessageBoxStyle.IconWarning, Window) != MessageBoxStyle.Yes) return;
        var settings = _operations.ResetSettings();
        if (settings is null) ShowResult(false, "", "", "Could not reset settings.", "Reset error");
        else
        {
            _replaceSettings(settings);
            ShowResult(true, "Settings were reset to defaults.", "Reset settings", "", "Reset error");
        }
    }

    private void OpenFolder(object? sender, CommandEventArgs args)
        => ShowResult(_operations.OpenSettingsFolder(), "", "", "Could not open the settings folder.", "Open folder error");

    private string? SavePath(string title, string source)
    {
        using var dialog = new FileDialog(Window, title, Path.GetDirectoryName(source) ?? "", Path.GetFileName(source),
            "JSON files (*.json)|*.json|All files (*.*)|*.*", FileDialogStyle.DefaultSave);
        return dialog.ShowModal() == StandardId.Ok ? dialog.Path : null;
    }

    private string? OpenPath(string title, string source)
    {
        using var dialog = new FileDialog(Window, title, Path.GetDirectoryName(source) ?? "", Path.GetFileName(source),
            "JSON files (*.json)|*.json|All files (*.*)|*.*", FileDialogStyle.DefaultOpen);
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
