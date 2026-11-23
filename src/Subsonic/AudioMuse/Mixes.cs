using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.AudioMuse;

/// <summary>Fallback is limited to absent similarity results; connection and authentication failures remain failures.</summary>
internal sealed class Mixes(Subsonic.Client subsonic, Client? audioMuse = null)
{
    internal async Task<Result> SimilarAsync(string id, SeedKind kind = SeedKind.Song, int count = 50, CancellationToken token = default)
    {
        Feature.Id(id);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Entry[] songs;
        try { songs = await subsonic.Library.GetSimilarSongsAsync(id, count, token: token).ConfigureAwait(false); }
        catch (RequestException error) when (error.Kind == FailureKind.Unsupported) { songs = []; }
        if (songs.Length > 0)
            return new(songs.Select(song => new Track { Id = song.Id, Title = song.Title, Artist = song.Artist, Album = song.Album }).ToArray(), "", Songs: songs);
        return audioMuse is null ? new([], "") : await audioMuse.SimilarAsync(id, kind, token).ConfigureAwait(false);
    }
}
