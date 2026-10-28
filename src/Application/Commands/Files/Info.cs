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
    private void OpenContainingFolder()
    {
        // Translators: Spoken when the user asks to show the folder of what is playing but it is a stream rather than a file on this computer.
        if (!_guard.RequireLocalFile(Tr("Open containing folder is available only for local files."), out var path))
            return;
        TryStart(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true },
            // Translators: Spoken when the folder holding the current file could not be shown.
            Tr("Could not open containing folder."));
    }

    private void OpenFileProperties()
    {
        // Translators: Spoken when the user asks for the Windows properties of what is playing but it is a stream rather than a file on this computer.
        if (!_guard.RequireLocalFile(Tr("File properties are available only for local files."), out var path))
            return;
        if (!WindowsShell.ShowFileProperties(path))
            _speech.Speak(
                // Translators: Spoken when the Windows properties window for the current file could not be shown.
                Tr("Could not open file properties."),
                // Translators: The short wording spoken when Windows could not show a file's properties.
                Tr("Open failed."));
    }

    private void OpenedFiles()
    {
        if (_spotify.IsActive)
        {
            ShowSpotifyTracks(_spotify.CurrentIndex);
            return;
        }
        if (_radio.IsActive)
        {
            _radio.ShowOpenedFiles();
            return;
        }
        if (!_guard.RequireAnyFile())
            return;
        ShowOpenedFiles(_player.CurrentIndex);
    }

    private void ShowSpotifyTracks(int selected)
    {
        while (_spotify.IsActive)
        {
            var tracks = _spotify.CurrentQueue;
            if (tracks.Count == 0) return;
            var request = _view.Dialogs.ChooseOpenedFile(tracks.Count,
                index => index >= 0 && index < tracks.Count
                    ? tracks[index].Title : string.Empty,
                selected);
            if (request is null) return;
            if (request.Value.Action == LunaPlayer.UI.Playlists.OpenedFilesAction.Jump)
            {
                _spotify.GoToIndex(request.Value.SelectedIndex);
                return;
            }
            var duration = tracks.Sum(track => (long)Math.Max(0, track.DurationMs)) / 1000.0;
            // Translators: Title of the information dialog for the current Spotify queue.
            _view.Dialogs.ShowTextInfo(Tr("Playlist Info"), string.Join(Environment.NewLine,
                // Translators: Spotify playlist summary. {count} is the number of tracks in the queue.
                TrFormat("Number of tracks: {count}", tracks.Count),
                // Translators: Line in the Spotify queue information dialog. {value} is the formatted combined playing time.
                TrFormat("Total duration: {value}", PlaybackTimeFormatter.Format(duration))));
            selected = request.Value.SelectedIndex;
        }
    }

    /// <summary>Shows the list of loaded files. Written as a step rather than a loop because asking for the
    /// playlist summary no longer blocks: the scan runs while the window stays live, and this list comes back
    /// only once it has finished.</summary>
    private void ShowOpenedFiles(int selected)
    {
        if (_player.Count == 0)
            return;
        // The names are not worked out here: the list asks for the ones it is about to draw.
        var files = _player.Items.ToArray();
        var request = _view.Dialogs.ChooseOpenedFile(files.Length,
            index => index >= 0 && index < files.Length ? _player.DisplayName(files[index]) : string.Empty,
            selected);
        if (request is null)
            return;
        if (request.Value.Action == LunaPlayer.UI.Playlists.OpenedFilesAction.Jump)
        {
            _player.GoToItem(files[request.Value.SelectedIndex]);
            return;
        }
        ShowPlaylistInformation(() => ShowOpenedFiles(request.Value.SelectedIndex));
    }

    private void ShowPlaylistInformation(Action completed)
    {
        var prompt = new ProgressPrompt(
            // Translators: Title of the progress window shown while details of every file in the playlist are being read.
            Tr("Loading playlist info"),
            // Translators: First message in the progress window, shown before the first file has been read.
            Tr("Preparing..."),
            // Translators: Progress message naming the file being read right now. {name} is the file name.
            update => TrFormat("Reading: {name}", update.Name));
        // Everything the scan needs is read here, on the UI thread. mpv's properties belong to it, and the
        // scan itself only gets plain numbers to work from.
        var files = _player.Items.ToArray();
        var currentPath = _player.CurrentPath;
        var currentIndex = _player.CurrentIndex;
        var duration = _player.Duration;
        var elapsed = _player.Elapsed;
        var remaining = _player.Remaining;
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (report, token) => _playlistInfo.Build(
                files, currentPath, currentIndex, duration, elapsed, remaining, report, token),
            totals =>
            {
                // Translators: Title of the window listing details of every file in the playlist.
                _view.Dialogs.ShowTextInfo(Tr("Playlist Info"), Describe(totals));
                completed();
            });
    }

    /// <summary>Turns the scan's numbers into the text shown to the user. On the UI thread, because the
    /// translation lookup is a wxWidgets object and the scan runs on a worker thread.</summary>
    private static string Describe(PlaylistTotals totals) => string.Join(Environment.NewLine,
        // Translators: The playlist summary. {count} is how many files are loaded.
        TrFormat("Number of files: {count}", totals.FileCount),
        // Translators: The playlist summary. {value} is a size such as "12.5 MB".
        TrFormat("Total size: {value}", FormatSize(totals.TotalBytes)),
        // Translators: The playlist summary. {value} is a duration as hours:minutes:seconds.
        TrFormat("Total duration: {value}", PlaybackTimeFormatter.Format(totals.TotalDuration)),
        // Translators: The playlist summary: how much of the whole playlist has already played.
        TrFormat("Elapsed: {value}", PlaybackTimeFormatter.Format(totals.Elapsed)),
        // Translators: The playlist summary: how much of the whole playlist is left to play.
        TrFormat("Remaining: {value}", PlaybackTimeFormatter.Format(totals.Remaining)));

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.##} {units[unit]}";
    }

    private void AnnounceFileInfo()
    {
        if (!_spotify.IsActive && !_guard.RequireFile(out _))
            return;

        var now = Environment.TickCount64;
        if (now - _fileInfoLastPress > FileInfoResetMilliseconds)
            _fileInfoPressCount = 0;
        _fileInfoLastPress = now;
        _fileInfoPressCount++;

        switch (_fileInfoPressCount)
        {
            case 1:
                if (_spotify.IsActive)
                {
                    if (_spotify.CurrentTitle is { } spotifyTitle) _speech.Speak(spotifyTitle, spotifyTitle);
                    break;
                }
                var path = _player.CurrentPath!;
                var name = _player.CurrentName ?? MediaLibrary.DisplayName(path);
                _speech.Speak(name, name);
                break;
            case 2:
                var link = _spotify.IsActive ? _spotify.CurrentLink : _player.CurrentPath;
                if (link is not null) _speech.Speak(link, link);
                break;
            default:
                var location = _spotify.IsActive ? _spotify.CurrentLink : _player.CurrentPath;
                if (location is null) break;
                var copied = _clipboard.SetText(location);
                _speech.Speak(
                    copied
                        // Translators: Spoken once the Spotify track link has been copied to the clipboard.
                        ? (_spotify.IsActive
                            // Translators: Spoken confirmation after copying a Spotify track's public web address.
                            ? Tr("Track link copied to clipboard.")
                            // Translators: Spoken confirmation after copying the current file's location.
                            : Tr("File path copied to clipboard."))
                        // Translators: Spoken when the full location of the current file could not be copied to the clipboard.
                        : Tr("Unable to copy to clipboard."),
                    copied
                        // Translators: Short announcement after copying the current file path succeeds.
                        ? Tr("Copied.")
                        // Translators: Short announcement after copying the current file path fails.
                        : Tr("Copy failed."));
                _fileInfoPressCount = 0;
                break;
        }
    }

    private void AnnounceMetadata()
    {
        if (!_spotify.IsActive && !_guard.RequireFile(out _)) return;
        var metadata = _player.CurrentMetadata;
        var display = _spotify.IsActive && _spotify.CurrentTrack is { } track
            ? metadata.Display with { Title = track.Name, Artist = track.Artists }
            : metadata.Display;
        var lines = new List<string>();
        void Add(string value, Func<string, string> format)
        {
            if (!string.IsNullOrWhiteSpace(value)) lines.Add(format(value));
        }
        if (_player.InRadioSession)
        {
            var station = metadata.Value("icy-name", "service_name");
            Add(station.Length > 0 ? station : _player.CurrentDisplayName ?? string.Empty,
                // Translators: Line spoken by Ctrl+Alt+I identifying the radio station. {station} is its name from the stream or radio directory.
                value => TrFormat("Station: {station}", value));
        }
        Add(display.Title,
            // Translators: Line spoken by Ctrl+Alt+I. {title} is the current file or radio programme title.
            value => TrFormat("Title: {title}", value));
        Add(display.Artist,
            // Translators: Line spoken by Ctrl+Alt+I. {artist} is the performer named by the current media or radio broadcast.
            value => TrFormat("Artist: {artist}", value));
        Add(display.Album,
            // Translators: Line spoken by Ctrl+Alt+I. {album} is the album named in the current media's metadata.
            value => TrFormat("Album: {album}", value));
        Add(metadata.Value("album_artist", "albumartist"),
            // Translators: Line spoken by Ctrl+Alt+I. {artist} is the album artist, which can differ from the current track's performer.
            value => TrFormat("Album artist: {artist}", value));
        Add(metadata.Value("genre", "icy-genre"),
            // Translators: Line spoken by Ctrl+Alt+I. {genre} is the genre supplied by the file or radio stream.
            value => TrFormat("Genre: {genre}", value));
        Add(metadata.Value("description", "comment", "icy-description"),
            // Translators: Line spoken by Ctrl+Alt+I. {description} is a description or comment supplied by the current media or radio station.
            value => TrFormat("Description: {description}", value));
        Add(metadata.Value("date", "year"),
            // Translators: Line spoken by Ctrl+Alt+I. {date} is the release or recording date exactly as supplied by the media.
            value => TrFormat("Date: {date}", value));
        Add(metadata.Value("track", "tracknumber"),
            // Translators: Line spoken by Ctrl+Alt+I. {number} is the track's number within its album.
            value => TrFormat("Track number: {number}", value));
        Add(metadata.Value("disc", "discnumber"),
            // Translators: Line spoken by Ctrl+Alt+I. {number} is the disc number within a multi-disc album.
            value => TrFormat("Disc number: {number}", value));
        Add(metadata.Value("composer"),
            // Translators: Line spoken by Ctrl+Alt+I. {composer} is the composer named in the media's metadata.
            value => TrFormat("Composer: {composer}", value));
        Add(metadata.Value("language"),
            // Translators: Line spoken by Ctrl+Alt+I. {language} is the language name or code supplied by the current media.
            value => TrFormat("Language: {language}", value));
        Add(metadata.Value("publisher", "organization"),
            // Translators: Line spoken by Ctrl+Alt+I. {publisher} is the publisher or organization named by the current media.
            value => TrFormat("Publisher: {publisher}", value));
        Add(metadata.Value("copyright"),
            // Translators: Line spoken by Ctrl+Alt+I. {copyright} is the media's copyright notice.
            value => TrFormat("Copyright: {copyright}", value));
        Add(metadata.Value("icy-url", "website", "purl", "url"),
            // Translators: Line spoken by Ctrl+Alt+I. {url} is the website supplied by the media or radio station.
            value => TrFormat("Website: {url}", value));
        Add(metadata.Value("icy-br"),
            // Translators: Line spoken by Ctrl+Alt+I. {bitrate} is the radio stream's advertised bitrate in kilobits per second. Keep kbps as the bitrate unit.
            value => TrFormat("Bitrate: {bitrate} kbps", value));
        _speech.SpeakText(lines.Count > 0 ? string.Join(Environment.NewLine, lines)
            // Translators: Spoken by Ctrl+Alt+I when the current media provides no metadata to announce.
            : Tr("No metadata is available."));
    }

    private void AnnounceArtist()
    {
        if (!_spotify.IsActive && !_guard.RequireFile(out _)) return;
        var artist = _spotify.IsActive ? _spotify.CurrentTrack?.Artists : _player.CurrentArtist;
        if (!string.IsNullOrWhiteSpace(artist))
            _speech.Speak(artist, artist);
        else
            // Translators: Spoken by Shift+I when the current file or radio broadcast provides no artist information.
            _speech.SpeakText(Tr("Unknown artist."));
    }

    private void AnnounceTitle()
    {
        if (_spotify.IsActive)
        {
            if (_spotify.CurrentTitle is { } spotifyTitle) _speech.Speak(spotifyTitle, spotifyTitle);
            return;
        }
        if (!_guard.RequireFile(out _))
            return;
        var currentTitle = _player.InRadioSession ? _player.CurrentBroadcast.Title : _player.CurrentTitle;
        if (currentTitle is { Length: > 0 } title)
            _speech.Speak(title, title);
        else if (_player.InRadioSession)
            _speech.Speak(
                // Translators: Spoken by I when the live radio station supplies no programme title.
                Tr("The current radio broadcast has no title."),
                // Translators: Short announcement by I when the live radio station supplies no programme title.
                Tr("No title."));
        else
            _speech.Speak(
                // Translators: Spoken when the current media file contains no title metadata.
                Tr("The current file has no title."),
                // Translators: Short announcement when the current media file contains no title metadata.
                Tr("No title."));
    }
}
