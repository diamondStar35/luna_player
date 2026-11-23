using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Tests.Subsonic.Media;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class MediaTests
{
    [TestMethod]
    public async Task OriginalDefaultAndTranscodedStreamsKeepTheirDistinctSemantics()
    {
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var original = new Sent(await client.Media.GetStreamAddressAsync("song/1 &+", new() { MaximumBitRate = 128 }), HttpMethod.Get, "", []);
        Assert.AreEqual("raw", original.Parameters["format"][0]);
        Assert.AreEqual("song/1 &+", original.Parameters["id"][0]);
        Assert.IsFalse(original.Parameters.ContainsKey("maxBitRate"));
        var standard = new Sent(await client.Media.GetStreamAddressAsync("s", new() { Mode = StreamingMode.ServerDefault }), HttpMethod.Get, "", []);
        Assert.IsFalse(standard.Parameters.ContainsKey("format"));
        var transcode = new Sent(await client.Media.GetStreamAddressAsync("s", new()
        { Mode = StreamingMode.Transcode, Format = "opus", MaximumBitRate = 128, TimeOffset = 12.5, EstimateContentLength = true }), HttpMethod.Get, "", []);
        Assert.AreEqual("opus", transcode.Parameters["format"][0]);
        Assert.AreEqual("128", transcode.Parameters["maxBitRate"][0]);
        Assert.AreEqual("12", transcode.Parameters["timeOffset"][0]);
        Assert.AreEqual("true", transcode.Parameters["estimateContentLength"][0]);
        Assert.AreEqual(2, handler.Requests.Count);
    }

    [TestMethod]
    public async Task DecisionUsesJsonCapabilitiesAndStreamPreservesOpaqueDecisionToken()
    {
        using var handler = new Handler((sent, token) => sent.Endpoint == "getTranscodeStream"
            ? Task.FromResult(Fixture.Raw("audio", "audio/ogg")) : Fixture.Reply(sent, token));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var decision = await client.Media.GetTranscodeDecisionAsync("s", new ClientInfo
        {
            Name = "LunaPlayer", Platform = "Windows", MaxAudioBitrate = 512000, MaxTranscodingAudioBitrate = 256000,
            DirectPlayProfiles = [new() { Containers = ["flac"], AudioCodecs = ["flac"] }],
            CodecProfiles = [new() { Name = "flac", Limitations = [new() { Name = "audioChannels", Comparison = "LessThanEqual", Values = ["2"] }] }]
        });
        Assert.AreEqual(96000, decision.SourceStream!.AudioSampleRate);
        var sent = handler.Requests.Single(x => x.Endpoint == "getTranscodeDecision");
        Assert.AreEqual(HttpMethod.Post, sent.Method);
        using var body = JsonDocument.Parse(sent.Body);
        Assert.AreEqual(256000, body.RootElement.GetProperty("maxTranscodingAudioBitrate").GetInt32());
        Assert.AreEqual("flac", body.RootElement.GetProperty("directPlayProfiles")[0].GetProperty("containers")[0].GetString());
        await using var stream = await client.Media.OpenTranscodeAsync("s", decision.TranscodeParams, 2);
        var query = handler.Requests.Last().Parameters;
        Assert.AreEqual("opaque+/=&%token", query["transcodeParams"][0]);
        Assert.AreEqual("2", query["offset"][0]);
        Assert.IsFalse(handler.Requests.Last().Uri.Query.Contains("%252B", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ByteResumeRequiresMatchingPartialContent()
    {
        using var handler = new Handler((_, _) =>
        {
            var reply = Fixture.Raw("bytes", "audio/flac", HttpStatusCode.PartialContent);
            reply.Content.Headers.ContentRange = new ContentRangeHeaderValue(100, 104, 200);
            reply.Headers.AcceptRanges.Add("bytes");
            reply.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "../song.flac" };
            return Task.FromResult(reply);
        });
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await using var response = await client.Media.OpenStreamAsync("s", byteOffset: 100);
        Assert.AreEqual("bytes=100-", handler.Requests[0].Headers["Range"][0]);
        Assert.AreEqual(100L, response.RangeStart);
        Assert.IsTrue(response.SupportsRanges);
        Assert.AreEqual("song.flac", response.SuggestedFileName);
        using var reader = new StreamReader(response.Stream);
        Assert.AreEqual("bytes", await reader.ReadToEndAsync());
    }

    [TestMethod]
    public async Task IgnoredByteRangeIsRejectedRatherThanPlayingFromWrongPosition()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw("all", "audio/flac")));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var error = await Assert.ThrowsExactlyAsync<RequestException>(() => client.Media.OpenStreamAsync("s", byteOffset: 100));
        Assert.AreEqual(FailureKind.InvalidResponse, error.Kind);
    }

    [TestMethod]
    [DataRow("application/json", "{\"subsonic-response\":{\"status\":\"failed\",\"error\":{\"code\":50,\"message\":\"Denied\"}}}")]
    [DataRow("text/xml", "<subsonic-response status=\"failed\"><error code=\"50\" message=\"Denied\"/></subsonic-response>")]
    public async Task BinaryEndpointsReportProtocolErrorsInsteadOfSavingThem(string type, string text)
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw(text, type)));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var error = await Assert.ThrowsExactlyAsync<RequestException>(() => client.Media.OpenDownloadAsync("s"));
        Assert.AreEqual(FailureKind.Forbidden, error.Kind);
        Assert.AreEqual(50, error.Code);
    }

    [TestMethod]
    public async Task OpeningMediaDoesNotReadItsBodyAndDisposingClientCancelsActiveRead()
    {
        using var body = new WaitingStream();
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StreamContent(body) }));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await using var response = await client.Media.OpenStreamAsync("s");
        Assert.AreEqual(0, body.Reads);
        var read = response.Stream.ReadAsync(new byte[10]).AsTask();
        await body.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        client.Dispose();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await read.WaitAsync(TimeSpan.FromSeconds(5)));
        response.Dispose();
        response.Dispose();
        Assert.IsTrue(body.Disposed);
    }

    [TestMethod]
    public async Task CoverArtAndAvatarUseBinaryRequestsWithExpectedParameters()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw("image", "image/png")));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await using var art = await client.Media.OpenCoverArtAsync("cover", 256);
        await using var avatar = await client.Media.OpenAvatarAsync("user");
        Assert.AreEqual("256", handler.Requests[0].Parameters["size"][0]);
        Assert.AreEqual("user", handler.Requests[1].Parameters["username"][0]);
        Assert.AreEqual("image/png", art.ContentType);
    }

    [TestMethod]
    [DataRow("application/xml")]
    [DataRow("application/json")]
    public async Task StalledErrorBodiesRespectTheRequestDeadline(string contentType)
    {
        using var body = new WaitingStream();
        using var handler = new Handler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) };
            response.Content.Headers.ContentType = new(contentType);
            return Task.FromResult(response);
        });
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options with { RequestTimeout = TimeSpan.FromMilliseconds(100) }, http);
        var error = await Assert.ThrowsExactlyAsync<RequestException>(async () =>
            await client.Media.OpenDownloadAsync("s").WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.AreEqual(FailureKind.Timeout, error.Kind);
        Assert.IsTrue(body.Disposed);
    }

    private sealed class WaitingStream : Stream
    {
        internal int Reads;
        internal bool Disposed;
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Reads); Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
}
