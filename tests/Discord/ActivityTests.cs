using System.Text;
using System.Text.Json;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;

namespace LunaPlayer.Tests.Discord;

[TestClass, TestCategory("Unit")]
public sealed class ActivityTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(2000000000);
    private static readonly PresenceText Texts = new("Idle", "Playing", "Paused", "Using Luna Player",
        "Untitled media", "Watch online", "Listen online", "Download Luna Player");
    private static DiscordSettings Options() => new() { Enabled = true };
    private static PresenceMedia Media() => new("track", "A title", "An artist", null, true, false, 30, 120, 1);
    private static JsonDocument Build(ActivityBuilder builder, DiscordSettings options, PresenceMedia? media, DateTimeOffset? now = null)
        => JsonDocument.Parse(builder.Build(options, media, Texts, now ?? Now)!);

    [TestMethod]
    public void PlayingPausedAndResumedClocksReflectPlaybackAndSpeed()
    {
        var builder = new ActivityBuilder(Now);
        using var playing = Build(builder, Options(), Media());
        Assert.AreEqual(1999999970L, playing.RootElement.GetProperty("timestamps").GetProperty("start").GetInt64());
        Assert.AreEqual(2000000090L, playing.RootElement.GetProperty("timestamps").GetProperty("end").GetInt64());
        using var paused = Build(builder, Options(), Media() with { Playing = false });
        Assert.IsFalse(paused.RootElement.TryGetProperty("timestamps", out _));
        StringAssert.StartsWith(paused.RootElement.GetProperty("state").GetString()!, "Paused");
        using var resumed = Build(builder, Options(), Media() with { Speed = 2 }, Now.AddSeconds(10));
        Assert.AreEqual(1999999995L, resumed.RootElement.GetProperty("timestamps").GetProperty("start").GetInt64());
        Assert.AreEqual(2000000055L, resumed.RootElement.GetProperty("timestamps").GetProperty("end").GetInt64());
    }

    [TestMethod]
    public void PositionSamplesAreCoalescedButSeeksAndNewTracksChangeClock()
    {
        var builder = new ActivityBuilder(Now);
        var first = builder.Build(Options(), Media(), Texts, Now);
        Assert.AreEqual(first, builder.Build(Options(), Media() with { Position = 31.2 }, Texts, Now.AddSeconds(1)));
        using var seek = Build(builder, Options(), Media() with { Position = 75 });
        Assert.AreEqual(1999999925L, seek.RootElement.GetProperty("timestamps").GetProperty("start").GetInt64());
        using var next = Build(builder, Options(), Media() with { Identity = "next", Position = 0 });
        Assert.AreEqual(2000000000L, next.RootElement.GetProperty("timestamps").GetProperty("start").GetInt64());
    }

    [TestMethod]
    public void IdleAndDisabledOptionsClearPresence()
    {
        var builder = new ActivityBuilder(Now);
        var options = Options();
        using var idle = Build(builder, options, null);
        Assert.AreEqual("Idle", idle.RootElement.GetProperty("details").GetString());
        options.ShowIdle = false;
        Assert.IsNull(builder.Build(options, null, Texts, Now));
        Assert.IsNotNull(builder.Build(options, Media() with { Playing = false }, Texts, Now));
        options.Enabled = false;
        Assert.IsNull(builder.Build(options, Media(), Texts, Now));
    }

    [TestMethod]
    public void HiddenDetailsRemoveMediaTextArtworkAndMediaButton()
    {
        var options = Options(); options.ShowDetails = false;
        using var activity = Build(new(Now), options, Media() with { PublicUrl = "https://youtu.be/abcdefghijk" });
        var json = activity.RootElement.GetRawText();
        Assert.IsFalse(json.Contains("A title", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("An artist", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("abcdefghijk", StringComparison.Ordinal));
        Assert.AreEqual(1, activity.RootElement.GetProperty("buttons").GetArrayLength());
        options.ShowButtons = false;
        using var noButtons = Build(new(Now), options, Media());
        Assert.IsFalse(noButtons.RootElement.TryGetProperty("buttons", out _));
    }

    [TestMethod]
    public void VideoUsesWatchingTypeAndThumbnailAndCanonicalButton()
    {
        using var activity = Build(new(Now), Options(), Media() with
        { Video = true, PublicUrl = "https://www.youtube.com/watch?v=abcdefghijk&token=private" });
        Assert.AreEqual(3, activity.RootElement.GetProperty("type").GetInt32());
        Assert.AreEqual("https://i.ytimg.com/vi/abcdefghijk/hqdefault.jpg",
            activity.RootElement.GetProperty("assets").GetProperty("large_image").GetString());
        Assert.AreEqual("https://www.youtube.com/watch?v=abcdefghijk",
            activity.RootElement.GetProperty("buttons")[0].GetProperty("url").GetString());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("file:///C:/private/song.mp3")]
    [DataRow("https://stream.example/live/user/password")]
    [DataRow("http://127.0.0.1/audio")]
    [DataRow("https://youtube.com.evil.example/watch?v=abcdefghijk")]
    [DataRow("https://user:password@youtube.com/watch?v=abcdefghijk")]
    [DataRow("https://www.youtube.com:8888/watch?v=abcdefghijk")]
    [DataRow("https://youtu.be/bad")]
    public void PrivateOrInvalidLinksAreNeverShared(string? source)
        => Assert.IsNull(ActivityBuilder.PublicLink(source));

    [TestMethod]
    [DataRow("https://youtu.be/abcdefghijk?secret=anything")]
    [DataRow("https://music.youtube.com/watch?v=abcdefghijk")]
    [DataRow("https://m.youtube.com/shorts/abcdefghijk")]
    [DataRow("https://www.youtube.com/embed/abcdefghijk")]
    [DataRow("https://www.youtube.com/live/abcdefghijk")]
    public void YouTubeLinksAreCanonicalized(string source)
        => Assert.AreEqual("https://www.youtube.com/watch?v=abcdefghijk", ActivityBuilder.PublicLink(source)!.Url);

    [TestMethod]
    public void SpotifyLinkStripsTrackingParameters()
        => Assert.AreEqual("https://open.spotify.com/track/1234567890123456789012",
            ActivityBuilder.PublicLink("https://open.spotify.com/track/1234567890123456789012?si=private")!.Url);

    [TestMethod]
    public void TextLimitsPreserveUnicodeAndRemoveControls()
    {
        var title = ActivityBuilder.Text(string.Concat(Enumerable.Repeat("界😀", 100)));
        Assert.IsTrue(Encoding.UTF8.GetByteCount(title) <= 128);
        Assert.IsFalse(title.Contains('\uFFFD'));
        Assert.IsTrue(Encoding.UTF8.GetByteCount(ActivityBuilder.Text(new string('界', 100), 32)) <= 32);
        Assert.AreEqual("x ", ActivityBuilder.Text(" x\n"));
        Assert.AreEqual("😀 ", ActivityBuilder.Text("😀"));
    }

    [TestMethod]
    public void InvalidOrLiveDurationsDoNotProduceAnEndTimestamp()
    {
        foreach (var duration in new double?[] { null, 0, double.NaN, double.PositiveInfinity, 10 })
        {
            using var activity = Build(new(Now), Options(), Media() with { Duration = duration });
            Assert.IsFalse(activity.RootElement.GetProperty("timestamps").TryGetProperty("end", out _));
        }
    }

    [TestMethod]
    public void SettingsAreOptInCopiedAndNativeAotSerializable()
    {
        var settings = new Settings();
        Assert.IsFalse(settings.Discord.Enabled);
        settings.Discord = Options();
        var copy = settings.Copy(); copy.Discord.ShowButtons = false;
        Assert.IsTrue(settings.Discord.ShowButtons);
        var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        var restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings)!;
        restored.ValidateStored();
        using var document = JsonDocument.Parse(json);
        Assert.IsFalse(document.RootElement.GetProperty("discord").TryGetProperty("clientId", out _));
        Assert.IsTrue(restored.Discord.Enabled);
        var old = JsonSerializer.Deserialize("{\"version\":4}", SettingsJsonContext.Default.Settings)!;
        old.ValidateStored();
        Assert.IsFalse(old.Discord.Enabled);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not-a-number")]
    [DataRow("12345678901234567X")]
    [DataRow("99999999999999999999")]
    public void InvalidApplicationIdsAreRejected(string id)
        => Assert.IsFalse(PublisherApplication.IsValidApplicationId(id));

    [TestMethod]
    public void LegacyPerUserApplicationIdIsIgnoredAndNotWrittenBack()
    {
        var settings = JsonSerializer.Deserialize(
            "{\"discord\":{\"enabled\":true,\"clientId\":\"123456789012345679\"}}", SettingsJsonContext.Default.Settings)!;
        settings.ValidateStored();
        var serialized = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        using var document = JsonDocument.Parse(serialized);
        Assert.IsFalse(document.RootElement.GetProperty("discord").TryGetProperty("clientId", out _));
        Assert.IsTrue(settings.Discord.Enabled);
    }
}
