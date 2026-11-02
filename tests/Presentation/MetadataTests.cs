using LunaPlayer.Application.Commands.Files;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;
using LunaPlayer.YouTube;

namespace LunaPlayer.Tests.Presentation;

[TestClass, TestCategory("Unit")]
public sealed class MetadataTests
{
    [TestMethod]
    public void UntaggedLocalFileDoesNotUseArtistFromAnOutgoingMetadataSnapshot()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine
        {
            CurrentMetadata = new(new("Previous title", "Previous artist", "Previous album"), new Dictionary<string, string>()),
        };
        using var player = new Player(engine);
        var reader = new MetadataReader(player);
        player.OpenFile(NativeHost.Wave(workspace, "untagged.wav"));
        Assert.AreEqual(string.Empty, reader.Read().Display.Artist);
        Assert.AreEqual(string.Empty, reader.Read().Display.Title);
        Assert.AreEqual(string.Empty, reader.Read().Display.Album);
    }

    [TestMethod]
    public void YouTubeUsesTheCurrentVideosChannelWhenArtistMetadataIsMissing()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        const string url = "https://www.youtube.com/watch?v=abcdefghijk";
        player.OpenStream("https://cdn.example/audio", "A video", url);
        var video = new YouTubeResult("abcdefghijk", "A video", "Channel name", null, url, "");
        Assert.AreEqual("Channel name", new MetadataReader(player).Read(video).Display.Artist);
    }

    [TestMethod]
    public void ArtistTagIsIndependentOfAMissingTitle()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        player.OpenFile(TaggedWave.Write(workspace, "artist-only.wav", artist: "Current artist"));
        var tags = new MetadataReader(player).Read().Display;
        Assert.AreEqual(string.Empty, tags.Title);
        Assert.AreEqual("Current artist", tags.Artist);
    }

    [TestMethod]
    public void ChannelFallbackNeverReplacesTheVideosArtistTag()
    {
        using var engine = new FakeEngine { CurrentMetadata = new(new("Title", "Performer", ""), new Dictionary<string, string>()) };
        using var player = new Player(engine);
        const string url = "https://www.youtube.com/watch?v=abcdefghijk";
        player.OpenStream("https://cdn.example/audio", "A video", url);
        var video = new YouTubeResult("abcdefghijk", "A video", "Channel name", null, url, "");
        Assert.AreEqual("Performer", new MetadataReader(player).Read(video).Display.Artist);
    }

    [TestMethod]
    public void PreviousVideosChannelDoesNotLeakIntoAnotherVideoOrLocalFile()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var video = new YouTubeResult("abcdefghijk", "Previous video", "Old channel", null,
            "https://www.youtube.com/watch?v=abcdefghijk", "");
        var reader = new MetadataReader(player);
        player.OpenStream("https://cdn.example/audio", "Current video", "https://www.youtube.com/watch?v=lmnopqrstuv");
        Assert.AreEqual(string.Empty, reader.Read(video).Display.Artist);
        player.OpenFile(NativeHost.Wave(workspace));
        Assert.AreEqual(string.Empty, reader.Read(video).Display.Artist);
    }

    [TestMethod]
    public void RadioStillUsesItsLiveProgrammeMetadata()
    {
        using var engine = new FakeEngine { CurrentMetadata = new(new("Performer - Song", "", ""), new Dictionary<string, string>()) };
        using var player = new Player(engine);
        player.PlayRadioStation("https://radio.example/audio", "Station", "https://radio.example", null, out _);
        var tags = new MetadataReader(player).Read().Display;
        Assert.AreEqual("Song", tags.Title);
        Assert.AreEqual("Performer", tags.Artist);
    }

    [TestMethod]
    [DataRow("https://stream.example/audio")]
    [DataRow("luna-spotify://track/123")]
    public void OtherStreamsKeepTheirNativeMetadata(string path)
    {
        using var engine = new FakeEngine
        {
            CurrentMetadata = new(new("Current title", "Current artist", "Current album"), new Dictionary<string, string>()),
        };
        using var player = new Player(engine);
        player.OpenStream(path);
        Assert.AreEqual(engine.CurrentMetadata, new MetadataReader(player).Read());
    }
}
