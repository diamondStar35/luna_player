using LunaPlayer.Playlist;
using WxSharp;

namespace LunaPlayer.UI.Playlists;

internal sealed record PlaylistRequest(string Action, Guid Id);
