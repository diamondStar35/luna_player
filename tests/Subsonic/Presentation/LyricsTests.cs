using LunaPlayer.Subsonic.Models;
using LunaPlayer.UI.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class LyricsTests
{
    [TestMethod]
    public void PositiveOffsetMovesLyricsEarlierAndSeekingCanMoveBackwards()
    {
        var lyrics = new StructuredLyrics { Synced = true, Offset = 100, Lines = [new() { Start = 1000, Value = "A" }, new() { Start = 2000, Value = "B" }] };
        Assert.AreEqual(-1, LyricsTimeline.At(lyrics, 0.8));
        Assert.AreEqual(0, LyricsTimeline.At(lyrics, 0.9));
        Assert.AreEqual(1, LyricsTimeline.At(lyrics, 1.9));
        Assert.AreEqual(0, LyricsTimeline.At(lyrics, 1));
        Assert.AreEqual(-1, LyricsTimeline.At(lyrics with { Synced = false }, 3));
    }
}
