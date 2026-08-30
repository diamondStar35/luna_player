using LunaPlayer.Bookmarks;

namespace LunaPlayer.Configuration;

internal sealed class BackupService
{
    private readonly SettingsStore _settings;
    private readonly BookmarkStore _bookmarks;

    internal BackupService(SettingsStore settings, BookmarkStore bookmarks)
    {
        _settings = settings;
        _bookmarks = bookmarks;
    }

    internal string SettingsPath => _settings.Path;
    internal string BookmarksPath => _bookmarks.FilePath;

    internal bool ExportSettings(string destination, PlayerSettings current)
        => _settings.SaveExplicit(current) && Copy(_settings.Path, destination);

    internal PlayerSettings? ImportSettings(string source)
    {
        if (!_settings.TryRead(source, out var imported) || !_settings.SaveExplicit(imported)) return null;
        return imported;
    }

    internal PlayerSettings? ResetSettings()
    {
        var defaults = new PlayerSettings();
        return _settings.SaveExplicit(defaults) ? defaults : null;
    }

    internal bool ExportBookmarks(string destination) => _bookmarks.Export(destination);
    internal bool ImportBookmarks(string source) => _bookmarks.Import(source);

    private static bool Copy(string source, string destination)
    {
        try
        {
            if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                File.Copy(source, destination, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }
}
