using LunaPlayer.Actions;
using LunaPlayer.Equalizer;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal sealed record MainMenuComponents(
    MenuBar MenuBar,
    int SelectionMenuIndex,
    int BookmarksMenuIndex,
    int MarkedMenuIndex,
    int VideoMenuIndex,
    IReadOnlyList<MenuItem> PlaybackItems,
    IReadOnlyList<MenuItem> MediaFileItems,
    IReadOnlyList<MenuItem> LocalFileItems,
    IReadOnlyList<MenuItem> MarkedItems,
    IReadOnlyList<MenuItem> LocalEditItems,
    IReadOnlyList<MenuItem> BookmarkItems,
    IReadOnlyDictionary<ActionId, MenuItem> SelectionItems,
    MenuItem RepeatSelectionsItem,
    IReadOnlyList<MenuItem> VideoItems,
    IReadOnlyList<MenuItem> AudioTrackItems,
    IReadOnlyDictionary<int, string> SubtitleCommands,
    IReadOnlyDictionary<string, MenuItem> SubtitleItems,
    Menu SubtitleMenu,
    MenuItem SubtitleMenuItem,
    MenuItem SubtitleRememberItem,
    IReadOnlyDictionary<int, LunaPlayer.UI.Playlists.RecentCommand> RecentCommands,
    Menu RecentFilesMenu,
    Menu RecentFoldersMenu,
    Menu RecentPlaylistsMenu,
    IReadOnlyDictionary<int, string> EqualizerCommands,
    IReadOnlyDictionary<string, MenuItem> EqualizerItems,
    Menu EqualizerMenu,
    MenuItem MarkCurrentItem,
    MenuItem MarkAllItem,
    MenuItem ShuffleItem,
    MenuItem RepeatFileItem,
    MenuItem SilenceRemovalItem,
    MenuItem FullScreenItem,
    MenuItem StartRecordingItem,
    MenuItem PauseRecordingItem,
    MenuItem StopRecordingItem,
    ChapterMenuComponents Chapters);
/// <summary>Assembles the menu bar from one builder per top-level menu (see Menus/). Holds the shared Build
/// orchestrator and the Add/Label helpers; each menu lives in its own file.</summary>
internal static partial class Builder
{
    internal static MainMenuComponents Build(
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts,
        IReadOnlyList<PresetEntry> presets,
        LunaPlayer.UI.Playlists.RecentMenuState recents)
    {
        var file = BuildFileMenu(commandIds, shortcuts, recents);
        var edit = BuildEditMenu(commandIds, shortcuts);
        var selection = BuildSelectionMenu(commandIds, shortcuts);
        var bookmarks = BuildBookmarksMenu(commandIds, shortcuts);
        var marked = BuildMarkedMenu(commandIds, shortcuts);
        var player = BuildPlayerMenu(commandIds, shortcuts, presets);
        var video = BuildVideoMenu(commandIds, shortcuts);
        var recording = BuildRecordingMenu(commandIds, shortcuts);
        var tools = BuildToolsMenu(commandIds, shortcuts);
        var help = BuildHelpMenu(commandIds, shortcuts);

        var menuBar = new MenuBar();
        // Translators: Name of the File menu in the menu bar.
        menuBar.Append(file.Menu, Tr("File"));
        // Translators: Name of the Edit menu in the menu bar.
        menuBar.Append(edit.Menu, Tr("Edit"));
        var selectionMenuIndex = 2;
        // Translators: Name of the menu holding commands for marking and saving parts of a media file.
        menuBar.Append(selection.Menu, Tr("Selection"));
        // Translators: Name of the Bookmarks menu in the menu bar.
        menuBar.Append(bookmarks.Menu, Tr("Bookmarks"));
        var markedMenuIndex = 4;
        // Translators: Name of the menu bar menu holding the things that can be done to the marked files at once.
        menuBar.Append(marked.Menu, Tr("Actions for marked files"));
        // Translators: Name of the Player menu in the menu bar, holding the playing, seeking and volume items.
        menuBar.Append(player.Menu, Tr("Player"));
        // The index returned below is written out as a number, so a menu inserted before the Video menu would
        // leave it pointing at the wrong one.
        var videoMenuIndex = 6;
        // Translators: Name of the menu bar menu holding what can be done with the YouTube video being played.
        menuBar.Append(video.Menu, Tr("Video options"));
        // Last indexed menu, and it has to be: the four indices returned below are written out as numbers,
        // so a menu put in front of any of them would leave those pointing at the wrong one.
        // Translators: Name of the menu bar menu holding what can be recorded and how.
        menuBar.Append(recording.Menu, Tr("Recording"));
        // Safe after the indexed menus above: nothing counts the tools or help menus by position.
        // Translators: Name of the Tools menu in the menu bar, holding the media converter.
        menuBar.Append(tools, Tr("Tools"));
        // Translators: Name of the Help menu in the menu bar.
        menuBar.Append(help, Tr("Help"));
        return new MainMenuComponents(
            menuBar, selectionMenuIndex, 3, markedMenuIndex, videoMenuIndex,
            player.PlaybackItems, file.MediaFileItems, file.LocalFileItems, marked.Items,
            edit.LocalEditItems, bookmarks.Items, selection.Items, selection.RepeatItem,
            video.Items, player.AudioTrackItems,
            player.SubtitleCommands, player.SubtitleItems, player.SubtitleMenu, player.SubtitleMenuItem,
            player.SubtitleRememberItem, file.RecentCommands, file.RecentFilesMenu, file.RecentFoldersMenu,
            file.RecentPlaylistsMenu, player.EqualizerCommands, player.EqualizerItems, player.EqualizerMenu,
            edit.MarkCurrentItem, edit.MarkAllItem, player.ShuffleItem, player.RepeatItem, player.SilenceItem,
            player.FullScreenItem, recording.StartItem, recording.PauseItem, recording.StopItem, player.Chapters);
    }

    /// <summary>Appends an action item (labelled with its shortcut) and records it for group enable/disable.</summary>
    private static void Add(
        Menu menu,
        ICollection<MenuItem> items,
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts,
        ActionId action,
        string label)
        => items.Add(menu.Append(commandIds[action], Label(label, action, shortcuts)));

    /// <summary>The label with the action's shortcut after a tab, or the bare label when none is bound.</summary>
    private static string Label(string label, ActionId action, ShortcutManager shortcuts)
        => shortcuts.Get(action) is Shortcut shortcut
            ? $"{label}\t{shortcut.ToDisplayString()}"
            : label;
}

