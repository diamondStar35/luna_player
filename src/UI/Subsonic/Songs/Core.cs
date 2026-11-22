using LunaPlayer.Application;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Subsonic.Servers;
using LunaPlayer.UI.Playlists;
using LunaPlayer.UI.Services;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal enum SongAction { Favorite, Unfavorite, Rate, Lyrics, Details, Bookmark, Download, DirectDownload, QueueDownload }

/// <summary>Song dialogs and asynchronous operations shared by the browser and current-song commands.</summary>
internal sealed partial class SongActions(Window? owner, Connections connections, Player player,
    IApplicationDispatcher dispatcher, LunaPlayer.Downloads.DownloadQueue downloads, IDialogService dialogs,
    Action<string> speak) : IDisposable
{
    private readonly Window? _owner = owner;
    private readonly Connections _connections = connections;
    private readonly Player _player = player;
    private readonly IApplicationDispatcher _dispatcher = dispatcher;
    private readonly LunaPlayer.Downloads.DownloadQueue _downloads = downloads;
    private readonly IDialogService _dialogs = dialogs;
    private readonly Action<string> _speak = speak;
    private readonly Lifetime _lifetime = new();
    private readonly HashSet<Form> _forms = [];

    internal OpenedFileMenu? Menu(string path)
    {
        var reference = Reference.Parse(path);
        if (reference?.Kind != MediaKind.Song || !_connections.Profiles.Any(profile => profile.Id == reference.ServerId)) return null;
        var song = _connections.Metadata.Get(reference.ServerId, reference.Id);
        return new(Words.SetRating, Words.Star, Words.Unstar, song is null ? null : !string.IsNullOrEmpty(song.Starred));
    }

    internal void Execute(string path, OpenedFilesAction action) => Execute(path, action switch
    {
        OpenedFilesAction.Rate => SongAction.Rate, OpenedFilesAction.Favorite => SongAction.Favorite,
        OpenedFilesAction.Unfavorite => SongAction.Unfavorite, _ => SongAction.Details
    });

    internal void Execute(string path, SongAction action, double? position = null)
    {
        var reference = Reference.Parse(path);
        if (reference is null || _lifetime.Token.IsCancellationRequested) return;
        if (action == SongAction.Bookmark) { Bookmark(reference.ServerId, reference.Id, position ?? 0); return; }
        if (action is SongAction.Download or SongAction.DirectDownload or SongAction.QueueDownload)
        {
            var song = _connections.Metadata.Get(reference.ServerId, reference.Id);
            Row row = song is not null ? Row.From(song) : new(reference.Kind == MediaKind.Podcast ? RowKind.Podcast : RowKind.Song,
                reference.Id, _player.CurrentTitle ?? reference.Id);
            Downloads(action == SongAction.Download ? Command.Download : action == SongAction.DirectDownload ? Command.Direct : Command.Enqueue,
                [row], reference.ServerId);
            return;
        }
        if (reference.Kind != MediaKind.Song) return;
        Run(async token => _connections.Metadata.Get(reference.ServerId, reference.Id)
            ?? await _connections.Client(reference.ServerId).Library.GetSongAsync(reference.Id, token).ConfigureAwait(false), song =>
        {
            _connections.Metadata.Observe(reference.ServerId, song);
            var row = Row.From(song);
            switch (action)
            {
                case SongAction.Rate:
                    var form = RatingDialog.Create(_owner, [row], rating =>
                        Run(token => _connections.RateAsync(reference.ServerId, [reference.Id], rating, token), result =>
                        {
                            if (result.Failures.Length > 0) Error(result.Failures[0].Error);
                            else _speak(Words.RatingSaved);
                        }));
                    Keep(form); form.Show(); break;
                case SongAction.Favorite: case SongAction.Unfavorite:
                    var starred = action == SongAction.Favorite;
                    Run(token => _connections.StarAsync(reference.ServerId, [reference.Id], starred, token), result =>
                    {
                        if (result.Failures.Length > 0) Error(result.Failures[0].Error);
                        else _speak(starred ? Words.FavoriteAdded : Words.FavoriteRemoved);
                    }); break;
                case SongAction.Lyrics: Lyrics(reference.ServerId, row); break;
                case SongAction.Details: Details(row); break;
            }
        });
    }

    private async void Run<T>(Func<CancellationToken, Task<T>> work, Action<T> done)
    {
        var token = _lifetime.Token;
        try
        {
            var result = await Task.Run(() => work(token), token).ConfigureAwait(false);
            _lifetime.Post(_dispatcher.Post, () => { try { done(result); } catch (Exception error) { Error(error); } });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error) { _lifetime.Post(_dispatcher.Post, () => Error(error)); }
    }

    private void Keep(Form form)
    { _forms.Add(form); form.Closed += _ => { _forms.Remove(form); form.Dispose(); }; }
    private void Info(string text) => Messages.Show(text, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, _owner, ownerOnly: true);
    private void Error(Exception error) => Messages.Show(Words.Error(error), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _owner, ownerOnly: true);

    public void Dispose()
    {
        _lifetime.Dispose();
        foreach (var form in _forms.ToArray()) form.Dispose();
        _forms.Clear();
    }
}
