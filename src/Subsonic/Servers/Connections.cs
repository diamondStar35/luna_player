using LunaPlayer.Playback;
using LunaPlayer.Media;
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
        Changed?.Invoke(); return saved;
    }
    internal async Task RemoveAsync(Guid id, CancellationToken token = default)
    {
        await _profilesGate.WaitAsync(token).ConfigureAwait(false);
        try { await store.RemoveAsync(id, token).ConfigureAwait(false); Profiles = await store.LoadAsync(token).ConfigureAwait(false); }
        finally { _profilesGate.Release(); }
        Changed?.Invoke();
    }
    public bool Handles(string address) => Reference.IsAddress(address);
    public async Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
    {
        await _initialLoad.WaitAsync(token).ConfigureAwait(false);
        var reference = Reference.Parse(address) ?? throw new ArgumentException(nameof(address));
        var profile = Profile(reference.ServerId);
        var client = Client(profile.Id);
        if (reference.Kind == Playback.MediaKind.Radio)
        {
            var station = (await client.Radio.GetAllAsync(token).ConfigureAwait(false)).Single(x => x.Id == reference.Id);
            return new(station.StreamUrl, new MediaTags(station.Name, "", ""), Live: true);
        }
        Models.Entry song;
        if (reference.Kind == Playback.MediaKind.Podcast)
        {
            var episode = await client.Podcasts.FindEpisodeAsync(reference.Id, token).ConfigureAwait(false);
            if (episode.StreamId.Length == 0) throw new RequestException(FailureKind.Unsupported, "stream");
            song = new() { Id = episode.StreamId, Title = episode.Title, Duration = episode.Duration, Suffix = episode.Suffix };
        }
        else
        {
            song = await client.Library.GetSongAsync(reference.Id, token).ConfigureAwait(false);
            Metadata.Observe(profile.Id, song);
        }
        var options = profile.Streaming with { TimeOffset = null };
        var offset = 0d;
        var seekByReopening = options.Mode == StreamingMode.Transcode
            && (await client.DiscoverAsync(token: token).ConfigureAwait(false)).Extension("transcodeOffset") == Support.Supported;
        if (position is > 0 && seekByReopening)
        { offset = position.Value; options = options with { TimeOffset = offset }; }
        var uri = await client.Media.GetStreamAddressAsync(song.Id, options, token).ConfigureAwait(false);
        return new(uri.AbsoluteUri, new MediaTags(song.Title, song.DisplayArtist.Length > 0 ? song.DisplayArtist : song.Artist, song.Album),
            song.Duration, new Dictionary<string, string>(profile.Headers), Offset: offset, SeekByReopening: seekByReopening);
    }
    public void Dispose()
    {
        lock (_clients)
        {
            foreach (var client in _clients.Values.Select(x => x.Client).Concat(_retired)) client.Dispose();
            _clients.Clear(); _retired.Clear();
        }
    }
}
