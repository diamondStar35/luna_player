using System.Net;
using System.Text.RegularExpressions;
using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Browsing;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void Details(Row row)
    {
        if (row.Kind is RowKind.Song or RowKind.Bookmark)
        { _songs.Execute(row.Reference(_server).Address, SongAction.Details); return; }
        var client = _connections.Client(_server);
        var columns = Columns.For(_location, [row], _query.Length > 0);
        var summary = string.Join(Environment.NewLine, columns.Select(column => Columns.Heading(column) + ": " + Columns.Cell(row, column)));
        Run(async token =>
        {
            if (row.Kind == RowKind.Artist)
            {
                var info = await client.Library.GetArtistInfoAsync(row.Id, token: token);
                return (Text: Plain(info.Biography), Link: info.LastFmUrl);
            }
            if (row.Kind == RowKind.Album)
            {
                var info = await client.Library.GetAlbumInfoAsync(row.Id, token: token);
                return (Text: Plain(info.Notes), Link: info.LastFmUrl);
            }
            return (Text: summary, Link: "");
        }, info =>
        {
            if (!_visible) return;
            var form = new Form(_window, row.Name, closeOnly: true);
            var field = form.Text(Words.Details, info.Text, multiline: true); field.Editable = false;
            field.MinSize = new Size(500, 300);
            if (Uri.TryCreate(info.Link, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http")
                form.Button(Words.InformationLink, () => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }));
            Keep(form, form); form.Show();
        });
    }
    private static string Plain(string text) => WebUtility.HtmlDecode(Regex.Replace(text, "<[^>]*>", " "));
    private void Related(Command command, Row[] rows)
    {
        if (rows.Length == 0) return;
        var server = _server; var client = _connections.Client(server); var profile = _connections.Profile(server);
        var libraries = SelectedLibraries(); var catalog = _connections.Catalog(server);
        async Task<Row[]> Read(CancellationToken token)
        {
            if (command == Command.SimilarArtists)
                return (await client.Library.GetArtistInfoAsync(rows[0].Id, token: token)).SimilarArtists.Select(Row.From).ToArray();
            if (command == Command.Top)
            {
                var byId = (await client.DiscoverAsync(token: token)).Extension("topSongsByArtistId") == Support.Supported;
                return (await client.Library.GetTopSongsAsync(byId ? rows[0].Id : rows[0].Name, byId: byId, token: token)).Select(Row.From).ToArray();
            }
            if (command == Command.Sonic) return (await client.Library.GetSonicSimilarTracksAsync(rows[0].Id, token: token)).Where(x => x.Song is not null).Select(x => Row.From(x.Song!)).ToArray();
            if (command == Command.Path && rows.Length == 2) return (await client.Library.FindSonicPathAsync(rows[0].Id, rows[1].Id, token: token)).Where(x => x.Song is not null).Select(x => Row.From(x.Song!)).ToArray();
            using var ai = CreateAudioMuse(profile);
            var seed = rows[0];
            if (seed.Kind == RowKind.Album)
            {
                var album = await client.Library.GetAlbumAsync(seed.Id, token);
                if (album.Songs.Length == 0) return [];
                seed = Row.From(album.Songs[0]);
            }
            var result = await new LunaPlayer.Subsonic.AudioMuse.Mixes(client, ai).SimilarAsync(seed.Id,
                rows[0].Kind == RowKind.Artist ? LunaPlayer.Subsonic.AudioMuse.SeedKind.Artist : LunaPlayer.Subsonic.AudioMuse.SeedKind.Song, token: token);
            return await ResolveAudioMuse(client, result, token);
        }
        async Task<Row[]> ScopedRead(CancellationToken token) => await catalog.FilterAsync(await Read(token), libraries, token);
        Run(ScopedRead, found => { if (_visible && _server == server) ShowDerived(found, ScopedRead,
            command == Command.SimilarArtists ? Category.Artists : Category.Songs); }, command: command);
    }
    private void Scan()
    {
        var server = _server; var client = _connections.Client(server);
        Run(token => client.Scanning.GetStatusAsync(token), status =>
        {
            if (_server != server) return;
            if (!status.Scanning) { Info(Words.ScanFinished); return; }
            var form = new Form(_window, Words.ScanStatus, closeOnly: true);
            var text = form.Text(Words.Status, Words.ScanInProgress, multiline: true); text.Editable = false;
            Keep(form, form); form.Show();
            Run(async token =>
            {
                await foreach (var current in client.Scanning.WatchAsync(token: token))
                    form.Post(() =>
                    {
                        text.ChangeValue(current.Scanning ? Words.ScanInProgress : Words.ScanFinished);
                        if (!current.Scanning) _speak(Words.ScanFinished);
                    });
                return true;
            }, _ => { }, form.Token, form.Error, Command.ScanStatus);
        }, command: Command.ScanStatus);
    }
    private void Bookmark()
    {
        var reference = LunaPlayer.Subsonic.Playback.Reference.Parse(_player.CurrentPath);
        if (reference?.ServerId != _server || !_player.IsLoaded) { Info(Words.CurrentOnly); return; }
        _songs.Bookmark(reference.ServerId, reference.Id, _player.SourceElapsed ?? 0);
    }
    private void SaveQueue()
    {
        var server = _server; var client = _connections.Client(server);
        var items = _player.Items.Where(x => x.ServerMedia is { Kind: LunaPlayer.Subsonic.Playback.MediaKind.Song } source && source.ServerId == server).ToArray();
        if (items.Length != _player.Items.Count && !Confirm(Words.QueueSubset)) return;
        var index = Array.FindIndex(items, x => x.Id == _player.CurrentItemId);
        var position = index >= 0 ? (long)((_player.SourceElapsed ?? 0) * 1000) : 0;
        Change(async token =>
        {
            var byIndex = (await client.DiscoverAsync(token: token)).Extension("indexBasedQueue") == Support.Supported;
            await client.Listening.SaveQueueAsync(items.Select(x => x.ServerMedia!.Id).ToArray(),
                current: items.Length > 0 ? items[Math.Max(0, index)].ServerMedia!.Id : null, positionMs: position,
                currentIndex: items.Length > 0 ? Math.Max(0, index) : null, byIndex: byIndex, token: token);
        });
    }
}
