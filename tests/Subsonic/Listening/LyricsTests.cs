using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Listening;

namespace LunaPlayer.Tests.Subsonic.Listening;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class LyricsTests
{
    [TestMethod]
    public async Task LegacyFallbackRecognizesRepeatedTimestampsAndKeepsTheOffset()
    {
        using var handler = new Handler((sent, token) => Task.FromResult(sent.Endpoint switch
        {
            "getOpenSubsonicExtensions" => Fixture.Json("\"openSubsonicExtensions\":[]"),
            "getLyrics" => Fixture.Json("\"lyrics\":{\"value\":\"[ar:Artist]\\n[offset:100]\\n[00:30.00]Later\\n[00:01.5][00:10.025]Chorus\\n\"}"),
            _ => Fixture.Json()
        }));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        var lyrics = (await client.Listening.GetSongLyricsAsync("song", "Artist", "Title")).Single();
        Assert.IsTrue(lyrics.Synced);
        Assert.AreEqual(100d, lyrics.Offset);
        CollectionAssert.AreEqual(new long?[] { 1500, 10025, 30000 }, lyrics.Lines.Select(line => line.Start).ToArray());
        CollectionAssert.AreEqual(new[] { "Chorus", "Chorus", "Later" }, lyrics.Lines.Select(line => line.Value).ToArray());
        Assert.IsFalse(handler.Requests.Any(request => request.Endpoint == "getLyricsBySongId"));
    }

    [TestMethod]
    public void PlainLyricsKeepVerseBreaksAndInvalidTimestampsStayReadable()
    {
        var plain = LyricsParser.Parse("First\r\n\r\nSecond\r\n[00:99]Not a time\r\n\r\n");
        Assert.IsFalse(plain.Synced);
        CollectionAssert.AreEqual(new[] { "First", "", "Second", "[00:99]Not a time" }, plain.Lines.Select(line => line.Value).ToArray());
        Assert.IsEmpty(LyricsParser.Parse("[ar:Artist]\n[offset:-200]\n").Lines);
    }

    [TestMethod]
    public async Task ANotFoundSongIdEndpointFallsBackButAuthenticationFailuresRemainErrors()
    {
        using var handler = new Handler((sent, token) => sent.Endpoint == "getLyricsBySongId"
            ? Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)) : Fixture.Reply(sent, token));
        using var http = new HttpClient(handler); using var client = new Client(Fixture.Options, http);
        Assert.AreEqual("Lyrics", (await client.Listening.GetSongLyricsAsync("song", "Artist", "Title")).Single().Lines.Single().Value);
        using var denied = new Handler((sent, token) => sent.Endpoint == "getLyricsBySongId"
            ? Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)) : Fixture.Reply(sent, token));
        using var deniedHttp = new HttpClient(denied); using var deniedClient = new Client(Fixture.Options, deniedHttp);
        await Assert.ThrowsAsync<RequestException>(() => deniedClient.Listening.GetSongLyricsAsync("song", "Artist", "Title"));
        Assert.IsFalse(denied.Requests.Any(request => request.Endpoint == "getLyrics"));
    }
}
