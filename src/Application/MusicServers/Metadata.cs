using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Subsonic.Servers;
using LunaPlayer.UI.Services;

namespace LunaPlayer.Application.MusicServers;

/// <summary>Enriches restored server items and current artwork without doing network work on the app loop.</summary>
internal sealed partial class Metadata : IDisposable
{
    private readonly Connections _connections;
    private readonly Player _player;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly Lifetime _lifetime = new();
    private readonly HashSet<Guid> _refreshed = [];
    internal event Action<Guid, Exception?>? Status;

    internal Metadata(Connections connections, Player player, IApplicationDispatcher dispatcher)
    {
        _connections = connections; _player = player; _dispatcher = dispatcher;
        connections.Metadata.Changed += Changed;
        player.CurrentChanged += CurrentChanged;
    }

    internal void ProfilesChanged()
    {
        foreach (var profile in _connections.Profiles) _player.RetryServerMedia(profile.Id);
        _refreshed.Clear(); _artworkIdentity = null; CurrentChanged();
    }
    private void Changed(Guid server, Entry[] songs)
        => _lifetime.Post(_dispatcher.Post, () => _player.UpdateCatalog(server, songs));

    private void CurrentChanged()
    {
        if (_lifetime.Token.IsCancellationRequested) return;
        Artwork();
        var references = _player.Items.Select(item => item.ServerMedia).Where(reference => reference?.Kind == MediaKind.Song)
            .Select(reference => reference!).GroupBy(reference => reference.ServerId);
        foreach (var group in references)
        {
            var profile = _connections.Profiles.FirstOrDefault(profile => profile.Id == group.Key);
            if (profile?.RefreshMetadataOnStartup != true || !_refreshed.Add(group.Key)) continue;
            _ = Refresh(group.Key, group.Select(reference => reference.Id).Distinct(StringComparer.Ordinal).ToArray());
        }
    }

    private async Task Refresh(Guid server, string[] ids)
    {
        var token = _lifetime.Token;
        try
        {
            var client = _connections.Client(server); var entries = new Entry[ids.Length];
            await Parallel.ForEachAsync(Enumerable.Range(0, ids.Length), new ParallelOptions
            { MaxDegreeOfParallelism = 4, CancellationToken = token }, async (index, cancellation) =>
            {
                entries[index] = await client.Library.GetSongAsync(ids[index], cancellation).ConfigureAwait(false);
            }).ConfigureAwait(false);
            if (!token.IsCancellationRequested) _connections.Metadata.Observe(server, entries);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error)
        {
            // Optional startup enrichment must not interrupt playback. Selecting a track still
            // resolves fresh metadata and reports any failure through the normal playback path.
            _lifetime.Post(_dispatcher.Post, () => Status?.Invoke(server, error));
        }
    }

    public void Dispose()
    {
        _lifetime.Dispose(); _artworkRequest?.Cancel(); _artworkRequest?.Dispose(); _artworkRequest = null;
        _connections.Metadata.Changed -= Changed; _player.CurrentChanged -= CurrentChanged;
        lock (_covers) _covers.Clear();
    }
}
