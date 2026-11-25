using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LunaPlayer.Subsonic.AudioMuse;

internal sealed partial class Client
{
    internal async IAsyncEnumerable<ProgressEvent> GeneratePlaylistProgressAsync(string prompt,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        await using var events = ReadProgressAsync(prompt, token).GetAsyncEnumerator(token);
        while (true)
        {
            bool next;
            try { next = await events.MoveNextAsync().ConfigureAwait(false); }
            catch (HttpRequestException) { throw new RequestException(FailureKind.Transport, "chat/api/chatPlaylistStream"); }
            catch (IOException) { throw new RequestException(FailureKind.Transport, "chat/api/chatPlaylistStream"); }
            catch (OperationCanceledException) when (!token.IsCancellationRequested && !_disposedToken.IsCancellationRequested)
            { throw new RequestException(FailureKind.Timeout, "chat/api/chatPlaylistStream"); }
            if (!next) yield break;
            yield return events.Current;
        }
    }
    private async IAsyncEnumerable<ProgressEvent> ReadProgressAsync(string prompt,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        const string endpoint = "chat/api/chatPlaylistStream";
        using var timeout = Deadline(token, true);
        using var response = await SendAsync(endpoint, PlaylistBody(prompt), timeout.Token).ConfigureAwait(false);
        if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase))
            throw new RequestException(FailureKind.InvalidResponse, endpoint);
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var data = new StringBuilder();
        var completed = false;
        while (true)
        {
            var line = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
            if (line is null)
            {
                if (data.Length > 0) throw new RequestException(FailureKind.InvalidResponse, endpoint);
                break;
            }
            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (data.Length > 0) data.Append('\n');
                data.Append(line.AsSpan(5).TrimStart(' '));
                if (data.Length > 1024 * 1024) throw new RequestException(FailureKind.InvalidResponse, endpoint);
            }
            if (line.Length != 0 || data.Length == 0) continue;
            Reply? reply;
            try { reply = JsonSerializer.Deserialize(data.ToString(), AudioMuseJson.Default.Reply); }
            catch (JsonException) { throw new RequestException(FailureKind.InvalidResponse, endpoint); }
            data.Clear();
            if (reply is null) throw new RequestException(FailureKind.InvalidResponse, endpoint);
            if (reply.Type == "error" || reply.Error is not null || reply.ErrorMessage is not null)
                throw new RequestException(FailureKind.Rejected, endpoint, serverMessage: reply.ErrorMessage ?? reply.Error ?? reply.Message);
            if (reply.Type == "done")
            {
                completed = true;
                yield return new ProgressEvent("done", reply.Message ?? "", Decode(reply, endpoint));
                break;
            }
            yield return new ProgressEvent(reply.Type ?? "log", reply.Message ?? "");
        }
        if (!completed) throw new RequestException(FailureKind.InvalidResponse, endpoint);
    }
}
