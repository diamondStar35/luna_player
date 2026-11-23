using System.Net;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Playback;

namespace LunaPlayer.Subsonic.Servers;

internal sealed partial class Connections
{
    // Completed plans only: cancelling navigation must cancel its network work, rather than
    // queueing abandoned negotiations ahead of the user's latest selection. Tokens stay in memory.
    private readonly Dictionary<Reference, (Profile Profile, DateTimeOffset Created, StreamPlan Plan)> _streams = [];
    private readonly HashSet<Client> _legacyStreams = [];
    private long _streamRevision;
    internal void ForgetStreams(Guid server)
    {
        lock (_clients)
        {
            _streamRevision++;
            foreach (var key in _streams.Keys.Where(key => key.ServerId == server).ToArray()) _streams.Remove(key);
            if (_clients.TryGetValue(server, out var connection)) _legacyStreams.Remove(connection.Client);
        }
    }
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
        StreamPlan? plan = null;
        long revision;
        lock (_clients)
        {
            revision = _streamRevision;
            if (_streams.TryGetValue(reference, out var cached) && cached.Profile == profile
                && client.Clock.GetUtcNow() - cached.Created < TimeSpan.FromMinutes(5)) plan = cached.Plan;
        }
        if (plan is null)
        {
            plan = await CreateStreamAsync(reference, profile, client, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            lock (_clients)
            {
                if (!_disposed && revision == _streamRevision && Profiles.Any(saved => saved.Id == profile.Id && saved == profile))
                {
                    if (_streams.Count >= 128)
                        _streams.Remove(_streams.MinBy(entry => entry.Value.Created).Key);
                    _streams[reference] = (profile, client.Clock.GetUtcNow(), plan);
                }
            }
        }
        return await plan.AtAsync(position, token).ConfigureAwait(false);
    }

    private async Task<StreamPlan> CreateStreamAsync(Reference reference, Profile profile, Client client, CancellationToken token)
    {
        Entry song;
        if (reference.Kind == Playback.MediaKind.Podcast)
        {
            var episode = await client.Podcasts.FindEpisodeAsync(reference.Id, token).ConfigureAwait(false);
            if (episode.StreamId.Length == 0) throw new RequestException(FailureKind.Unsupported, "stream");
            song = new() { Id = episode.StreamId, Title = episode.Title, Duration = episode.Duration, Suffix = episode.Suffix };
        }
        else
        {
            song = Metadata.Get(profile.Id, reference.Id) ?? await client.Library.GetSongAsync(reference.Id, token).ConfigureAwait(false);
            Metadata.Observe(profile.Id, song);
        }
        var options = profile.Streaming with { TimeOffset = null };
        var headers = new Dictionary<string, string>(profile.Headers);
        if (options.Mode == StreamingMode.Original)
            return new(client, song, options, headers, await client.Media.GetStreamAddressAsync(song.Id, options, token).ConfigureAwait(false), false, StreamCapabilities.Source(song));

        var capabilities = await client.DiscoverAsync(token: token).ConfigureAwait(false);
        bool legacy;
        lock (_clients) legacy = _legacyStreams.Contains(client);
        var declined = false;
        // The extension has no per-kind capability flag. Common servers implement only
        // songs, so podcasts retain the portable streamId route instead of failing negotiation.
        if (!legacy && reference.Kind == Playback.MediaKind.Song && capabilities.Extension("transcoding") == Support.Supported)
        {
            try
            {
                var decision = await client.Media.GetTranscodeDecisionAsync(song.Id, StreamCapabilities.For(options), token: token).ConfigureAwait(false);
                if (decision.CanDirectPlay is null || decision.CanTranscode is null)
                    throw new RequestException(FailureKind.InvalidResponse, "getTranscodeDecision");
                if (options.Mode == StreamingMode.ServerDefault && decision.CanDirectPlay == true)
                {
                    var raw = options with { Mode = StreamingMode.Original };
                    return new(client, song, raw, headers, await client.Media.GetStreamAddressAsync(song.Id, raw, token).ConfigureAwait(false), false,
                        decision.SourceStream is { } source ? StreamCapabilities.Format(source, false) : StreamCapabilities.Source(song));
                }
                if (decision.CanTranscode == true)
                {
                    if (decision.TranscodeParams.Length == 0 || decision.TranscodeStream is null)
                        throw new RequestException(FailureKind.InvalidResponse, "getTranscodeDecision");
                    var format = StreamCapabilities.Format(decision.TranscodeStream, true);
                    return new(client, song, options, headers, await client.Media.GetTranscodeAddressAsync(song.Id, decision.TranscodeParams, token: token).ConfigureAwait(false),
                        true, format, decision.TranscodeParams);
                }
                declined = true;
            }
            catch (RequestException error) when (error.Kind == FailureKind.Unsupported || error.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NotImplemented)
            { lock (_clients) _legacyStreams.Add(client); }
        }
        // Legacy Server Default may ignore timeOffset when returning the original file.
        // Only a confirmed decision gives us a reliable trimmed origin for that mode.
        var reopen = !declined && options.Mode == StreamingMode.Transcode && capabilities.Extension("transcodeOffset") == Support.Supported;
        return new(client, song, options, headers, await client.Media.GetStreamAddressAsync(song.Id, options, token).ConfigureAwait(false), reopen, null);
    }
}
