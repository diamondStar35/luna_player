using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.Media;
using System.Diagnostics;

namespace LunaPlayer.Application.Commands.Files;

internal sealed partial class Files
{
    private bool OpenLocalPlaylist(string path)
    {
        _settings.General.LastDirectory = Path.GetDirectoryName(path) ?? string.Empty;
        if (!UsePlaylistResult(path, PlaylistReader.ReadLocal(path), network: false))
            return false;
        RecordRecent(RecentKind.Playlist, path);
        return true;
    }

    private void OpenNetworkPlaylist(string address)
    {
        var prompt = new ProgressPrompt(
            // Translators: Title of the progress window shown while a playlist is downloaded.
            Tr("Opening playlist"),
            // Translators: Message shown while a playlist is downloaded.
            Tr("Downloading playlist..."),
            _ => Tr("Downloading playlist..."))
        {
            Proportional = false,
        };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (_, token) => PlaylistReader.ReadNetwork(address, token),
            result => UsePlaylistResult(address, result, network: true));
    }

    private bool UsePlaylistResult(string source, M3uPlaylistResult result, bool network)
    {
        if (result.Error is string error)
        {
            _view.ShowError(
                // Translators: Shown when an M3U or M3U8 playlist could not be read. {error} is the reason.
                TrFormat("Could not open the playlist: {error}", error), Tr("Error"));
            return false;
        }
        // An HLS manifest is itself one stream. Its non-comment lines are media chunks and variant
        // manifests, not tracks for Luna's opened-files list; mpv understands the manifest directly.
        if (result.IsHls)
            return network ? _player.OpenStream(source) : _player.OpenFile(source);
        if (result.Entries.Count == 0)
        {
            _view.ShowError(
                // Translators: Shown when an M3U or M3U8 file contains no usable local files or web links.
                Tr("The playlist contains no playable entries."), Tr("Error"));
            return false;
        }
        if (_player.OpenPlaylist(result.Entries))
            return true;
        _view.ShowError(
            // Translators: Shown when the entries in an M3U or M3U8 playlist could not be loaded.
            Tr("Could not load the playlist entries."), Tr("Error"));
        return false;
    }
}
