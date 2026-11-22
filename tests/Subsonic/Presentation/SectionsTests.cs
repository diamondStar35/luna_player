using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Servers;
using LunaPlayer.UI.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class SectionsTests
{
    [TestMethod]
    public void ProfilesDefaultToAllSectionsAndHideOnlyTheirOwnChoices()
    {
        var profile = new Profile { Name = "Fixture", Server = new Uri("https://invalid/") };
        CollectionAssert.AreEqual(Enum.GetValues<Category>(), Sections.Visible(profile));
        var hidden = profile with { HiddenSections = [nameof(Category.Radio), "FutureSection"] };
        Assert.DoesNotContain(Category.Radio, Sections.Visible(hidden));
        Assert.Contains(Category.RandomAlbums, Sections.Visible(hidden));
        Assert.Contains(Category.Radio, Sections.Visible(profile));
    }
}
