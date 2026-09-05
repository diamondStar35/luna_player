namespace LunaPlayer.Actions;

/// <summary>The commands that record sound.</summary>
///
/// <remarks>
/// Kept apart from the rest because recording is a feature that can be finished, changed or taken away as
/// a whole, and because none of these depend on anything being played: recording works with the player
/// idle, which is most of what it is for.
/// </remarks>
internal static class RecordingActionDefinitions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that opens the window where recording is set up and run.
        new(ActionId.OpenRecordingInterface, Tr("Open the recording interface"),
            new("r", ShortcutModifiers.Alt)),
        // The three keys the Python player uses. Function keys because recording is started and stopped
        // while something else has the keyboard's attention, and F7 to F9 sit together under one hand.
        // Translators: Name of the command that begins recording.
        new(ActionId.StartRecording, Tr("Start recording"), new("f9")),
        // Translators: Name of the command that holds a recording where it is, or starts it again.
        new(ActionId.PauseRecording, Tr("Pause or resume recording"), new("f7")),
        // Translators: Name of the command that ends a recording and closes the file.
        new(ActionId.StopRecording, Tr("Stop recording"), new("f8")),
        // Translators: Name of the command that opens the folder recordings are saved into.
        new(ActionId.OpenRecordingsFolder, Tr("Open the recordings folder")),
    ];
}
