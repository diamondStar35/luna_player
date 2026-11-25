using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.UI.Subsonic;

internal enum ViewContent { Artists, Albums, Songs, Genres, Playlists, Bookmarks, Radio, Podcasts, Episodes, Favorites, SearchResults }

/// <summary>Describes the visible collection independently of the category from which it was opened.</summary>
internal static class Views
{
    internal static Category Section(Location location) => location.Container switch
    {
        RowKind.Artist => Category.Albums,
        RowKind.Album or RowKind.Playlist or RowKind.Genre => Category.Songs,
        RowKind.PodcastChannel => Category.Podcasts,
        _ => location.Category
    };

    internal static ViewContent Content(Location location, bool searching, int searchType)
    {
        if (searching) return searchType switch
        { 1 => ViewContent.Artists, 2 => ViewContent.Albums, 3 => ViewContent.Songs, _ => ViewContent.SearchResults };
        if (location.Container == RowKind.PodcastChannel) return ViewContent.Episodes;
        return Section(location) switch
        {
            Category.Artists => ViewContent.Artists,
            Category.Albums or Category.Added or Category.Recent or Category.Frequent or Category.RandomAlbums => ViewContent.Albums,
            Category.Genres => ViewContent.Genres, Category.Playlists => ViewContent.Playlists,
            Category.Bookmarks => ViewContent.Bookmarks, Category.Radio => ViewContent.Radio,
            Category.Podcasts => ViewContent.Podcasts, Category.Favorites => ViewContent.Favorites,
            _ => ViewContent.Songs
        };
    }

    internal static string Heading(ViewContent content) => content switch
    {
        ViewContent.Artists => Words.Artists, ViewContent.Albums => Words.Albums, ViewContent.Songs => Words.Songs,
        ViewContent.Genres => Words.Genres, ViewContent.Playlists => Words.Playlists, ViewContent.Bookmarks => Words.Bookmarks,
        ViewContent.Radio => Words.Radio, ViewContent.Podcasts => Words.Podcasts, ViewContent.Episodes => Words.Episodes,
        ViewContent.Favorites => Words.Favorites, _ => Words.SearchResults
    };
}
