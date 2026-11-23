using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Playback;

/// <summary>Reuses negotiation, but builds each trimmed request from its own source position.</summary>
internal sealed record StreamPlan(Client Client, Entry Song, StreamOptions Options,
    IReadOnlyDictionary<string, string> Headers, Uri Address, bool Reopen,
    StreamFormat? Format, string? TranscodeParams = null, string MediaType = "song")
{
    internal async Task<MediaInput> AtAsync(double? position, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        // Both offset APIs accept whole seconds. mpv seeks the fractional remainder locally.
        var requested = position is > 0 && double.IsFinite(position.Value) ? position.Value : 0;
        var offset = Reopen ? Math.Floor(requested) : 0;
        var address = Address;
        if (offset > 0)
            address = TranscodeParams is { } parameters
                ? await Client.Media.GetTranscodeAddressAsync(Song.Id, parameters, checked((long)offset), MediaType, token).ConfigureAwait(false)
                : await Client.Media.GetStreamAddressAsync(Song.Id, Options with { TimeOffset = offset }, token).ConfigureAwait(false);
        return new(address.AbsoluteUri, new MediaTags(Song.Title,
            Song.DisplayArtist.Length > 0 ? Song.DisplayArtist : Song.Artist, Song.Album), Song.Duration,
            Headers, Offset: offset, SeekByReopening: Reopen, Format: Format, RequestedPosition: requested);
    }
}
