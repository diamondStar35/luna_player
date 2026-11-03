using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using LunaPlayer.Spotify.Playback;
using LunaPlayer.Spotify.Playback.AccessPoint;

namespace LunaPlayer.Tests.Spotify;

[TestClass, DoNotParallelize, TestCategory("Unit")]
public sealed class ConnectionTests
{
    [TestMethod]
    public async Task SharedMetadata()
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

    [TestMethod]
    public async Task TemporaryRefusal()
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

    private static object? Field(Requests value, string name) => typeof(Requests).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value);
    private static void Set(HttpTokens value, string name, object data) => typeof(HttpTokens).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(value, data);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class AudioService : HttpMessageHandler
    {
        internal int MetadataReads;
        internal int RangeReads;
        internal TaskCompletionSource? HoldMetadata { get; init; }
        internal TaskCompletionSource MetadataStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly byte[] _audio = Encrypt();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            byte[] response;
            if (request.RequestUri!.Host == "apresolve.spotify.com")
                response = Encoding.UTF8.GetBytes("{\"spclient\":[\"metadata.test:443\"]}");
            else if (request.RequestUri.Host == "metadata.test" && request.RequestUri.AbsolutePath.StartsWith("/extended-metadata", StringComparison.Ordinal))
            {
                MetadataReads++;
                var track = Track(42, File(1, 2));
                response = Bytes(2, Bytes(3, Bytes(3, Bytes(2, track))));
                MetadataStarted.TrySetResult();
                if (HoldMetadata is not null) return CompleteMetadataAsync(response, HoldMetadata.Task, token);
            }
            else if (request.RequestUri.Host == "metadata.test")
                response = Bytes(2, "https://audio.test/track"u8.ToArray());
            else
            {
                RangeReads++;
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
