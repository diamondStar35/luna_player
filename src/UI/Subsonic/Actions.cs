using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Listening;
using LunaPlayer.Subsonic.Media;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private int _playRevision;
    private string _username = "";
    private Capabilities? _capabilities;
    private LunaPlayer.Subsonic.Models.User? _user;
    private void Execute(Command command)
    {
        if (_disposed || _server == Guid.Empty || !Allows(command, Context(Selected))) return;
        var server = _server; var rows = Selected; var client = _connections.Client(server);
        switch (command)
        {
            case Command.Rate: Rate(rows); break;
            case Command.Play: case Command.Append:
                if (rows.Length == 0 || rows.Any(x => !x.Playable && x.Kind is not (RowKind.Artist or RowKind.Album or RowKind.Playlist))) return;
                var revision = ++_playRevision;
                var scope = SelectedLibraries();
                Run(token => _connections.Catalog(server).ExpandAsync(rows, token, scope), songs =>
                {
                    if (command == Command.Play && revision != _playRevision) return;
                    if (songs.Length == 0) { Reload(); Info(Words.EmptyExpansion); return; }
                    _connections.Metadata.Observe(server, songs.Where(song => song.Song is not null).Select(song => song.Song!));
                    var added = _player.InsertMedia(songs.Select(x => (x.Reference(server).Address, x.Tags)).ToArray(), command == Command.Play,
                        rows.Length == 1 && rows[0].Kind == RowKind.Bookmark ? rows[0].Position / 1000d : null);
                    if (command == Command.Append) _speak(added > 0 ? Words.CurrentPlaylistItemsAdded(added) : Words.AlreadyInPlaylist);
                }, command: command); break;
            case Command.Direct: case Command.Download: case Command.Enqueue: Downloads(command, rows, server); break;
            case Command.Favorite: case Command.Unfavorite:
                Star(rows, command == Command.Favorite); break;
            case Command.CreatePlaylist: Playlist(null, []); break;
            case Command.SendPlaylist: SendPlaylist(); break;
            case Command.RemovePlaylistItems: RemovePlaylistItems(rows); break;
            case Command.EditPlaylist: if (rows.Length == 1 && rows[0].Kind == RowKind.Playlist && Owns(rows[0])) EditPlaylist(rows[0]); break;
            case Command.AddPlaylist: AddToPlaylist(rows, server); break;
            case Command.Rename: if (rows.Length == 1) Rename(rows[0]); break;
            case Command.Remove: if (rows.Length == 1) Remove(rows[0]); break;
            case Command.Details: if (rows.Length == 1) Details(rows[0]); break;
            case Command.Lyrics: if (rows.Length == 1) Lyrics(rows[0]); break;
            case Command.Top: case Command.Similar: case Command.SimilarArtists: case Command.Sonic: case Command.Path: Related(command, rows); break;
            case Command.Bookmark: Bookmark(); break;
            case Command.SaveQueue: SaveQueue(); break;
            case Command.LoadQueue:
                Run(async token => await client.Listening.GetQueueAsync((await client.DiscoverAsync(token: token)).Extension("indexBasedQueue") == Support.Supported, token),
                    queue => { _connections.Metadata.Observe(server, queue.Entries); _player.ApplyServerQueue(server, queue, play: true); _speak(Words.QueueLoaded); }, command: command); break;
            case Command.Scan: case Command.FullScan:
                if (_user?.AdminRole == false) return;
                Change(async token => { await client.Scanning.StartAsync(command == Command.FullScan, token); }, command: command); break;
            case Command.ScanStatus: Scan(); break;
            case Command.Info:
                Run(token => client.DiscoverAsync(true, token), info => ShowText(Words.ServerInfo,
                    info.Server.Type + " " + info.Server.ServerVersion + Environment.NewLine + string.Join(Environment.NewLine, info.Extensions.Select(x => x.Name + " " + string.Join(", ", x.Versions))))); break;
            case Command.NewRadio: Radio(null); break;
            case Command.Subscribe:
                var form = new Form(_window, Words.Subscribe); var feed = form.Text(Words.Feed);
                form.SaveAsync = async token => { var address = feed.Value; await Task.Run(() => client.Podcasts.SubscribeAsync(address, token)); return true; };
                SavedForm(form); break;
            case Command.RefreshPodcasts: Change(client.Podcasts.RefreshAsync, command: command); break;
            case Command.EpisodeDownload: if (rows.Length == 1) Change(token => client.Podcasts.DownloadEpisodeAsync(rows[0].Id, token), command: command); break;
            case Command.EpisodeDelete: if (rows.Length == 1 && Confirm(Words.ConfirmEpisode)) Change(token => client.Podcasts.DeleteEpisodeAsync(rows[0].Id, token), command: command); break;
            case Command.AiSearch: case Command.AiGenerate: case Command.Alchemy: AudioMuse(command, rows); break;
        }
    }
    private void Change(Func<CancellationToken, Task> action, Command? command = null)
    {
        Run(async token => { await action(token).ConfigureAwait(false); return true; }, _ =>
        {
            Reload();
            var message = command switch
            {
                Command.Scan or Command.FullScan => Words.ScanStarted,
                Command.RefreshPodcasts => Words.PodcastRefreshStarted,
                Command.EpisodeDownload => Words.EpisodeDownloadStarted,
                Command.EpisodeDelete => Words.EpisodeDeleted,
                _ => Words.Saved
            };
            _speak(message);
        }, command: command);
    }
    private void SavedForm(Form form, Func<string>? confirmation = null)
    { Keep(form, form); form.Closed += accepted => { if (accepted) { Reload(); _speak(confirmation?.Invoke() ?? Words.Saved); } }; form.Show(); }
    private void ShowText(string title, string text)
    { var form = new Form(_window, title, closeOnly: true); var field = form.Text(Words.Details, text, multiline: true); field.Editable = false; field.MinSize = new Size(500, 330); Keep(form, form); form.Show(); }
}
