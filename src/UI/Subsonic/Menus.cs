using LunaPlayer.Subsonic.Browsing;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private readonly HashSet<(Guid Server, Command Command)> _unavailableCommands = [];

    private CommandContext Context(Row[] rows, bool serverActions = true) => new(rows, _location, _user, _capabilities, _username,
        serverActions && _player.IsLoaded && LunaPlayer.Subsonic.Playback.Reference.Parse(_player.CurrentPath) is { Kind: LunaPlayer.Subsonic.Playback.MediaKind.Song } current && current.ServerId == _server,
        serverActions && _player.Items.Any(item => item.ServerMedia is { Kind: LunaPlayer.Subsonic.Playback.MediaKind.Song } reference && reference.ServerId == _server),
        serverActions && _connections.Profile(_server).AudioMuse?.Server is not null);

    private bool Allows(Command command, CommandContext context) => !_unavailableCommands.Contains((_server, command))
        && !(_unsupported.Contains((_server, Category.Podcasts)) && command is Command.Subscribe or Command.RefreshPodcasts or Command.EpisodeDownload or Command.EpisodeDelete)
        && !(_unsupported.Contains((_server, Category.Radio)) && command == Command.NewRadio)
        && CommandPolicy.Allows(command, context);

    private void RecordUnavailable(Guid server, Command command, Exception error)
    {
        if (error is not LunaPlayer.Subsonic.RequestException request) return;
        var serverOperation = command is Command.Scan or Command.FullScan or Command.ScanStatus or Command.NewRadio
            or Command.Subscribe or Command.RefreshPodcasts or Command.CreatePlaylist or Command.SendPlaylist or Command.LoadQueue or Command.SaveQueue;
        if (request.Kind == LunaPlayer.Subsonic.FailureKind.Unsupported
            || serverOperation && request.Kind == LunaPlayer.Subsonic.FailureKind.Forbidden)
            _unavailableCommands.Add((server, command));
    }

    private void Menu(bool general, Window? source = null)
    {
        if (_disposed || _server == Guid.Empty) return;
        var rows = source == _tree ? [] : Selected;
        var context = Context(rows);
        using var menu = new Menu();
        var commands = new Dictionary<int, Command>();
        _shortcuts.Apply(_shortcutSettings.MusicServers, new Dictionary<LunaPlayer.Actions.ActionId, LunaPlayer.Actions.Shortcut>());
        void Add(Menu target, Command command, string label)
        {
            if (!Allows(command, context)) return;
            var action = Bindings.ForMenu(command, rows);
            if (action is { } id && _shortcuts.Get(id) is { } shortcut) label += "\t" + shortcut.ToDisplayString();
            // A menu owns its automatic IDs and releases them when destroyed. Each popup needs
            // fresh IDs; retaining the integers for the next popup reuses unreserved wx IDs.
            var item = IdManager.NewId(); target.Append(item, label); commands[item] = command;
        }
        Add(menu, Command.Play, Words.Play); Add(menu, Command.Append, Words.Append);
        Add(menu, Command.Direct, Words.Direct); Add(menu, Command.Download, Words.Download); Add(menu, Command.Enqueue, Words.Enqueue);
        Add(menu, Command.Favorite, Words.Star); Add(menu, Command.Unfavorite, Words.Unstar); Add(menu, Command.Rate, Words.SetRating);
        Add(menu, Command.Details, Words.Details); Add(menu, Command.Lyrics, Words.Lyrics);
        Add(menu, Command.Top, Words.Top); Add(menu, Command.SimilarArtists, Words.SimilarArtists);
        Add(menu, Command.Similar, Words.InstantMix); Add(menu, Command.Sonic, Words.Sonic); Add(menu, Command.Path, Words.Path);
        Add(menu, Command.AddPlaylist, Words.AddRemotePlaylist); Add(menu, Command.RemovePlaylistItems, Words.RemoveFromPlaylist);
        Add(menu, Command.EditPlaylist, Words.Edit); Add(menu, Command.Rename, Words.Rename); Add(menu, Command.Remove, Words.Remove);
        Add(menu, Command.EpisodeDownload, Words.DownloadEpisode); Add(menu, Command.EpisodeDelete, Words.DeleteEpisode);
        if (general)
        {
            var server = new Menu();
            Add(server, Command.CreatePlaylist, Words.CreatePlaylist); Add(server, Command.SendPlaylist, Words.SendPlaylist);
            Add(server, Command.Bookmark, Words.Bookmark); Add(server, Command.SaveQueue, Words.ServerSaveQueue); Add(server, Command.LoadQueue, Words.ServerLoadQueue);
            Add(server, Command.NewRadio, Words.CreateRadio); Add(server, Command.Subscribe, Words.Subscribe); Add(server, Command.RefreshPodcasts, Words.RefreshPodcasts);
            Add(server, Command.Scan, Words.Scan); Add(server, Command.FullScan, Words.FullScan); Add(server, Command.ScanStatus, Words.ScanStatus);
            Add(server, Command.Info, Words.ServerInfo); menu.AppendSubMenu(server, Words.Server);
            if (context.HasAudioMuse)
            {
                var ai = new Menu(); Add(ai, Command.AiSearch, Words.AiSearch); Add(ai, Command.AiGenerate, Words.AiGenerate); Add(ai, Command.Alchemy, Words.Alchemy);
                menu.AppendSubMenu(ai, Words.AudioMuse);
            }
        }
        var chosen = (source ?? _list).GetPopupMenuSelectionFromUser(menu);
        if (commands.TryGetValue(chosen, out var selected)) Execute(selected);
    }
    private bool Owns(Row row) => CommandPolicy.Owns(row, _user, _username);
}
