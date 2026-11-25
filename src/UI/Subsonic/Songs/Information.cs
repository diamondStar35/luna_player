using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class SongActions
{
    private void Details(Guid server, Row row)
    {
        var form = new Form(_owner, row.Name, closeOnly: true);
        var summary = string.Join(Environment.NewLine, Columns.Tracks.Select(column => Columns.Heading(column) + ": " + Columns.Cell(row, column)));
        var current = LunaPlayer.Subsonic.Playback.Reference.Parse(_player.CurrentPath);
        if (current?.ServerId == server && current.Id == row.Id && _player.CurrentStreamFormat is { } format)
        {
            var stream = StreamPresentation.Describe(format);
            if (stream.Length > 0) summary += Environment.NewLine + stream;
        }
        form.Text(Words.Details, summary, multiline: true).Editable = false;
        Keep(form); form.Show();
    }

    internal void Bookmark(Guid server, string id, double position)
    {
        var form = new Form(_owner, Words.Bookmark);
        var comment = form.Text(Words.Comment); var value = "";
        form.SaveAsync = _ => { value = comment.Value; return Task.FromResult(true); };
        form.Closed += accepted =>
        {
            if (!accepted) return;
            Run(async token =>
            {
                await _connections.Client(server).Listening.CreateBookmarkAsync(id, (long)(Math.Max(0, position) * 1000), value, token).ConfigureAwait(false);
                return true;
            }, _ => { _dialogs.MusicServerContentChanged(server); _speak(Words.BookmarkAdded); });
        };
        Keep(form); form.Show();
    }
}
