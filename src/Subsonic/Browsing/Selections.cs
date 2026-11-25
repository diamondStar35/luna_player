using System.Runtime.CompilerServices;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Browsing;

/// <summary>Resolves selections without changing playback or persisting a credential-bearing media address.</summary>
internal sealed class Selections(Client client)
{
    internal async IAsyncEnumerable<Entry> ResolveAsync(IEnumerable<Selection> selections,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var preceding = new HashSet<string>(StringComparer.Ordinal);
        foreach (var selection in selections)
        {
            token.ThrowIfCancellationRequested();
            Entry[] entries = selection.Kind switch
            {
                SelectionKind.Song => [await client.Library.GetSongAsync(selection.Id, token).ConfigureAwait(false)],
                SelectionKind.Album => (await client.Library.GetAlbumAsync(selection.Id, token).ConfigureAwait(false)).Songs,
                SelectionKind.Artist => await ArtistSongsAsync(selection.Id, token).ConfigureAwait(false),
                SelectionKind.Playlist => (await client.Playlists.GetAsync(selection.Id, token).ConfigureAwait(false)).Entries,
                _ => throw new ArgumentOutOfRangeException(nameof(selections))
            };
            // Keep repeated occurrences within a playlist; remove only overlaps with earlier selections.
            var current = new HashSet<string>(StringComparer.Ordinal);
            foreach (var song in entries)
                if (!preceding.Contains(song.Id)) { current.Add(song.Id); yield return song; }
            preceding.UnionWith(current);
        }
    }
    private async Task<Entry[]> ArtistSongsAsync(string id, CancellationToken token)
    {
        var artist = await client.Library.GetArtistAsync(id, token).ConfigureAwait(false);
        var songs = new List<Entry>();
        foreach (var album in artist.Albums)
            songs.AddRange((await client.Library.GetAlbumAsync(album.Id, token).ConfigureAwait(false)).Songs);
        return songs.ToArray();
    }
}
