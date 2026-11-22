using System.Net;
using System.Text;
using LunaPlayer.Downloads;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Downloads;

[TestClass, TestCategory("Unit")]
public sealed class ProviderTests
{
    [TestMethod]
    public void RoutingUsesTheOriginalProviderAndRejectsLocalFilesRadioAndManifests()
    {
        var server = Guid.NewGuid();
        Assert.AreEqual(SourceKind.YouTube, Source.Resolve("https://cdn.invalid/audio", "https://www.youtube.com/watch?v=abc123"));
        Assert.AreEqual(SourceKind.MusicServer, Source.Resolve(new Reference(server, "song").Address, null));
        Assert.AreEqual(SourceKind.MusicServer, Source.Resolve(new Reference(server, "episode", MediaKind.Podcast).Address, null));
        Assert.AreEqual(SourceKind.None, Source.Resolve(new Reference(server, "station", MediaKind.Radio).Address, null));
        Assert.AreEqual(SourceKind.None, Source.Resolve(@"C:\Music\song.mp3", null));
        Assert.AreEqual(SourceKind.HttpFile, Source.Resolve("https://example.invalid/music.flac?token=value", null));
        Assert.AreEqual(SourceKind.None, Source.Resolve("https://example.invalid/radio", null, live: true));
        Assert.AreEqual(SourceKind.None, Source.Resolve("https://example.invalid/live.m3u8", null));
        Assert.AreEqual(SourceKind.None, Source.Resolve(null, null));
    }

    [TestMethod]
    public void HttpNamesPreserveExtensionsAndAvoidReservedWindowsNames()
    {
        var uri = new Uri("https://example.invalid/music.flac?secret=hidden");
        Assert.AreEqual("Title.flac", Source.FileName(uri, "Title"));
        Assert.AreEqual("music.flac", Source.FileName(uri, ""));
        Assert.AreEqual("_CON.flac", Source.FileName(uri, "CON"));
        StringAssert.EndsWith(Source.FileName(uri, new string('a', 300)), ".flac");
        Assert.IsFalse(Source.FileName(uri, "title").Contains("hidden", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task HttpDownloadsPreserveBytesAndDoNotOverwriteExistingFiles()
    {
        using var workspace = new Workspace(); var path = Path.Combine(workspace.Root, "music.flac");
        await File.WriteAllTextAsync(path, "original");
        using var client = new HttpClient(new Handler());
        var job = HttpJob.Create(new Uri("https://example.invalid/music.flac"), path, client);
        var result = await Task.Run(() => job.Run(_ => { }, default));
        Assert.IsTrue(result.Success); Assert.AreEqual("original", await File.ReadAllTextAsync(path));
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("download bytes"), await File.ReadAllBytesAsync(result.FilePath));
        Assert.AreNotEqual(path, result.FilePath);
    }

    [TestMethod]
    public async Task AFailedHttpCopyRemovesItsIncompleteFile()
    {
        using var workspace = new Workspace(); using var client = new HttpClient(new Handler());
        var path = Path.Combine(workspace.Root, "partial.flac");
        var job = HttpJob.Create(new Uri("https://example.invalid/music.flac"), path, client);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => job.Run(_ => throw new InvalidOperationException(), default)));
        Assert.IsFalse(File.Exists(path));
    }

    private sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes("download bytes")) });
    }
}
