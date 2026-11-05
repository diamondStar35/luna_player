using System.Text.Json.Nodes;
using PyYt;

namespace LunaPlayer.YouTube.Metadata;

/// <summary>Everything Luna asks of YouTube that is not the playable stream itself: searching, and the
/// title/author/duration/description of a video or playlist.</summary>
///
/// <remarks>
/// Backed by PyYt, which reads YouTube's own web endpoints and hands back JSON. It cannot decipher a
/// playable stream URL - that is yt-dlp's job now, and the only job it has - so there is deliberately no
/// Resolve or Download here. The library's public surface returns <see cref="JsonObject"/> trees and keeps
/// its navigation helpers to itself, so this walks the trees with the small helpers in <see cref="Mapping"/>
/// and maps them onto the neutral <see cref="YouTubeResult"/> the rest of the player speaks in.
///
/// PyYt is async throughout; the player drives it from background jobs and bridges to it synchronously with
/// <see cref="Wait{T}"/>, which is only ever called off the UI thread.
/// </remarks>
internal sealed class Client
{
    /// <summary>Runs a search and returns its first page together with the object that pages the rest.</summary>
    /// <remarks>
    /// Only the first batch is taken here. YouTube answers a search in batches of its own choosing, and
    /// collecting the whole configured count takes one web request per batch, sequentially - so taking it all
    /// before the caller returns keeps the results window from opening for as long as every batch takes. The
    /// rest is fetched by the caller: the results feed fills it in while the window is open.
    /// </remarks>
    /// <param name="filter">The row the user picked in the search dialog's filter, 0-5; see the mapping below.</param>
    internal (IReadOnlyList<YouTubeResult> Items, Page Page) Search(
        string query, int filter, int count, CancellationToken token)
    {
        int limit = Math.Max(1, count);
        SearchBase search = filter switch
        {
            1 => new CustomSearch(query, SearchMode.Livestreams, limit: limit),
            2 => new CustomSearch(query, VideoSortOrder.UploadDate, limit: limit),
            3 => new CustomSearch(query, VideoSortOrder.ViewCount, limit: limit),
            4 => new PlaylistsSearch(query, limit: limit),
            5 => new ChannelsSearch(query, limit: limit),
            // No filter: videos only, which is what the player played before filters existed.
            _ => new VideosSearch(query, limit: limit),
        };
        var page = new Page(search);
        // The first batch only; Take returns as soon as it has rows, however many the batch carries,
        // and pages fetched afterwards are the caller's to order.
        return (Wait(page.Take(1, token)), page);
    }

    /// <summary>The title of a playlist and every video in it.</summary>
    internal (string Title, IReadOnlyList<YouTubeResult> Items) Playlist(string url, CancellationToken token)
    {
        using var playlist = new PyYt.Playlist(url);
        // Collect the whole playlist: the results feed shows it in one go.
        while (playlist.HasMoreVideos)
        {
            token.ThrowIfCancellationRequested();
            Wait(playlist.GetNextVideosAsync(token));
        }
        var items = new List<YouTubeResult>();
        foreach (JsonNode? node in playlist.Videos)
            if (node is JsonObject video && Mapping.ToResult(video) is YouTubeResult row)
                items.Add(row);
        return (Mapping.Str(playlist.Info, "title") ?? string.Empty, items);
    }

    /// <summary>A video's long description.</summary>
    internal string Description(string url, CancellationToken token)
        => Mapping.Str(Wait(PyYt.Video.GetAsync(url, cancellationToken: token)), "description") ?? string.Empty;

    /// <summary>The words YouTube offers to finish what the user has typed, for the search box's suggestions.
    /// </summary>
    /// <remarks>
    /// Called from a background task the search dialog starts on each keystroke, never the UI thread, so it
    /// blocks with <see cref="Wait{T}"/> like the rest. An empty list is a perfectly good answer - nothing to
    /// offer - so the caller need not tell "no suggestions" apart from "the query was blank".
    /// </remarks>
    internal IReadOnlyList<string> Suggestions(string query, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<string>();
        JsonObject response = Wait(PyYt.Suggestions.GetAsync(query, cancellationToken: token));
        if (response["result"] is not JsonArray words)
            return Array.Empty<string>();
        var found = new List<string>(words.Count);
        foreach (JsonNode? node in words)
            if (node is JsonValue value && value.TryGetValue(out string? word) && !string.IsNullOrEmpty(word))
                found.Add(word);
        return found;
    }

    /// <summary>Runs an async call to completion on the calling thread. Only ever called from a background
    /// job, never the UI thread, so the block it does is a block on a thread that is meant to wait.</summary>
    private static T Wait<T>(Task<T> task) => task.GetAwaiter().GetResult();

    private static void Wait(Task task) => task.GetAwaiter().GetResult();
}
