using LunaPlayer.Subsonic.Browsing;
using WxSharp;
using RemotePlaylist = LunaPlayer.Subsonic.Models.Playlist;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void EditPlaylist(Row row)
    { var server = _server; var client = _connections.Client(server); Run(token => client.Playlists.GetAsync(row.Id, token), value => { if (_server == server) Playlist(value, value.Entries.Select(Row.From).ToArray()); }); }
    private void Playlist(RemotePlaylist? original, Row[] initial, string? defaultName = null)
    {
        var server = _server; var client = _connections.Client(server);
        var form = new Form(_window, Words.Playlists); form.Window.MinSize = new Size(760, 640);
        var name = form.Text(Words.Name, original?.Name ?? defaultName ?? "");
        var description = form.Text(Words.Description, original?.Comment ?? "");
        var isPublic = form.Check(Words.Public, original?.Public ?? false);
        var entries = initial.ToList();
        form.HelpText = Words.PlaylistHelp;
        form.HasChanges = () => name.Value != (original?.Name ?? "") || description.Value != (original?.Comment ?? "")
            || isPublic.Checked != (original?.Public ?? false) || !entries.Select(x => x.Id).SequenceEqual(initial.Select(x => x.Id));
        var list = Form.Field(form.Panel, form.Body, Words.Songs,
            () => new ListCtrl(form.Panel, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual), proportion: 1);
        for (var column = 0; column < Columns.Tracks.Length; column++)
            list.InsertColumn(column, Columns.Heading(Columns.Tracks[column]), Columns.Width(Columns.Tracks[column]));
        list.MinSize = new Size(600, 230);
        string Cell(Row row, int column) => Columns.Cell(row, Columns.Tracks[column]);
        list.SetDataSource(entries, Cell);
        var actions = new BoxSizer(Orientation.Horizontal); form.Body.Add(actions, flags: SizerFlags.All, border: 5);
        Button(form.Panel, actions, Words.AddSelection, () => PickSongs(form.Window,
            selected => { entries.AddRange(selected); list.UpdateItemCount(); }, count => name.Value.Trim().Length == 0
                ? Words.NewDraftItemsAdded(count) : Words.DraftItemsAdded(count, name.Value.Trim())));
        Button? remove = null, up = null, down = null;
        void Remove()
        {
            var selected = list.GetSelectedIndices(); if (selected.Length == 0) return;
            var target = (int)selected.Min();
            foreach (var index in selected) list.SetSelected(index, false);
            foreach (var index in selected.OrderDescending()) entries.RemoveAt((int)index);
            list.UpdateItemCount();
            if (entries.Count > 0)
            {
                list.RefreshItems(0, entries.Count - 1); target = Math.Min(target, entries.Count - 1);
                list.SetSelected(target); list.SetFocused(target); list.EnsureVisible(target);
            }
            Update();
        }
        void Move(int direction)
        {
            var indices = list.GetSelectedIndices().Select(index => (int)index).ToArray();
            var moved = Reordering.Move(entries, indices, direction, (int)list.FocusedIndex);
            if (moved is null) return;
            foreach (var index in indices) list.SetSelected(index, false);
            list.RefreshItems(0, entries.Count - 1);
            foreach (var index in moved.Selected) list.SetSelected(index);
            if (moved.Focused >= 0) { list.SetFocused(moved.Focused); list.EnsureVisible(moved.Focused); }
            Update();
        }
        remove = Button(form.Panel, actions, Words.Remove, Remove);
        up = Button(form.Panel, actions, Words.Up, () => Move(-1)); down = Button(form.Panel, actions, Words.Down, () => Move(1));
        void Update()
        {
            var indices = list.GetSelectedIndices(); remove?.Show(indices.Length > 0);
            up?.Show(indices.Length > 0 && !indices.Contains(0)); down?.Show(indices.Length > 0 && !indices.Contains(entries.Count - 1)); form.Window.Layout();
        }
        list.ItemSelected += (_, _) => Update(); list.ItemDeselected += (_, _) => Update();
        list.Bind(WxEvents.CharHook, (_, key) =>
        { if (key.Code == Key.Delete) Remove(); else if (key.Shift && key.Code == Key.Up) Move(-1); else if (key.Shift && key.Code == Key.Down) Move(1); else key.Skip(); });
        var id = original?.Id; var changed = original?.Changed;
        var savedName = original?.Name ?? "";
        var conflict = Words.PlaylistChanged;
        form.SaveAsync = async token =>
        {
            var title = name.Value.Trim(); if (title.Length == 0) throw new ArgumentException(Words.Name);
            var comment = description.Value; var publicPlaylist = isPublic.Checked; var songs = entries.Select(x => x.Id).ToArray();
            await Task.Run(async () =>
            {
                if (id is not null)
                {
                    var latest = await client.Playlists.GetAsync(id, token);
                    if (changed is not null && latest.Changed != changed) throw new InvalidOperationException(conflict);
                    await client.Playlists.ReplaceAsync(id, songs, token);
                }
                else id = (await client.Playlists.CreateAsync(title, songs, token)).Id;
                // Read the new revision even if the following metadata write fails; retrying must not duplicate entries.
                changed = (await client.Playlists.GetAsync(id, token)).Changed;
                await client.Playlists.UpdateAsync(id, title, comment, publicPlaylist, token: token);
            }, token);
            savedName = title;
            return true;
        };
        Update(); SavedForm(form, () => Words.ServerPlaylistSaved(savedName));
    }
    private void PickSongs(Window owner, Action<Row[]> accepted, Func<int, string> confirmation)
    {
        var server = _server; var client = _connections.Client(server);
        var form = new Form(owner, Words.Songs, Words.AddSelectedSongs); var search = form.Text(Words.Search);
        var list = Form.Field(form.Panel, form.Body, Words.Songs,
            () => new ListCtrl(form.Panel, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual), proportion: 1);
        for (var column = 0; column < Columns.Tracks.Length; column++)
            list.InsertColumn(column, Columns.Heading(Columns.Tracks[column]), Columns.Width(Columns.Tracks[column]));
        list.MinSize = new Size(500, 250);
        Row[] results = [];
        CancellationTokenSource? request = null; IDisposable? timer = null; var generation = 0;
        void Search()
        {
            if (form.Token.IsCancellationRequested) return;
            request?.Cancel(); request?.Dispose(); request = CancellationTokenSource.CreateLinkedTokenSource(form.Token);
            var token = request.Token; var revision = ++generation; var query = search.Value.Trim();
            var libraries = _connections.Profile(server).LibraryIds;
            Run(token => _connections.Catalog(server).ReadAsync(new(Category.Songs), query, 3, null, null, token, libraries), rows =>
            { if (form.Token.IsCancellationRequested || revision != generation) return; results = rows; list.SetDataSource(results, (row, col) => Columns.Cell(row, Columns.Tracks[col])); }, token, form.Error);
        }
        search.TextChanged += (_, _) => { timer?.Dispose(); timer = _dispatcher.Repeat(TimeSpan.FromMilliseconds(Math.Max(1, _connections.Profile(server).Timing.SearchDelayMilliseconds)), () => { timer?.Dispose(); Search(); }); };
        var addedCount = 0;
        form.SaveAsync = _ =>
        {
            var selected = list.GetSelectedIndices().Select(index => results[(int)index]).ToArray();
            if (selected.Length == 0) throw new ArgumentException(Words.ChooseSongs);
            accepted(selected); addedCount = selected.Length; return Task.FromResult(true);
        };
        form.Closed += saved => { if (saved) _speak(confirmation(addedCount)); };
        form.Disposing += () => { timer?.Dispose(); request?.Cancel(); request?.Dispose(); request = null; };
        Keep(form, form); form.Show(); Search();
    }
    private void SendPlaylist()
    {
        var server = _server;
        var eligible = _player.Items.Where(item => item.ServerMedia is { Kind: LunaPlayer.Subsonic.Playback.MediaKind.Song } reference
            && reference.ServerId == server).ToArray();
        if (eligible.Length == 0 || eligible.Length != _player.Items.Count && !Confirm(Words.PlaylistSubset)) return;
        var items = eligible.DistinctBy(item => item.ServerMedia!.Id, StringComparer.Ordinal).ToArray();
        Run(async token =>
        {
            var rows = new List<Row>(); var client = _connections.Client(server);
            foreach (var item in items)
            {
                var id = item.ServerMedia!.Id;
                rows.Add(Row.From(_connections.Metadata.Get(server, id) ?? await client.Library.GetSongAsync(id, token).ConfigureAwait(false)));
            }
            return rows.ToArray();
        }, rows => { if (_server == server) Playlist(null, rows, Words.CurrentPlaylist); });
    }
    private void RemovePlaylistItems(Row[] rows)
    {
        if (_location.Container != RowKind.Playlist || _location.Id is not { } id || !Confirm(Words.ConfirmRemoveEntries)) return;
        var server = _server; var client = _connections.Client(server);
        var indexes = rows.Where(row => row.PlaylistIndex.HasValue).Select(row => row.PlaylistIndex!.Value).Distinct().ToArray();
        var displayed = _rows.Select(row => row.Id).ToArray();
        var conflict = Words.PlaylistChanged;
        Change(async token =>
        {
            var current = await client.Playlists.GetAsync(id, token).ConfigureAwait(false);
            if (!current.Entries.Select(entry => entry.Id).SequenceEqual(displayed)) throw new InvalidOperationException(conflict);
            await client.Playlists.UpdateAsync(id, removeIndexes: indexes, token: token).ConfigureAwait(false);
        });
    }
    private void AddToPlaylist(Row[] rows, Guid server)
    {
        if (rows.Length == 0) return; var client = _connections.Client(server);
        Run(async token => (Playlists: await client.Playlists.GetAllAsync(token: token), Songs: await new Catalog(client).ExpandAsync(rows, token)), result =>
        {
            if (_server != server) return;
            var playlists = result.Playlists.Where(x => x.Owner == _username || _user?.AdminRole == true).ToArray();
            if (playlists.Length == 0) { Playlist(null, result.Songs); return; }
            var form = new Form(_window, Words.SelectPlaylist);
            var choice = form.Choice(Words.Playlists, playlists.Select(x => x.Name));
            var destination = "";
            form.Button(Words.CreatePlaylist, () => { form.Complete(false); Playlist(null, result.Songs); });
            form.SaveAsync = async token =>
            {
                var playlist = playlists[choice.SelectedIndex];
                await Task.Run(() => client.Playlists.UpdateAsync(playlist.Id, add: result.Songs.Select(x => x.Id), token: token), token);
                destination = playlist.Name; return true;
            };
            SavedForm(form, () => Words.ServerPlaylistItemsAdded(result.Songs.Length, destination));
        });
    }
}
