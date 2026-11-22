using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Subsonic.Servers;

namespace LunaPlayer.Application;

internal sealed partial class Features
{
    private Connections? _musicConnections;
    private LunaPlayer.Subsonic.Listening.Synchronization? _musicSynchronization;
    private MusicServers.Metadata? _musicMetadata;

    private void BindMusicServers(Commands.ActionRouter router, LunaPlayer.Downloads.DownloadQueue downloads)
    {
        var connections = new Connections(new Store(Path.Combine(Paths.RootDirectory, "subsonic-servers.json")));
        _musicConnections = connections;
        _view.Dialogs.ConfigureMusicServerItems(connections, _player, downloads, text => _speech.SpeakText(text));
        _player.SetMediaResolver(connections, _dispatcher.Post);
        var synchronization = new LunaPlayer.Subsonic.Listening.Synchronization(connections, _player, _dispatcher);
        _musicSynchronization = synchronization;
        var metadata = new MusicServers.Metadata(connections, _player, _dispatcher);
        _musicMetadata = metadata;
        metadata.Status += (server, error) => _view.Dialogs.MusicServerStatus(server, error);
        connections.Changed += () => _dispatcher.Post(() =>
        {
            if (_disposed) return;
            synchronization.ProfilesChanged(); metadata.ProfilesChanged(); _view.Dialogs.RefreshMusicServers();
        });
        _player.MediaResolutionFailed += OnMusicResolutionFailed;
        synchronization.Status += (server, error) => _view.Dialogs.MusicServerStatus(server, error);
        router.Register(ActionId.OpenMusicServers, () => _view.Dialogs.ShowMusicServers(connections, _player, downloads,
            () => router.Execute(ActionId.OpenDownloadQueue), text => _speech.SpeakText(text), _settings.Shortcuts));
        void Current(ActionId id, LunaPlayer.UI.Subsonic.SongAction action) => router.Register(id, () =>
        {
            if (_player.IsLoaded && LunaPlayer.Subsonic.Playback.Reference.Parse(_player.CurrentPath) is { Kind: LunaPlayer.Subsonic.Playback.MediaKind.Song })
                _view.Dialogs.ActOnServerSong(_player.CurrentPath!, action, _player.SourceElapsed);
        });
        Current(ActionId.CurrentServerFavorite, LunaPlayer.UI.Subsonic.SongAction.Favorite);
        Current(ActionId.CurrentServerUnfavorite, LunaPlayer.UI.Subsonic.SongAction.Unfavorite);
        Current(ActionId.CurrentServerRate, LunaPlayer.UI.Subsonic.SongAction.Rate);
        Current(ActionId.CurrentServerLyrics, LunaPlayer.UI.Subsonic.SongAction.Lyrics);
        Current(ActionId.CurrentServerDetails, LunaPlayer.UI.Subsonic.SongAction.Details);
        var loading = connections.LoadAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                await loading.ConfigureAwait(false);
                _dispatcher.Post(() => { if (!_disposed) { synchronization.ProfilesChanged(); metadata.ProfilesChanged(); _view.Dialogs.RefreshMusicServers(); } });
            }
            catch (Exception error) { _dispatcher.Post(() => { if (!_disposed) OnMusicResolutionFailed(error); }); }
        });
    }
    private void OnMusicResolutionFailed(Exception error)
    {
        // Translators: Error opening music from a saved server. {details} is the server or connection failure.
        _view.Dialogs.ShowMainError(TrFormat("The music-server item could not be opened.\n\n{details}",
            error is LunaPlayer.Subsonic.RequestException request ? request.ServerMessage ?? request.Kind.ToString() : error.Message));
    }
}
