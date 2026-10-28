using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal static partial class Builder
{
    private static Menu BuildHelpMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        var updatesMenu = new Menu();
        // Translators: Help menu item that checks whether a newer Luna Player release is available.
        updatesMenu.Append(commandIds[ActionId.CheckAppUpdates], Label(Tr("Check for app updates"), ActionId.CheckAppUpdates, shortcuts));
        // Translators: Help menu item that fetches a newer yt-dlp. "YouTube" is a service name.
        updatesMenu.Append(commandIds[ActionId.UpdateYouTubeComponents], Label(Tr("Update YouTube components"), ActionId.UpdateYouTubeComponents, shortcuts));
        var helpMenu = new Menu();
        // Translators: Help menu item that opens the user guide matching the application language.
        helpMenu.Append(commandIds[ActionId.UserGuide], Label(Tr("User guide"), ActionId.UserGuide, shortcuts));
        // Translators: Help menu item that shows information about Luna Player.
        helpMenu.Append(commandIds[ActionId.About], Label(Tr("About"), ActionId.About, shortcuts));
        // Translators: Help menu item that opens the GitHub page for the installed release.
        helpMenu.Append(commandIds[ActionId.ReleaseNotes], Label(Tr("Release notes"), ActionId.ReleaseNotes, shortcuts));
        // Translators: Help menu item that opens the PayPal donation page in a web browser.
        helpMenu.Append(commandIds[ActionId.Donate], Label(Tr("Donate"), ActionId.Donate, shortcuts));
        helpMenu.AppendSeparator();
        // Translators: Help submenu containing the commands that update Luna Player and its YouTube tools.
        helpMenu.AppendSubMenu(updatesMenu, Tr("Updates"));
        return helpMenu;
    }
}
