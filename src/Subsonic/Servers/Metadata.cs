using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Servers;

/// <summary>Shares recently observed song metadata across browsing, feedback, and active playlists.</summary>
internal sealed class Metadata
{
    private const int Capacity = 4096;
    private readonly Lock _gate = new();
    private readonly Dictionary<TrackReference, Entry> _songs = [];
    private readonly Queue<TrackReference> _order = [];
    internal event Action<Guid, Entry[]>? Changed;

    internal Entry? Get(Guid server, string id)
    { lock (_gate) return _songs.GetValueOrDefault(new(server, id)); }

    internal void Observe(Guid server, Entry song) => Observe(server, [song]);

    internal void Observe(Guid server, IEnumerable<Entry> songs)
    {
        var changed = new List<Entry>();
        lock (_gate)
        {
            foreach (var song in songs)
            {
                var key = new TrackReference(server, song.Id);
                if (_songs.TryGetValue(key, out var previous) && previous == song) continue;
                if (!_songs.ContainsKey(key))
                {
                    if (_songs.Count >= Capacity) _songs.Remove(_order.Dequeue());
                    _order.Enqueue(key);
                }
                _songs[key] = song; changed.Add(song);
            }
        }
        if (changed.Count > 0) Changed?.Invoke(server, changed.ToArray());
    }
}
