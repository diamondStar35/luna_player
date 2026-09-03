using LunaPlayer.Configuration;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Common;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Playlists;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace LunaPlayer.YouTube;

/// <summary>A search that has been started and can be asked for more.</summary>
///
/// <remarks>
/// The enumerator is the continuation. YouTube pages a search with a token it hands back with each page,
/// and YoutubeExplode keeps that token inside the enumerator rather than exposing it - so holding the
/// enumerator is holding the position, and there is nothing else to store. It is not thread-safe and is
/// only ever advanced from one place at a time, which is what <see cref="Take"/> being the only way in
/// arranges.
/// </remarks>
internal sealed class SearchPage : IAsyncDisposable
{
    private readonly IAsyncEnumerator<Batch<ISearchResult>> _batches;
    /// <summary>Holds the enumerator to one user at a time - a page being taken, or the close.</summary>
    private readonly SemaphoreSlim _turn = new(1, 1);
    private bool _exhausted;

    internal SearchPage(IAsyncEnumerator<Batch<ISearchResult>> batches) => _batches = batches;

    /// <summary>Whether there may be more. False only once YouTube has said there is nothing left.
    /// </summary>
    internal bool HasMore => !_exhausted;

    /// <summary>The next <paramref name="count"/> videos, or as many as remain.</summary>
    ///
    /// <remarks>
    /// YouTube decides how big a batch is - about twenty - so the count is a target rather than a
    /// promise: batches are taken until it is met and the one that meets it is taken whole, which is
    /// what the caller wants anyway. Videos are the only kind kept; a search also returns channels and
    /// playlists, and neither belongs in a list whose every row is played.
    /// </remarks>
    internal async Task<IReadOnlyList<YouTubeResult>> Take(int count, CancellationToken token)
    {
        var found = new List<YouTubeResult>();
        await _turn.WaitAsync(token).ConfigureAwait(false);
        try
        {
            while (found.Count < count)
            {
                token.ThrowIfCancellationRequested();
                if (!await _batches.MoveNextAsync().ConfigureAwait(false))
                {
                    _exhausted = true;
                    break;
                }
                foreach (var result in _batches.Current.Items.OfType<VideoSearchResult>())
                    found.Add(ExplodeClient.ToResult(result));
            }
        }
        finally
        {
            _turn.Release();
        }
        return found;
    }

    /// <remarks>
    /// Waits for a page still being taken. Closing the list while one is in flight is ordinary - the user
    /// arrows onto the last row and then presses Escape - and disposing an async enumerator while it is
    /// being advanced is undefined, which showed up as a request failing inside a task nobody was watching.
    /// The wait is not cancellable: the point of it is to close cleanly, not quickly.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await _turn.WaitAsync().ConfigureAwait(false);
        try
        {
            await _batches.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _turn.Release();
        }
    }
}

/// <summary>Everything the player asks of YouTube.</summary>
///
/// <remarks>
/// The one place in the program that blocks on a task. YoutubeExplode is asynchronous throughout and the
/// rest of the player is not; rather than spread <c>async</c> through the action handlers for the sake of
/// one feature, the waiting is done here - and only ever on a worker thread, because every caller is
/// either a background job or a prefetch. Blocking the UI thread on any of this would freeze the player
/// for as long as YouTube took to answer.
/// </remarks>
internal sealed class ExplodeClient
{
    private readonly YoutubeClient _client = new();

    /// <summary>Starts a search and takes its first page.</summary>
    internal (IReadOnlyList<YouTubeResult> Items, SearchPage Page) Search(
        string query, int count, CancellationToken token)
    {
        var page = new SearchPage(
            _client.Search.GetResultBatchesAsync(query, SearchFilter.Video, token).GetAsyncEnumerator(token));
        return (Wait(page.Take(count, token)), page);
    }

    /// <summary>Every video in a playlist, and what the playlist is called.</summary>
    internal (string Title, IReadOnlyList<YouTubeResult> Items) Playlist(string url, CancellationToken token)
    {
        var id = PlaylistId.Parse(url);
        var playlist = Wait(_client.Playlists.GetAsync(id, token));
        var videos = Wait(_client.Playlists.GetVideosAsync(id, token).CollectAsync());
        return (playlist.Title, [.. videos.Select(ToResult)]);
    }

    /// <summary>One video's details, for a link the user gave rather than chose from a list.</summary>
    internal YouTubeResult Video(string url, CancellationToken token)
        => ToResult(Wait(_client.Videos.GetAsync(VideoId.Parse(url), token)));

    /// <summary>The text the uploader wrote under a video.</summary>
    internal string Description(string url, CancellationToken token)
        => Wait(_client.Videos.GetAsync(VideoId.Parse(url), token)).Description;

    /// <summary>Turns a video into something playable, reporting rather than throwing when it cannot.
    /// </summary>
    /// <param name="item">What is already known about the video. A search gave the player everything the
    /// results window shows, so nothing is asked for twice; a bare link did not, and passes
    /// <see cref="YouTubeResult.None"/> to have the details fetched here.</param>
    internal ResolveOutcome Resolve(
        string watchUrl, YouTubeResult item, bool audioOnly, YouTubeQuality quality, CancellationToken token)
    {
        try
        {
            var id = VideoId.Parse(watchUrl);
            if (item.Url.Length == 0)
                item = ToResult(Wait(_client.Videos.GetAsync(id, token)));
            var manifest = Wait(_client.Videos.Streams.GetManifestAsync(id, token));
            if (StreamPicker.Choose(manifest, audioOnly, quality) is not { } chosen)
                return ResolveOutcome.Failed(ResolveFailure.NoStream);
            return ResolveOutcome.Ok(new Resolved(
                item, chosen.Url, chosen.AudioUrl, StreamPicker.ExpiryOf(chosen.Url, chosen.AudioUrl)));
        }
        catch (Exception failure)
        {
            return Explain(failure, token);
        }
    }

    /// <summary>Saves a video into <paramref name="folder"/> and returns the file it wrote.</summary>
    ///
    /// <remarks>
    /// One stream, so one file. Playback pairs a picture stream with a separate sound stream, but joining
    /// those into a file needs ffmpeg, which the player does not ship - so a video download takes the
    /// muxed stream, which carries both and is what yt-dlp falls back to for the same reason.
    ///
    /// The stream is chosen before the name is built, because the name ends in the container the chosen
    /// stream actually uses. Asking twice could answer differently, and did.
    /// </remarks>
    /// <param name="report">The name being written and how far through it is, as a fraction from nought to
    /// one. Called from a worker thread, and called once with nought before the first byte arrives so the
    /// progress window can name the file from its first tick.</param>
    internal string Download(
        string watchUrl,
        string folder,
        bool audioOnly,
        YouTubeQuality quality,
        Action<string, double> report,
        CancellationToken token)
    {
        var id = VideoId.Parse(watchUrl);
        var video = Wait(_client.Videos.GetAsync(id, token));
        var manifest = Wait(_client.Videos.Streams.GetManifestAsync(id, token));
        var chosen = audioOnly ? BestAudio(manifest) : BestWhole(manifest, quality);
        // Sound was asked for and the video has none on its own. The whole thing still carries it, and a
        // file with a picture in it is better than no file.
        chosen ??= audioOnly ? BestWhole(manifest, YouTubeQuality.Best) : BestAudio(manifest);
        if (chosen is null)
            throw new InvalidOperationException("The video offers no stream that can be saved as one file.");
        var path = Paths.Unused(Path.Combine(
            folder, $"{Paths.SafeFileName(video.Title)}.{chosen.Container.Name}"));
        var name = Path.GetFileName(path);
        report(name, 0);
        Wait(_client.Videos.Streams.DownloadAsync(
            chosen, path, new Progress<double>(fraction => report(name, fraction)), token));
        return path;
    }

    private static IStreamInfo? BestAudio(StreamManifest manifest)
        => manifest.GetAudioOnlyStreams()
            .OrderByDescending(stream => stream.Container == Container.Mp4)
            .ThenByDescending(stream => stream.Bitrate.BitsPerSecond)
            .FirstOrDefault();

    private static IStreamInfo? BestWhole(StreamManifest manifest, YouTubeQuality quality)
    {
        var limit = quality switch
        {
            YouTubeQuality.Low => 360,
            YouTubeQuality.Medium => 720,
            _ => int.MaxValue,
        };
        var whole = manifest.GetMuxedStreams().ToList();
        return whole.Where(stream => stream.VideoResolution.Height <= limit)
            .OrderByDescending(stream => stream.VideoResolution.Height)
            .ThenByDescending(stream => stream.Bitrate.BitsPerSecond)
            .FirstOrDefault()
            // Every muxed stream is taller than the cap allows. A file above the asked-for quality beats
            // no file at all, so the smallest of them is taken.
            ?? whole.OrderBy(stream => stream.VideoResolution.Height).FirstOrDefault();
    }

    /// <summary>Reads what went wrong well enough to say something useful about it. Shared with the
    /// search and playlist jobs, so one broken network or one rate limit is worded the same wherever it
    /// turns up.</summary>
    /// <remarks>
    /// The detail is whatever the library said, untranslated: it is a diagnostic, and the sentence the
    /// user reads is chosen from the enum. This mirrors the Python player, which keeps yt-dlp's first
    /// error line beside its own wording rather than in place of it.
    /// </remarks>
    internal static ResolveOutcome Explain(Exception failure, CancellationToken token) => failure switch
    {
        // Only when the token really is set. HttpClient reports its own request timeout as a
        // TaskCanceledException, which derives from this one - and reading that as "the user cancelled"
        // makes a dead network the one failure the player says nothing at all about.
        OperationCanceledException when token.IsCancellationRequested => ResolveOutcome.Cancelled,
        OperationCanceledException => ResolveOutcome.Failed(ResolveFailure.Network, failure.Message),
        RequestLimitExceededException => ResolveOutcome.Failed(ResolveFailure.RateLimited, failure.Message),
        VideoUnplayableException or VideoRequiresPurchaseException
            => ResolveOutcome.Failed(ResolveFailure.Unplayable, failure.Message),
        VideoUnavailableException or PlaylistUnavailableException
            => ResolveOutcome.Failed(ResolveFailure.Unavailable, failure.Message),
        HttpRequestException or IOException
            => ResolveOutcome.Failed(ResolveFailure.Network, failure.Message),
        // A change at YouTube's end shows up here, as a parse that no longer fits what arrived. It is a
        // failure of this program rather than of the request, so it says so rather than blaming the video.
        YoutubeExplodeException => ResolveOutcome.Failed(ResolveFailure.Unknown, failure.Message),
        _ => ResolveOutcome.Failed(ResolveFailure.Unknown, failure.Message),
    };

    internal static YouTubeResult ToResult(VideoSearchResult video)
        => Build(video.Id, video.Title, video.Author, video.Duration);

    private static YouTubeResult ToResult(PlaylistVideo video)
        => Build(video.Id, video.Title, video.Author, video.Duration);

    private static YouTubeResult ToResult(Video video)
        => Build(video.Id, video.Title, video.Author, video.Duration);

    /// <remarks>
    /// The address is built from the id rather than taken from the library, so every one of these is
    /// spelled the same way whichever listing it came from. That spelling is what the resolve cache and
    /// the playlist both key on, and a <c>youtu.be</c> address and a <c>watch?v=</c> address for one video
    /// must not look like two.
    /// </remarks>
    private static YouTubeResult Build(VideoId id, string title, Author author, TimeSpan? duration)
        => new(id.Value, title, author.ChannelTitle, duration, WatchUrl(id.Value), author.ChannelUrl);

    /// <summary>The one spelling of a video's address the player uses.</summary>
    internal static string WatchUrl(string id) => $"https://www.youtube.com/watch?v={id}";

    /// <summary>The canonical address of whatever video a link names, or null when it names none.
    /// </summary>
    internal static string? Canonical(string link)
        => VideoId.TryParse(link) is VideoId id ? WatchUrl(id.Value) : null;

    private static T Wait<T>(Task<T> task) => task.GetAwaiter().GetResult();

    private static T Wait<T>(ValueTask<T> task) => task.GetAwaiter().GetResult();

    private static void Wait(ValueTask task) => task.GetAwaiter().GetResult();
}
