using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal static partial class Builder
{
    private sealed record FileMenuParts(
        Menu Menu,
        IReadOnlyDictionary<int, LunaPlayer.UI.Playlists.RecentCommand> RecentCommands,
        Menu RecentFilesMenu,
        Menu RecentFoldersMenu,
        Menu RecentPlaylistsMenu,
        IReadOnlyList<MenuItem> LocalFileItems,
        IReadOnlyList<MenuItem> MediaFileItems);

    private static FileMenuParts BuildFileMenu(
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts,
        LunaPlayer.UI.Playlists.RecentMenuState recents)
    {
        var fileMenu = new Menu();
        var playlists = new Menu();
        foreach (var action in PlaylistActions.All)
            playlists.Append(commandIds[action.Id], Label(action.Label, action.Id, shortcuts));
        // Translators: File menu submenu for saved playlists and adding files or folders.
        fileMenu.AppendSubMenu(playlists, Tr("Playlists"));
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
        // changes), so they are routed by command id like the equalizer and subtitle entries. Existing rows
        // keep their ids when the list changes; newly inserted rows always receive fresh ids.
        var recentCommands = new Dictionary<int, LunaPlayer.UI.Playlists.RecentCommand>();
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
        LunaPlayer.UI.Playlists.RecentMenuState recents,
        IDictionary<int, LunaPlayer.UI.Playlists.RecentCommand> commands)
        => UpdateRecentsMenus(filesMenu, foldersMenu, playlistsMenu, null, recents, commands);

    internal static void UpdateRecentsMenus(
        Menu filesMenu,
        Menu foldersMenu,
        Menu playlistsMenu,
        LunaPlayer.UI.Playlists.RecentMenuState? previous,
        LunaPlayer.UI.Playlists.RecentMenuState recents,
        IDictionary<int, LunaPlayer.UI.Playlists.RecentCommand> commands)
    {
        UpdateRecentsSubmenu(filesMenu, RecentKind.File, previous?.Files, recents.Files,
            // Translators: Shown in the Recent files submenu when no files have been opened yet.
            Tr("No recent files"),
            // Translators: Recent files submenu item that empties the list of recently opened files.
            Tr("Clear recent files"), commands);
        UpdateRecentsSubmenu(foldersMenu, RecentKind.Folder, previous?.Folders, recents.Folders,
            // Translators: Shown in the Recent folders submenu when no folders have been opened yet.
            Tr("No recent folders"),
            // Translators: Recent folders submenu item that empties the list of recently opened folders.
            Tr("Clear recent folders"), commands);
        UpdateRecentsSubmenu(playlistsMenu, RecentKind.Playlist, previous?.Playlists, recents.Playlists,
            // Translators: Shown in the Recent playlists submenu when no playlists have been opened yet.
            Tr("No recent playlists"),
            // Translators: Recent playlists submenu item that empties the list of recently opened playlists.
            Tr("Clear recent playlists"), commands);
    }

    /// <summary>Fills one Recents submenu: a numbered item per entry (routed by command id), then a
    /// separator and the kind's Clear command - or a single disabled placeholder when the kind is empty.
    /// Existing rows keep their ids; only new rows reserve a new id.</summary>
    private static void UpdateRecentsSubmenu(
        Menu menu,
        RecentKind kind,
        IReadOnlyList<LunaPlayer.UI.Playlists.RecentMenuEntry>? previous,
        IReadOnlyList<LunaPlayer.UI.Playlists.RecentMenuEntry> entries,
        string emptyLabel,
        string clearLabel,
        IDictionary<int, LunaPlayer.UI.Playlists.RecentCommand> commands)
    {
        if (previous is not null && previous.SequenceEqual(entries))
            return;
        if (previous is { Count: > 0 } && entries.Count > 0)
        {
            // The separator and Clear command remain at the bottom. Shrink from the end or insert before
            // them, then relabel surviving rows instead of deleting and recreating the entire submenu.
            for (var index = previous.Count - 1; index >= entries.Count; index--)
            {
                var item = menu[index];
                commands.Remove(item.Id);
                menu.Delete(item);
            }
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var item = index < previous.Count
                    ? menu[index]
                    : menu.Insert(index, IdManager.NewId(), entry.Label);
                if (index < previous.Count && previous[index].Label != entry.Label)
                    item.Label = entry.Label;
                commands[item.Id] = new LunaPlayer.UI.Playlists.RecentCommand(LunaPlayer.UI.Playlists.RecentAction.Open, kind, entry.Path);
            }
            return;
        }
        // Switching between a placeholder and a populated list changes the menu structure. Discard only
        // this kind's commands, leaving the other two submenus and their command mappings intact.
        while (menu.Count > 0)
        {
            var item = menu[0];
            commands.Remove(item.Id);
            menu.Delete(item);
        }
        if (entries.Count == 0)
        {
            menu.Append(IdManager.NewId(), emptyLabel).Enabled = false;
            return;
        }
        foreach (var entry in entries)
        {
            var id = IdManager.NewId();
            commands[id] = new LunaPlayer.UI.Playlists.RecentCommand(LunaPlayer.UI.Playlists.RecentAction.Open, kind, entry.Path);
            menu.Append(id, entry.Label);
        }
        menu.AppendSeparator();
        var clearId = IdManager.NewId();
        commands[clearId] = new LunaPlayer.UI.Playlists.RecentCommand(LunaPlayer.UI.Playlists.RecentAction.Clear, kind, string.Empty);
        menu.Append(clearId, clearLabel);
    }
}
