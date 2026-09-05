using System.Text.Json.Serialization;

namespace LunaPlayer.Actions;

[JsonConverter(typeof(JsonStringEnumConverter<ActionId>))]
internal enum ActionId
{
    OpenFile, OpenLink, OpenFolder, OpenContainingFolder, OpenFileProperties, OpenedFiles, CloseFile, CloseAllFiles,
    OpenPreferences, Exit, AnnounceFileInfo, RenameFile, DeleteFile, CopyFile, PasteFile, ToggleMarkCurrent,
    ToggleMarkAll, ClearMarks, AnnounceMarkedCount, MarkedCopyToFolder, MarkedMoveToFolder,
    MarkedCopyToClipboard, MarkedDelete, AddBookmark, ManageBookmarks, JumpBookmark1, JumpBookmark2,
    JumpBookmark3, JumpBookmark4, JumpBookmark5, JumpBookmark6, JumpBookmark7, JumpBookmark8,
    JumpBookmark9, JumpBookmark10, PlayPause, SeekBackward, SeekForward, SeekBackwardX2, SeekForwardX2,
    SeekBackwardX4, SeekForwardX4, SeekStart, SeekEnd, SeekStep1, SeekStep2, SeekStep3, SeekStep4, SeekStep5,
    SeekStep6, SeekStep7, SeekStep8, SeekStep9, SeekStep0, SeekStepCustom,
    VolumeUp, VolumeDown, VolumeMaximize, VolumeMinimize, AnnounceVolume, AnnounceElapsed, AnnounceRemaining,
    AnnounceDuration, AnnouncePercent, AnnounceSpeed, ToggleVerbosity, SpeedUp, SpeedDown, ResetSpeed,
    StartSelection, EndSelection, ClearSelection, JumpPercent10, JumpPercent15, JumpPercent20, JumpPercent25,
    JumpPercent30, JumpPercent35, JumpPercent40, JumpPercent45, JumpPercent50, JumpPercent55, JumpPercent60,
    JumpPercent65, JumpPercent70, JumpPercent75, JumpPercent80, JumpPercent85, JumpPercent90, JumpPercent95,
    JumpPercent100, PreviousTrack, NextTrack, FirstTrack, GoToFile, LastTrack, ToggleShuffle, ToggleRepeatFile,
    GoToTime, SoundCards,
    ToggleSilenceRemoval,
    OpenYouTubeLink, SearchYouTube, OpenFavorites, VideoDownload, VideoDescription, VideoCopyLink,
    UpdateYouTubeComponents,
    // Appended at the end on purpose: these names are what the settings file stores for a shortcut, so
    // inserting one anywhere else would rename every action after it.
    OpenRecordingInterface, StartRecording, PauseRecording, StopRecording, OpenRecordingsFolder,
}

internal sealed record ActionDefinition(ActionId Id, string Label, Shortcut? PrimaryShortcut = null, Shortcut? SecondaryShortcut = null);

internal static class ActionRegistry
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [.. MediaActionDefinitions.All, .. PlaybackActionDefinitions.All, .. YouTubeActionDefinitions.All,
        .. RecordingActionDefinitions.All];
}
