using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic.Listening;

internal enum StarKind { Song, Album, Artist }
internal enum PlaybackState { Playing, Paused, Stopped }

internal sealed class Listening(Session session, Func<string, int, CancellationToken, Task> require) : Feature(session)
{
    internal Task SetStarredAsync(IEnumerable<string> ids, bool starred, StarKind kind = StarKind.Song, CancellationToken token = default)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        return Change(starred ? "star" : "unstar", new Parameters().Many(kind switch
        { StarKind.Album => "albumId", StarKind.Artist => "artistId", _ => "id" }, ids.Select(Id)), token);
    }
    internal Task SetRatingAsync(string id, int rating, CancellationToken token = default)
    {
        if (rating is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(rating));
        return Change("setRating", new Parameters().With("id", Id(id)).With("rating", rating), token);
    }
    internal Task ScrobbleAsync(string id, bool submission, DateTimeOffset? time = null, CancellationToken token = default)
        => Change("scrobble", new Parameters().With("id", Id(id)).With("submission", submission).With("time", time?.ToUnixTimeMilliseconds()), token);
    internal async Task ReportPlaybackAsync(string id, long positionMs, PlaybackState state, double playbackRate = 1,
        bool ignoreScrobble = true, string mediaType = "song", CancellationToken token = default)
    {
        if (positionMs < 0 || !double.IsFinite(playbackRate) || playbackRate <= 0 || !Enum.IsDefined(state))
            throw new ArgumentOutOfRangeException(nameof(positionMs));
        await require("playbackReport", 1, token).ConfigureAwait(false);
        await Change("reportPlayback", new Parameters().With("mediaId", Id(id)).With("mediaType", Id(mediaType))
            .With("positionMs", positionMs).With("state", state.ToString().ToLowerInvariant())
            .With("playbackRate", playbackRate).With("ignoreScrobble", ignoreScrobble), token).ConfigureAwait(false);
    }
    internal async Task<NowPlayingEntry[]> GetNowPlayingAsync(CancellationToken token = default)
        => Required((await Session.ReadAsync("getNowPlaying", token: token).ConfigureAwait(false)).NowPlaying, "getNowPlaying").Entries;
    internal async Task<Bookmark[]> GetBookmarksAsync(CancellationToken token = default)
        => Required((await Session.ReadAsync("getBookmarks", token: token).ConfigureAwait(false)).Bookmarks, "getBookmarks").Bookmarks;
    internal Task CreateBookmarkAsync(string id, long positionMs, string? comment = null, CancellationToken token = default)
    {
        if (positionMs < 0) throw new ArgumentOutOfRangeException(nameof(positionMs));
        return Change("createBookmark", new Parameters().With("id", Id(id)).With("position", positionMs).With("comment", comment), token);
    }
    internal Task DeleteBookmarkAsync(string id, CancellationToken token = default)
        => Change("deleteBookmark", new Parameters().With("id", Id(id)), token);
    internal async Task<PlayQueue> GetQueueAsync(bool byIndex = false, CancellationToken token = default)
    {
        if (byIndex) await require("indexBasedQueue", 1, token).ConfigureAwait(false);
        var endpoint = byIndex ? "getPlayQueueByIndex" : "getPlayQueue";
        var response = await Session.ReadAsync(endpoint, token: token).ConfigureAwait(false);
        return Required(byIndex ? response.PlayQueueByIndex : response.PlayQueue, endpoint);
    }
    internal async Task SaveQueueAsync(IReadOnlyList<string> ids, string? current = null, long positionMs = 0,
        int? currentIndex = null, bool byIndex = false, CancellationToken token = default)
    {
        if (positionMs < 0 || currentIndex < 0 || byIndex && ids.Count > 0 && (!currentIndex.HasValue || currentIndex >= ids.Count)
            || byIndex && ids.Count == 0 && currentIndex.HasValue) throw new ArgumentOutOfRangeException(nameof(currentIndex));
        if (byIndex) await require("indexBasedQueue", 1, token).ConfigureAwait(false);
        await Change(byIndex ? "savePlayQueueByIndex" : "savePlayQueue", new Parameters().Many("id", ids.Select(Id))
            .With("current", byIndex ? null : current).With("currentIndex", byIndex ? currentIndex : null)
            .With("position", positionMs), token).ConfigureAwait(false);
    }
    internal async Task<Lyrics> GetLyricsAsync(string? artist = null, string? title = null, CancellationToken token = default)
        => Required((await Session.ReadAsync("getLyrics", new Parameters().With("artist", artist).With("title", title), token).ConfigureAwait(false)).Lyrics, "getLyrics");
    internal async Task<StructuredLyrics[]> GetLyricsBySongIdAsync(string id, CancellationToken token = default)
    {
        await require("songLyrics", 1, token).ConfigureAwait(false);
        return Required((await Session.ReadAsync("getLyricsBySongId", new Parameters().With("id", Id(id)), token).ConfigureAwait(false)).LyricsList, "getLyricsBySongId").Lyrics;
    }
}

/// <summary>Caller supplies actual listening time, so a seek past the threshold cannot manufacture a play.</summary>
internal sealed class PlayTracker
{
    private bool _submitted;
    private TimeSpan _listened;
    internal bool AddListeningTime(TimeSpan elapsed, TimeSpan? duration)
    {
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (_submitted) return false;
        _listened += elapsed;
        var threshold = duration is { TotalSeconds: > 0 } known ? Math.Min(240, known.TotalSeconds / 2) : 240;
        if (_listened.TotalSeconds < threshold) return false;
        return _submitted = true;
    }
    internal void Reset() { _submitted = false; _listened = TimeSpan.Zero; }
}
