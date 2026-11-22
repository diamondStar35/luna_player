using LunaPlayer.Subsonic.Browsing;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void Rename(Row row)
    {
        var client = _connections.Client(_server);
        if (row.Kind == RowKind.Radio) { Radio(row); return; }
        if (row.Kind == RowKind.Bookmark)
        {
            var form = new Form(_window, Words.Bookmark); var comment = form.Text(Words.Comment, row.Description);
            form.SaveAsync = token => { var text = comment.Value; return Task.Run(async () => { await client.Listening.CreateBookmarkAsync(row.Id, row.Position, text, token); return true; }, token); };
            SavedForm(form); return;
        }
        if (row.Kind != RowKind.Playlist || !Owns(row)) return;
        var edit = new Form(_window, Words.Rename); var name = edit.Text(Words.Name, row.Name);
        edit.SaveAsync = token => { var text = name.Value; return Task.Run(async () => { await client.Playlists.UpdateAsync(row.Id, name: text, token: token); return true; }, token); };
        SavedForm(edit);
    }
    private void Remove(Row row)
    {
        var client = _connections.Client(_server);
        switch (row.Kind)
        {
            case RowKind.Playlist when Owns(row): if (Confirm(Words.RemovePlaylist)) Change(token => client.Playlists.DeleteAsync(row.Id, token)); break;
            case RowKind.Bookmark: if (Confirm(Words.RemoveBookmark)) Change(token => client.Listening.DeleteBookmarkAsync(row.Id, token)); break;
            case RowKind.Radio when _user?.AdminRole != false: if (Confirm(Words.RemoveRadio)) Change(token => client.Radio.DeleteAsync(row.Id, token)); break;
            case RowKind.PodcastChannel when _user?.PodcastRole != false: if (Confirm(Words.RemovePodcast)) Change(token => client.Podcasts.UnsubscribeAsync(row.Id, token)); break;
        }
    }
    private void Radio(Row? row)
    {
        if (_user?.AdminRole == false) return;
        var server = _server; var client = _connections.Client(server);
        if (row is null) Show(null);
        else Run(token => client.Radio.GetAllAsync(token), stations => Show(stations.FirstOrDefault(x => x.Id == row.Id)));
        void Show(LunaPlayer.Subsonic.Models.RadioStation? station)
        {
            if (_server != server) return;
            var form = new Form(_window, Words.Radio); var name = form.Text(Words.Name, station?.Name ?? "");
            var stream = form.Text(Words.StreamAddress, station?.StreamUrl ?? ""); var website = form.Text(Words.Homepage, station?.HomePageUrl ?? "");
            form.SaveAsync = token =>
            {
                var title = name.Value; var address = stream.Value; var homepage = website.Value.Trim().Length == 0 ? null : website.Value;
                return Task.Run(async () =>
                {
                    if (station is null) await client.Radio.CreateAsync(address, title, homepage, token);
                    else await client.Radio.UpdateAsync(station.Id, address, title, homepage, token);
                    return true;
                }, token);
            };
            SavedForm(form);
        }
    }
}
