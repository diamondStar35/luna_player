using LunaPlayer.Media;
using LunaPlayer.Subsonic.Models;
using Reference = LunaPlayer.Subsonic.Playback.Reference;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    internal byte[]? Artwork { get; private set; }
    internal void UpdateCatalog(Guid server, IEnumerable<Entry> songs)
    {
        var changed = false; var current = false;
        var paths = Items.Where(item => item.ServerMedia?.ServerId == server).Select(item => item.Path).ToHashSet(StringComparer.Ordinal);
        foreach (var song in songs)
        {
            var path = new Reference(server, song.Id).Address;
            if (!paths.Contains(path)) continue;
            var tags = new MediaTags(song.Title, song.DisplayArtist.Length > 0 ? song.DisplayArtist : song.Artist, song.Album);
            _catalogTags[path] = tags; _playlist.SetTitle(path, song.Title);
            changed = true; current |= CurrentPath == path;
        }
        if (current) MetadataChanged?.Invoke();
        if (changed) StateChanged?.Invoke();
    }
    internal void SetArtwork(string? identity, byte[]? artwork)
    {
        if (identity is not null && CurrentPath != identity) return;
        Artwork = artwork;
        MetadataChanged?.Invoke();
    }
}
