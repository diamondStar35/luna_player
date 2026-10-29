using LunaPlayer.Application;
using LunaPlayer.Application.Presentation;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Discord;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class NativePlaybackTests
{
    private sealed class Dispatcher : IApplicationDispatcher
    {
        public void Post(Action action) => action();
        public IDisposable Repeat(TimeSpan interval, Action action) => throw new NotSupportedException();
    }

    [TestMethod]
    public async Task UntaggedNativeMediaUsesFilenameWithoutExtension()
    {
        using var workspace = new Workspace();
        using var engine = NativeHost.Create();
        using var player = new Player(engine);
        using var spotify = new LunaPlayer.Spotify.Playback.Sessions(new(), player, new Dispatcher());
        Assert.IsTrue(player.OpenFile(NativeHost.Wave(workspace, "My music.wav")));
        await NativeHost.Until(() => !engine.IsLoading && engine.Duration is > 0);
        var media = PresenceMediaReader.Read(player, new(), spotify, "Untitled media", true)!;
        Assert.AreEqual("My music", media.Title);
        Assert.AreEqual(string.Empty, media.Artist);
    }
}
