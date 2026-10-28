using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Equalizer;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI.Equalizer;
using LunaPlayer.YouTube;

namespace LunaPlayer.UI.Preferences;
internal sealed record PrefsOps(
    string SettingsPath,
    string BookmarksPath,
    string SettingsFolder,
    Func<string, bool> ExportSettings,
    Func<string, Settings?> ImportSettings,
    Func<Settings?> ResetSettings,
    Func<string, bool> ExportBookmarks,
    Func<string, bool> ImportBookmarks,
    /// <summary>Why the last backup or restore failed, for the message that reports it.</summary>
    Func<string> LastBackupError,
    Func<bool> OpenSettingsFolder,
    Func<LunaPlayer.UI.Contracts.UiOperation> RegisterFiles,
    Func<LunaPlayer.UI.Contracts.UiOperation> UnregisterFiles,
    /// <summary>Fetches the programs a YouTube download needs. Nothing else on the settings page uses
    /// them, so this is the only way in.</summary>
    /// <summary>Fetches the programs yt-dlp needs, from the release line the page currently shows.
    /// Reports for itself, behind its own window, so there is nothing for the page to say afterwards.
    /// </summary>
    /// <remarks>
    /// The channel is passed in rather than read from the settings, because the settings still hold the
    /// old one: the page edits a copy and applies it when the window is accepted. Reading it there would
    /// fetch from whichever line was chosen last time, not the one on screen.
    /// </remarks>
    Action<YtDlpChannel> DownloadYouTubeComponents,
    Action<Settings> ApplyImmediate);
