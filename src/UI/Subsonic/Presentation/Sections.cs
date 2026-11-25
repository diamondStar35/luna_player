using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Servers;

namespace LunaPlayer.UI.Subsonic;

internal static class Sections
{
    internal static Category[] Visible(Profile? profile) => Enum.GetValues<Category>()
        .Where(category => profile?.HiddenSections.Contains(category.ToString(), StringComparer.Ordinal) != true).ToArray();

    internal static string Name(Category category) => category switch
    {
        Category.Artists => Words.Artists, Category.Albums => Words.Albums, Category.Songs => Words.Songs,
        Category.Genres => Words.Genres, Category.Favorites => Words.Favorites, Category.Added => Words.RecentlyAdded,
        Category.Recent => Words.Recent, Category.Frequent => Words.Frequent, Category.Random => Words.Random,
        Category.Playlists => Words.Playlists, Category.Bookmarks => Words.Bookmarks, Category.NowPlaying => Words.NowPlaying,
        Category.Radio => Words.Radio, Category.Podcasts => Words.Podcasts, Category.RandomAlbums => Words.RandomAlbums,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };
}
