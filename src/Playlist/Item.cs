using LunaPlayer.Media;

namespace LunaPlayer.Playlist;

/// <summary>A source occurrence owns all its chapter items. Identity is independent of its path, so
/// queued transitions and shuffled navigation can distinguish consecutive chapters of one book.</summary>
internal sealed record PlaylistItem(Guid SourceId, string Path, Chapter? Chapter = null)
{
    internal string Id => $"{SourceId:N}/{Chapter?.Index ?? -1}";
    internal double Start => Chapter?.Start ?? 0;
    internal double? End => Chapter?.End;
}
