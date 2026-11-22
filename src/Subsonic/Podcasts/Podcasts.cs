using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic.Podcasts;

/// <summary>Generic Subsonic podcast operations; Navidrome's unsupported replies are not hidden.</summary>
internal sealed class Podcasts(Session session, Func<string, int, CancellationToken, Task> require) : Feature(session)
{
    internal async Task<PodcastChannel[]> GetChannelsAsync(string? id = null, bool includeEpisodes = true, CancellationToken token = default)
        => Required((await Session.ReadAsync("getPodcasts", new Parameters().With("id", id).With("includeEpisodes", includeEpisodes), token).ConfigureAwait(false)).Podcasts, "getPodcasts").Channels;
    internal async Task<PodcastEpisode[]> GetNewestEpisodesAsync(int count = 20, CancellationToken token = default)
        => Required((await Session.ReadAsync("getNewestPodcasts", new Parameters().With("count", Browsing.Library.Count(count)), token).ConfigureAwait(false)).NewestPodcasts, "getNewestPodcasts").Episodes;
    internal async Task<PodcastEpisode> GetEpisodeAsync(string id, CancellationToken token = default)
    {
        await require("getPodcastEpisode", 1, token).ConfigureAwait(false);
        return Required((await Session.ReadAsync("getPodcastEpisode", new Parameters().With("id", Id(id)), token).ConfigureAwait(false)).PodcastEpisode, "getPodcastEpisode");
    }
    internal Task SubscribeAsync(string feedUrl, CancellationToken token = default)
        => Change("createPodcastChannel", new Parameters().With("url", Radio.Radio.Url(feedUrl)), token);
    internal Task UnsubscribeAsync(string id, CancellationToken token = default)
        => Change("deletePodcastChannel", new Parameters().With("id", Id(id)), token);
    internal Task RefreshAsync(CancellationToken token = default) => Change("refreshPodcasts", null, token);
    internal Task DownloadEpisodeAsync(string id, CancellationToken token = default)
        => Change("downloadPodcastEpisode", new Parameters().With("id", Id(id)), token);
    internal Task DeleteEpisodeAsync(string id, CancellationToken token = default)
        => Change("deletePodcastEpisode", new Parameters().With("id", Id(id)), token);
}
