using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void Lyrics(Row row) => _songs.Lyrics(_server, row);
}
