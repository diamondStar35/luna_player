using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal static partial class Builder
{
    private sealed record RecordingMenuParts(Menu Menu, MenuItem StartItem, MenuItem PauseItem, MenuItem StopItem);

    private static RecordingMenuParts BuildRecordingMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        // Recording needs nothing loaded and nothing playing, so the window and the folder are always
        // available. The three that run a recording are not: they follow the recorder itself, which is why
        // they are kept rather than added to one of the enable lists above - each has its own condition.
        var recordingMenu = new Menu();
        // Translators: Recording menu item that opens the window where recording is set up and run.
        recordingMenu.Append(commandIds[ActionId.OpenRecordingInterface], Label(Tr("Open the recording interface..."), ActionId.OpenRecordingInterface, shortcuts));
        recordingMenu.AppendSeparator();
        // Translators: Recording menu item that begins recording.
        var startRecordingItem = recordingMenu.Append(commandIds[ActionId.StartRecording], Label(Tr("Start recording"), ActionId.StartRecording, shortcuts));
        // Translators: Recording menu item that holds a recording where it is, or starts it again.
        var pauseRecordingItem = recordingMenu.Append(commandIds[ActionId.PauseRecording], Label(Tr("Pause"), ActionId.PauseRecording, shortcuts));
        // Translators: Recording menu item that ends a recording and closes the file.
        var stopRecordingItem = recordingMenu.Append(commandIds[ActionId.StopRecording], Label(Tr("Stop"), ActionId.StopRecording, shortcuts));
        recordingMenu.AppendSeparator();
        // Translators: Recording menu item that opens the folder recordings are saved into.
        recordingMenu.Append(commandIds[ActionId.OpenRecordingsFolder], Label(Tr("Open recordings folder"), ActionId.OpenRecordingsFolder, shortcuts));
        return new RecordingMenuParts(recordingMenu, startRecordingItem, pauseRecordingItem, stopRecordingItem);
    }
}
