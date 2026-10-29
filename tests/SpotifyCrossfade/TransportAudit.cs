using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using LunaPlayer.Spotify.Playback;
using LunaPlayer.Spotify.Playback.AccessPoint;

namespace LunaPlayer.Tests.SpotifyCrossfade;

internal static class TransportAudit
{
    internal static async Task SharedMetadata()
    {
        using var handler = new AudioService { HoldMetadata = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var http = new HttpClient(handler);
        var resolver = new Resolver(http);
        var credentials = new Login.Credentials("fixture", 1, [1]);
        var tokens = new HttpTokens(http, "fixture", "fixture");
        Set(tokens, "_clientToken", "fixture"); Set(tokens, "_clientExpires", DateTimeOffset.MaxValue);
        Set(tokens, "_accessToken", "fixture"); Set(tokens, "_accessExpires", DateTimeOffset.MaxValue);
        var files = new Files(http, resolver, tokens, credentials);
        using var pipe = new KeyPipe();
        using var requests = new Requests(new Codec(pipe, new byte[32], new byte[32]));
        using var connection = (Connection)typeof(Connection).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single().Invoke([requests, files, http, resolver, credentials, "fixture"]);
        using var navigation = new CancellationTokenSource();
        const string uri = "spotify:track:0000000000000000000001";
        var abandoned = connection.GetAudioAsync(uri, 320, navigation.Token, speculative: true);
        await handler.MetadataStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var selected = connection.GetAudioAsync(uri, 320, default);
        navigation.Cancel();
        try { await abandoned; throw new InvalidOperationException("Abandoned metadata waiter stayed active."); }
        catch (OperationCanceledException) { }
        Check(handler.MetadataReads == 1, "Selecting a prefetched destination duplicated metadata.");
        handler.HoldMetadata.SetResult();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        TaskCompletionSource<byte[]>? pending;
        while ((pending = Field(requests, "_pending") as TaskCompletionSource<byte[]>) is null) await Task.Delay(5, timeout.Token);
        pending.SetResult(new byte[16]);
        using var audio = await selected.WaitAsync(timeout.Token);
        using (var second = await connection.GetAudioAsync(uri, 320, timeout.Token))
            Check(!ReferenceEquals(audio, second), "Metadata sharing also shared decoder ownership.");
        Check(audio.CanRead && handler.MetadataReads == 1 && pipe.Writes == 1,
            "Metadata/key reuse restarted work or disposed another stream.");
    }

    internal static async Task TemporaryRefusal()
    {
        using var handler = new AudioService(); using var http = new HttpClient(handler);
        var resolver = new Resolver(http);
        var credentials = new Login.Credentials("fixture", 1, [1]);
        var tokens = new HttpTokens(http, "fixture", "fixture");
        Set(tokens, "_clientToken", "fixture"); Set(tokens, "_clientExpires", DateTimeOffset.MaxValue);
        Set(tokens, "_accessToken", "fixture"); Set(tokens, "_accessExpires", DateTimeOffset.MaxValue);
        using var pipe = new KeyPipe();
        using var requests = new Requests(new Codec(pipe, new byte[32], new byte[32]));
        using var connection = (Connection)typeof(Connection).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single().Invoke([requests, new Files(http, resolver, tokens, credentials), http, resolver, credentials, "fixture"]);
        var loading = connection.GetAudioAsync("spotify:track:0000000000000000000001", 320, default);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        for (var number = 1; number <= 2; number++)
        {
            TaskCompletionSource<byte[]>? pending = null;
            while (pipe.Writes < number || (pending = Field(requests, "_pending") as TaskCompletionSource<byte[]>) is null || pending.Task.IsCompleted)
                await Task.Delay(5, timeout.Token);
            pending.SetException(new AudioKeyUnavailableException(2));
        }
        try { using var unexpected = await loading.WaitAsync(timeout.Token); throw new InvalidOperationException("Refusal was hidden."); }
        catch (AudioKeyUnavailableException error) { Check(error.Code == 2, "Temporary refusal became permanent unavailability."); }
        Check(pipe.Writes == 2 && handler.RangeReads == 0, "Code 2 triggered fallback quality requests or downloaded audio.");
    }

    internal static async Task Alternatives()
    {
        using var handler = new AudioService { FailFirstMetadata = true };
        using var http = new HttpClient(handler);
        var resolver = new Resolver(http);
        var credentials = new Login.Credentials("fixture", 1, [1]);
        var tokens = new HttpTokens(http, "fixture", "fixture");
        Set(tokens, "_clientToken", "fixture"); Set(tokens, "_clientExpires", DateTimeOffset.MaxValue);
        Set(tokens, "_accessToken", "fixture"); Set(tokens, "_accessExpires", DateTimeOffset.MaxValue);
        var files = new Files(http, resolver, tokens, credentials);
        var candidates = await files.GetTrackFilesAsync("spotify:track:0000000000000000000001", 320, default);
        Check(handler.MetadataReads == 2, "A transient metadata response was not retried once.");
        Check(candidates.Length == 3 && candidates[0].KeyTrack == new TrackId(42)
            && candidates[1].KeyTrack == new TrackId(42) && candidates[2].KeyTrack == new TrackId(43),
            "Canonical and alternative track IDs were not preserved.");
        Check(candidates.Select(file => file.Format).SequenceEqual([2, 1, 0]), "Quality fallback order changed.");

        using var pipe = new KeyPipe();
        using var requests = new Requests(new Codec(pipe, new byte[32], new byte[32]));
        using var connection = (Connection)typeof(Connection).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single().Invoke([requests, files, http, resolver, credentials, "fixture"]);
        var loading = connection.GetAudioAsync("spotify:track:0000000000000000000001", 320, default);
        for (var number = 1; number <= 3; number++)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            TaskCompletionSource<byte[]>? pending = null;
            while (pipe.Writes < number || (pending = Field(requests, "_pending") as TaskCompletionSource<byte[]>) is null || pending.Task.IsCompleted)
                await Task.Delay(5, timeout.Token);
            if (number == 1) pending.TrySetException(new AudioKeyUnavailableException(1));
            else pending.TrySetResult(new byte[16]);
        }
        using var audio = await loading.WaitAsync(TimeSpan.FromSeconds(5));
        var header = new byte[4]; audio.ReadExactly(header);
        Check(header.AsSpan().SequenceEqual("OggS"u8), "Fallback audio used the wrong body offset or key.");
        Check(pipe.Writes == 3, "Unavailable versions prevented reaching the playable alternative.");
    }

    internal static async Task RangeRetry()
    {
        using var handler = new AudioService { FailFirstRange = true };
        using var http = new HttpClient(handler);
        using var audio = await BufferedAudioStream.OpenAsync(http, ["https://audio.test/track"], default);
        Check(handler.RangeReads == 2 && audio.Length > 167, "A transient CDN range failure was not retried once.");
        using var stopped = new CancellationTokenSource(); stopped.Cancel();
        try { await BufferedAudioStream.OpenAsync(http, ["https://audio.test/track"], stopped.Token); throw new InvalidOperationException("Cancellation was ignored."); }
        catch (OperationCanceledException) { }
        Check(handler.RangeReads == 2, "Cancelled navigation retried a range request.");
    }

    internal static async Task TokenRefresh()
    {
        using var handler = new AudioService { RejectFirstToken = true };
        using var http = new HttpClient(handler);
        var tokens = new HttpTokens(http, "fixture", "fixture");
        Set(tokens, "_clientToken", "fixture"); Set(tokens, "_clientExpires", DateTimeOffset.MaxValue);
        Set(tokens, "_accessToken", "fixture"); Set(tokens, "_accessExpires", DateTimeOffset.MaxValue);
        var files = new Files(http, new Resolver(http), tokens, new Login.Credentials("fixture", 1, [1]));
        await files.GetTrackFilesAsync("spotify:track:0000000000000000000001", 320, default);
        Check(handler.MetadataReads == 2 && handler.TokenRefreshes == 1, "An early-expired access token was not refreshed once.");
    }

    private static object? Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value);
    private static void Set(object value, string name, object data) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(value, data);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class AudioService : HttpMessageHandler
    {
        internal bool FailFirstMetadata { get; init; }
        internal bool FailFirstRange { get; init; }
        internal bool RejectFirstToken { get; init; }
        internal int MetadataReads;
        internal int RangeReads;
        internal int TokenRefreshes;
        internal TaskCompletionSource? HoldMetadata { get; init; }
        internal TaskCompletionSource MetadataStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly byte[] _audio = Encrypt();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            byte[] response;
            if (request.RequestUri!.Host == "apresolve.spotify.com")
                response = Encoding.UTF8.GetBytes("{\"spclient\":[\"metadata.test:443\"]}");
            else if (request.RequestUri.Host == "login5.spotify.com")
            {
                TokenRefreshes++;
                response = Bytes(1, Bytes(2, "refreshed"u8.ToArray()).Concat(Number(4, 3600)).ToArray());
            }
            else if (request.RequestUri.Host == "metadata.test" && request.RequestUri.AbsolutePath.StartsWith("/extended-metadata", StringComparison.Ordinal))
            {
                MetadataReads++;
                if (MetadataReads == 1 && FailFirstMetadata) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                if (MetadataReads == 1 && RejectFirstToken) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                var alternative = Track(43, File(3, 0));
                var track = Track(42, File(1, 2), File(2, 1), Bytes(13, alternative));
                response = Bytes(2, Bytes(3, Bytes(3, Bytes(2, track))));
                MetadataStarted.TrySetResult();
                if (HoldMetadata is not null) return CompleteMetadataAsync(response, HoldMetadata.Task, token);
            }
            else if (request.RequestUri.Host == "metadata.test")
                response = request.RequestUri.AbsolutePath.EndsWith(string.Concat(Enumerable.Repeat("02", 20)), StringComparison.Ordinal)
                    ? Number(1, 1) : Bytes(2, "https://audio.test/track"u8.ToArray());
            else
            {
                if (++RangeReads == 1 && FailFirstRange) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                var range = request.Headers.Range!.Ranges.Single();
                var start = (int)range.From!.Value; var end = (int)Math.Min(range.To!.Value, _audio.Length - 1);
                var content = new ByteArrayContent(_audio[start..(end + 1)]);
                content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, _audio.Length);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = content });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(response) });
        }

        private static async Task<HttpResponseMessage> CompleteMetadataAsync(byte[] response, Task released, CancellationToken token)
        {
            await released.WaitAsync(token);
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(response) };
        }

        private static byte[] Track(int id, params byte[][] fields)
        {
            var raw = new byte[16]; new TrackId((UInt128)id).WriteRaw(raw);
            return Bytes(1, raw).Concat(fields.SelectMany(field => field)).ToArray();
        }
        private static byte[] File(byte id, ulong format) => Bytes(12, Bytes(1, Enumerable.Repeat(id, 20).ToArray()).Concat(Number(2, format)).ToArray());
        private static byte[] Bytes(int field, byte[] value) { using var stream = new MemoryStream(); ProtoWire.WriteBytes(stream, field, value); return stream.ToArray(); }
        private static byte[] Number(int field, ulong value) { using var stream = new MemoryStream(); ProtoWire.WriteNumber(stream, field, value); return stream.ToArray(); }
        private static byte[] Encrypt()
        {
            var clear = new byte[4096]; "OggS"u8.CopyTo(clear.AsSpan(167));
            using var cipher = new AudioDecryptStream(new MemoryStream(clear), new byte[16]);
            var encrypted = new byte[clear.Length]; cipher.ReadExactly(encrypted); return encrypted;
        }
    }

    private sealed class KeyPipe : Stream
    {
        internal int Writes;
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) { await Task.Delay(Timeout.Infinite, token); return 0; }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default) { token.ThrowIfCancellationRequested(); Interlocked.Increment(ref Writes); return ValueTask.CompletedTask; }
    }
}
