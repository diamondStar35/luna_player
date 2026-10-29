using LunaPlayer.Application;
using LunaPlayer.Application.Presentation;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Discord;

[TestClass, TestCategory("Unit")]
public sealed class PlaybackTests
{
    private sealed class Dispatcher : IApplicationDispatcher
    {
        public void Post(Action action) => action();
        public IDisposable Repeat(TimeSpan interval, Action action) => throw new NotSupportedException();
    }

    [TestMethod]
    public void LocalPlaybackPauseSeekSpeedAndStopAreMapped()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        using var spotify = new LunaPlayer.Spotify.Playback.Sessions(new(), player, new Dispatcher());
        var tags = new MediaTagsCache();
        PresenceMedia? Read() => PresenceMediaReader.Read(player, tags, spotify, "Untitled media", true);
        Assert.IsNull(Read());
        player.OpenFile(@"C:\private\song.mp3");
        var playing = Read()!;
        Assert.IsTrue(playing.Playing);
        Assert.IsFalse(playing.Title.Contains("private", StringComparison.Ordinal));
        player.Pause();
        Assert.IsFalse(Read()!.Playing);
        player.SeekAbsolute(5);
        engine.SetSpeed(2);
        Assert.AreEqual(5d, Read()!.Position);
        Assert.AreEqual(2d, Read()!.Speed);
        player.Stop();
        Assert.IsNotNull(player.CurrentPath);
        Assert.IsNull(Read());
    }

    [TestMethod]
    public void ResolvedYouTubeUsesWatchPageAndDetailsCanBeOmitted()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        using var spotify = new LunaPlayer.Spotify.Playback.Sessions(new(), player, new Dispatcher());
        player.OpenStream("https://cdn.example/audio?token=secret", "A video", "https://youtu.be/abcdefghijk", audioOnly: true);
        var tags = new MediaTagsCache();
        var media = PresenceMediaReader.Read(player, tags, spotify, "Untitled media", true)!;
        Assert.AreEqual("A video", media.Title);
        Assert.AreEqual("https://youtu.be/abcdefghijk", media.PublicUrl);
        var hidden = PresenceMediaReader.Read(player, tags, spotify, "Untitled media", false)!;
        Assert.AreEqual(string.Empty, hidden.Title);
        Assert.AreEqual(string.Empty, hidden.Artist);
        Assert.IsNull(hidden.PublicUrl);
    }

    [TestMethod]
    public void YouTubeCatalogAuthorIsUsedOnlyForThePlayingVideo()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        using var spotify = new LunaPlayer.Spotify.Playback.Sessions(new(), player, new Dispatcher());
        const string url = "https://www.youtube.com/watch?v=abcdefghijk";
        player.OpenStream("https://cdn.example/audio", "A video", url);
        var video = new LunaPlayer.YouTube.YouTubeResult("abcdefghijk", "Catalog title", "Channel name", null, url, "");
        var media = PresenceMediaReader.Read(player, new(), spotify, "Untitled media", true, video)!;
        Assert.AreEqual("Catalog title", media.Title);
        Assert.AreEqual("Channel name", media.Artist);
        var unrelated = PresenceMediaReader.Read(player, new(), spotify, "Untitled media", true,
            video with { Url = "https://www.youtube.com/watch?v=lmnopqrstuv" })!;
        Assert.AreEqual("A video", unrelated.Title);
        Assert.AreEqual(string.Empty, unrelated.Artist);
    }

    [TestMethod]
    public void StreamAddressCannotBecomeTitleOrPublicButton()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        using var spotify = new LunaPlayer.Spotify.Playback.Sessions(new(), player, new Dispatcher());
        player.OpenStream("https://stream.example/user/password", "https://stream.example/user/password");
        var media = PresenceMediaReader.Read(player, new(), spotify, "Untitled media", true)!;
        Assert.AreEqual("Untitled media", media.Title);
        Assert.IsNull(ActivityBuilder.PublicLink(media.PublicUrl));
    }
}
