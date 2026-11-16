using LunaPlayer.Configuration;
using LunaPlayer.Media;
using LunaPlayer.Media.Discs;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback.Discs;

[TestClass, TestCategory("Unit")]
public sealed class NavigationTests
{
    private static DiscInfo Disc(int titles = 1, string identity = "disc-a")
        => new(@"F:\AUDIO_TS", DiscFormat.Audio, identity, "Album",
            Enumerable.Range(0, titles).Select(index => new Title(index)).ToArray(), 0);
    private static Chapter[] Chapters => [new(0, null, 0, 3), new(1, null, 3, 6), new(2, null, 6, 10)];

    [TestMethod]
    public void SingleTitleNavigatesSongsBySeekingWithoutReloading()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        Assert.IsTrue(player.OpenDisc(Disc()));
        player.SetChapters(player.CurrentPath!, Chapters);
        Assert.IsTrue(player.CanNavigateDisc(1));
        Assert.IsFalse(player.CanNavigateDisc(-1));
        Assert.IsTrue(player.Next(false));
        Assert.AreEqual(3d, engine.Position);
        Assert.HasCount(1, engine.Loads);
        Assert.IsTrue(player.Next(false));
        Assert.AreEqual(6d, engine.Position);
        Assert.IsFalse(player.Next(false));
        Assert.IsTrue(player.Previous(false));
        Assert.AreEqual(3d, engine.Position);
        Assert.HasCount(1, engine.Loads);
    }

    [TestMethod]
    public void RapidNavigationAdvancesFromThePendingChapter()
    {
        using var engine = new FakeEngine { DeferSeeks = true };
        using var player = new Player(engine);
        player.OpenDisc(Disc());
        player.SetChapters(player.CurrentPath!, Chapters);
        Assert.IsTrue(player.Next(false));
        Assert.IsTrue(player.Next(false));
        Assert.IsTrue(player.Previous(false));
        CollectionAssert.AreEqual(new[] { 3d, 6d, 3d }, engine.RequestedSeeks);
        Assert.HasCount(1, engine.Loads);
    }

    [TestMethod]
    public void MultipleTitlesNavigateTitlesEvenWhenEachContainsChapters()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var disc = Disc(2);
        player.OpenDisc(disc);
        player.SetChapters(player.CurrentPath!, Chapters);
        Assert.IsTrue(player.Next(false));
        Assert.AreEqual(disc.Address(1), player.CurrentPath);
        Assert.AreEqual(0d, engine.Position);
        Assert.HasCount(2, engine.Loads);
        Assert.IsTrue(player.Previous(false));
        Assert.AreEqual(disc.Address(0), player.CurrentPath);
    }

    [TestMethod]
    public void SingleTitleWithoutAnotherChapterHasNoNavigationDestination()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        player.OpenDisc(Disc());
        player.SetChapters(player.CurrentPath!, [new(0, null, 0, 10)]);
        Assert.IsFalse(player.Next(true));
        Assert.IsFalse(player.Previous(true));
        Assert.IsFalse(player.CanNavigateDisc(1));
    }

    [TestMethod]
    public void DiscNavigationDoesNotChangeTheAudiobookDefault()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        player.SetAudiobookMode(true);
        player.OpenDisc(Disc(2));
        player.SetChapters(player.CurrentPath!, Chapters);
        Assert.IsFalse(player.AudiobookMode);
        Assert.AreEqual(2, player.Count);
        player.OpenFile("book.m4b");
        player.SetChapters("book.m4b", Chapters);
        Assert.IsTrue(player.AudiobookMode);
        Assert.AreEqual(3, player.Count);
    }

    [TestMethod]
    public void SessionRestoresDiscAndItsChapterNavigation()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        player.OpenDisc(Disc());
        player.SetChapters(player.CurrentPath!, Chapters);
        player.SeekAbsolute(3);
        player.PlaySessionStream("https://example.invalid/audio", "Online", null, null);
        Assert.IsFalse(player.IsDisc);
        player.LeaveSession();
        Assert.IsTrue(player.IsDisc);
        Assert.AreEqual(3d, engine.Position);
        Assert.IsTrue(player.Next(false));
        Assert.AreEqual(6d, engine.Position);
    }

    [TestMethod]
    public void DifferentDiscsAtTheSameDriveHaveIndependentSavedPositions()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine, new PositionStore(Path.Combine(workspace.Root, "positions.json")));
        player.TrackPositions(true);
        player.OpenDisc(Disc(identity: "first"));
        player.SeekAbsolute(4);
        player.SavePosition();
        player.OpenDisc(Disc(identity: "second"));
        Assert.AreEqual(0d, engine.Position);
        player.OpenDisc(Disc(identity: "first"));
        Assert.AreEqual(4d, engine.Position);
    }

    [TestMethod]
    public void RejectingAnUnavailableTitlePreservesCurrentPlayback()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        player.OpenFile("music.mp3");
        Assert.IsFalse(player.OpenDisc(Disc(), 12));
        Assert.AreEqual("music.mp3", player.CurrentPath);
        Assert.HasCount(1, engine.Loads);
    }
}
