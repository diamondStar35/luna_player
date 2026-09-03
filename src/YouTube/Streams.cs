using System.Globalization;
using LunaPlayer.Configuration;
using YoutubeExplode.Videos.Streams;

namespace LunaPlayer.YouTube;

/// <summary>Why a video could not be turned into something playable.</summary>
///
/// <remarks>
/// An enum rather than a message, because resolving happens on a worker thread and <c>Tr</c> may only be
/// called on the UI thread. The words are chosen where the failure is reported, from this and the raw
/// detail beside it.
/// </remarks>
internal enum ResolveFailure
{
    None,
    /// <summary>The caller asked for the work to stop, or the session it belonged to ended.</summary>
    Cancelled,
    /// <summary>The video is gone, private, or was never there.</summary>
    Unavailable,
    /// <summary>The video exists but YouTube will not serve it: age-gated, region-locked, or paid for.
    /// </summary>
    Unplayable,
    /// <summary>The video exists and is playable, but it offers nothing this player can use.</summary>
    NoStream,
    /// <summary>The yt-dlp resolver was asked for and the programs it needs are not installed.</summary>
    MissingComponents,
    /// <summary>YouTube is refusing requests from this address for the time being.</summary>
    RateLimited,
    /// <summary>The request never reached YouTube, or its answer never arrived.</summary>
    Network,
    Unknown,
}

/// <summary>A video and the addresses that play it.</summary>
/// <param name="Item">The video itself, so a caller has its title without asking again.</param>
/// <param name="Url">What to open. The whole video when it carries its own sound, the picture alone when
/// it does not.</param>
/// <param name="AudioUrl">The sound, when <paramref name="Url"/> is picture only. YouTube stops serving
/// the two together above 360p, so anything better arrives as a pair.</param>
/// <param name="Expires">When these addresses stop working. They are signed and short-lived, which is why
/// a resolve cannot simply be kept.</param>
internal sealed record Resolved(YouTubeResult Item, string Url, string? AudioUrl, DateTimeOffset Expires)
{
    internal bool IsFresh => DateTimeOffset.UtcNow < Expires;
}

/// <summary>What came of a resolve: the streams, or the reason there are none.</summary>
internal readonly record struct ResolveOutcome(Resolved? Value, ResolveFailure Failure, string Detail = "")
{
    internal static ResolveOutcome Ok(Resolved value) => new(value, ResolveFailure.None);

    internal static ResolveOutcome Failed(ResolveFailure failure, string detail = "")
        => new(null, failure, detail);

    internal static ResolveOutcome Cancelled { get; } = new(null, ResolveFailure.Cancelled);
}

/// <summary>Chooses which of a video's streams to play, and works out how long they will last.</summary>
///
/// <remarks>
/// This replaces the yt-dlp format strings the Python player passes on the command line - <c>bestaudio
/// [ext=m4a]/bestaudio/best</c> and its video equivalents - with the same preferences expressed against
/// the stream list directly. The order of the fallbacks is theirs, so the two players choose the same
/// stream where they can.
/// </remarks>
internal static class StreamPicker
{
    /// <summary>How tall a picture each quality setting allows. Best is unbounded.</summary>
    private static int MaxHeight(YouTubeQuality quality) => quality switch
    {
        YouTubeQuality.Low => 360,
        YouTubeQuality.Medium => 720,
        _ => int.MaxValue,
    };

    /// <summary>The streams to play, or null when the video offers nothing usable.</summary>
    /// <remarks>
    /// Quality is ignored for sound, as the Python player's selector ignores it: there is no meaningful
    /// scale to apply, and the largest audio stream YouTube offers is small next to any picture.
    /// </remarks>
    internal static (string Url, string? AudioUrl)? Choose(
        StreamManifest manifest, bool audioOnly, YouTubeQuality quality)
    {
        if (!audioOnly)
            return ChooseVideo(manifest, quality);
        return ChooseAudio(manifest) is string sound ? (sound, null) : null;
    }

    private static string? ChooseAudio(StreamManifest manifest)
    {
        var streams = manifest.GetAudioOnlyStreams().ToList();
        IStreamInfo? chosen = Best(streams.Where(stream => stream.Container == Container.Mp4))
            ?? Best(streams);
        // A live stream and a few older videos have no separate sound at all, only the whole video.
        // Playing that and ignoring the picture is what yt-dlp's trailing "best" amounts to.
        chosen ??= Best(manifest.GetMuxedStreams());
        return chosen?.Url;
    }

    private static (string Url, string? AudioUrl)? ChooseVideo(StreamManifest manifest, YouTubeQuality quality)
    {
        var limit = MaxHeight(quality);
        var picture = manifest.GetVideoOnlyStreams()
            .Where(stream => stream.VideoResolution.Height <= limit)
            .OrderByDescending(stream => stream.VideoResolution.Height)
            .ThenByDescending(stream => stream.VideoQuality.Framerate)
            // Preferred last among the things that are equal, so it breaks a tie rather than costing
            // resolution: mp4 is the container mpv and Windows handle best, but not at 360p when 720p
            // was asked for.
            .ThenByDescending(stream => stream.Container == Container.Mp4)
            .ThenByDescending(stream => stream.Bitrate.BitsPerSecond)
            .FirstOrDefault();
        if (picture is null)
        {
            // Nothing separate under the cap. Live streams and a few others are served whole, so the
            // muxed list is the only place left to look.
            var whole = Best(manifest.GetMuxedStreams()
                .Where(stream => stream.VideoResolution.Height <= limit))
                ?? Best(manifest.GetMuxedStreams());
            return whole is null ? null : (whole.Url, null);
        }
        // Picture with no sound to go with it is worse than a smaller picture that has some.
        return ChooseAudio(manifest) is string sound ? (picture.Url, sound) : null;
    }

    private static T? Best<T>(IEnumerable<T> streams) where T : class, IStreamInfo
        => streams.OrderByDescending(stream => stream.Bitrate.BitsPerSecond).FirstOrDefault();

    /// <summary>How long a resolved address is good for.</summary>
    ///
    /// <remarks>
    /// YouTube signs these addresses and states the deadline in the address itself, so it is read rather
    /// than guessed. A margin comes off it because playback starts some time after the resolve and the
    /// deadline applies to the request, not to the video; half an hour stands in when there is no
    /// <c>expire</c> to read, which is well inside the shortest lifetime YouTube is known to issue.
    /// </remarks>
    internal static DateTimeOffset ExpiryOf(string url)
    {
        var margin = TimeSpan.FromMinutes(2);
        var stated = StatedExpiry(url);
        if (stated is not DateTimeOffset expiry)
            return DateTimeOffset.UtcNow + TimeSpan.FromMinutes(30) - margin;
        return expiry - margin;
    }

    /// <summary>The earlier of the deadlines two addresses state, so a pair is treated as one.</summary>
    internal static DateTimeOffset ExpiryOf(string url, string? audioUrl)
    {
        var first = ExpiryOf(url);
        return audioUrl is null ? first : first < ExpiryOf(audioUrl) ? first : ExpiryOf(audioUrl);
    }

    private static DateTimeOffset? StatedExpiry(string url)
    {
        if (!LunaPlayer.Media.LinkValidator.TryGetHttpUrl(url, out var uri))
            return null;
        var query = uri.Query;
        if (query.Length <= 1)
            return null;
        foreach (var pair in query[1..].Split('&'))
        {
            if (!pair.StartsWith("expire=", StringComparison.Ordinal))
                continue;
            if (long.TryParse(pair.AsSpan("expire=".Length), NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
            return null;
        }
        return null;
    }
}
