using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Servers;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void Rate(Row[] rows)
    {
        if (!Ratings.CanRate(rows)) return;
        var server = _server;
        var form = RatingDialog.Create(_window, rows, rating =>
            Run(token => _connections.RateAsync(server, rows.Select(row => row.Id), rating, token), result =>
            {
                if (result.Failures.Length > 0) Error(new InvalidOperationException(Words.PartialFeedback(result.Failures.Length)
                    + Environment.NewLine + string.Join(Environment.NewLine, result.Failures.Take(5).Select(item => Words.Error(item.Error)))));
                else _speak(Words.RatingSaved);
            }));
        Keep(form, form); form.Show();
    }
    private void Unavailable(string address) => Post(() =>
    {
        var reference = LunaPlayer.Subsonic.Playback.Reference.Parse(address);
        if (reference?.ServerId != _server) return;
        _rows = _rows.Select(row => row.Id == reference.Id && row.Kind is RowKind.Song or RowKind.Bookmark
            ? row with { Available = false, Status = Words.UnavailableItem } : row).ToArray();
        Sort();
    });

    private void Star(Row[] rows, bool starred)
    {
        var server = _server; var client = _connections.Client(server);
        Run(async token =>
        {
            var updated = new HashSet<(RowKind Kind, string Id)>(); var failures = new List<Exception>();
            var songs = rows.Where(row => row.Kind is RowKind.Song or RowKind.Bookmark).ToArray();
            if (songs.Length > 0)
            {
                var result = await _connections.StarAsync(server, songs.Select(row => row.Id), starred, token).ConfigureAwait(false);
                var failed = result.Failures.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
                foreach (var row in songs) if (!failed.Contains(row.Id)) updated.Add((row.Kind, row.Id));
                failures.AddRange(result.Failures.Select(item => item.Error));
            }
            foreach (var row in rows.Where(row => row.Kind is RowKind.Artist or RowKind.Album).DistinctBy(row => (row.Kind, row.Id)))
            {
                try
                {
                    await client.Listening.SetStarredAsync([row.Id], starred, row.Kind == RowKind.Artist
                        ? LunaPlayer.Subsonic.Listening.StarKind.Artist : LunaPlayer.Subsonic.Listening.StarKind.Album, token).ConfigureAwait(false);
                    updated.Add((row.Kind, row.Id));
                }
                catch (LunaPlayer.Subsonic.RequestException error) { failures.Add(error); }
            }
            return (updated, failures);
        }, result =>
        {
            if (_server == server)
            { _rows = _rows.Select(row => result.updated.Contains((row.Kind, row.Id)) ? row with { Favorite = starred } : row).ToArray(); Sort(); }
            if (result.failures.Count > 0) Error(new InvalidOperationException(Words.PartialFeedback(result.failures.Count)
                + Environment.NewLine + string.Join(Environment.NewLine, result.failures.Take(5).Select(Words.Error))));
            else _speak(starred ? Words.FavoriteAdded : Words.FavoriteRemoved);
        });
    }

    private void MetadataChanged(Guid server, Entry[] songs) => Post(() =>
    {
        if (server != _server) return;
        var metadata = songs.DistinctBy(song => song.Id).ToDictionary(song => song.Id, StringComparer.Ordinal);
        var changed = false;
        _rows = _rows.Select(row =>
        {
            if (row.Kind is not (RowKind.Song or RowKind.Bookmark) || !metadata.TryGetValue(row.Id, out var song)
                || ReferenceEquals(row.Song, song)) return row;
            changed = true;
            return Row.From(song) with { Kind = row.Kind, Position = row.Position, Description = row.Description,
                PlaylistIndex = row.PlaylistIndex, Owner = row.Owner };
        }).ToArray();
        if (changed) Sort();
    });
}

internal static class RatingDialog
{
    internal static Form Create(Window? owner, Row[] rows, Action<int> save)
    {
        var form = new Form(owner, Words.Rating);
        var chosen = Ratings.Common(rows);
        if (!chosen.HasValue) form.Body.Add(new StaticText(form.Panel, label: Words.MixedRatings), flags: SizerFlags.All, border: 5);
        var options = new RadioButton[6];
        for (var rating = 0; rating < options.Length; rating++)
        {
            var value = rating;
            var button = new RadioButton(form.Panel, label: value == 0 ? Words.NotRated : Words.Stars(value), groupStart: value == 0);
            options[value] = button;
            button.Selected += (_, _) => chosen = value;
            form.Body.Add(button, flags: SizerFlags.All, border: 5);
        }
        if (chosen is { } initial) options[initial].Value = true;
        else foreach (var option in options) option.Value = false;
        var submitted = 0;
        form.SaveAsync = _ =>
        {
            if (!chosen.HasValue) throw new ArgumentException(Words.ChooseRating);
            submitted = chosen.Value; return Task.FromResult(true);
        };
        form.Closed += accepted => { if (accepted) save(submitted); };
        return form;
    }
}
