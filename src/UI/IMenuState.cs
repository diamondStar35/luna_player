using LunaPlayer.Actions;
using LunaPlayer.Equalizer;
using LunaPlayer.UI.Equalizer;

namespace LunaPlayer.UI;

/// <summary>Which selection commands make sense for the current file and slot.</summary>
internal readonly record struct SelectionMenuState(
    bool Available,
    bool Previous,
    bool Next,
    bool ClearCurrent,
    bool ClearAll,
    bool SaveSelection,
    bool SaveSelections,
    bool Repeat);

/// <summary>The state of the menus and their checkable items, kept in step with what the player is doing,
/// plus the menu choices that are not fixed actions (equalizer presets, subtitle tracks, recents).</summary>
internal interface IMenuState
{
    /// <summary>The user has chosen an equalizer preset from the menu, or null to switch it off.</summary>
    /// <remarks>
    /// Not an <see cref="ActionId"/>, because which presets exist is not something the fixed action table
    /// can describe and no key is bound to any of them.
    /// </remarks>
    event Action<string?>? EqualizerPresetRequested;

    /// <summary>The user has chosen a subtitle track from the menu, or null for Off. Not an
    /// <see cref="ActionId"/>, for the same reason as <see cref="EqualizerPresetRequested"/>: which subtitle
    /// tracks exist changes per file.</summary>
    event Action<int?>? SubtitleTrackRequested;

    /// <summary>The user picked an item in a Recents submenu - to open, or to clear that kind. Not an
    /// <see cref="ActionId"/>, like the equalizer and subtitle entries: which recents exist changes as files
    /// are opened.</summary>
    event Action<RecentCommand>? RecentRequested;

    void SetMediaLoaded(bool loaded);
    void SetShuffleChecked(bool isChecked);
    void SetRepeatFileChecked(bool isChecked);
    void SetSilenceRemovalChecked(bool isChecked);
    void SetSelectionState(SelectionMenuState state);

    /// <summary>Ticks the equalizer preset in force, or the Off item when <paramref name="presetId"/> is
    /// null.</summary>
    void SetEqualizerPreset(string? presetId);

    /// <summary>Builds the equalizer submenu again, for when a preset has been added, renamed or removed.
    /// </summary>
    void RebuildEqualizerMenu(IReadOnlyList<PresetEntry> presets, string? selected);

    void SetEditState(bool hasLocalFile, bool hasMedia);
    void SetBookmarkState(bool enabled);
    void SetMarkState(bool currentMarked, bool allMarked);
    void SetMarkedActionsEnabled(bool enabled);
    void SetVideoOptionsEnabled(bool enabled);

    /// <summary>Enables or greys out the full-screen menu item; on only while a real picture is playing.</summary>
    void SetFullScreenAvailable(bool available);

    /// <summary>Enables or greys out the audio-track commands; on only while the file has more than one
    /// audio track to switch between.</summary>
    void SetAudioTrackControlsEnabled(bool enabled);

    /// <summary>Rebuilds the Subtitles submenu's track list from the file's tracks, ticking the active one
    /// (or Off when <paramref name="activeId"/> is null). The fixed load/remember commands below are left in
    /// place. Called a moment after each file loads.</summary>
    void RebuildSubtitleMenu(IReadOnlyList<SubtitleMenuEntry> entries, int? activeId);

    /// <summary>Ticks or clears the "remember subtitle for this session" box.</summary>
    void SetSubtitleRemember(bool on);

    /// <summary>Rebuilds the three Recents submenus (files, folders, playlists) from newest-first entries
    /// whose labels are already numbered and worded. An empty kind shows a disabled placeholder.</summary>
    void RebuildRecentsMenu(
        IReadOnlyList<RecentMenuEntry> files,
        IReadOnlyList<RecentMenuEntry> folders,
        IReadOnlyList<RecentMenuEntry> playlists);

    /// <summary>Ticks the subtitle track being read, or the Off item when <paramref name="activeId"/> is null,
    /// without rebuilding the submenu - for when the choice changes but the track list has not.</summary>
    void SetSubtitleSelection(int? activeId);

    /// <summary>Enables or disables the whole Subtitles submenu - off when nothing is loaded, since there is
    /// no file to read or attach a subtitle to; on whenever a file is open, embedded subtitles or not,
    /// because one can always be loaded from a file or a URL.</summary>
    void SetSubtitleControlsEnabled(bool enabled);

    /// <summary>Puts the recording menu into the state the recorder is in.</summary>
    void SetRecordingState(LunaPlayer.Recording.RecordingState state);
}
