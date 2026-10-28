using LunaPlayer.Configuration;
using LunaPlayer.Media;
namespace LunaPlayer.Playlist;
internal sealed partial class PlaylistState
{

    internal bool? ToggleCurrentMarked()
    {
        return _marks.Toggle(CurrentPath);
    }

    internal bool ToggleAllMarked()
    {
        return _marks.ToggleAll(SourceFiles);
    }

    internal bool ClearMarked()
    {
        return _marks.Clear();
    }
    internal int MarkedCount => _marks.Count(SourceFiles);
    internal bool IsCurrentMarked => _marks.Contains(CurrentPath);
    internal bool AreAllMarked => _marks.AreAll(SourceFiles);
    internal IReadOnlyList<string> MarkedFiles
        => _marks.Files(SourceFiles);

}
