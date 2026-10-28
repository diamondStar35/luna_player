using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal static partial class Builder
{
    private static Menu BuildToolsMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        // Appended after all the indexed menus below, between Recording and Help, which nothing counts by
        // position - so the Tools menu can hold whatever it likes without disturbing the returned indices.
        var toolsMenu = new Menu();
        // Translators: Tools menu item that opens the window for converting files to another audio format.
        toolsMenu.Append(commandIds[ActionId.OpenMediaConverter], Label(Tr("Media converter..."), ActionId.OpenMediaConverter, shortcuts));
        // Translators: Tools menu item opening the internet radio station browser.
        toolsMenu.Append(commandIds[ActionId.OpenRadioBrowser], Label(Tr("Radio browser…"), ActionId.OpenRadioBrowser, shortcuts));
        // Translators: Opens the Spotify home, search and library interface.
        toolsMenu.Append(commandIds[ActionId.OpenSpotifyBrowser], Label(Tr("Spotify..."), ActionId.OpenSpotifyBrowser, shortcuts));
        // Translators: Tools menu item that opens the YouTube download queue.
        toolsMenu.Append(commandIds[ActionId.OpenDownloadQueue], Label(Tr("Download queue..."), ActionId.OpenDownloadQueue, shortcuts));

        // IPTV is a submenu of Tools rather than a top-level menu: two items do not earn a place on the menu
        // bar, and the source manager and channel browser are tools like the converter above them.
        var iptvMenu = new Menu();
        // Translators: IPTV menu item that opens the window where IPTV sources are added and edited.
        iptvMenu.Append(commandIds[ActionId.OpenIptvSources], Label(Tr("Manage IPTV sources..."), ActionId.OpenIptvSources, shortcuts));
        // Translators: IPTV menu item that opens the list of channels the chosen IPTV source holds.
        iptvMenu.Append(commandIds[ActionId.OpenIptvChannels], Label(Tr("Browse channels..."), ActionId.OpenIptvChannels, shortcuts));
        // Translators: Tools submenu holding the IPTV source manager and channel browser.
        toolsMenu.AppendSubMenu(iptvMenu, Tr("IPTV"));

        // A submenu of Tools for the same reason IPTV is: setting, cancelling and querying the timer are three
        // small commands that belong together rather than each on the menu bar.
        var sleepMenu = new Menu();
        // Translators: Sleep-timer menu item that opens the window where a timer is set.
        sleepMenu.Append(commandIds[ActionId.OpenSleepTimer], Label(Tr("Set timer..."), ActionId.OpenSleepTimer, shortcuts));
        // Translators: Sleep-timer menu item that turns off a timer that is counting down.
        sleepMenu.Append(commandIds[ActionId.CancelSleepTimer], Label(Tr("Cancel timer"), ActionId.CancelSleepTimer, shortcuts));
        // Translators: Sleep-timer menu item that speaks how long is left before the timer fires.
        sleepMenu.Append(commandIds[ActionId.AnnounceSleepTimerRemaining], Label(Tr("Time remaining"), ActionId.AnnounceSleepTimerRemaining, shortcuts));
        // Translators: Tools submenu holding the sleep-timer commands.
        toolsMenu.AppendSubMenu(sleepMenu, Tr("Sleep timer"));
        return toolsMenu;
    }
}
