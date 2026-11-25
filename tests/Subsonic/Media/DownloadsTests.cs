using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Media;

namespace LunaPlayer.Tests.Subsonic.Media;

[TestClass, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class DownloadsTests
{
    [TestMethod]
    public async Task OriginalDownloadReportsBytesWithoutClosingDestination()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw("abcdef", "audio/flac")));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        using var output = new MemoryStream();
        var updates = new List<DownloadProgress>();
        Assert.AreEqual(6L, await client.Downloads.CopyAsync("s", output, new InlineProgress<DownloadProgress>(updates.Add)));
        Assert.IsTrue(output.CanWrite);
        Assert.AreEqual("raw", handler.Requests[0].Parameters["format"][0]);
        Assert.AreEqual(0L, updates[0].BytesReceived);
        Assert.AreEqual(6L, updates[^1].BytesReceived);
        Assert.AreEqual(6L, updates[^1].TotalBytes);
    }

    [TestMethod]
    public async Task TruncatedDownloadLeavesExistingDestinationAndNoPartialFile()
    {
        using var directory = new TemporaryDirectory();
        var destination = Path.Combine(directory.Path, "song.flac");
        await File.WriteAllTextAsync(destination, "keep");
        using var handler = new Handler((_, _) =>
        {
            var reply = Fixture.Raw("short", "audio/flac"); reply.Content.Headers.ContentLength = 100;
            return Task.FromResult(reply);
        });
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await Assert.ThrowsExactlyAsync<IOException>(() => client.Downloads.SaveAsync("s", destination, true));
        Assert.AreEqual("keep", await File.ReadAllTextAsync(destination));
        Assert.AreEqual(1, Directory.GetFiles(directory.Path).Length);
    }

    [TestMethod]
    public async Task BatchRetainsSuccessfulFilesAndReportsIndividualFailuresInOrder()
    {
        using var directory = new TemporaryDirectory();
        using var handler = new Handler((sent, _) => Task.FromResult(sent.Parameters["id"][0] == "bad"
            ? Fixture.Raw("{\"subsonic-response\":{\"status\":\"failed\",\"error\":{\"code\":70}}}")
            : Fixture.Raw("audio", "audio/flac")));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        var results = await client.Downloads.SaveBatchAsync([
            new("ok", Path.Combine(directory.Path, "ok.flac")), new("bad", Path.Combine(directory.Path, "bad.flac"))]);
        Assert.IsNull(results[0].Error);
        Assert.AreEqual(5L, results[0].Bytes);
        Assert.IsInstanceOfType<RequestException>(results[1].Error);
        Assert.IsTrue(File.Exists(results[0].Item.Destination));
        Assert.IsFalse(File.Exists(results[1].Item.Destination));
        Assert.AreEqual(1, Directory.GetFiles(directory.Path).Length);
    }

    [TestMethod]
    public async Task CancelledDownloadRemovesPartialFile()
    {
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        using var handler = new Handler((_, _) => Task.FromResult(Fixture.Raw(new string('x', 100000), "audio/flac")));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.Downloads.SaveAsync("s", Path.Combine(directory.Path, "s.flac"),
            progress: new InlineProgress<DownloadProgress>(p => { if (p.BytesReceived > 0) cancellation.Cancel(); }), token: cancellation.Token));
        Assert.AreEqual(0, Directory.GetFiles(directory.Path).Length);
    }

    [TestMethod]
    public async Task ExistingOrDuplicateDestinationsAreRejectedBeforeNetworkAccess()
    {
        using var directory = new TemporaryDirectory();
        var target = Path.Combine(directory.Path, "song.flac");
        await File.WriteAllTextAsync(target, "keep");
        using var handler = new Handler(Fixture.Reply);
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        await Assert.ThrowsExactlyAsync<IOException>(() => client.Downloads.SaveAsync("s", target));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => client.Downloads.SaveBatchAsync([new("a", target), new("b", target.ToUpperInvariant())]));
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public async Task DownloadWithoutContentLengthReportsAnUnknownTotal()
    {
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        { Content = new StreamContent(new NonSeekableStream("audio"u8.ToArray())) }));
        using var http = new HttpClient(handler);
        using var client = new Client(Fixture.Options, http);
        using var output = new MemoryStream();
        var updates = new List<DownloadProgress>();
        Assert.AreEqual(5L, await client.Downloads.CopyAsync("s", output, new InlineProgress<DownloadProgress>(updates.Add)));
        Assert.IsTrue(updates.All(x => x.TotalBytes is null));
        Assert.AreEqual(5L, updates[^1].BytesReceived);
    }

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    { public override bool CanSeek => false; }
}
