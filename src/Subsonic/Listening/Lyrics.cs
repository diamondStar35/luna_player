using System.Net;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Listening;

internal sealed partial class Listening
{
    internal async Task<StructuredLyrics[]> GetSongLyricsAsync(string id, string artist, string title, CancellationToken token = default)
    {
        try
        {
            var versions = await GetLyricsBySongIdAsync(id, token).ConfigureAwait(false);
            var available = versions.Select(version => version.Synced ? version with
                { Lines = version.Lines.Where(line => line.Start is >= 0).OrderBy(line => line.Start).ToArray() } : version)
                .Where(version => version.Lines.Length > 0).ToArray();
            if (available.Length > 0) return available;
        }
        catch (RequestException error) when (error.Kind == FailureKind.Unsupported || error.StatusCode == HttpStatusCode.NotFound || error.Code == 70)
        {
            // Older servers can provide lyrics by artist/title without the song-ID endpoint.
        }
        return [LyricsParser.Parse((await GetLyricsAsync(artist, title, token).ConfigureAwait(false)).Value)];
    }
}
