using LunaPlayer.Subsonic.Listening;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Servers;

internal sealed record FeedbackFailure(string Id, RequestException Error);
internal sealed record FeedbackResult(int Changed, FeedbackFailure[] Failures);

internal sealed partial class Connections
{
    internal Task<FeedbackResult> RateAsync(Guid server, IEnumerable<string> ids, int rating, CancellationToken token)
    {
        if (rating is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(rating));
        return FeedbackAsync(server, ids, async (client, entry) =>
        {
            await client.Listening.SetRatingAsync(entry.Id, rating, token).ConfigureAwait(false);
            return entry with { UserRating = rating };
        }, token);
    }

    internal Task<FeedbackResult> StarAsync(Guid server, IEnumerable<string> ids, bool starred, CancellationToken token)
        => FeedbackAsync(server, ids, async (client, entry) =>
        {
            await client.Listening.SetStarredAsync([entry.Id], starred, StarKind.Song, token).ConfigureAwait(false);
            return entry with { Starred = starred ? DateTimeOffset.UtcNow.ToString("O") : null };
        }, token);

    private async Task<FeedbackResult> FeedbackAsync(Guid server, IEnumerable<string> ids,
        Func<Client, Entry, Task<Entry>> apply, CancellationToken token)
    {
        var client = Client(server); var changed = new List<Entry>(); var failures = new List<FeedbackFailure>();
        try
        {
            foreach (var id in ids.Distinct(StringComparer.Ordinal))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var song = Metadata.Get(server, id) ?? await client.Library.GetSongAsync(id, token).ConfigureAwait(false);
                    changed.Add(await apply(client, song).ConfigureAwait(false));
                }
                catch (RequestException error) { failures.Add(new(id, error)); }
            }
        }
        finally { Metadata.Observe(server, changed); }
        return new(changed.Count, failures.ToArray());
    }
}
