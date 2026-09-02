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

    /// <summary>Why the last operation failed, or an empty string when it did not. Every message shown for a
    /// failure here carries it: "could not export settings" on its own tells the user nothing they can do
    /// something about, where "access to the path is denied" does.</summary>
    internal string LastError { get; private set; } = string.Empty;

    internal bool ExportSettings(string destination, PlayerSettings current)
    {
        if (!_settings.SaveExplicit(current))
            return Failed(_settings.LastError);
        return Copy(_settings.Path, destination, out var error) ? Succeeded() : Failed(error);
    }

    internal PlayerSettings? ImportSettings(string source)
    {
        if (!_settings.TryRead(source, out var imported))
        {
            _ = Failed(_settings.LastError);
            return null;
        }
        if (!_settings.SaveExplicit(imported))
        {
            _ = Failed(_settings.LastError);
            return null;
        }
        _ = Succeeded();
        return imported;
    }

    internal PlayerSettings? ResetSettings()
    {
        var defaults = new PlayerSettings();
        if (!_settings.SaveExplicit(defaults))
        {
            _ = Failed(_settings.LastError);
            return null;
        }
        _ = Succeeded();
        return defaults;
    }

    internal bool ExportBookmarks(string destination)
        => _bookmarks.Export(destination) ? Succeeded() : Failed(_bookmarks.LastError);

    internal bool ImportBookmarks(string source)
        => _bookmarks.Import(source) ? Succeeded() : Failed(_bookmarks.LastError);

    private bool Succeeded()
    {
        LastError = string.Empty;
        return true;
    }

    private bool Failed(string error)
    {
        LastError = error;
        return false;
    }

    private static bool Copy(string source, string destination, out string error)
    {
        error = string.Empty;
        try
        {
            // Exporting over the live file would empty it: File.Copy truncates the destination first.
            if (!Paths.AreSame(source, destination))
                File.Copy(source, destination, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error = exception.Message;
            return false;
        }
    }
}
