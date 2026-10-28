using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Equalizer;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI.Equalizer;
using LunaPlayer.YouTube;

namespace LunaPlayer.UI.Playback;

/// <summary>One subtitle track as the Subtitles menu shows it: the mpv track id and the label already worded
/// for the user. The "Off" entry is not one of these - the menu always carries it itself.</summary>
internal readonly record struct SubtitleMenuEntry(int Id, string Label);
