using LunaPlayer.Configuration;

namespace LunaPlayer.YouTube;

/// <summary>Where a list of videos came from.</summary>
internal enum SessionKind
{
    /// <summary>A search, which can be asked for more.</summary>
    Search,
    /// <summary>A playlist, which arrives whole.</summary>
    Playlist,
}

/// <summary>A list of videos the user is working through, and everything that follows from it.</summary>
///
/// <remarks>
/// One of these exists from the moment a search or a playlist comes back until the user opens another or
/// closes the window. It is what makes the next video after this one a meaningful idea: the playlist holds
/// stream addresses, which say nothing about order or about what comes next, and this holds the order the
/// results window showed.
///
/// The options are frozen when it is made. Changing the quality setting halfway through should not leave
/// the first half of a list resolved one way and the second another, and the cache is keyed on them, so a
/// session that read them afresh each time would quietly stop finding its own prefetches.
/// </remarks>
internal sealed class YouTubeSession : IDisposable
{
    private readonly List<YouTubeResult> _items;
    private readonly CancellationTokenSource _cancellation = new();

    /// <summary>Taken while the source is alive and kept.</summary>
    /// <remarks>
    /// <c>CancellationTokenSource.Token</c> throws once the source is disposed, and a session can be
    /// disposed while a window that was opened on it is still on screen. A token read afterwards is not a
    /// problem in itself - it simply reads as cancelled, which is exactly right - so it is captured here
    /// rather than left to throw at whichever caller happens to ask last.
    /// </remarks>
    private readonly CancellationToken _token;

    internal YouTubeSession(
        SessionKind kind,
        IEnumerable<YouTubeResult> items,
        bool audioOnly,
        YouTubeQuality quality,
        SearchPage? page = null)
    {
        Kind = kind;
        _items = [.. items];
        AudioOnly = audioOnly;
        Quality = quality;
        Page = page;
        _token = _cancellation.Token;
    }

    internal SessionKind Kind { get; }

    /// <summary>The heading above the list. It names what kind of list this is and nothing more, as the
    /// Python player's does: the window is opened straight from the search box, so what was searched for is
    /// still the last thing the user typed.</summary>
    internal string Label => Kind switch
    {
        // Translators: Heading above the list of videos a YouTube search found.
        SessionKind.Search => Tr("Search results"),
        // Translators: Heading above the list of videos in a YouTube playlist.
        _ => Tr("Playlist videos"),
    };

    internal IReadOnlyList<YouTubeResult> Items => _items;

    internal bool AudioOnly { get; }

    internal YouTubeQuality Quality { get; }

    /// <summary>The search this came from, held so it can be asked for more. Null for a playlist, which
    /// has no more to give.</summary>
    internal SearchPage? Page { get; }

    /// <summary>Which row the user was on. Kept so closing the results window and coming back to it lands
    /// where they left rather than at the top.</summary>
    internal int Selected { get; set; }

    /// <summary>Cancelled when the session ends, abandoning every resolve started on its behalf.</summary>
    /// <remarks>
    /// A source rather than the Python player's flag, which cannot be reset once set and so poisons a
    /// session that outlives its first cancellation. This one is owned by the session and dies with it, so
    /// that state is unreachable.
    /// </remarks>
    internal CancellationToken Token => _token;

    internal bool IsCancelled => _token.IsCancellationRequested;

    /// <summary>Adds a page of results to the end.</summary>
    internal void Append(IEnumerable<YouTubeResult> items) => _items.AddRange(items);

    /// <summary>Where in the list a video is, or -1.</summary>
    internal int IndexOf(string watchUrl)
        => _items.FindIndex(item => string.Equals(item.Url, watchUrl, StringComparison.Ordinal));

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        // The enumerator holds a live response from YouTube. Closing it waits for a page still being
        // taken - advancing and disposing the same enumerator at once is undefined - but nothing here
        // waits for that, because nobody is reading a search that has ended.
        if (Page is SearchPage page)
            _ = page.DisposeAsync().AsTask();
    }
}
