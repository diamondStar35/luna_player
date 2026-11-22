using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.UI.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class ViewsTests
{
    [TestMethod]
    public void ArtistAlbumAndBackNavigationUseTheVisibleCollectionForBothTreeAndHeading()
    {
        Location[] locations = [new(Category.Artists), new(Category.Artists, RowKind.Artist, "artist"),
            new(Category.Artists, RowKind.Album, "album")];
        Category[] sections = [Category.Artists, Category.Albums, Category.Songs];
        ViewContent[] content = [ViewContent.Artists, ViewContent.Albums, ViewContent.Songs];
        foreach (var index in new[] { 0, 1, 2, 1, 0 })
        {
            Assert.AreEqual(sections[index], Views.Section(locations[index]));
            Assert.AreEqual(content[index], Views.Content(locations[index], false, 0));
        }
    }

    [TestMethod]
    [DataRow((int)RowKind.Playlist)]
    [DataRow((int)RowKind.Genre)]
    public void CollectionsOpenedOutsideSongsStillPresentSongs(int container)
    {
        var location = new Location(Category.Favorites, (RowKind)container, "collection");
        Assert.AreEqual(Category.Songs, Views.Section(location));
        Assert.AreEqual(ViewContent.Songs, Views.Content(location, false, 0));
    }

    [TestMethod]
    public void RootAlbumCollectionsAndPodcastEpisodesHaveSpecificHeadings()
    {
        foreach (var category in new[] { Category.Albums, Category.Added, Category.Recent, Category.Frequent, Category.RandomAlbums })
            Assert.AreEqual(ViewContent.Albums, Views.Content(new(category), false, 0));
        Assert.AreEqual(ViewContent.Songs, Views.Content(new(Category.Random), false, 0));
        Assert.AreEqual(ViewContent.Favorites, Views.Content(new(Category.Favorites), false, 0));
        var episodes = new Location(Category.Podcasts, RowKind.PodcastChannel, "podcast");
        Assert.AreEqual(Category.Podcasts, Views.Section(episodes));
        Assert.AreEqual(ViewContent.Episodes, Views.Content(episodes, false, 0));
    }

    [TestMethod]
    public void SearchHeadingsFollowTheSearchTypeRatherThanThePreviouslyOpenedCollection()
    {
        var location = new Location(Category.Artists, RowKind.Artist, "artist");
        ViewContent[] headings = [ViewContent.SearchResults, ViewContent.Artists, ViewContent.Albums, ViewContent.Songs];
        for (var type = 0; type < headings.Length; type++)
            Assert.AreEqual(headings[type], Views.Content(location, true, type));
    }
}
