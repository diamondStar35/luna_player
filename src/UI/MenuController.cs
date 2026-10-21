using System.Globalization;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Equalizer;
using LunaPlayer.Recording;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>Owns the menu bar: the command-id maps, the groups enabled/disabled together, and the dynamic
/// equalizer, subtitle and recents submenus. The main window forwards its IMenuState calls here.</summary>
internal sealed class MenuController
{
    private readonly Dictionary<ActionId, int> _commandIds = [];
    private readonly Dictionary<int, ActionId> _commands = [];
    private readonly List<MenuItem> _playbackItems = [];
    private readonly List<MenuItem> _mediaFileItems = [];
    private readonly List<MenuItem> _localFileItems = [];
    private readonly List<MenuItem> _markedItems = [];
    private readonly List<MenuItem> _videoItems = [];
    private readonly List<MenuItem> _audioTrackItems = [];
    private readonly List<MenuItem> _localEditItems = [];
    private readonly List<MenuItem> _bookmarkItems = [];
    private readonly Dictionary<ActionId, MenuItem> _selectionItems = [];
    private readonly Dictionary<int, string> _equalizerCommands = [];
    private readonly Dictionary<string, MenuItem> _equalizerItems = new(StringComparer.Ordinal);
    private readonly Menu _equalizerMenu;
    private readonly Dictionary<int, string> _subtitleCommands = [];
    private readonly Dictionary<string, MenuItem> _subtitleItems = new(StringComparer.Ordinal);
    private readonly Menu _subtitleMenu;
    private readonly MenuItem _subtitleMenuItem;
    private readonly MenuItem _subtitleRememberItem;
    private readonly Dictionary<int, RecentCommand> _recentCommands = [];
    private readonly Menu _recentFilesMenu;
    private readonly Menu _recentFoldersMenu;
    private readonly Menu _recentPlaylistsMenu;
    private RecentMenuState _recents;
    private readonly MenuBar _menuBar;
    private readonly int _bookmarksMenuIndex;
    private readonly int _selectionMenuIndex;
    private readonly int _markedMenuIndex;
    private readonly int _videoMenuIndex;
    private readonly MenuItem _markCurrentItem;
    private readonly MenuItem _markAllItem;
    private readonly MenuItem _shuffleItem;
    private readonly MenuItem _repeatFileItem;
    private readonly MenuItem _silenceRemovalItem;
    private readonly MenuItem _repeatSelectionsItem;
    private readonly MenuItem _fullScreenItem;
    private readonly MenuItem _startRecordingItem;
    private readonly MenuItem _pauseRecordingItem;
    private readonly MenuItem _stopRecordingItem;
    private ShortcutManager _shortcuts;
    internal MenuController(
        Frame frame,
        IReadOnlyList<ActionDefinition> actions,
        ShortcutManager shortcuts,
        IReadOnlyList<PresetEntry> presets,
        RecentMenuState recents)
    {
        _shortcuts = shortcuts;
        _recents = recents;
        BuildCommandIds(actions);
        var menu = MainMenuBuilder.Build(_commandIds, shortcuts, presets, recents);
        _menuBar = menu.MenuBar;
        _selectionMenuIndex = menu.SelectionMenuIndex;
        _bookmarksMenuIndex = menu.BookmarksMenuIndex;
        _markedMenuIndex = menu.MarkedMenuIndex;
        _videoMenuIndex = menu.VideoMenuIndex;
        _playbackItems.AddRange(menu.PlaybackItems);
        _mediaFileItems.AddRange(menu.MediaFileItems);
        _localFileItems.AddRange(menu.LocalFileItems);
        _markedItems.AddRange(menu.MarkedItems);
        _videoItems.AddRange(menu.VideoItems);
        _audioTrackItems.AddRange(menu.AudioTrackItems);
        _localEditItems.AddRange(menu.LocalEditItems);
        _bookmarkItems.AddRange(menu.BookmarkItems);
        foreach (var item in menu.SelectionItems)
            _selectionItems[item.Key] = item.Value;
        foreach (var command in menu.EqualizerCommands)
            _equalizerCommands[command.Key] = command.Value;
        foreach (var item in menu.EqualizerItems)
            _equalizerItems[item.Key] = item.Value;
        _equalizerMenu = menu.EqualizerMenu;
        foreach (var command in menu.SubtitleCommands)
            _subtitleCommands[command.Key] = command.Value;
        foreach (var item in menu.SubtitleItems)
            _subtitleItems[item.Key] = item.Value;
        _subtitleMenu = menu.SubtitleMenu;
        _subtitleMenuItem = menu.SubtitleMenuItem;
        _subtitleRememberItem = menu.SubtitleRememberItem;
        _recentFilesMenu = menu.RecentFilesMenu;
        _recentFoldersMenu = menu.RecentFoldersMenu;
        _recentPlaylistsMenu = menu.RecentPlaylistsMenu;
        foreach (var command in menu.RecentCommands)
            _recentCommands[command.Key] = command.Value;
        _markCurrentItem = menu.MarkCurrentItem;
        _markAllItem = menu.MarkAllItem;
        _shuffleItem = menu.ShuffleItem;
        _repeatFileItem = menu.RepeatFileItem;
        _silenceRemovalItem = menu.SilenceRemovalItem;
        _repeatSelectionsItem = menu.RepeatSelectionsItem;
        _fullScreenItem = menu.FullScreenItem;
        _startRecordingItem = menu.StartRecordingItem;
        _pauseRecordingItem = menu.PauseRecordingItem;
        _stopRecordingItem = menu.StopRecordingItem;
        frame.SetMenuBar(_menuBar);
    }

    /// <summary>The menu-command id reserved for each action, used to build the accelerator table.</summary>
    internal IReadOnlyDictionary<ActionId, int> CommandIds => _commandIds;

    /// <summary>Resolves a menu-command id to the action it stands for.</summary>
    internal bool TryGetAction(int commandId, out ActionId action) => _commands.TryGetValue(commandId, out action);

    /// <summary>Resolves a menu-command id to the equalizer preset it selects (null = Off); false if not one.</summary>
    internal bool TryGetEqualizerPreset(int commandId, out string? presetId)
    {
        if (_equalizerCommands.TryGetValue(commandId, out var key))
        {
            presetId = key.Length == 0 ? null : key;
            return true;
        }
        presetId = null;
        return false;
    }

    /// <summary>Resolves a menu-command id to the subtitle track it selects (null = Off); false if not one.</summary>
    internal bool TryGetSubtitleTrack(int commandId, out int? trackId)
    {
        if (_subtitleCommands.TryGetValue(commandId, out var key))
        {
            trackId = key.Length == 0 ? null : int.Parse(key, CultureInfo.InvariantCulture);
            return true;
        }
        trackId = null;
        return false;
    }

    /// <summary>Resolves a menu-command id to the recents command it stands for.</summary>
    internal bool TryGetRecent(int commandId, out RecentCommand recent) => _recentCommands.TryGetValue(commandId, out recent);

    internal void SetShuffleChecked(bool isChecked) => _shuffleItem.Checked = isChecked;

    internal void SetRepeatFileChecked(bool isChecked) => _repeatFileItem.Checked = isChecked;

    internal void SetSilenceRemovalChecked(bool isChecked) => _silenceRemovalItem.Checked = isChecked;

    internal void SetSelectionState(SelectionMenuState state)
    {
        _menuBar.EnableTop(_selectionMenuIndex, state.Available);
        foreach (var item in _selectionItems.Values)
            item.Enabled = state.Available;
        _selectionItems[ActionId.PreviousSelection].Enabled = state.Previous;
        _selectionItems[ActionId.NextSelection].Enabled = state.Next;
        _selectionItems[ActionId.ClearSelectionPoint].Enabled = state.ClearCurrent;
        _selectionItems[ActionId.ClearAllSelections].Enabled = state.ClearAll;
        _selectionItems[ActionId.SaveSelection].Enabled = state.SaveSelection;
        _selectionItems[ActionId.SaveSelections].Enabled = state.SaveSelections;
        _repeatSelectionsItem.Checked = state.Repeat;
    }

    /// <summary>Ticks the full-screen item to match the window.</summary>
    internal void SetFullScreenChecked(bool fullScreen) => _fullScreenItem.Checked = fullScreen;

    internal void SetFullScreenAvailable(bool available) => _fullScreenItem.Enabled = available;

    internal void SetEqualizerPreset(string? presetId)
    {
        var key = presetId ?? MainMenuBuilder.EqualizerOffKey;
        if (_equalizerItems.TryGetValue(key, out var item))
            item.Checked = true;
    }

    internal void RebuildEqualizerMenu(IReadOnlyList<PresetEntry> presets, string? selected)
    {
        // Emptied and filled again rather than replaced. The submenu is already attached to the menu bar,
        // and swapping it would leave the bar pointing at a menu nothing else knows about.
        while (_equalizerMenu.Count > 0)
            _equalizerMenu.Delete(_equalizerMenu[0]);
        _equalizerCommands.Clear();
        _equalizerItems.Clear();
        MainMenuBuilder.FillEqualizerMenu(
            _equalizerMenu, presets, _commandIds, _shortcuts, _equalizerCommands, _equalizerItems);
        SetEqualizerPreset(selected);
    }

    internal void RebuildSubtitleMenu(IReadOnlyList<SubtitleMenuEntry> entries, int? activeId)
    {
        // Only the dynamic radio region is rebuilt - the items above the first separator. The fixed commands
        // below it (load from file/URL, remember) keep their permanent action ids; deleting them would free
        // those ids, and the next rebuild re-adding the same id trips a wxWidgets auto-id assert.
        while (_subtitleMenu.Count > 0 && _subtitleMenu[0].Kind != MenuItemKind.Separator)
            _subtitleMenu.Delete(_subtitleMenu[0]);
        _subtitleCommands.Clear();
        _subtitleItems.Clear();
        MainMenuBuilder.FillSubtitleTracks(_subtitleMenu, entries, activeId, _subtitleCommands, _subtitleItems);
    }

    internal void SetSubtitleRemember(bool on) => _subtitleRememberItem.Checked = on;

    internal void SetSubtitleSelection(int? activeId)
    {
        var key = activeId?.ToString(CultureInfo.InvariantCulture) ?? MainMenuBuilder.SubtitleOffKey;
        if (_subtitleItems.TryGetValue(key, out var item))
            item.Checked = true;
    }

    internal void SetSubtitleControlsEnabled(bool enabled) => _subtitleMenuItem.Enabled = enabled;
    internal void RebuildRecentsMenu(
        IReadOnlyList<RecentMenuEntry> files,
        IReadOnlyList<RecentMenuEntry> folders,
        IReadOnlyList<RecentMenuEntry> playlists)
    {
        var recents = new RecentMenuState(files, folders, playlists);
        // Rebuilding attached menus triggers native layout and accessibility updates for every deletion and
        // insertion. Keep existing rows and command ids, changing only the entries whose content changed.
        MainMenuBuilder.UpdateRecentsMenus(
            _recentFilesMenu,
            _recentFoldersMenu,
            _recentPlaylistsMenu,
            _recents,
            recents,
            _recentCommands);
        _recents = recents;
    }

    internal void SetMediaLoaded(bool loaded)
    {
        foreach (var item in _playbackItems)
            item.Enabled = loaded;
        foreach (var item in _mediaFileItems)
            item.Enabled = loaded;
    }

    internal void SetEditState(bool hasLocalFile, bool hasMedia)
    {
        foreach (var item in _localEditItems)
            item.Enabled = hasLocalFile;
        foreach (var item in _localFileItems)
            item.Enabled = hasLocalFile;
        _markCurrentItem.Enabled = hasMedia;
        _markAllItem.Enabled = hasMedia;
    }

    internal void SetBookmarkState(bool enabled)
    {
        foreach (var item in _bookmarkItems)
            item.Enabled = enabled;
        _menuBar.EnableTop(_bookmarksMenuIndex, enabled);
    }

    internal void SetMarkState(bool currentMarked, bool allMarked)
    {
        _markCurrentItem.Checked = currentMarked;
        _markAllItem.Checked = allMarked;
    }
    internal void SetRecordingState(RecordingState state)
    {
        var idle = state is RecordingState.Idle;
        _startRecordingItem.Enabled = idle;
        // Both of these need a recording to act on, and neither cares whether it is running or held.
        _pauseRecordingItem.Enabled = !idle;
        _stopRecordingItem.Enabled = !idle;
        // Said the way the button in the window says it, so the menu and the window never disagree about
        // what the key does. The shortcut is kept: it is the part after the tab, which the menu owns.
        var parts = _pauseRecordingItem.Label.Split('	');
        var label = state is RecordingState.Paused
            // Translators: Recording menu item that starts a paused recording again.
            ? Tr("Resume")
            // Translators: Recording menu item that holds a recording where it is.
            : Tr("Pause");
        _pauseRecordingItem.Label = parts.Length > 1 ? $"{label}	{parts[1]}" : label;
    }

    internal void SetVideoOptionsEnabled(bool enabled)
    {
        foreach (var item in _videoItems) item.Enabled = enabled;
        _menuBar.EnableTop(_videoMenuIndex, enabled);
    }

    internal void SetAudioTrackControlsEnabled(bool enabled)
    {
        foreach (var item in _audioTrackItems) item.Enabled = enabled;
    }

    internal void SetMarkedActionsEnabled(bool enabled)
    {
        foreach (var item in _markedItems) item.Enabled = enabled;
        _menuBar.EnableTop(_markedMenuIndex, enabled);
    }

    /// <summary>Relabels every menu item with its newly-bound key (keeping the text before the tab). The
    /// window rebuilds the accelerator table separately.</summary>
    internal void ApplyShortcuts(ShortcutManager shortcuts)
    {
        _shortcuts = shortcuts;
        foreach (var pair in _commandIds)
        {
            var item = _menuBar.FindItem(pair.Value);
            if (item is null || item.Kind == MenuItemKind.Separator) continue;
            var baseLabel = item.Label.Split('\t')[0];
            item.Label = shortcuts.Get(pair.Key) is Shortcut shortcut
                ? $"{baseLabel}\t{shortcut.ToDisplayString()}"
                : baseLabel;
        }
    }

    private void BuildCommandIds(IEnumerable<ActionDefinition> actions)
    {
        foreach (var action in actions)
        {
            if (action.Id == ActionId.OpenFile)
                AddCommand(action.Id, StandardId.Open);
            else if (action.Id == ActionId.Exit)
                AddCommand(action.Id, StandardId.Exit);
            else
                AddCommand(action.Id, IdManager.NewId());
        }
    }

    /// <summary>An IdManager id is never released: the menu bar holds the reservation and returns it when
    /// destroyed, so releasing it here would be a double free (wxWidgets asserts on it).</summary>
    private void AddCommand(ActionId action, int id)
    {
        _commandIds[action] = id;
        _commands[id] = action;
    }
}

