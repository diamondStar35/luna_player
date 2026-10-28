using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
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
        if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var document = LunaPlayer.Playlist.DocumentStore.Read(path);
                var resolved = LunaPlayer.Playlist.DocumentResolver.Resolve(document.Entries);
                if (resolved.Unavailable.Count > 0)
                    // Translators: Warning listing missing or empty entries while opening a JSON playlist. {paths} lists the locations.
                    _view.Dialogs.ShowWarning(TrFormat("Unavailable or empty playlist entries:\n{paths}", string.Join('\n', resolved.Unavailable)), document.Name);
                if (resolved.Files.Count == 0) return false;
                _player.OpenSaved(document.Entries, resolved.Files);
                RecordRecent(RecentKind.Playlist, path);
                return true;
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                // Translators: Error opening a JSON playlist. {error} is the storage or format failure.
                _view.Dialogs.ShowError(TrFormat("Could not open the playlist: {error}", exception.Message), AppInfo.Name);
                return false;
            }
        }
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
            // Translators: Status shown while a playlist download is still running.
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
            _view.Dialogs.ShowError(
                // Translators: Shown when an M3U or M3U8 playlist could not be read. {error} is the reason.
                TrFormat("Could not open the playlist: {error}", error),
                // Translators: Title of an error shown when a playlist cannot be opened.
                Tr("Error"));
            return false;
        }
        // An HLS manifest is itself one stream. Its non-comment lines are media chunks and variant
        // manifests, not tracks for Luna's opened-files list; mpv understands the manifest directly.
        if (result.IsHls)
            return network ? _player.OpenStream(source) : _player.OpenFile(source);
        if (result.Entries.Count == 0)
        {
            _view.Dialogs.ShowError(
                // Translators: Shown when an M3U or M3U8 file contains no usable local files or web links.
                Tr("The playlist contains no playable entries."),
                // Translators: Title of an error shown for an empty or unusable playlist.
                Tr("Error"));
            return false;
        }
        if (_player.OpenPlaylist(result.Entries))
            return true;
        _view.Dialogs.ShowError(
            // Translators: Shown when the entries in an M3U or M3U8 playlist could not be loaded.
            Tr("Could not load the playlist entries."),
            // Translators: Title of an error shown when playlist entries cannot be loaded.
            Tr("Error"));
        return false;
    }
}
