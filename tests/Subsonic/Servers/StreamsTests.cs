using System.Net;
using System.Text.Json;
using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Subsonic.Servers;

namespace LunaPlayer.Tests.Subsonic.Servers;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class StreamsTests
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public async Task NegotiatedTranscodingUsesTheOutputFormatAndWholeSecondOrigins(int setting)
    {
        var mode = (StreamingMode)setting;
        using var rig = new Rig(mode);
        var input = await rig.Resolve(23.75);
        var request = new Sent(new(input.Address), HttpMethod.Get, "", []);
        Assert.AreEqual("getTranscodeStream", request.Endpoint);
        Assert.AreEqual("23", request.Parameters["offset"][0]);
        Assert.AreEqual("opaque+/=&%token", request.Parameters["transcodeParams"][0]);
        Assert.AreEqual(23d, input.Offset); Assert.AreEqual(23.75, input.RequestedPosition);
        Assert.IsTrue(input.SeekByReopening);
        Assert.AreEqual("opus", input.Format!.Codec); Assert.AreEqual("ogg", input.Format.Container);
        Assert.AreEqual(128000, input.Format.BitRate); Assert.IsNull(input.Format.SampleRate);
        Assert.AreEqual(true, input.Format.Transcoded);
        var decision = rig.Handler.Requests.Single(sent => sent.Endpoint == "getTranscodeDecision");
        Assert.AreEqual(HttpMethod.Post, decision.Method);
        using var body = JsonDocument.Parse(decision.Body);
        Assert.AreEqual("Windows", body.RootElement.GetProperty("platform").GetString());
        if (mode == StreamingMode.Transcode)
        {
            Assert.AreEqual(0, body.RootElement.GetProperty("directPlayProfiles").GetArrayLength());
            Assert.AreEqual(128000, body.RootElement.GetProperty("maxTranscodingAudioBitrate").GetInt32());
            Assert.AreEqual("opus", body.RootElement.GetProperty("transcodingProfiles")[0].GetProperty("audioCodec").GetString());
        }
        var later = await rig.Resolve(4.25);
        Assert.AreEqual(4d, later.Offset); Assert.AreEqual(4.25, later.RequestedPosition);
        Assert.AreEqual(1, rig.Handler.Requests.Count(sent => sent.Endpoint == "getTranscodeDecision"));
    }

    [TestMethod]
    public async Task ServerDefaultDirectPlayDoesNotApplyATranscodedOffset()
    {
        using var rig = new Rig(StreamingMode.ServerDefault, (sent, token) => sent.Endpoint == "getTranscodeDecision"
            ? Task.FromResult(Fixture.Json("\"transcodeDecision\":{\"canDirectPlay\":true,\"canTranscode\":true,\"sourceStream\":{\"protocol\":\"http\",\"container\":\"flac\",\"codec\":\"flac\",\"audioSamplerate\":96000}}"))
            : Fixture.Reply(sent, token));
        var input = await rig.Resolve(9.5);
        var sent = new Sent(new(input.Address), HttpMethod.Get, "", []);
        Assert.AreEqual("stream", sent.Endpoint); Assert.AreEqual("raw", sent.Parameters["format"][0]);
        Assert.IsFalse(sent.Parameters.ContainsKey("timeOffset"));
        Assert.AreEqual(0d, input.Offset); Assert.IsFalse(input.SeekByReopening);
        Assert.AreEqual("flac", input.Format!.Codec); Assert.AreEqual(96000, input.Format.SampleRate);
        Assert.AreEqual(false, input.Format.Transcoded);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task LegacyUnknownStreamsKeepTheirFullFileTimeline(int setting)
    {
        var mode = (StreamingMode)setting;
        using var rig = new Rig(mode, (sent, token) => sent.Endpoint == "getOpenSubsonicExtensions"
            ? Task.FromResult(Fixture.Json("\"openSubsonicExtensions\":[{\"name\":\"transcodeOffset\",\"versions\":[1]}]"))
            : Fixture.Reply(sent, token));
        var input = await rig.Resolve(9);
        var sent = new Sent(new(input.Address), HttpMethod.Get, "", []);
        Assert.AreEqual(0d, input.Offset); Assert.IsFalse(input.SeekByReopening);
        Assert.IsFalse(sent.Parameters.ContainsKey("timeOffset"));
        Assert.AreEqual(mode == StreamingMode.Original, sent.Parameters.ContainsKey("format"));
        Assert.AreEqual(0, rig.Handler.Requests.Count(sent => sent.Endpoint == "getTranscodeDecision"));
        if (mode == StreamingMode.ServerDefault) Assert.IsNull(input.Format, "The requested settings cannot establish a legacy stream's actual format.");
        else Assert.IsEmpty(rig.Handler.Requests);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.NotImplemented)]
    public async Task AdvertisedButMissingDecisionFallsBackWithoutRetryingEverySong(HttpStatusCode status)
    {
        using var rig = new Rig(StreamingMode.Transcode, (sent, token) => sent.Endpoint == "getTranscodeDecision"
            ? Task.FromResult(Fixture.Raw("missing", "text/plain", status)) : Fixture.Reply(sent, token));
        var first = await rig.Resolve(9);
        var second = await rig.Connections.ResolveAsync(new Reference(rig.Profile.Id, "second").Address, 0, CancellationToken.None);
        Assert.IsTrue(first.Address.Contains("format=opus", StringComparison.Ordinal));
        Assert.IsTrue(first.SeekByReopening); Assert.IsNull(first.Format);
        Assert.IsTrue(second.Address.Contains("format=opus", StringComparison.Ordinal));
        Assert.AreEqual(1, rig.Handler.Requests.Count(sent => sent.Endpoint == "getTranscodeDecision"));
    }

    [TestMethod]
    public async Task ADeclinedFormatUsesLegacyCompatibilityWithoutClaimingItsOutputIsTranscoded()
    {
        using var rig = new Rig(StreamingMode.Transcode, (sent, token) => sent.Endpoint == "getTranscodeDecision"
            ? Task.FromResult(Fixture.Json("\"transcodeDecision\":{\"canDirectPlay\":false,\"canTranscode\":false}")) : Fixture.Reply(sent, token));
        var input = await rig.Resolve(9);
        Assert.IsTrue(input.Address.Contains("format=opus", StringComparison.Ordinal));
        Assert.IsNull(input.Format); Assert.IsFalse(input.SeekByReopening); Assert.AreEqual(0d, input.Offset);
    }

    [TestMethod]
    [DataRow("\"transcodeDecision\":{\"canTranscode\":true}", "InvalidResponse")]
    [DataRow("\"transcodeDecision\":{\"canDirectPlay\":false,\"canTranscode\":true,\"transcodeParams\":\"token\"}", "InvalidResponse")]
    [DataRow("\"status\":\"failed\",\"error\":{\"code\":40}", "Rejected")]
    public async Task InvalidDecisionsAndAuthenticationFailuresAreNotHiddenByFallback(string payload, string kind)
    {
        using var rig = new Rig(StreamingMode.ServerDefault, (sent, token) => sent.Endpoint == "getTranscodeDecision"
            ? Task.FromResult(Fixture.Json(payload)) : Fixture.Reply(sent, token));
        var error = await Assert.ThrowsExactlyAsync<RequestException>(() => rig.Resolve(0));
        Assert.AreEqual(kind, error.Kind.ToString());
    }

    [TestMethod]
    public async Task PreviousAndNextReuseNegotiationUntilRefreshOrProfileChanges()
    {
        using var rig = new Rig(StreamingMode.ServerDefault);
        await rig.Resolve(0);
        var second = new Reference(rig.Profile.Id, "second").Address;
        rig.Connections.Metadata.Observe(rig.Profile.Id, new Entry { Id = "second", Title = "Second" });
        await rig.Connections.ResolveAsync(second, 0, CancellationToken.None);
        await rig.Resolve(0); await rig.Connections.ResolveAsync(second, 0, CancellationToken.None);
        Assert.AreEqual(2, rig.Handler.Requests.Count(sent => sent.Endpoint == "getTranscodeDecision"));
        Assert.IsFalse(rig.Handler.Requests.Any(sent => sent.Endpoint == "getSong"));
        await rig.Connections.InspectAsync(rig.Profile.Id, refresh: true); await rig.Resolve(0);
        Assert.AreEqual(3, rig.Handler.Requests.Count(sent => sent.Endpoint == "getTranscodeDecision"));
        await rig.Connections.SaveAsync(rig.Profile with { Streaming = new() { Mode = StreamingMode.Original } });
        var original = await rig.Resolve(0);
        Assert.IsTrue(original.Address.Contains("format=raw", StringComparison.Ordinal));
        Assert.AreEqual(3, rig.Handler.Requests.Count(sent => sent.Endpoint == "getTranscodeDecision"));
    }

    [TestMethod]
    public async Task ExpiredDecisionsAreRenewedWithoutDiscardingBrowsedMetadata()
    {
        using var rig = new Rig(StreamingMode.ServerDefault);
        await rig.Resolve(0); rig.Clock.Now += TimeSpan.FromMinutes(4); await rig.Resolve(4);
        Assert.AreEqual(1, rig.Handler.Requests.Count(sent => sent.Endpoint == "getTranscodeDecision"));
        rig.Clock.Now += TimeSpan.FromMinutes(1); await rig.Resolve(5);
        Assert.AreEqual(2, rig.Handler.Requests.Count(sent => sent.Endpoint == "getTranscodeDecision"));
        Assert.IsFalse(rig.Handler.Requests.Any(sent => sent.Endpoint == "getSong"));
    }

    [TestMethod]
    public async Task CancelledNavigationDoesNotHoldUpTheNextNegotiation()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var decisions = 0;
        using var rig = new Rig(StreamingMode.ServerDefault, async (sent, token) =>
        {
            if (sent.Endpoint == "getTranscodeDecision" && Interlocked.Increment(ref decisions) == 1)
            { started.SetResult(); await Task.Delay(Timeout.Infinite, token); }
            return await Fixture.Reply(sent, token);
        });
        using var cancellation = new CancellationTokenSource();
        var opening = rig.Connections.ResolveAsync(new Reference(rig.Profile.Id, "song").Address, 0, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)); cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await opening);
        var next = await rig.Resolve(2);
        Assert.IsTrue(next.Address.Contains("getTranscodeStream", StringComparison.Ordinal));
        Assert.AreEqual(2, decisions);
    }

    private sealed class Clock : TimeProvider
    {
        internal DateTimeOffset Now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Rig : IDisposable
    {
        private readonly TemporaryDirectory _directory = new();
        internal readonly Handler Handler;
        private readonly HttpClient _http;
        internal readonly Connections Connections;
        internal readonly Profile Profile;
        internal readonly Clock Clock = new();
        internal Rig(StreamingMode mode, Func<Sent, CancellationToken, Task<HttpResponseMessage>>? reply = null)
        {
            Handler = new(reply ?? Fixture.Reply); _http = new(Handler);
            Connections = new(new Store(Path.Combine(_directory.Path, "servers.json")), profile => new Client(profile.ConnectionOptions() with { TimeProvider = Clock }, _http));
            Profile = Connections.SaveAsync(new Profile { Name = "Music", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials,
                Streaming = new() { Mode = mode, Format = "opus", MaximumBitRate = mode == StreamingMode.Transcode ? 128 : null } }).GetAwaiter().GetResult();
            Connections.Metadata.Observe(Profile.Id, new Entry { Id = "song", Title = "Song", Suffix = "flac", Duration = 60 });
        }
        internal Task<LunaPlayer.Playback.MediaInput> Resolve(double position)
            => Connections.ResolveAsync(new Reference(Profile.Id, "song").Address, position, CancellationToken.None);
        public void Dispose() { Connections.Dispose(); _http.Dispose(); Handler.Dispose(); _directory.Dispose(); }
    }
}
