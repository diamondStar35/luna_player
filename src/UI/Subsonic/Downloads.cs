using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void Downloads(Command command, Row[] rows, Guid server)
        => _songs.Downloads(command, rows, server, SelectedLibraries());
}
