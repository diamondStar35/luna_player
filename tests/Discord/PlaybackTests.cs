using System.Text.Json;
using LunaPlayer.Application;
using LunaPlayer.Application.Presentation;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;
using LunaPlayer.Playback;
using LunaPlayer.Media;
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
        Assert.AreEqual("song", playing.Title);
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
        Assert.AreEqual(SourceKind.YouTube, media.Kind);
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
    [DataRow(null)]
    [DataRow("https://stream.example/user/password")]
    public void StreamAddressCannotBecomeTitleOrPublicButton(string? title)
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        using var spotify = new LunaPlayer.Spotify.Playback.Sessions(new(), player, new Dispatcher());
        player.OpenStream("https://stream.example/user/password", title);
        var media = PresenceMediaReader.Read(player, new(), spotify, "Untitled media", true)!;
        Assert.AreEqual("Untitled media", media.Title);
        Assert.IsNull(PublicLinks.Catalog(media.PublicUrl));
    }

    [TestMethod]
    public void LocalMetadataIsPreferredAndBooksKeepTitleSeparateFromChapter()
    {
        using var engine = new FakeEngine
        { CurrentMetadata = new Metadata(new("Song title", "Performer", "Book title"), new Dictionary<string, string>()) };
        using var player = new Player(engine);
        using var spotify = new LunaPlayer.Spotify.Playback.Sessions(new(), player, new Dispatcher());
        player.OpenFile(@"C:\private\filename.m4b");
        var cache = new MediaTagsCache();
        var local = PresenceMediaReader.Read(player, cache, spotify, "Untitled media", true)!;
        Assert.AreEqual("Song title", local.Title);
        Assert.AreEqual("Performer", local.Artist);
        player.SetAudiobookMode(true);
        var chapterless = PresenceMediaReader.Read(player, cache, spotify, "Untitled media", true)!;
        Assert.AreEqual(SourceKind.Local, chapterless.Kind);
        Assert.AreEqual("Song title", chapterless.Title);
        Assert.AreEqual("Performer", chapterless.Artist);
        Assert.IsNull(chapterless.Chapter);
        player.SetChapters(player.CurrentPath!, [new Chapter(0, "Opening chapter", 0, 10)]);
        var book = PresenceMediaReader.Read(player, cache, spotify, "Untitled media", true)!;
        Assert.AreEqual(SourceKind.Book, book.Kind);
        Assert.AreEqual("Book title", book.Title);
        Assert.AreEqual("Opening chapter", book.Chapter);
        Assert.AreEqual(string.Empty, book.Artist);
        Assert.IsNull(book.ArtworkUrl);
        var hidden = PresenceMediaReader.Read(player, cache, spotify, "Untitled media", false)!;
        Assert.AreEqual(string.Empty, hidden.Title);
        Assert.IsNull(hidden.Chapter);
        player.SetChapters(player.CurrentPath!, []);
        var cleared = PresenceMediaReader.Read(player, cache, spotify, "Untitled media", true)!;
        Assert.AreEqual(SourceKind.Local, cleared.Kind);
        Assert.AreEqual("Song title", cleared.Title);
        Assert.AreEqual("Performer", cleared.Artist);
        Assert.IsNull(cleared.Chapter);
        Assert.IsTrue(player.AudiobookMode);
    }

    [TestMethod]
    public void SwitchingFromAChapteredBookToMusicRestoresMusicPresence()
    {
        using var engine = new FakeEngine
        { CurrentMetadata = new Metadata(new("Book title", "Author", "Book album"), new Dictionary<string, string>()) };
        using var player = new Player(engine);
        using var spotify = new LunaPlayer.Spotify.Playback.Sessions(new(), player, new Dispatcher());
        var cache = new MediaTagsCache();
        player.SetAudiobookMode(true);
        player.OpenFile(@"C:\private\book.m4b");
        player.SetChapters(player.CurrentPath!, [new Chapter(0, "Opening chapter", 0, 10)]);
        Assert.AreEqual(SourceKind.Book, PresenceMediaReader.Read(player, cache, spotify, "Untitled media", true)!.Kind);

        engine.CurrentMetadata = new(new("Song title", "Performer", "Music album"), new Dictionary<string, string>());
        player.OpenFile(@"C:\private\song.mp3");
        var media = PresenceMediaReader.Read(player, cache, spotify, "Untitled media", true)!;
        Assert.IsTrue(player.AudiobookMode);
        Assert.AreEqual(SourceKind.Local, media.Kind);
        Assert.AreEqual("Song title", media.Title);
        Assert.AreEqual("Performer", media.Artist);
        Assert.IsNull(media.Chapter);
        using var activity = JsonDocument.Parse(new ActivityBuilder(DateTimeOffset.UtcNow).Build(
            new DiscordSettings { Enabled = true }, media, Examples.English, DateTimeOffset.UtcNow)!);
        Assert.AreEqual("music on Luna", activity.RootElement.GetProperty("name").GetString());
        Assert.AreEqual("Song title", activity.RootElement.GetProperty("state").GetString());
        Assert.AreEqual("Performer", activity.RootElement.GetProperty("assets").GetProperty("large_text").GetString());
    }

    [TestMethod]
    public void RadioKeepsStationNameAndUsesTheArtistAnnouncedByPlayer()
    {
        using var engine = new FakeEngine
        { CurrentMetadata = new Metadata(new("Artist - Broadcast title", "", ""), new Dictionary<string, string>()) };
        using var player = new Player(engine);
        using var spotify = new LunaPlayer.Spotify.Playback.Sessions(new(), player, new Dispatcher());
        player.SetAudiobookMode(true);
        player.PlayRadioStation("https://radio.example/stream", "Station name", "radio-browser://station", null, out _);
        var station = new LunaPlayer.Radio.Station { Name = "Station name", Favicon = "https://radio.example/logo.png" };
        var media = PresenceMediaReader.Read(player, new(), spotify, "Untitled media", true, station: station)!;
        Assert.AreEqual(SourceKind.Radio, media.Kind);
        Assert.AreEqual("Station name", media.Title);
        Assert.AreEqual(player.CurrentArtist, media.Artist);
        Assert.AreEqual("Artist", media.Artist);
        Assert.AreEqual("https://radio.example/stream", media.PublicUrl);
        Assert.AreEqual(station.Favicon, media.ArtworkUrl);
    }
}
