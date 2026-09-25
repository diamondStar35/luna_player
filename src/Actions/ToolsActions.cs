namespace LunaPlayer.Actions;

/// <summary>The commands under the Tools menu.</summary>
///
/// <remarks>
/// Kept apart like recording is, and for the same reason: the tools are self-contained features that need
/// nothing loaded and nothing playing. The media converter works with the player idle, which is most of
/// what it is for - a folder is sent to it from Windows Explorer and turned into audio while nothing is
/// open at all.
/// </remarks>
internal static class ToolsActionDefinitions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that opens the window for converting files to another audio format.
        new(ActionId.OpenMediaConverter, Tr("Open the media converter")),
    ];
}
