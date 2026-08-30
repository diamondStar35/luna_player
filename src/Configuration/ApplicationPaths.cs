namespace LunaPlayer.Configuration;

internal sealed class ApplicationPaths
{
    internal ApplicationPaths()
    {
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        RootDirectory = Path.Combine(applicationData, "Luna Player");
    }

    internal string RootDirectory { get; }
    internal string SettingsFile => Path.Combine(RootDirectory, "settings.json");
    internal string LegacySettingsFile => Path.Combine(RootDirectory, "settings.ini");
    internal string BookmarksFile => Path.Combine(RootDirectory, "bookmarks.json");
    internal string PositionsFile => Path.Combine(RootDirectory, "positions.json");
}
