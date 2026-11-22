using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.UI.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class RatingsTests
{
    [TestMethod]
    public void UnratedAndMixedSelectionsAreDifferentStates()
    {
        Assert.AreEqual(0, Ratings.Common([new(RowKind.Song, "a", "A"), new(RowKind.Song, "b", "B", Rating: 0)]));
        Assert.AreEqual(4, Ratings.Common([new(RowKind.Song, "a", "A", Rating: 4), new(RowKind.Song, "b", "B", Rating: 4)]));
        Assert.IsNull(Ratings.Common([new(RowKind.Song, "a", "A", Rating: 4), new(RowKind.Song, "b", "B", Rating: 2)]));
        Assert.AreEqual(0, Ratings.Common([new(RowKind.Song, "a", "A", Rating: 99)]));
        Assert.IsFalse(Ratings.CanRate([]));
    }
}
