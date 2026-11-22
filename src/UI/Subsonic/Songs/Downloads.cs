using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Media;
using LunaPlayer.Subsonic.Models;
using WxSharp;
using Job = LunaPlayer.Downloads.Job;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class SongActions
{
    internal void Downloads(Command command, Row[] rows, Guid server, IReadOnlyList<string>? scope = null)
    {
        if (rows.Length == 0 || rows.Any(x => x.Kind == RowKind.Radio)) return;
        var profile = _connections.Profile(server); var client = _connections.Client(server);
        var libraries = scope ?? [];
        var unavailable = Words.UnavailableEpisode;
        Run(async token =>
        {
            var expanded = await _connections.Catalog(server).ExpandAsync(rows, token, libraries);
            var songs = new List<Entry>();
            foreach (var row in expanded.DistinctBy(x => (x.Kind, x.Id)))
            {
                if (row.Kind == RowKind.Podcast)
                {
                    var episode = await client.Podcasts.FindEpisodeAsync(row.Id, token);
                    if (episode.StreamId.Length == 0) throw new InvalidOperationException(unavailable);
                    songs.Add(new() { Id = episode.StreamId, Title = episode.Title, Suffix = episode.Suffix });
                }
                else songs.Add(row.Song ?? await client.Library.GetSongAsync(row.Id, token));
            }
            return songs.ToArray();
        }, songs =>
        {
            if (songs.Length == 0) return;
            void Start(string? folder, string? name, bool queued, bool? albumFolders = null)
            {
                var jobs = songs.Select(song => DownloadJob.Create(profile with { Downloads = profile.Downloads with { AlbumFolders = albumFolders ?? profile.Downloads.AlbumFolders } }, client, song, songs.Length == 1 ? name : null, folder)).ToArray();
                if (queued) { foreach (var job in jobs) _downloads.Add(job); _speak(Words.DownloadQueued); return; }
                Job request;
                if (jobs.Length == 1) request = jobs[0];
                else
                {
                    var completed = 0; var lastPath = "";
                    request = new Job(Words.DownloadCount(jobs.Length), (report, token) =>
                    {
                        while (completed < jobs.Length)
                        {
                            var result = jobs[completed].Run(value => report(value with
                            { Percent = (completed + value.Percent / 100) * 100 / jobs.Length }), token);
                            if (!result.Success) return result;
                            lastPath = result.FilePath; completed++;
                        }
                        return LunaPlayer.Downloads.Outcome.Saved(lastPath);
                    });
                }
                var id = _downloads.Add(request, startImmediately: true);
                _dialogs.ShowDownloadProgress(_downloads, id);
            }
            if (command != Command.Download) { Start(null, null, command == Command.Enqueue); return; }
            var form = new Form(_owner, Words.Download, Words.DownloadNow);
            var folder = form.Text(Words.Folder, profile.Downloads.Folder.Length > 0 ? profile.Downloads.Folder : Configuration.Paths.DefaultDownloadsDirectory);
            form.Button(Words.Browse, () => { if (form.ChooseFolder(folder.Value) is { } path) folder.Value = path; });
            var organize = form.Check(Words.AlbumFolders, profile.Downloads.AlbumFolders);
            var name = songs.Length == 1 ? form.Text(Words.FileName, songs[0].Title) : null;
            var destination = ""; string? fileName = null; var albumFolders = false; var queued = false;
            void Capture() { destination = folder.Value; fileName = name?.Value; albumFolders = organize.Checked; }
            form.SaveAsync = _ => { Capture(); return Task.FromResult(true); };
            form.Button(Words.Enqueue, () => { Capture(); queued = true; form.Complete(true); });
            form.Closed += accepted => { if (accepted) Start(destination, fileName, queued, albumFolders); };
            Keep(form); form.Show();
        });
    }
}
