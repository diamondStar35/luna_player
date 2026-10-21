namespace LunaPlayer.YouTube;

/// <summary>Whether a YouTube operation worked and what to tell the user when it did not.</summary>
internal readonly record struct YouTubeOutcome(bool Success, string Error = "")
{
    internal static YouTubeOutcome Ok { get; } = new(true);
}

/// <summary>A source of search or channel results that can be drawn from a batch at a time.</summary>
internal interface IResultPage
{
    bool HasMore { get; }
    Task<IReadOnlyList<YouTubeResult>> Take(int count, CancellationToken token);
}

/// <summary>What a result row stands for: a video to play, or a playlist or channel to browse into.</summary>
internal enum YouTubeItemType
{
    Video,
    Playlist,
    Channel,
}

/// <summary>Whether a play resolves the video stream or the audio stream alone. Chosen per play - Enter for
/// the video, Ctrl+Enter for the sound - rather than fixed for the session, so one list serves both.
/// </summary>
internal enum PlayMode
{
    /// <summary>The picture, at the video quality.</summary>
    Video,
    /// <summary>The sound alone, at the audio quality.</summary>
    Audio,
}

/// <summary>One video, as a search or a playlist reports it.</summary>
/// <remarks>
/// Everything here comes from the listing itself; nothing in it requires the video to have been opened,
/// which is what lets a results window be filled from one
/// request rather than one per row.
/// </remarks>
/// <param name="Id">The eleven character video id.</param>
/// <param name="Title">What the video is called.</param>
/// <param name="Author">Who published it.</param>
/// <param name="Duration">How long it runs, or null for a live stream, which has no end to report.</param>
/// <param name="Url">The address of the video itself.</param>
/// <param name="ChannelUrl">The address of the channel that published it, or empty when the listing did
/// not say. The results window offers to open it, and has to be able to refuse when it is not there.</param>
internal readonly record struct YouTubeResult(
    string Id,
    string Title,
    string Author,
    TimeSpan? Duration,
    string Url,
    string ChannelUrl)
{
    /// <summary>The view count as the listing worded it ("1.2M views"), or null when it said nothing.
    /// For a playlist row this instead carries the count of videos, and for a channel the subscriber
    /// count - the one secondary figure each of those kinds has to show.</summary>
    internal string? Views { get; init; }

    /// <summary>When it was published, as the listing worded it ("3 years ago"), or null. For a channel
    /// row this instead carries the count of videos.</summary>
    internal string? PublishedTime { get; init; }

    /// <summary>Whether this row is a video, a playlist, or a channel. Defaults to a video, which is what
    /// every row was before playlists and channels could appear among the results.</summary>
    internal YouTubeItemType ItemType { get; init; }

    internal static YouTubeResult None { get; } =
        new(string.Empty, string.Empty, string.Empty, null, string.Empty, string.Empty);
}
