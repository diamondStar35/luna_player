using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Playback;

namespace LunaPlayer.Subsonic.Servers;

/// <summary>Profiles select new connections. In-flight jobs retain their original account until completion.</summary>
internal sealed partial class Connections(Store store, Func<Profile, Client>? connect = null) : IMediaResolver, IDisposable
{
    internal Metadata Metadata { get; } = new();
    private readonly Dictionary<Guid, (Profile Profile, Client Client, Browsing.Catalog Catalog)> _clients = [];
    private readonly List<Client> _retired = [];
    private readonly SemaphoreSlim _profilesGate = new(1, 1);
    internal Profile[] Profiles { get; private set; } = [];
    internal event Action? Changed;
    internal Guid? LastServer => store.LastServer;
    private int _selectionRevision;
    internal async Task SelectAsync(Guid server, CancellationToken token = default)
    {
        var revision = Interlocked.Increment(ref _selectionRevision);
        await _profilesGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (revision != Volatile.Read(ref _selectionRevision) || !Profiles.Any(profile => profile.Id == server)) return;
            await Task.Run(() => store.SelectAsync(server, token), token).ConfigureAwait(false);
        }
        finally { _profilesGate.Release(); }
    }
    private Task _initialLoad = Task.CompletedTask;
    internal Task LoadAsync(CancellationToken token = default) => _initialLoad = LoadProfilesAsync(token);
    private async Task LoadProfilesAsync(CancellationToken token)
    {
        await _profilesGate.WaitAsync(token).ConfigureAwait(false);
        try { Profiles = await store.LoadAsync(token).ConfigureAwait(false); }
        finally { _profilesGate.Release(); }
    }
    internal Profile Profile(Guid id) => Profiles.FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException();
    internal Client Client(Guid id)
    {
        var profile = Profile(id);
        lock (_clients)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_clients.TryGetValue(id, out var cached) && cached.Profile == profile) return cached.Client;
            if (cached.Client is not null) _retired.Add(cached.Client);
            var client = connect?.Invoke(profile) ?? new Client(profile.ConnectionOptions());
            _clients[id] = (profile, client, new(client));
            return client;
        }
    }
    internal Browsing.Catalog Catalog(Guid id)
    {
        _ = Client(id);
        lock (_clients) return _clients[id].Catalog;
    }
    internal async Task<Profile> SaveAsync(Profile profile, CancellationToken token = default)
    {
        await _profilesGate.WaitAsync(token).ConfigureAwait(false);
        Profile saved;
        try { saved = await store.SaveAsync(profile, token).ConfigureAwait(false); Profiles = await store.LoadAsync(token).ConfigureAwait(false); }
        finally { _profilesGate.Release(); }
        ForgetStreams(saved.Id);
        Metadata.Clear(saved.Id);
        Changed?.Invoke(); return saved;
    }
    internal async Task RemoveAsync(Guid id, CancellationToken token = default)
    {
        await _profilesGate.WaitAsync(token).ConfigureAwait(false);
        try { await store.RemoveAsync(id, token).ConfigureAwait(false); Profiles = await store.LoadAsync(token).ConfigureAwait(false); }
        finally { _profilesGate.Release(); }
        ForgetStreams(id);
        Metadata.Clear(id);
        Changed?.Invoke();
    }
    public bool Handles(string address) => Reference.IsAddress(address);
    public void Dispose()
    {
        lock (_clients)
        {
            if (_disposed) return;
            _disposed = true;
        }
        _lifetime.Cancel();
        lock (_clients)
        {
            foreach (var client in _clients.Values.Select(x => x.Client).Concat(_retired)) client.Dispose();
            _clients.Clear(); _retired.Clear(); _inspections.Clear(); _streams.Clear(); _legacyStreams.Clear();
        }
        _lifetime.Dispose();
    }
}
