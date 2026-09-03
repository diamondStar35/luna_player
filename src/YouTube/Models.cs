namespace LunaPlayer.YouTube;

/// <summary>One video, as a search or a playlist reports it.</summary>
/// <remarks>
/// A port of the Python player's <c>YtItem</c>. Everything here comes from the listing itself; nothing
/// in it requires the video to have been opened, which is what lets a results window be filled from one
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
    internal static YouTubeResult None { get; } =
        new(string.Empty, string.Empty, string.Empty, null, string.Empty, string.Empty);
}

/// <summary>Whether an operation worked, and what to tell the user when it did not.</summary>
/// <remarks>
/// The same shape as <see cref="LunaPlayer.UI.UiOperation"/>, and for the same reason: a caller that
/// only wants to report a failure should not have to catch anything to find out there was one.
/// </remarks>
/// <param name="Error">Empty when <paramref name="Success"/> is true; otherwise a message already in
/// the user's language, ready to show.</param>
internal readonly record struct YouTubeOutcome(bool Success, string Error = "")
{
    internal static YouTubeOutcome Ok { get; } = new(true);
}
