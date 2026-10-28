using System.Globalization;
using LunaPlayer.Actions;
using LunaPlayer.Equalizer;
using LunaPlayer.Playback;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal static partial class Builder
{
    private sealed record PlayerMenuParts(
        Menu Menu,
        IReadOnlyList<MenuItem> PlaybackItems,
        IReadOnlyList<MenuItem> AudioTrackItems,
        IReadOnlyDictionary<int, string> EqualizerCommands,
        IReadOnlyDictionary<string, MenuItem> EqualizerItems,
        Menu EqualizerMenu,
        IReadOnlyDictionary<int, string> SubtitleCommands,
        IReadOnlyDictionary<string, MenuItem> SubtitleItems,
        Menu SubtitleMenu,
        MenuItem SubtitleMenuItem,
        MenuItem SubtitleRememberItem,
        MenuItem ShuffleItem,
        MenuItem RepeatItem,
        MenuItem SilenceItem,
        MenuItem FullScreenItem,
        ChapterMenuComponents Chapters);

    private static PlayerMenuParts BuildPlayerMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts, IReadOnlyList<PresetEntry> presets)
    {
        var playbackItems = new List<MenuItem>();
        var playerMenu = new Menu();
        // Translators: Player menu item that starts playing, or pauses playing.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.PlayPause, Tr("Play/Pause"));

        var speedMenu = new Menu();
        // Translators: Item in the Speed submenu: play the file faster.
        Add(speedMenu, playbackItems, commandIds, shortcuts, ActionId.SpeedUp, Tr("Increase Speed"));
        // Translators: Item in the Speed submenu: play the file slower.
        Add(speedMenu, playbackItems, commandIds, shortcuts, ActionId.SpeedDown, Tr("Decrease Speed"));
        // Translators: Item in the Speed submenu: return the playing speed to normal.
        Add(speedMenu, playbackItems, commandIds, shortcuts, ActionId.ResetSpeed, Tr("Reset Speed"));
        // Translators: Player submenu holding the items that change how fast the file plays.
        playerMenu.AppendSubMenu(speedMenu, Tr("Speed"));
        var pitchMenu = new Menu();
        // Translators: Item in the Pitch submenu: raise audio pitch without changing playback speed.
        Add(pitchMenu, playbackItems, commandIds, shortcuts, ActionId.PitchUp, Tr("Raise Pitch"));
        // Translators: Item in the Pitch submenu: lower audio pitch without changing playback speed.
        Add(pitchMenu, playbackItems, commandIds, shortcuts, ActionId.PitchDown, Tr("Lower Pitch"));
        // Translators: Item in the Pitch submenu: restore the audio's original pitch.
        Add(pitchMenu, playbackItems, commandIds, shortcuts, ActionId.ResetPitch, Tr("Reset Pitch"));
        // Translators: Item in the Pitch submenu: speak the current pitch adjustment.
        Add(pitchMenu, playbackItems, commandIds, shortcuts, ActionId.AnnouncePitch, Tr("Speak Pitch"));
        // Translators: Player submenu holding the controls that raise or lower audio pitch.
        playerMenu.AppendSubMenu(pitchMenu, Tr("Pitch"));

        var panMenu = new Menu();
        // Translators: Item in the Pan submenu: move the sound toward the left speaker.
        Add(panMenu, playbackItems, commandIds, shortcuts, ActionId.PanLeft, Tr("Pan Left"));
        // Translators: Item in the Pan submenu: move the sound toward the right speaker.
        Add(panMenu, playbackItems, commandIds, shortcuts, ActionId.PanRight, Tr("Pan Right"));
        // Translators: Item in the Pan submenu: speak the current left/right audio balance.
        Add(panMenu, playbackItems, commandIds, shortcuts, ActionId.AnnouncePan, Tr("Speak Pan"));
        // Translators: Player submenu holding the left/right audio balance controls.
        playerMenu.AppendSubMenu(panMenu, Tr("Pan"));

        var equalizerCommands = new Dictionary<int, string>();
        var equalizerItems = new Dictionary<string, MenuItem>(StringComparer.Ordinal);
        var equalizerMenu = new Menu();
        FillEqualizerMenu(equalizerMenu, presets, commandIds, shortcuts, equalizerCommands, equalizerItems);
        // Translators: Player submenu holding the equalizer presets and the commands for editing them.
        playerMenu.AppendSubMenu(equalizerMenu, Tr("Equalizer"));
        playerMenu.AppendSeparator();

        // Translators: Player menu item that plays the file before the current one.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.PreviousTrack, Tr("Previous"));
        // Translators: Player menu item that plays the file after the current one.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.NextTrack, Tr("Next"));
        // Translators: Player menu item that plays the first file in the list.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.FirstTrack, Tr("First File"));
        // Translators: Player menu item that asks for a file number and jumps to it.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.GoToFile, Tr("Go to file..."));
        // Translators: Player menu item that plays the last file in the list.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.LastTrack, Tr("Last File"));
        var shuffleItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleShuffle],
            // Translators: Player menu item that turns playing files in random order on or off. It is ticked while it is on.
            Label(Tr("Shuffle"), ActionId.ToggleShuffle, shortcuts));
        playbackItems.Add(shuffleItem);
        var repeatItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleRepeatFile],
            // Translators: Player menu item that turns repeating the current file on or off. It is ticked while it is on.
            Label(Tr("Repeat File"), ActionId.ToggleRepeatFile, shortcuts));
        playbackItems.Add(repeatItem);
        var silenceItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleSilenceRemoval],
            // Translators: Player menu item that turns skipping silent parts of the file on or off. It is ticked while it is on.
            Label(Tr("Enable silence removal filter"), ActionId.ToggleSilenceRemoval, shortcuts));
        playbackItems.Add(silenceItem);

        var loopMenu = new Menu();
        // Translators: Item in the A-B loop submenu: mark the start of the part to repeat.
        // A and B name the two ends of the loop and are usually left as they are.
        Add(loopMenu, playbackItems, commandIds, shortcuts, ActionId.StartSelection, Tr("Set A (loop start)"));
        // Translators: Item in the A-B loop submenu: mark the end of the part to repeat.
        // A and B name the two ends of the loop and are usually left as they are.
        Add(loopMenu, playbackItems, commandIds, shortcuts, ActionId.EndSelection, Tr("Set B (loop end)"));
        // Translators: Item in the A-B loop submenu: forget the marked part and play the whole file again.
        // A and B name the two ends of the loop and are usually left as they are.
        Add(loopMenu, playbackItems, commandIds, shortcuts, ActionId.ClearSelection, Tr("Clear A-B loop"));
        // Translators: Player submenu holding the items that repeat one part of the file.
        // A and B name the two ends of the loop and are usually left as they are.
        playerMenu.AppendSubMenu(loopMenu, Tr("A-B loop"));
        playerMenu.AppendSeparator();

        // Translators: Player menu item that moves back in the file by one seek step.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekBackward, Tr("Rewind"));
        // Translators: Player menu item that moves forward in the file by one seek step.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekForward, Tr("Forward"));
        // Translators: Player menu item that jumps to the beginning of the file.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekStart, Tr("Beginning"));
        // Translators: Player menu item that jumps to the end of the file.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.SeekEnd, Tr("End"));
        // Translators: Player menu item that asks for a time and jumps to it.
        Add(playerMenu, playbackItems, commandIds, shortcuts, ActionId.GoToTime, Tr("Go to time..."));
        playerMenu.AppendSeparator();
        var jumpMenu = new Menu();
        foreach (var jump in PlaybackActions.PercentJumps)
            Add(jumpMenu, playbackItems, commandIds, shortcuts, jump.Id, $"{jump.Percent}%");
        // Translators: Player submenu holding items that jump to a position given as a percentage, from 10% to 100%.
        playerMenu.AppendSubMenu(jumpMenu, Tr("Jump to Percentage"));

        var movementMenu = new Menu();
        foreach (var step in PlaybackActions.SeekSteps)
            Add(movementMenu, playbackItems, commandIds, shortcuts, step.Id, step.Label);
        // Translators: Player submenu for choosing how far one press of the seek keys moves.
        playerMenu.AppendSubMenu(movementMenu, Tr("Seek amount"));
        playerMenu.AppendSeparator();
        // Translators: Player menu item that opens the list of audio output devices to play through.
        playerMenu.Append(commandIds[ActionId.SoundCards], Label(Tr("Sound Cards..."), ActionId.SoundCards, shortcuts));
        // Switching between a file's audio tracks. Gated on there being more than one to switch between, not
        // on a file being loaded, so the three stay out of playbackItems and travel in their own list.
        var audioTrackItems = new List<MenuItem>();
        var audioTrackMenu = new Menu();
        // Translators: Item in the Audio track submenu that switches to the next audio track in the file.
        Add(audioTrackMenu, audioTrackItems, commandIds, shortcuts, ActionId.NextAudioTrack, Tr("Next"));
        // Translators: Item in the Audio track submenu that switches to the previous audio track in the file.
        Add(audioTrackMenu, audioTrackItems, commandIds, shortcuts, ActionId.PreviousAudioTrack, Tr("Previous"));
        audioTrackMenu.AppendSeparator();
        // Translators: Item in the Audio track submenu that opens the window listing the file's audio tracks to choose from.
        Add(audioTrackMenu, audioTrackItems, commandIds, shortcuts, ActionId.OpenAudioTracks, Tr("Track list..."));
        // Translators: Player submenu holding the commands that switch between a file's audio tracks.
        playerMenu.AppendSubMenu(audioTrackMenu, Tr("Audio track"));
        var chapters = BuildChaptersMenu(playerMenu, commandIds, shortcuts);
        // The subtitles to read aloud. The dynamic region - Off, then one radio item per track - is rebuilt
        // per file; the fixed tail below it (load from file, load from URL, the remember tick box) is built
        // once here and never deleted, so its permanent action ids stay reserved. Off and the tracks carry no
        // ActionId, like the equalizer presets. The quick on/off key (ActionId.ToggleSubtitles) is a bare
        // accelerator, not shown here.
        var subtitleCommands = new Dictionary<int, string>();
        var subtitleItems = new Dictionary<string, MenuItem>(StringComparer.Ordinal);
        var subtitleMenu = new Menu();
        FillSubtitleTracks(subtitleMenu, [], null, subtitleCommands, subtitleItems);
        var subtitleRememberItem = AppendSubtitleCommands(subtitleMenu, commandIds, shortcuts);
        // Translators: Player submenu listing the file's subtitles to read aloud, with Off at the top.
        var subtitleMenuItem = playerMenu.AppendSubMenu(subtitleMenu, Tr("Subtitles"));
        // Full screen is gated on a picture playing, not on a file being loaded, so it stays out of playbackItems.
        var fullScreenItem = playerMenu.AppendCheckItem(
            commandIds[ActionId.ToggleFullScreen],
            // Translators: Player menu item that fills the whole screen with the video, or leaves that mode. It is ticked while full screen.
            Label(Tr("Full screen"), ActionId.ToggleFullScreen, shortcuts));
        return new PlayerMenuParts(
            playerMenu, playbackItems, audioTrackItems, equalizerCommands, equalizerItems, equalizerMenu,
            subtitleCommands, subtitleItems, subtitleMenu, subtitleMenuItem, subtitleRememberItem,
            shuffleItem, repeatItem, silenceItem, fullScreenItem, chapters);
    }
    /// <summary>What <see cref="MainMenuComponents.EqualizerCommands"/> holds for the item that switches
    /// the equalizer off, in place of a preset name. Not a name any preset can have, because a preset
    /// whose name was empty could not be stored.</summary>
    internal const string EqualizerOffKey = "";

    /// <summary>The key the Subtitles menu holds for the Off item, in place of a track id.</summary>
    internal const string SubtitleOffKey = "";

    /// <summary>Puts Off, every preset and the two editing commands into the equalizer submenu.</summary>
    ///
    /// <remarks>
    /// Called again whenever the user adds or removes a preset, so it fills a menu rather than making
    /// one: the submenu itself is already attached to the menu bar and outlives its contents.
    ///
    /// Off and the presets are one unbroken run of radio items on purpose. wxWidgets ends a radio group at
    /// the first item that is not one, a separator included, and two groups would let Off stay ticked
    /// beside a ticked preset. The two commands below the separator are ordinary items with an
    /// <see cref="ActionId"/> apiece, so they can be given a shortcut; the presets cannot, because which
    /// presets exist is not something the fixed action table can describe.
    /// </remarks>
    internal static void FillEqualizerMenu(
        Menu menu,
        IReadOnlyList<PresetEntry> presets,
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts,
        IDictionary<int, string> commands,
        IDictionary<string, MenuItem> items)
    {
        // Translators: Item in the Equalizer submenu that leaves the sound unequalized. It is ticked while no preset is in use.
        AddEqualizerItem(menu, commands, items, EqualizerOffKey, Tr("Off"));
        foreach (var preset in presets)
            AddEqualizerItem(menu, commands, items, preset.Id, preset.Name);
        menu.AppendSeparator();
        menu.Append(commandIds[ActionId.EditEqualizerPreset],
            // Translators: Item in the Equalizer submenu that opens the current preset for editing.
            Label(Tr("Edit current preset..."), ActionId.EditEqualizerPreset, shortcuts));
        menu.Append(commandIds[ActionId.ManageEqualizerPresets],
            // Translators: Item in the Equalizer submenu that opens the list of presets.
            Label(Tr("Manage presets..."), ActionId.ManageEqualizerPresets, shortcuts));
    }

    /// <remarks>
    /// These carry no shortcut and no <see cref="ActionId"/>: which presets exist changes while the player
    /// runs, so there is no fixed action for a key to be bound to.
    /// </remarks>
    private static void AddEqualizerItem(
        Menu menu,
        IDictionary<int, string> commands,
        IDictionary<string, MenuItem> items,
        string presetId,
        string label)
    {
        var id = IdManager.NewId();
        commands[id] = presetId;
        items[presetId] = menu.AppendRadioItem(id, label);
    }
    /// <summary>Fills the Subtitles submenu's dynamic region: Off and one radio item per track, ticking the
    /// active one. Items are inserted at the top so they sit above the fixed command tail that
    /// <see cref="AppendSubtitleCommands"/> added once - see <see cref="MainFrame.RebuildSubtitleMenu"/> for
    /// why the tail is never part of the rebuild.</summary>
    ///
    /// <remarks>
    /// Off and the tracks are one unbroken run of radio items, for the same reason the equalizer's are - see
    /// <see cref="FillEqualizerMenu"/>. They get a fresh auto id each time, which deletion safely frees; the
    /// fixed tail keeps its permanent action ids precisely because it is not rebuilt.
    /// </remarks>
    internal static void FillSubtitleTracks(
        Menu menu,
        IReadOnlyList<LunaPlayer.UI.Playback.SubtitleMenuEntry> entries,
        int? activeId,
        IDictionary<int, string> commands,
        IDictionary<string, MenuItem> items)
    {
        var position = 0;
        // Translators: Item at the top of the Subtitles menu that reads no subtitles. Ticked by default.
        InsertSubtitleRadio(menu, position++, commands, items, SubtitleOffKey, Tr("Off"));
        foreach (var entry in entries)
            InsertSubtitleRadio(menu, position++, commands, items, entry.Id.ToString(CultureInfo.InvariantCulture), entry.Label);
        var key = activeId?.ToString(CultureInfo.InvariantCulture) ?? SubtitleOffKey;
        if (items.TryGetValue(key, out var item))
            item.Checked = true;
    }

    /// <summary>Adds the fixed tail of the Subtitles submenu once, below a separator: load from a file, load
    /// from a URL, and the session-only remember tick box. Returns the remember item so its tick can be set
    /// later. These carry an <see cref="ActionId"/> apiece, so they take a shortcut and go through the
    /// ordinary action path; they are built once and never deleted, so their permanent ids are not freed and
    /// re-used - which wxWidgets asserts on.</summary>
    internal static MenuItem AppendSubtitleCommands(
        Menu menu,
        IReadOnlyDictionary<ActionId, int> commandIds,
        ShortcutManager shortcuts)
    {
        menu.AppendSeparator();
        menu.Append(commandIds[ActionId.LoadSubtitleFile],
            // Translators: Subtitles submenu item that loads a subtitle file from this computer.
            Label(Tr("Load from file..."), ActionId.LoadSubtitleFile, shortcuts));
        menu.Append(commandIds[ActionId.LoadSubtitleUrl],
            // Translators: Subtitles submenu item that loads a subtitle from a web address.
            Label(Tr("Load from URL..."), ActionId.LoadSubtitleUrl, shortcuts));
        return menu.AppendCheckItem(commandIds[ActionId.RememberSubtitle],
            // Translators: Subtitles submenu item that keeps the chosen subtitle selected until the player closes.
            Label(Tr("Remember subtitle for this session"), ActionId.RememberSubtitle, shortcuts));
    }

    /// <remarks>Carries no shortcut and no <see cref="ActionId"/>, like the equalizer presets: which subtitle
    /// tracks exist changes with the file, so there is no fixed action for a key to be bound to.</remarks>
    private static void InsertSubtitleRadio(
        Menu menu,
        int position,
        IDictionary<int, string> commands,
        IDictionary<string, MenuItem> items,
        string trackKey,
        string label)
    {
        var id = IdManager.NewId();
        commands[id] = trackKey;
        items[trackKey] = menu.Insert(position, id, label, kind: MenuItemKind.Radio);
    }
}

