using System.Text;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Subsonic.Servers;
using LunaPlayer.Tests.Infrastructure;
using Enrichment = LunaPlayer.Application.MusicServers.Metadata;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class EnrichmentTests
{
    [TestMethod]
    public async Task RestoredUnplayedTitlesRefreshWhenEnabled()
    {
        using var directory = new TemporaryDirectory(); using var handler = new Handler((sent, _) => Task.FromResult(Fixture.Json(
            "\"song\":{\"id\":\"" + sent.Parameters["id"][0] + "\",\"title\":\"Fresh " + sent.Parameters["id"][0] + "\"}")));
        using var http = new HttpClient(handler); using var connections = new Connections(new Store(Path.Combine(directory.Path, "profiles.json")), profile => new Client(profile.ConnectionOptions(), http));
        var profile = await connections.SaveAsync(new Profile { Name = "Fixture", Server = Fixture.Options.Server, Credentials = Fixture.Options.Credentials });
        var pump = new Pump(); using var player = new Player(new FakeEngine()); player.SetMediaResolver(connections, pump.Post);
        using var metadata = new Enrichment(connections, player, pump);
        var first = new Reference(profile.Id, "a").Address; var second = new Reference(profile.Id, "b").Address;
        player.InsertMedia([(first, new MediaTags("Old A", "", "")), (second, new MediaTags("Old B", "", ""))], true);
        await pump.Until(() => player.IsLoaded && player.DisplayName(second) == "Fresh b");
        Assert.AreEqual("Fresh a", player.CurrentTitle);
        Assert.IsTrue(handler.Requests.Any(request => request.Parameters.GetValueOrDefault("id")?[0] == "b"));
    }

    [TestMethod]
    public async Task ArtworkFromAnOutgoingSongCannotReplaceTheCurrentPicture()
    {
        using var directory = new TemporaryDirectory(); var firstCover = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new Handler(async (sent, _) =>
        {
            var id = sent.Parameters["id"][0];
            if (sent.Endpoint == "getCoverArt")
            {
                if (id == "a") { requested.TrySetResult(); return await firstCover.Task; }
                return Fixture.Raw("current picture", "image/png");
            }
            return Fixture.Json("\"song\":{\"id\":\"" + id + "\",\"title\":\"" + id + "\",\"coverArt\":\"" + id + "\"}");
        });
        using var http = new HttpClient(handler); using var connections = new Connections(new Store(Path.Combine(directory.Path, "profiles.json")), profile => new Client(profile.ConnectionOptions(), http));
        var profile = await connections.SaveAsync(new Profile { Name = "Fixture", Server = Fixture.Options.Server,
            Credentials = Fixture.Options.Credentials, RefreshMetadataOnStartup = false });
        var pump = new Pump(); using var player = new Player(new FakeEngine()); player.SetMediaResolver(connections, pump.Post);
        using var metadata = new Enrichment(connections, player, pump);
        player.InsertMedia([(new Reference(profile.Id, "a").Address, MediaTags.None), (new Reference(profile.Id, "b").Address, MediaTags.None)], true);
        await pump.Until(() => player.IsLoaded && requested.Task.IsCompleted);
        player.Next(false);
        await pump.Until(() => player.IsLoaded && player.Artwork is not null);
        Assert.AreEqual("current picture", Encoding.UTF8.GetString(player.Artwork!));
        firstCover.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new ReleasingContent("outgoing picture", released) });
        await released.Task.WaitAsync(TimeSpan.FromSeconds(8)); pump.Drain();
        Assert.AreEqual("current picture", Encoding.UTF8.GetString(player.Artwork!));
        metadata.Dispose(); metadata.Dispose();
    }
    private sealed class ReleasingContent(string value, TaskCompletionSource released) : StringContent(value, Encoding.UTF8, "image/png")
    {
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) released.TrySetResult(); }
    }
}
