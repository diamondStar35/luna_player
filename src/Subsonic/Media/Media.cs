using System.Text.Json;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic.Media;

/// <summary>Produces transient media requests; authenticated addresses must not become saved track IDs.</summary>
internal sealed class Media(Session session, Func<string, int, CancellationToken, Task> require) : Feature(session)
{
    internal async Task<Uri> GetStreamAddressAsync(string id, StreamOptions? options = null, CancellationToken token = default)
        => Session.Address("stream", await StreamParametersAsync(id, options ?? new(), token).ConfigureAwait(false));
    internal async Task<Response> OpenStreamAsync(string id, StreamOptions? options = null, long? byteOffset = null, CancellationToken token = default)
        => await Session.OpenAsync("stream", await StreamParametersAsync(id, options ?? new(), token).ConfigureAwait(false), token, byteOffset).ConfigureAwait(false);
    internal Task<Response> OpenDownloadAsync(string id, CancellationToken token = default)
        => Session.OpenAsync("download", new Parameters().With("id", Id(id)).With("format", "raw"), token);
    internal Task<Response> OpenCoverArtAsync(string id, int? size = null, CancellationToken token = default)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        return Session.OpenAsync("getCoverArt", new Parameters().With("id", Id(id)).With("size", size), token);
    }
    internal Task<Response> OpenAvatarAsync(string username, CancellationToken token = default)
        => Session.OpenAsync("getAvatar", new Parameters().With("username", Id(username)), token);
    internal async Task<TranscodeDecision> GetTranscodeDecisionAsync(string id, ClientInfo client, string mediaType = "song", CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        await require("transcoding", 1, token).ConfigureAwait(false);
        var result = await Session.ReadAsync("getTranscodeDecision", new Parameters().With("mediaId", Id(id))
            .With("mediaType", Id(mediaType)), token, json: JsonSerializer.SerializeToUtf8Bytes(client, ProtocolJson.Default.ClientInfo)).ConfigureAwait(false);
        return Required(result.TranscodeDecision, "getTranscodeDecision");
    }
    internal async Task<Response> OpenTranscodeAsync(string id, string transcodeParams, double offset = 0,
        string mediaType = "song", CancellationToken token = default)
    {
        if (!double.IsFinite(offset) || offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        await require("transcoding", 1, token).ConfigureAwait(false);
        // The token is opaque. Query encoding preserves it; clients must not reconstruct its contents.
        return await Session.OpenAsync("getTranscodeStream", new Parameters().With("mediaId", Id(id))
            .With("mediaType", Id(mediaType)).With("transcodeParams", Id(transcodeParams)).With("offset", offset), token).ConfigureAwait(false);
    }
    private async Task<Parameters> StreamParametersAsync(string id, StreamOptions options, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!Enum.IsDefined(options.Mode) || options.MaximumBitRate < 0
            || options.TimeOffset is { } offset && (!double.IsFinite(offset) || offset < 0))
            throw new ArgumentOutOfRangeException(nameof(options));
        if (options.Mode == StreamingMode.Transcode) ArgumentException.ThrowIfNullOrWhiteSpace(options.Format);
        if (options.TimeOffset is > 0) await require("transcodeOffset", 1, token).ConfigureAwait(false);
        return new Parameters().With("id", Id(id)).With("format", options.Mode switch
        { StreamingMode.Original => "raw", StreamingMode.Transcode => options.Format, _ => null })
            .With("maxBitRate", options.Mode == StreamingMode.Transcode ? options.MaximumBitRate : null)
            .With("timeOffset", options.TimeOffset).With("estimateContentLength", options.EstimateContentLength ? true : null);
    }
}
