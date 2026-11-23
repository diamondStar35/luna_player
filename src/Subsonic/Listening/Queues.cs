namespace LunaPlayer.Subsonic.Listening;

internal sealed partial class Synchronization
{
    private readonly Dictionary<Guid, QueueSnapshot> _queues = [];
    private bool _queueChanged = true;
    private sealed record QueueSnapshot(string[] Ids, string Key, Dictionary<string, int> Indexes)
    {
        internal static readonly QueueSnapshot Empty = new([], "", []);
    }

    private void QueueChanged() => _queueChanged = true;

    private QueueSnapshot QueueFor(Guid server)
    {
        if (_queueChanged)
        {
            _queueChanged = false; _queues.Clear();
            // Position sampling does not change the queue. Rebuild its arrays only after
            // player state changes, and share that snapshot across account workers.
            var songs = _player.Items.DistinctBy(item => item.SourceId)
                .Where(item => item.ServerMedia is { Kind: Playback.MediaKind.Song })
                .GroupBy(item => item.ServerMedia!.ServerId);
            foreach (var account in songs)
            {
                var items = account.ToArray();
                _queues[account.Key] = new(items.Select(item => item.ServerMedia!.Id).ToArray(),
                    string.Join("/", items.Select(item => item.Id)),
                    items.Select((item, index) => (Id: item.SourceId.ToString("N"), Index: index))
                        .ToDictionary(item => item.Id, item => item.Index, StringComparer.Ordinal));
            }
        }
        return _queues.GetValueOrDefault(server) ?? QueueSnapshot.Empty;
    }
}
