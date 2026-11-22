using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Models;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class SongActions
{
    internal void Lyrics(Guid server, Row row)
    {
        var client = _connections.Client(server);
        Run(async token =>
        {
            try
            {
                var versions = await client.Listening.GetLyricsBySongIdAsync(row.Id, token).ConfigureAwait(false);
                if (versions.Any(version => version.Lines.Length > 0)) return versions.Where(version => version.Lines.Length > 0).ToArray();
            }
            catch (RequestException error) when (error.Kind == FailureKind.Unsupported)
            { /* Plain lyrics remain available on older servers. */ }
            var plain = (await client.Listening.GetLyricsAsync(row.Artist, row.Name, token).ConfigureAwait(false)).Value;
            return new[] { new StructuredLyrics { Lines = plain.Split('\n').Select(line => new LyricLine { Value = line.TrimEnd('\r') }).ToArray() } };
        }, versions =>
        {
            if (versions.All(version => version.Lines.All(line => line.Value.Length == 0))) { Info(Words.NoLyrics); return; }
            var form = new Form(_owner, Words.Lyrics, closeOnly: true);
            var choice = versions.Length > 1 ? form.Choice(Words.LyricsVersion, versions.Select((version, index) =>
                version.Lang.Length > 0 ? version.Lang : (index + 1).ToString())) : null;
            var list = Form.Field(form.Panel, form.Body, Words.Lyrics,
                () => new ListCtrl(form.Panel, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual | ListCtrlStyle.SingleSelection), proportion: 1);
            list.MinSize = new Size(500, 300);
            var follow = form.Check(Words.FollowLyrics);
            var currentLine = -1; var version = versions[0];
            void Rebuild()
            {
                version = versions[Math.Clamp(choice?.SelectedIndex ?? 0, 0, versions.Length - 1)]; currentLine = -1;
                list.ClearAll(); list.InsertColumn(0, Words.CurrentPosition, 90); list.InsertColumn(1, Words.Lyrics, 430);
                list.SetDataSource(version.Lines, (line, column) => column == 1 ? line.Value : version.Synced && line.Start is { } start
                    && start >= 0 && start < TimeSpan.MaxValue.TotalMilliseconds - 1 ? TimeSpan.FromMilliseconds(start).ToString(@"m\:ss") : "");
                follow.Enabled = version.Synced;
            }
            Rebuild();
            if (choice is not null) choice.SelectionChanged += (_, _) => Rebuild();
            var subscription = _player.ObserveMediaTick(() =>
            {
                if (!follow.Checked || !version.Synced || form.Token.IsCancellationRequested) return;
                var reference = LunaPlayer.Subsonic.Playback.Reference.Parse(_player.CurrentPath);
                if (reference?.ServerId != server || reference.Id != row.Id || _player.SourceElapsed is not { } position) return;
                var line = LyricsTimeline.At(version, position);
                if (line < 0 || line == currentLine) return;
                if (currentLine >= 0) list.SetSelected(currentLine, false);
                currentLine = line; list.SetSelected(line); list.EnsureVisible(line);
            });
            form.Disposing += subscription.Dispose;
            Keep(form); form.Show();
        });
    }
}
