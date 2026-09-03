using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.YouTube;

/// <summary>The operations on a single video that need no session behind them.</summary>
///
/// <remarks>
/// Playing a list of videos lives in <see cref="YouTubeSessions"/>, because it needs the player and the
/// order the results window showed. What is left here is the work that answers about one address and then
/// finishes: saving it, and reading what the uploader wrote under it. Both run on a worker thread behind a
/// progress window, so both report a raw failure rather than a translated one.
///
/// Each of them can be done two ways. The player resolves and saves by itself, and yt-dlp does the same
/// through a program the user has to fetch first; which one runs is the one setting that decides it, and
/// the yt-dlp route falls back to the player's own when the program turns out not to be there. Saving in
/// particular therefore works out of the box, which in the Python player - where saving is yt-dlp's job
/// alone - it does not.
/// </remarks>
internal sealed class Backend
{
    private readonly ExplodeClient _client;
    private readonly YtDlpClient _ytDlp;
    private readonly PlayerSettings _settings;

    internal Backend(ExplodeClient client, YtDlpClient ytDlp, PlayerSettings settings)
    {
        _client = client;
        _ytDlp = ytDlp;
        _settings = settings;
    }

    /// <summary>Whether the programs yt-dlp needs have been fetched.</summary>
    internal static bool HasComponents => Tools.HasAll;

    /// <summary>Whether the work below should go through yt-dlp.</summary>
    /// <remarks>
    /// The setting alone, with no test for whether the programs are there. Somebody who turns this on has
    /// chosen yt-dlp for a reason - usually a video the player's own resolver will not play - so quietly
    /// answering with the resolver they rejected would hide the very failure they turned it on to avoid.
    /// When the programs are missing they are told so, and nothing else answers in yt-dlp's place.
    /// </remarks>
    internal bool PrefersYtDlp => _settings.YouTube.UseYtDlp;

    /// <summary>The text the uploader wrote under a video.</summary>
    /// <remarks>Runs on a worker thread, so the failure it reports is a code and a raw detail; the
    /// sentence the user reads is chosen back on the UI thread.</remarks>
    internal (string? Text, ResolveFailure Failure, string Detail) Describe(
        string watchUrl, CancellationToken token)
    {
        try
        {
            if (!PrefersYtDlp)
                return (_client.Description(watchUrl, token), ResolveFailure.None, string.Empty);
            if (!Tools.HasAll)
                return (null, ResolveFailure.MissingComponents, string.Empty);
            return _ytDlp.Description(watchUrl, token) is string described
                ? (described, ResolveFailure.None, string.Empty)
                : (null, ResolveFailure.Unavailable, string.Empty);
        }
        catch (Exception failure)
        {
            var explained = ExplodeClient.Explain(failure, token);
            return (null, explained.Failure, explained.Detail);
        }
    }

    /// <summary>Every video in a playlist, and what the playlist is called.</summary>
    /// <remarks>
    /// Through the same resolver as everything else, so a user who turned yt-dlp on gets it here too. A
    /// search is the one thing that does not: the Python player searches with a separate library rather
    /// than with yt-dlp, and so does this one.
    /// </remarks>
    internal (string Title, IReadOnlyList<YouTubeResult> Items, ResolveFailure Failure, string Detail) Playlist(
        string link, CancellationToken token)
    {
        try
        {
            if (!PrefersYtDlp)
            {
                var (title, items) = _client.Playlist(link, token);
                return (title, items, ResolveFailure.None, string.Empty);
            }
            if (!Tools.HasAll)
                return (string.Empty, [], ResolveFailure.MissingComponents, string.Empty);
            if (_ytDlp.Playlist(link, token) is not { } found)
                return (string.Empty, [], ResolveFailure.Unavailable, string.Empty);
            return (found.Title, found.Items, ResolveFailure.None, string.Empty);
        }
        catch (Exception failure)
        {
            var explained = ExplodeClient.Explain(failure, token);
            return (string.Empty, [], explained.Failure, explained.Detail);
        }
    }

    /// <summary>Saves a video into <paramref name="folder"/>, naming the file after the video.</summary>
    internal YouTubeOutcome Download(
        string watchUrl,
        string folder,
        bool audioOnly,
        YouTubeQuality quality,
        Action<ProgressUpdate> report,
        CancellationToken token)
    {
        try
        {
            if (PrefersYtDlp)
            {
                if (!Tools.HasAll)
                    return new YouTubeOutcome(false, MissingComponents);
                _ytDlp.Download(watchUrl, folder, audioOnly, quality,
                    (name, got, size) => report(Bytes(name, got, size)), token);
                return YouTubeOutcome.Ok;
            }
            _client.Download(watchUrl, folder, audioOnly, quality,
                (name, fraction) => report(Bytes(name, (long)(fraction * 100), 100)),
                token);
            return YouTubeOutcome.Ok;
        }
        catch (OperationCanceledException)
        {
            // The user's own doing, and the progress window has already gone. Nothing to report.
            return YouTubeOutcome.Ok;
        }
        catch (Exception failure)
        {
            return new YouTubeOutcome(false, failure.Message);
        }
    }

    /// <summary>What the user is told when yt-dlp was asked for and is not installed.</summary>
    /// <remarks>
    /// A property rather than a constant, so it is read at the moment it is needed. <c>Tr</c> may only be
    /// called on the UI thread, and a static initialiser would run wherever this type is first touched -
    /// which, for a download, is on a worker.
    /// </remarks>
    internal static string MissingComponents =>
        // Translators: Shown when something needs the extra programs for yt-dlp and they are not installed.
        Tr("YouTube components are missing.");

    /// <summary>One progress report from a download.</summary>
    /// <remarks>
    /// Bytes where the source knows them and hundredths where it does not, because the window shows the two
    /// sizes as well as the bar and a proportion cannot be turned back into them. The player's own
    /// downloader reports only a fraction, so its sizes read as unknown - which is honest, and is what the
    /// Python player shows for a download whose total yt-dlp did not state either.
    /// </remarks>
    private static ProgressUpdate Bytes(string name, long got, long size)
        => new((int)Math.Min(got, int.MaxValue), (int)Math.Min(size, int.MaxValue), name);
}
