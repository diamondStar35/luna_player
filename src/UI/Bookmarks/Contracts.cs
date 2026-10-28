using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Equalizer;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI.Equalizer;
using LunaPlayer.YouTube;

namespace LunaPlayer.UI.Bookmarks;
internal enum BookmarkManagementAction { Jump, Rename, Delete }

internal readonly record struct BookmarkListItem(string Id, string Name, string Position);
internal readonly record struct BookmarkManagementRequest(LunaPlayer.UI.Bookmarks.BookmarkManagementAction Action, string Id);
