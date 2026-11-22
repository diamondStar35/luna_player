using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Servers;
using WxSharp;
using Ai = LunaPlayer.Subsonic.AudioMuse;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private static Ai.Client? CreateAudioMuse(Profile profile, int? count = null)
        => profile.AudioMuse?.Server is { } server ? new Ai.Client(new()
        { Server = server, Token = profile.AudioMuse.Token, MusicServer = profile.AudioMuse.MusicServer, ResultCount = count ?? profile.AudioMuse.ResultCount }) : null;
    private void AudioMuse(Command command, Row[] selection)
    {
        var server = _server; var profile = _connections.Profile(server); var client = _connections.Client(server);
        var libraries = SelectedLibraries(); var catalog = _connections.Catalog(server);
        if (profile.AudioMuse?.Server is null) { Info(Words.NotConfigured); return; }
        var form = new Form(_window, command == Command.Alchemy ? Words.Alchemy : command == Command.AiGenerate ? Words.AiGenerate : Words.AiSearch, Words.Generate);
        var prompt = command == Command.Alchemy ? null : form.Text(Words.Prompt, multiline: true);
        var count = form.Number(Words.Count, profile.AudioMuse.ResultCount, 1, 500);
        var seeds = selection.Where(x => x.Kind is RowKind.Song or RowKind.Artist).Select(x =>
            (Row: x, Seed: new Ai.Seed(x.Id, x.Kind == RowKind.Artist ? Ai.SeedKind.Artist : Ai.SeedKind.Song))).ToList();
        if (command == Command.Alchemy)
        {
            var list = Form.Field(form.Panel, form.Body, Words.Items,
                () => new ListCtrl(form.Panel, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual), proportion: 1);
            list.InsertColumn(0, Words.Name, 280); list.InsertColumn(1, Words.Operation, 130); list.MinSize = new Size(500, 180);
            Button? remove = null;
            void Buttons() { remove?.Show(list.GetSelectedIndices().Length > 0); form.Window.Layout(); }
            void Refresh() { list.SetDataSource(seeds, (item, col) => col == 0 ? item.Row.Name : item.Seed.Operation == Ai.Operation.Add ? Words.Include : Words.Exclude); Buttons(); }
            form.Button(Words.Include, () => PickSongs(form.Window, added => { seeds.AddRange(added.Select(x => (x, new Ai.Seed(x.Id)))); Refresh(); }, Words.IncludedSeedsAdded));
            form.Button(Words.Exclude, () => PickSongs(form.Window, added => { seeds.AddRange(added.Select(x => (x, new Ai.Seed(x.Id, Operation: Ai.Operation.Subtract)))); Refresh(); }, Words.ExcludedSeedsAdded));
            remove = form.Button(Words.Remove, () => { foreach (var index in list.GetSelectedIndices().OrderDescending()) seeds.RemoveAt((int)index); Refresh(); });
            list.ItemSelected += (_, _) => Buttons(); list.ItemDeselected += (_, _) => Buttons();
            Refresh();
        }
        var status = form.Text(Words.Status, multiline: true); status.Editable = false;
        Row[] results = [];
        var lastProgress = "";
        Ai.Result? snapshot = null;
        form.SaveAsync = token =>
        {
            var text = prompt?.Value ?? ""; var number = count.Value; var items = seeds.Select(x => x.Seed).ToArray();
            return Task.Run(async () =>
            {
                using var ai = CreateAudioMuse(profile, number)!;
                Ai.Result value;
                if (command == Command.AiSearch) value = await ai.SearchAsync(text, token);
                else if (command == Command.Alchemy) value = await ai.AlchemyAsync(items, token);
                else
                {
                    Ai.Result? generated = null;
                    try
                    {
                        await foreach (var progress in ai.GeneratePlaylistProgressAsync(text, token))
                        {
                            if (progress.Result is not null) generated = progress.Result;
                            form.Post(() =>
                            {
                                if (token.IsCancellationRequested || progress.Message == lastProgress) return;
                                lastProgress = progress.Message; status.ChangeValue(progress.Message); _speak(progress.Message);
                            });
                        }
                    }
                    catch (RequestException error) when (error.Kind == FailureKind.Unsupported)
                    { generated = await ai.GeneratePlaylistAsync(text, token); }
                    value = generated ?? throw new RequestException(FailureKind.InvalidResponse, "chatPlaylist");
                }
                snapshot = value;
                results = await catalog.FilterAsync(await ResolveAudioMuse(client, value, token), libraries, token);
                return true;
            }, token);
        };
        Keep(form, form);
        form.Closed += accepted =>
        {
            if (!accepted || _server != server) return;
            var saved = snapshot;
            if (saved is not null) ShowDerived(results, async token => await catalog.FilterAsync(await ResolveAudioMuse(client, saved, token), libraries, token));
        };
        form.Show();
    }
    private static async Task<Row[]> ResolveAudioMuse(Client client, Ai.Result result, CancellationToken token)
    {
        var rows = new Row[result.Tracks.Length];
        await Parallel.ForEachAsync(Enumerable.Range(0, rows.Length), new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = token }, async (index, cancellation) =>
        {
            var track = result.Tracks[index];
            try { rows[index] = Row.From(await client.Library.GetSongAsync(track.Id, cancellation)); }
            catch (RequestException error) when (error.Code == 70 || error.StatusCode == System.Net.HttpStatusCode.NotFound)
            { rows[index] = new(RowKind.Unavailable, track.Id, track.Title, track.Credit, track.Album); }
        });
        return rows;
    }
}
