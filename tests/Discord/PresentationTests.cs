using System.Text;
using System.Text.Json;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;

namespace LunaPlayer.Tests.Discord;

[TestClass, TestCategory("Unit")]
public sealed class PresentationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(2000000000);
    private static readonly PresenceText Text = Examples.English;
    private static PresenceMedia Media(SourceKind kind, bool video = false)
        => new("item", "Media title", "Artist name", null, true, video, 30, 180, 1) { Kind = kind };
    private static JsonDocument Build(PresenceMedia? media, bool details = true, bool buttons = true)
        => JsonDocument.Parse(new ActivityBuilder(Now).Build(
            new() { Enabled = true, ShowDetails = details, ShowButtons = buttons }, media, Text, Now)!);

    [TestMethod]
    [DataRow(0, false, "Listening to music on Luna")]
    [DataRow(0, true, "Watching video on Luna")]
    [DataRow(1, true, "Watching YouTube on Luna")]
    [DataRow(1, false, "Listening to YouTube on Luna")]
    [DataRow(2, false, "Listening to radio on Luna")]
    [DataRow(3, false, "Listening to Spotify on Luna")]
    [DataRow(5, false, "Listening to Luna")]
    [DataRow(5, true, "Watching Luna")]
    public void MediaCardsKeepFullSourcePhraseAppHeadingTitleAndArtist(int kind, bool video, string phrase)
    {
        foreach (var playing in new[] { true, false })
        {
            using var json = Build(Media((SourceKind)kind, video) with { Playing = playing });
            var activity = json.RootElement;
            var watching = video;
            Assert.AreEqual(watching ? 3 : 2, activity.GetProperty("type").GetInt32());
            Assert.AreEqual(phrase, (watching ? "Watching " : "Listening to ") + activity.GetProperty("name").GetString());
            Assert.AreEqual(0, activity.GetProperty("status_display_type").GetInt32());
            Assert.AreEqual(playing ? "Luna Player" : "[Paused]: Luna Player", activity.GetProperty("details").GetString());
            Assert.AreEqual(watching && kind != (int)SourceKind.YouTube ? "Media title\nBy Artist name" : "Media title",
                activity.GetProperty("state").GetString());
            Assert.AreEqual("Artist name", activity.GetProperty("assets").GetProperty("large_text").GetString());
            Assert.AreEqual(1, activity.GetRawText().Split("Media title", StringSplitOptions.None).Length - 1);
            Assert.AreEqual("Download Luna Player", activity.GetProperty("buttons")[0].GetProperty("label").GetString());
            Assert.AreEqual("https://github.com/diamondStar35/luna_player/releases/latest",
                activity.GetProperty("buttons")[0].GetProperty("url").GetString());
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void IdleAndMediaWithoutArtistDoNotInventExtraLines(bool video)
    {
        using var idle = Build(null);
        Assert.AreEqual(0, idle.RootElement.GetProperty("type").GetInt32());
        Assert.AreEqual("Luna Player", idle.RootElement.GetProperty("name").GetString());
        Assert.AreEqual("Idle", idle.RootElement.GetProperty("details").GetString());
        Assert.IsFalse(idle.RootElement.TryGetProperty("state", out _));
        Assert.AreEqual("https://github.com/diamondStar35/luna_player/releases/latest",
            idle.RootElement.GetProperty("buttons")[0].GetProperty("url").GetString());
        var media = Media(video ? SourceKind.YouTube : SourceKind.Local, video) with { Artist = "" };
        using var playing = Build(media);
        Assert.AreEqual("Media title", playing.RootElement.GetProperty("state").GetString());
        Assert.IsFalse(playing.RootElement.GetProperty("assets").TryGetProperty("large_text", out _));
        using var paused = Build(media with { Playing = false });
        Assert.AreEqual("[Paused]: Luna Player", paused.RootElement.GetProperty("details").GetString());
        Assert.AreEqual("Media title", paused.RootElement.GetProperty("state").GetString());
        Assert.IsFalse(paused.RootElement.GetProperty("assets").TryGetProperty("large_text", out _));
        Assert.IsFalse(paused.RootElement.TryGetProperty("timestamps", out _));
    }

    [TestMethod]
    public void YouTubeWatchingKeepsOnlyTheTitleInItsBodyAndPlainArtistOnArtwork()
    {
        using var document = Build(Media(SourceKind.YouTube, true) with
        { Title = "Video\r\n title\t", Artist = "Artist\r\n name\t" });
        var activity = document.RootElement;
        Assert.AreEqual("Luna Player", activity.GetProperty("details").GetString());
        Assert.AreEqual("Video title", activity.GetProperty("state").GetString());
        Assert.AreEqual("Artist name", activity.GetProperty("assets").GetProperty("large_text").GetString());
    }

    [TestMethod]
    public void YouTubeWatchingDoesNotShortenLongTitlesToMakeRoomForAnArtistCredit()
    {
        using var document = Build(Media(SourceKind.YouTube, true) with
        { Title = string.Concat(Enumerable.Repeat("界😀", 100)), Artist = string.Concat(Enumerable.Repeat("歌😀", 100)) });
        var activity = document.RootElement;
        var state = activity.GetProperty("state").GetString()!;
        Assert.IsTrue(Encoding.UTF8.GetByteCount(state) <= 128);
        Assert.IsTrue(Encoding.UTF8.GetByteCount(state) > 120);
        Assert.IsFalse(state.Contains('\uFFFD'));
        Assert.IsFalse(state.Contains('\n'));
        Assert.IsFalse(state.Contains("By ", StringComparison.Ordinal));
        Assert.IsFalse(state.Contains('歌'));
        Assert.IsTrue(state.StartsWith("界😀", StringComparison.Ordinal));
        Assert.IsTrue(activity.GetProperty("assets").GetProperty("large_text").GetString()!.StartsWith("歌😀", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BooksKeepAppHeadingBookTitleAndChapter()
    {
        foreach (var playing in new[] { true, false })
        {
            using var book = Build(Media(SourceKind.Book) with { Playing = playing, Chapter = "Chapter 3" });
            Assert.AreEqual(2, book.RootElement.GetProperty("type").GetInt32());
            Assert.AreEqual("an audiobook on Luna", book.RootElement.GetProperty("name").GetString());
            Assert.AreEqual(playing ? "Luna Player" : "[Paused]: Luna Player", book.RootElement.GetProperty("details").GetString());
            Assert.AreEqual("Media title", book.RootElement.GetProperty("state").GetString());
            Assert.AreEqual("Chapter 3", book.RootElement.GetProperty("assets").GetProperty("large_text").GetString());
        }
        using var missing = Build(Media(SourceKind.Book));
        Assert.IsFalse(missing.RootElement.GetProperty("assets").TryGetProperty("large_text", out _));
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(1, true)]
    [DataRow(3, false)]
    public void HiddenDetailsKeepAppHeadingAndGenericTitleWithoutSourceOrArtist(int kind, bool video)
    {
        foreach (var playing in new[] { true, false })
        {
            using var json = Build(Media((SourceKind)kind, video) with { Playing = playing }, details: false);
            var activity = json.RootElement;
            Assert.AreEqual("Luna", activity.GetProperty("name").GetString());
            Assert.AreEqual(playing ? "Luna Player" : "[Paused]: Luna Player", activity.GetProperty("details").GetString());
            Assert.AreEqual("Media playback", activity.GetProperty("state").GetString());
            Assert.IsFalse(activity.GetProperty("assets").TryGetProperty("large_text", out _));
            Assert.AreEqual(1, activity.GetProperty("buttons").GetArrayLength());
        }
    }

    [TestMethod]
    public void ArtworkIsIndependentOfButtonsAndHidingDetailsRemovesIt()
    {
        var video = Media(SourceKind.YouTube, true) with { PublicUrl = "https://youtu.be/dQw4w9WgXcQ" };
        using var visible = Build(video, buttons: false);
        Assert.AreEqual("https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg",
            visible.RootElement.GetProperty("assets").GetProperty("large_image").GetString());
        Assert.IsFalse(visible.RootElement.TryGetProperty("buttons", out _));
        using var hidden = Build(video, details: false);
        Assert.AreEqual("luna", hidden.RootElement.GetProperty("assets").GetProperty("large_image").GetString());
        Assert.IsFalse(hidden.RootElement.GetRawText().Contains("dQw4w9WgXcQ", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow(true, "Watch on YouTube")]
    [DataRow(false, "Listen on YouTube")]
    public void YouTubeHasSourceAndDownloadButtons(bool video, string label)
    {
        using var json = Build(Media(SourceKind.YouTube, video) with { PublicUrl = "https://youtu.be/dQw4w9WgXcQ?si=secret" });
        var buttons = json.RootElement.GetProperty("buttons");
        Assert.AreEqual(2, buttons.GetArrayLength());
        Assert.AreEqual(label, buttons[0].GetProperty("label").GetString());
        Assert.AreEqual("https://www.youtube.com/watch?v=dQw4w9WgXcQ", buttons[0].GetProperty("url").GetString());
        Assert.AreEqual("Download Luna Player", buttons[1].GetProperty("label").GetString());
        Assert.AreEqual("https://github.com/diamondStar35/luna_player/releases/latest", buttons[1].GetProperty("url").GetString());
    }

    [TestMethod]
    public void SpotifyAndRadioUsePublicArtworkAndSourceLinks()
    {
        using var spotify = Build(Media(SourceKind.Spotify) with
        { PublicUrl = "https://open.spotify.com/track/1234567890123456789012", ArtworkUrl = "https://i.scdn.co/image/cover" });
        Assert.AreEqual("Open in Spotify", spotify.RootElement.GetProperty("buttons")[0].GetProperty("label").GetString());
        Assert.AreEqual("https://i.scdn.co/image/cover", spotify.RootElement.GetProperty("assets").GetProperty("large_image").GetString());
        using var radio = Build(Media(SourceKind.Radio) with
        { PublicUrl = "https://radio.example:8443/stream.mp3", ArtworkUrl = "https://radio.example/logo.png", Duration = null });
        Assert.AreEqual("Watch online", radio.RootElement.GetProperty("buttons")[0].GetProperty("label").GetString());
        Assert.AreEqual("https://radio.example:8443/stream.mp3", radio.RootElement.GetProperty("buttons")[0].GetProperty("url").GetString());
        Assert.IsFalse(radio.RootElement.GetProperty("timestamps").TryGetProperty("end", out _));
        using var hidden = Build(Media(SourceKind.Radio) with { PublicUrl = "https://radio.example/stream" }, details: false);
        Assert.AreEqual(1, hidden.RootElement.GetProperty("buttons").GetArrayLength());
    }

    [TestMethod]
    [DataRow("http://127.0.0.1/live")]
    [DataRow("http://192.168.1.12/live")]
    [DataRow("http://[::1]/live")]
    [DataRow("http://[fd00::1]/live")]
    [DataRow("http://[::ffff:192.168.1.12]/live")]
    [DataRow("https://user:password@radio.example/live")]
    [DataRow("https://radio.example/live/user/password/123")]
    [DataRow("https://radio.example/%6cive/user/password/123")]
    [DataRow("https://radio.example/stream?token=secret")]
    [DataRow("https://radio.example/stream?%70assword=secret")]
    [DataRow("http://radio.local/stream")]
    [DataRow("file:///C:/private/cover.jpg")]
    public void PrivateRadioLinksAndArtworkAreOmitted(string address)
    {
        Assert.IsNull(PublicLinks.Stream(address));
        using var activity = Build(Media(SourceKind.Radio) with { PublicUrl = address, ArtworkUrl = address });
        Assert.AreEqual(1, activity.RootElement.GetProperty("buttons").GetArrayLength());
        Assert.AreEqual("luna", activity.RootElement.GetProperty("assets").GetProperty("large_image").GetString());
    }

    [TestMethod]
    [DataRow("https://i.scdn.co/image/album")]
    [DataRow("https://image-cdn-fa.spotifycdn.com/image/album")]
    public void SpotifyRetainsAlbumArtworkWithoutChangingTrackNames(string image)
    {
        using var json = JsonDocument.Parse($$$"""
            {"album":{"images":[{"url":"http://localhost/cover"},{"url":"{{{image}}}"}]}}
            """);
        Assert.AreEqual(image, LunaPlayer.Spotify.Catalog.AlbumArtwork(json.RootElement));
        using var missing = JsonDocument.Parse("{}");
        Assert.IsNull(LunaPlayer.Spotify.Catalog.AlbumArtwork(missing.RootElement));
    }

    [TestMethod]
    public void PublicRadioAddressesRetainRequiredParametersAndPorts()
    {
        Assert.AreEqual("https://radio.example:8443/stream?id=123",
            PublicLinks.Stream("https://radio.example:8443/stream?id=123#player"));
        Assert.AreEqual("https://[2606:4700:4700::1111]/stream", PublicLinks.Stream("https://[2606:4700:4700::1111]/stream"));
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(3, false)]
    [DataRow(4, false)]
    public void KnownDurationIncludesBothProgressEndpoints(int kind, bool video)
    {
        using var document = Build(Media((SourceKind)kind, video));
        var activity = document.RootElement;
        Assert.AreEqual(video ? 3 : 2, activity.GetProperty("type").GetInt32());
        var timestamps = activity.GetProperty("timestamps");
        var start = timestamps.GetProperty("start").GetInt64();
        var end = timestamps.GetProperty("end").GetInt64();
        Assert.AreEqual(30L, Now.ToUnixTimeSeconds() - start);
        Assert.AreEqual(180L, end - start);
    }

    [TestMethod]
    public void ApplicationIconIsUsedAsFallbackWithoutBadgeOrReplacingOnlineArtwork()
    {
        const string icon = "https://cdn.discordapp.com/app-icons/1565330415529762956/930f1667326a41fa0865bd709fa45f0c.png?size=256";
        var builder = new ActivityBuilder(Now);
        var settings = new DiscordSettings { Enabled = true };
        using var local = JsonDocument.Parse(builder.Build(settings, Media(SourceKind.Local), Text, Now, icon)!);
        var assets = local.RootElement.GetProperty("assets");
        Assert.AreEqual(icon, assets.GetProperty("large_image").GetString());
        Assert.AreEqual("Artist name", assets.GetProperty("large_text").GetString());
        Assert.IsFalse(assets.TryGetProperty("small_image", out _));
        Assert.IsFalse(assets.TryGetProperty("small_text", out _));
        using var video = JsonDocument.Parse(builder.Build(settings, Media(SourceKind.YouTube, true) with
        { PublicUrl = "https://youtu.be/abcdefghijk" }, Text, Now, icon)!);
        var online = video.RootElement.GetProperty("assets");
        Assert.AreEqual("https://i.ytimg.com/vi/abcdefghijk/hqdefault.jpg", online.GetProperty("large_image").GetString());
        Assert.AreEqual("Artist name", online.GetProperty("large_text").GetString());
        Assert.IsFalse(online.TryGetProperty("small_image", out _));
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(3, false)]
    public void ArtworkLabelsAreSeparateFromWatchingArtistCreditsAndNeverHavePrefixOrVersion(int kind, bool video)
    {
        const string icon = "https://cdn.discordapp.com/app-icons/1565330415529762956/930f1667326a41fa0865bd709fa45f0c.png?size=256";
        using var document = JsonDocument.Parse(new ActivityBuilder(Now).Build(new DiscordSettings { Enabled = true },
            Media((SourceKind)kind, video), Text, Now, icon)!);
        var activity = document.RootElement;
        var assets = activity.GetProperty("assets");
        Assert.AreEqual("Artist name", assets.GetProperty("large_text").GetString());
        Assert.IsFalse(assets.TryGetProperty("small_text", out _));
        Assert.IsFalse(assets.TryGetProperty("small_image", out _));
        var hasCredit = video && kind != (int)SourceKind.YouTube;
        Assert.AreEqual(hasCredit ? 2 : 1, activity.GetRawText().Split("Artist name", StringSplitOptions.None).Length - 1);
        Assert.IsFalse(activity.GetRawText().Contains("Luna Player v", StringComparison.Ordinal));
        Assert.AreEqual("Luna Player", activity.GetProperty("details").GetString());
        Assert.AreEqual(hasCredit ? "Media title\nBy Artist name" : "Media title", activity.GetProperty("state").GetString());
    }
}
