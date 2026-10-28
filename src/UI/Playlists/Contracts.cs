using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Equalizer;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI.Equalizer;
using LunaPlayer.YouTube;

namespace LunaPlayer.UI.Playlists;
internal enum OpenedFilesAction { Jump, Information }

/// <summary>Whether the user picked a recent item to open, or its kind's Clear command.</summary>
internal enum RecentAction { Open, Clear }

internal readonly record struct OpenedFilesRequest(LunaPlayer.UI.Playlists.OpenedFilesAction Action, int SelectedIndex);

/// <summary>One entry in a Recents submenu: the path to reopen and the numbered label already worded for the
/// user (such as "1. song.mp3 — C:\Music").</summary>
internal readonly record struct RecentMenuEntry(string Path, string Label);

/// <summary>The three kinds of recent paths as they are displayed in the File menu.</summary>
internal readonly record struct RecentMenuState(
    IReadOnlyList<RecentMenuEntry> Files,
    IReadOnlyList<RecentMenuEntry> Folders,
    IReadOnlyList<RecentMenuEntry> Playlists);

/// <summary>What the user chose in a Recents submenu: open <see cref="Path"/>, or clear the whole
/// <see cref="Kind"/> (then <see cref="Path"/> is empty).</summary>
internal readonly record struct RecentCommand(LunaPlayer.UI.Playlists.RecentAction Action, RecentKind Kind, string Path);
