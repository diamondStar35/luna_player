using System.Net.Http.Headers;
using System.Text;

namespace LunaPlayer.Spotify;

/// <summary>Resolves playable encrypted audio files through Spotify's metadata service.</summary>
internal sealed class PlaybackFiles(HttpClient http, AccessPointResolver resolver,
    PlaybackHttpTokens tokens, AccessPointLogin.Credentials credentials)
{
    private readonly HttpClient _http = http;
    private readonly AccessPointResolver _resolver = resolver;
    private readonly PlaybackHttpTokens _tokens = tokens;
    private readonly AccessPointLogin.Credentials _credentials = credentials;

    internal async Task<(TrackId KeyTrack, byte[] FileId, int Format)> GetTrackFileAsync(string uri, int bitrate,
        CancellationToken cancellationToken)
    {
        if (!TrackId.TryParse(uri, out var trackId)) throw new ArgumentException(null, nameof(uri));
        using var query = new MemoryStream();
        ProtoWire.WriteNumber(query, 1, 10);
        using var entity = new MemoryStream();
        ProtoWire.WriteBytes(entity, 1, Encoding.UTF8.GetBytes(uri));
        ProtoWire.WriteBytes(entity, 2, query.ToArray());
        using var batch = new MemoryStream();
        ProtoWire.WriteBytes(batch, 2, entity.ToArray());
        var response = await RequestAsync(HttpMethod.Post,
            "/extended-metadata/v0/extended-metadata", batch.ToArray(), cancellationToken)
            .ConfigureAwait(false);
        var metadata = FindBytes(response, 2);
        var data = FindBytes(metadata, 3);
        var any = FindBytes(data, 3);
        var track = FindBytes(any, 2);
        var order = bitrate switch
        {
            <= 96 => new[] { 0, 6, 1, 5, 3, 2, 4 },
            <= 160 => new[] { 1, 5, 0, 6, 3, 2, 4 },
            _ => new[] { 2, 4, 3, 1, 5, 0, 6 },
        };
        if (ChooseFile(track, order) is { } selected)
            return (trackId, selected.FileId, selected.Format);
        var alternatives = new ProtoReader(track);
        while (alternatives.TryRead(out var field, out _, out var alternative))
        {
            if (field != 13) continue;
            var alternativeTrack = new ProtoReader(alternative);
            TrackId? alternateId = null;
            while (alternativeTrack.TryRead(out var item, out _, out var value))
                if (item == 1 && value.Length == 16) alternateId = TrackId.ReadRaw(value);
            if (alternateId is { } id && ChooseFile(alternative, order) is { } file)
                return (id, file.FileId, file.Format);
        }
        throw new InvalidDataException();
    }

    private static (byte[] FileId, int Format)? ChooseFile(ReadOnlySpan<byte> track, int[] order)
    {
        var reader = new ProtoReader(track);
        var files = new Dictionary<int, byte[]>();
        while (reader.TryRead(out var field, out _, out var bytes))
        {
            if (field != 12) continue;
            var file = new ProtoReader(bytes);
            byte[]? id = null;
            var format = -1;
            while (file.TryRead(out var item, out var number, out var value))
            {
                if (item == 1) id = value.ToArray();
                if (item == 2) format = checked((int)number);
            }
            if (id?.Length == 20) files[format] = id;
        }
        foreach (var format in order)
            if (files.TryGetValue(format, out var fileId)) return (fileId, format);
        return null;
    }

    internal async Task<string[]> GetCdnUrlsAsync(ReadOnlyMemory<byte> fileId,
        CancellationToken cancellationToken)
    {
        if (fileId.Length != 20) throw new ArgumentException(null, nameof(fileId));
        var response = await RequestAsync(HttpMethod.Get,
            "/storage-resolve/files/audio/interactive/" + Convert.ToHexStringLower(fileId.Span),
            null, cancellationToken).ConfigureAwait(false);
        var reader = new ProtoReader(response);
        var result = 0;
        var urls = new List<string>();
        while (reader.TryRead(out var field, out var number, out var bytes))
        {
            if (field == 1) result = (int)number;
            if (field == 2) urls.Add(Encoding.UTF8.GetString(bytes));
        }
        return result == 0 && urls.Count > 0 ? urls.ToArray() : throw new InvalidDataException();
    }

    internal async Task<BufferedAudioStream> OpenAudioAsync(ReadOnlyMemory<byte> fileId,
        CancellationToken cancellationToken)
    {
        if (fileId.Length != 20) throw new ArgumentException(null, nameof(fileId));
        var urls = await GetCdnUrlsAsync(fileId, cancellationToken).ConfigureAwait(false);
        return await BufferedAudioStream.OpenAsync(_http, urls, cancellationToken).ConfigureAwait(false);
    }

    private async Task<byte[]> RequestAsync(HttpMethod method, string path, byte[]? body,
        CancellationToken cancellationToken)
    {
        var (host, port) = await _resolver.ResolveAsync("spclient", cancellationToken)
            .ConfigureAwait(false);
        var salt = Random.Shared.NextInt64(0, uint.MaxValue + 1L);
        var url = $"https://{host}:{port}{path}?product=0&salt={salt}";
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            await _tokens.AccessTokenAsync(_credentials, cancellationToken).ConfigureAwait(false));
        request.Headers.TryAddWithoutValidation("client-token",
            await _tokens.ClientTokenAsync(cancellationToken).ConfigureAwait(false));
        request.Headers.UserAgent.ParseAdd("Spotify/124200290 Win32/0 (librespot)");
        if (body is not null) request.Content = new ByteArrayContent(body);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    private static byte[] FindBytes(ReadOnlySpan<byte> data, int field)
    {
        var reader = new ProtoReader(data);
        while (reader.TryRead(out var actual, out _, out var bytes))
            if (actual == field) return bytes.ToArray();
        throw new InvalidDataException($"Missing protobuf field {field}; fields: {DescribeFields(data)}");
    }

    private static string DescribeFields(ReadOnlySpan<byte> data)
    {
        var reader = new ProtoReader(data);
        var fields = new List<string>();
        while (reader.TryRead(out var field, out var number, out var bytes))
            fields.Add($"{field}:{(bytes.IsEmpty ? number : bytes.Length)}");
        return string.Join(",", fields);
    }
}
