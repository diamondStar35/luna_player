using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

internal static partial class MainMenuBuilder
{
    private sealed record FileMenuParts(
        Menu Menu,
        IReadOnlyDictionary<int, RecentCommand> RecentCommands,
        Menu RecentFilesMenu,
        Menu RecentFoldersMenu,
        Menu RecentPlaylistsMenu,
        IReadOnlyList<MenuItem> LocalFileItems,
        IReadOnlyList<MenuItem> MediaFileItems);

    private static FileMenuParts BuildFileMenu(
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts,
        RecentMenuState recents)
    {
        var fileMenu = new Menu();
        // Translators: File menu item that opens one or more media files. The three dots mean it opens a window to choose them.
        fileMenu.Append(commandIds[ActionId.OpenFile], Label(Tr("Open File..."), ActionId.OpenFile, shortcuts));
        // Translators: File menu item that asks for a link to a network stream and plays it.
        fileMenu.Append(commandIds[ActionId.OpenLink], Label(Tr("Open Link..."), ActionId.OpenLink, shortcuts));
        // Translators: File menu item that opens every media file in a folder.
        fileMenu.Append(commandIds[ActionId.OpenFolder], Label(Tr("Open Folder..."), ActionId.OpenFolder, shortcuts));
        // Translators: File menu item that asks for a YouTube address and plays the video or playlist it names.
        fileMenu.Append(commandIds[ActionId.OpenYouTubeLink], Label(Tr("Open YouTube Link..."), ActionId.OpenYouTubeLink, shortcuts));
        // Translators: File menu item that asks what to look for on YouTube and lists what it finds.
        fileMenu.Append(commandIds[ActionId.SearchYouTube], Label(Tr("Search YouTube..."), ActionId.SearchYouTube, shortcuts));
        // Translators: File menu item that lists the YouTube links and streams the user has saved.
        fileMenu.Append(commandIds[ActionId.OpenFavorites], Label(Tr("Favorite videos..."), ActionId.OpenFavorites, shortcuts));
        fileMenu.AppendSeparator();
        // The recently opened files, folders and playlists, each kind its own submenu. Filled from
        // recents.json and rebuilt as things open; entries carry no fixed ActionId (which recents exist
        // changes), so they are routed by command id like the equalizer and subtitle entries, and rebuilt
        // with fresh ids so no reserved id is ever deleted and reused.
        var recentCommands = new Dictionary<int, RecentCommand>();
        var recentMenu = new Menu();
        var recentFilesMenu = new Menu();
        var recentFoldersMenu = new Menu();
        var recentPlaylistsMenu = new Menu();
        // Translators: Submenu of the File menu's Recents submenu listing the files opened recently.
        recentMenu.AppendSubMenu(recentFilesMenu, Tr("Recent files"));
        // Translators: Submenu of the File menu's Recents submenu listing the folders opened recently.
        recentMenu.AppendSubMenu(recentFoldersMenu, Tr("Recent folders"));
        // Translators: Submenu of the File menu's Recents submenu listing the playlists opened recently.
        recentMenu.AppendSubMenu(recentPlaylistsMenu, Tr("Recent playlists"));
        // Translators: File menu submenu holding the recently opened files, folders and playlists.
        fileMenu.AppendSubMenu(recentMenu, Tr("Recents"));
        FillRecentsMenus(
            recentFilesMenu, recentFoldersMenu, recentPlaylistsMenu, recents, recentCommands);
        var localFileItems = new List<MenuItem>();
        var mediaFileItems = new List<MenuItem>();
        // Translators: File menu item that shows the folder holding the current file in Windows Explorer.
        Add(fileMenu, localFileItems, commandIds, shortcuts, ActionId.OpenContainingFolder, Tr("Open Containing Folder"));
        // Translators: File menu item that shows the Windows properties window for the current file.
        Add(fileMenu, localFileItems, commandIds, shortcuts, ActionId.OpenFileProperties, Tr("File properties..."));
        // Translators: File menu item that lists the files currently loaded in the player.
        Add(fileMenu, mediaFileItems, commandIds, shortcuts, ActionId.OpenedFiles, Tr("Opened Files..."));
        // Translators: File menu item that removes the current file from the player.
        Add(fileMenu, mediaFileItems, commandIds, shortcuts, ActionId.CloseFile, Tr("Close File"));
        // Translators: File menu item that removes every loaded file from the player.
        Add(fileMenu, mediaFileItems, commandIds, shortcuts, ActionId.CloseAllFiles, Tr("Close all files"));
        // Translators: File menu item that opens the player's settings window.
        fileMenu.Append(commandIds[ActionId.OpenPreferences], Label(Tr("Preferences..."), ActionId.OpenPreferences, shortcuts));
        fileMenu.AppendSeparator();
        // Translators: File menu item that quits the player.
        fileMenu.Append(commandIds[ActionId.Exit], Tr("Exit"));
        return new FileMenuParts(fileMenu, recentCommands, recentFilesMenu, recentFoldersMenu, recentPlaylistsMenu, localFileItems, mediaFileItems);
    }

    internal static void FillRecentsMenus(
        Menu filesMenu,
        Menu foldersMenu,
        Menu playlistsMenu,
        RecentMenuState recents,
        IDictionary<int, RecentCommand> commands)
    {
        FillRecentsSubmenu(filesMenu, RecentKind.File, recents.Files,
            // Translators: Shown in the Recent files submenu when no files have been opened yet.
            Tr("No recent files"),
            // Translators: Recent files submenu item that empties the list of recently opened files.
            Tr("Clear recent files"), commands);
        FillRecentsSubmenu(foldersMenu, RecentKind.Folder, recents.Folders,
            // Translators: Shown in the Recent folders submenu when no folders have been opened yet.
            Tr("No recent folders"),
            // Translators: Recent folders submenu item that empties the list of recently opened folders.
            Tr("Clear recent folders"), commands);
        FillRecentsSubmenu(playlistsMenu, RecentKind.Playlist, recents.Playlists,
            // Translators: Shown in the Recent playlists submenu when no playlists have been opened yet.
            Tr("No recent playlists"),
            // Translators: Recent playlists submenu item that empties the list of recently opened playlists.
            Tr("Clear recent playlists"), commands);
    }

    /// <summary>Fills one Recents submenu: a numbered item per entry (routed by command id), then a
    /// separator and the kind's Clear command - or a single disabled placeholder when the kind is empty.
    /// Everything uses a fresh id each rebuild, so no reserved action id is deleted and reused.</summary>
    internal static void FillRecentsSubmenu(
        Menu menu,
        RecentKind kind,
        IReadOnlyList<RecentMenuEntry> entries,
        string emptyLabel,
        string clearLabel,
        IDictionary<int, RecentCommand> commands)
    {
        if (entries.Count == 0)
        {
            menu.Append(IdManager.NewId(), emptyLabel).Enabled = false;
            return;
        }
        foreach (var entry in entries)
        {
            var id = IdManager.NewId();
            commands[id] = new RecentCommand(RecentAction.Open, kind, entry.Path);
            menu.Append(id, entry.Label);
        }
        menu.AppendSeparator();
        var clearId = IdManager.NewId();
        commands[clearId] = new RecentCommand(RecentAction.Clear, kind, string.Empty);
        menu.Append(clearId, clearLabel);
    }
}
