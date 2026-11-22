using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic.Playlists;

internal sealed class Playlists(Session session) : Feature(session)
{
    internal async Task<Models.Playlist[]> GetAllAsync(string? username = null, CancellationToken token = default)
        => Required((await Session.ReadAsync("getPlaylists", new Parameters().With("username", username), token).ConfigureAwait(false)).Playlists, "getPlaylists").Playlists;
    internal async Task<Models.Playlist> GetAsync(string id, CancellationToken token = default)
        => Required((await Session.ReadAsync("getPlaylist", new Parameters().With("id", Id(id)), token).ConfigureAwait(false)).Playlist, "getPlaylist");
    internal async Task<Models.Playlist> CreateAsync(string name, IEnumerable<string>? songs = null, CancellationToken token = default)
        => Required((await Session.ReadAsync("createPlaylist", new Parameters().With("name", Id(name))
            .Many("songId", songs?.Select(Id)), token, mutation: true).ConfigureAwait(false)).Playlist, "createPlaylist");
    internal async Task<Models.Playlist> ReplaceAsync(string id, IEnumerable<string> songs, CancellationToken token = default)
        => Required((await Session.ReadAsync("createPlaylist", new Parameters().With("playlistId", Id(id))
            .Many("songId", songs.Select(Id)), token, mutation: true).ConfigureAwait(false)).Playlist, "createPlaylist");
    internal Task UpdateAsync(string id, string? name = null, string? comment = null, bool? isPublic = null,
        IEnumerable<string>? add = null, IEnumerable<int>? removeIndexes = null, CancellationToken token = default)
    {
        if (name is not null) Id(name);
        var indexes = removeIndexes?.ToArray();
        if (indexes?.Any(x => x < 0) == true) throw new ArgumentOutOfRangeException(nameof(removeIndexes));
        // Removal addresses occurrences, not song IDs: a playlist may contain the same song more than once.
        return Change("updatePlaylist", new Parameters().With("playlistId", Id(id)).With("name", name)
            .With("comment", comment).With("public", isPublic).Many("songIdToAdd", add?.Select(Id))
            .Many("songIndexToRemove", indexes), token);
    }
    internal Task DeleteAsync(string id, CancellationToken token = default)
        => Change("deletePlaylist", new Parameters().With("id", Id(id)), token);
}
